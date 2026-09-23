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
            public string RequestSource;
            public string Rendered;
            public bool IsTmp;
            public bool Queued;
            public float FirstSeen;
        }

        private const int MaxTracked = 5000;
        private static readonly Dictionary<int, Entry> Entries = new Dictionary<int, Entry>();
        private static float nextSweep;
        [ThreadStatic] private static bool applying;

        internal static void OnSet(UnityEngine.Object control, ref string text, bool isTmp)
        {
            if (applying || !ValheimPlugin.IsRussian(Localization.instance) || control == null) return;
            int id = control.GetInstanceID();
            if (string.IsNullOrEmpty(text) || TextSafety.IsDemoChangelog(text) ||
                TranslationCache.IsKnownTranslation(text) || !PlaceholderGuard.NeedsTranslation(text))
            {
                Entries.Remove(id);
                return;
            }

            string source = text;
            string translated;
            string requestSource;
            bool cached = TranslationCache.TryGet("ui", source, out translated, out requestSource);
            if (cached) text = translated;
            bool delayed = !cached && requestSource == source && TextSafety.ContainsDigit(source);
            if (!cached && !delayed) TranslateWorker.Request("ui", requestSource);

            Entry previous;
            bool sameSource = Entries.TryGetValue(id, out previous) && previous.Source == source;
            float firstSeen = sameSource ? previous.FirstSeen : Time.realtimeSinceStartup;
            bool queued = cached || !delayed || (sameSource && previous.Queued);

            if (Entries.Count >= MaxTracked && !Entries.ContainsKey(id)) PruneDead();
            if (Entries.Count >= MaxTracked && !Entries.ContainsKey(id)) return;
            Entries[id] = new Entry
            {
                Control = new WeakReference(control),
                Source = source,
                RequestSource = requestSource,
                Rendered = text,
                IsTmp = isTmp,
                Queued = queued,
                FirstSeen = firstSeen
            };
        }

        internal static void Tick()
        {
            float now = Time.realtimeSinceStartup;
            if (now < nextSweep) return;
            nextSweep = now + 0.25f;
            var dead = new List<int>();
            foreach (var pair in Entries)
            {
                Entry entry = pair.Value;
                if (entry.Queued || now - entry.FirstSeen < 1f) continue;
                UnityEngine.Object obj = entry.Control.Target as UnityEngine.Object;
                if (obj == null) { dead.Add(pair.Key); continue; }
                string current = entry.IsTmp ? ((TMP_Text)obj).text : ((Text)obj).text;
                if (current != entry.Rendered) { dead.Add(pair.Key); continue; }
                TranslateWorker.Request("ui", entry.RequestSource);
                entry.Queued = true;
            }
            foreach (int id in dead) Entries.Remove(id);
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
