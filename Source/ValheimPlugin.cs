using System;
using System.Collections.Generic;
using System.Threading;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace ValheimAutoTranslator
{
    [BepInPlugin("ntxfloy.valheimautotranslator", "Valheim Auto Translator", "0.1.5")]
    public sealed class ValheimPlugin : BaseUnityPlugin
    {
        private Harmony harmony;
        private static int refreshNeeded;
        private static volatile bool languageKnown;
        private static volatile bool russianLanguage;
        private static float nextRefresh;
        private static int menuRepairFaults;
        private static bool menuRepairDisabled;
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            if (Application.isBatchMode)
            {
                Logger.LogInfo("Headless session: translation disabled.");
                return;
            }
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
            if (TextSafety.IsDemoChangelog(source) || TranslationCache.IsKnownTranslation(source)) return source;
            string translated;
            string requestSource;
            if (TranslationCache.TryGet(context, source, out translated, out requestSource)) return translated;
            TranslateWorker.Request(context, requestSource);
            return source;
        }

        internal static void ScheduleRefresh()
        {
            Interlocked.Exchange(ref refreshNeeded, 1);
        }

        private void Update()
        {
            try
            {
                if (IsRussian(Localization.instance))
                {
                    UiHarvest.Tick();
                    if (!menuRepairDisabled)
                    {
                        try { MenuChangelogRepair.Tick(); }
                        catch (Exception ex)
                        {
                            if (++menuRepairFaults >= 3)
                            {
                                menuRepairDisabled = true;
                                Logger.LogWarning("Menu changelog repair disabled after runtime errors: " + ex.Message);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Logger.LogWarning("UI upkeep failed: " + ex); }
            if (Time.realtimeSinceStartup < nextRefresh ||
                Interlocked.CompareExchange(ref refreshNeeded, 0, 1) != 1) return;
            nextRefresh = Time.realtimeSinceStartup + 1f;
            try
            {
                if (!IsRussian(Localization.instance)) return;
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
