using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace ValheimAutoTranslator
{
    [BepInPlugin("ntxfloy.valheimautotranslator", "Valheim Auto Translator", "0.1.3")]
    public sealed class ValheimPlugin : BaseUnityPlugin
    {
        private Harmony harmony;
        private static int refreshNeeded;
        private static volatile bool languageKnown;
        private static volatile bool russianLanguage;
        private static float nextRefresh;
        private static readonly List<WeakReference> Roots = new List<WeakReference>();
        private static readonly object RootLock = new object();
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            try
            {
                GATSettings settings = GATSettings.Load(Config);
                TranslationCache.Load(settings.model);
                TranslateWorker.Start(settings);
                harmony = new Harmony("ntxfloy.valheimautotranslator");
                harmony.PatchAll(typeof(ValheimPlugin).Assembly);
                Logger.LogInfo("Ready. Cached translations: " + TranslationCache.Count + "; " + TranslationCache.PathOnDisk);
            }
            catch (Exception ex) { Logger.LogError("Initialization failed: " + ex); }
        }

        private void OnDestroy()
        {
            try { if (harmony != null) harmony.UnpatchSelf(); } catch (Exception ex) { Logger.LogWarning(ex); }
            try { TranslateWorker.Stop(); } catch (Exception ex) { Logger.LogWarning(ex); }
        }

        internal static bool IsRussian(Localization localization)
        {
            if (languageKnown) return russianLanguage;
            if (localization == null) return false;
            SetLanguageState(localization.GetSelectedLanguage());
            return russianLanguage;
        }

        internal static void SetLanguageState(string language)
        {
            russianLanguage = !string.IsNullOrEmpty(language) &&
                (language.IndexOf("Russian", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 language.IndexOf("Рус", StringComparison.OrdinalIgnoreCase) >= 0);
            languageKnown = true;
        }

        internal static string Get(string context, string source)
        {
            if (string.IsNullOrEmpty(source)) return source;
            string translated;
            if (TranslationCache.TryGet(context, source, out translated)) return translated;
            TranslateWorker.Request(context, source);
            return source;
        }

        internal static void Register(Transform root)
        {
            if (root == null) return;
            lock (RootLock)
            {
                foreach (WeakReference reference in Roots)
                    if (ReferenceEquals(reference.Target, root)) return;
                Roots.Add(new WeakReference(root));
            }
        }

        internal static void ScheduleRefresh()
        {
            Interlocked.Exchange(ref refreshNeeded, 1);
        }

        private void Update()
        {
            if (Time.realtimeSinceStartup < nextRefresh ||
                Interlocked.CompareExchange(ref refreshNeeded, 0, 1) != 1) return;
            nextRefresh = Time.realtimeSinceStartup + 1f;
            try
            {
                var localization = Localization.instance;
                if (!IsRussian(localization)) return;
                FieldInfo field = AccessTools.Field(typeof(Localization), "m_cache");
                object cache = field != null ? field.GetValue(localization) : null;
                if (cache != null)
                {
                    MethodInfo evict = AccessTools.Method(cache.GetType(), "EvictAll");
                    if (evict != null) evict.Invoke(cache, null);
                }
                lock (RootLock)
                {
                    for (int i = Roots.Count - 1; i >= 0; i--)
                    {
                        Transform root = Roots[i].Target as Transform;
                        if (root == null) { Roots.RemoveAt(i); continue; }
                        localization.ReLocalizeAll(root);
                        localization.Localize(root);
                    }
                }
                UiHarvest.Refresh();
            }
            catch (Exception ex) { Logger.LogWarning("Refresh failed: " + ex); }
        }
    }

    internal static class GATLog
    {
        public static void Msg(string message) { if (ValheimPlugin.Log != null) ValheimPlugin.Log.LogInfo(message); }
        public static void Warn(string message) { if (ValheimPlugin.Log != null) ValheimPlugin.Log.LogWarning(message); }
        public static void Err(string message) { if (ValheimPlugin.Log != null) ValheimPlugin.Log.LogError(message); }
    }

    [HarmonyPatch(typeof(Localization), "Localize", new[] { typeof(string) })]
    internal static class PatchLocalizeString
    {
        private static void Prefix(Localization __instance, ref string text)
        {
            if (ValheimPlugin.IsRussian(__instance)) text = ValheimPlugin.Get("template", text);
        }
    }

    [HarmonyPatch(typeof(Localization), "SetLanguage", new[] { typeof(string) })]
    internal static class PatchSetLanguage
    {
        private static void Postfix(string language)
        {
            ValheimPlugin.SetLanguageState(language);
            ValheimPlugin.ScheduleRefresh();
        }
    }

    [HarmonyPatch(typeof(Localization), "Translate")]
    internal static class PatchTranslate
    {
        private static void Postfix(Localization __instance, ref string __result)
        {
            if (!ValheimPlugin.IsRussian(__instance) || string.IsNullOrEmpty(__result)) return;
            if (__result[0] == '[' && __result[__result.Length - 1] == ']') return;
            __result = ValheimPlugin.Get("keyed", __result);
        }
    }

    [HarmonyPatch(typeof(Localization), "Localize", new[] { typeof(Transform) })]
    internal static class PatchLocalizeRoot
    {
        private static void Postfix(Transform root) { ValheimPlugin.Register(root); }
    }

    [HarmonyPatch(typeof(MessageHud), "ShowMessage")]
    internal static class PatchShowMessage
    {
        private static void Prefix(ref string text)
        {
            if (ValheimPlugin.IsRussian(Localization.instance))
                text = ValheimPlugin.Get("message", text);
        }
    }
}
