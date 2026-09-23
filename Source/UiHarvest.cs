using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ValheimAutoTranslator
{
    internal static class UiHarvest
    {
        private sealed class Entry
        {
            public WeakReference Control;
            public string Source;
            public string Rendered;
            public bool IsTmp;
        }

        private const int MaxTracked = 5000;
        private static readonly Dictionary<int, Entry> Entries = new Dictionary<int, Entry>();
        [ThreadStatic] private static bool applying;

        internal static void OnSet(UnityEngine.Object control, ref string text, bool isTmp)
        {
            if (applying || !ValheimPlugin.IsRussian(Localization.instance) || control == null) return;
            int id = control.GetInstanceID();
            if (string.IsNullOrEmpty(text) || !PlaceholderGuard.NeedsTranslation(text))
            {
                Entries.Remove(id);
                return;
            }

            string source = text;
            string translated;
            if (TranslationCache.TryGet("ui", source, out translated)) text = translated;
            else TranslateWorker.Request("ui", source);

            if (Entries.Count >= MaxTracked && !Entries.ContainsKey(id)) PruneDead();
            if (Entries.Count >= MaxTracked && !Entries.ContainsKey(id)) return;
            Entries[id] = new Entry
            {
                Control = new WeakReference(control),
                Source = source,
                Rendered = text,
                IsTmp = isTmp
            };
        }

        private static void PruneDead()
        {
            var dead = new List<int>();
            foreach (var pair in Entries)
                if (pair.Value.Control.Target as UnityEngine.Object == null) dead.Add(pair.Key);
            foreach (int id in dead) Entries.Remove(id);
        }

        internal static void Refresh()
        {
            var dead = new List<int>();
            applying = true;
            try
            {
                foreach (var pair in Entries)
                {
                    Entry entry = pair.Value;
                    UnityEngine.Object obj = entry.Control.Target as UnityEngine.Object;
                    if (obj == null) { dead.Add(pair.Key); continue; }
                    string current;
                    if (entry.IsTmp) current = ((TMP_Text)obj).text;
                    else current = ((Text)obj).text;
                    if (current != entry.Rendered) { dead.Add(pair.Key); continue; }
                    string translated;
                    if (!TranslationCache.TryGet("ui", entry.Source, out translated) || translated == current) continue;
                    if (entry.IsTmp) ((TMP_Text)obj).text = translated;
                    else ((Text)obj).text = translated;
                    entry.Rendered = translated;
                }
                foreach (int id in dead) Entries.Remove(id);
            }
            finally { applying = false; }
        }
    }

    [HarmonyPatch(typeof(TMP_Text), "set_text", new[] { typeof(string) })]
    internal static class PatchTmpText
    {
        private static void Prefix(TMP_Text __instance, ref string value)
        {
            UiHarvest.OnSet(__instance, ref value, true);
        }
    }

    [HarmonyPatch(typeof(Text), "set_text", new[] { typeof(string) })]
    internal static class PatchUnityText
    {
        private static void Prefix(Text __instance, ref string value)
        {
            UiHarvest.OnSet(__instance, ref value, false);
        }
    }
}
