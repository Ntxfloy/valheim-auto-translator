using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;

namespace ValheimAutoTranslator
{
    public static class TranslationCache
    {
        private static readonly ConcurrentDictionary<string, string> Items =
            new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
        private static readonly ConcurrentDictionary<string, byte> TranslatedValues =
            new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
        private static readonly ConcurrentDictionary<string, byte> PermanentFailed =
            new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
        private static readonly object FileLock = new object();
        private static string path;
        private static string failedPath;

        public static string PathOnDisk { get { return path; } }
        public static int Count { get { return Items.Count; } }
        public static int PermanentFailedCount { get { return PermanentFailed.Count; } }

        private static string Key(string context, string source)
        {
            return context + "\t" + source;
        }

        public static bool IsPermanentFailed(string context, string source)
        {
            if (string.IsNullOrEmpty(source)) return false;
            return PermanentFailed.ContainsKey(Key(context, source));
        }

        public static void AddPermanentFailed(string context, string source, string reason)
        {
            if (string.IsNullOrEmpty(source)) return;
            string key = Key(context, source);
            if (!PermanentFailed.TryAdd(key, 0)) return;
            lock (FileLock)
            {
                try
                {
                    if (!string.IsNullOrEmpty(failedPath))
                    {
                        string line = Encode(context) + "\t" + Encode(source) + "\t" + (reason ?? "") + Environment.NewLine;
                        File.AppendAllText(failedPath, line, Encoding.UTF8);
                    }
                }
                catch { }
            }
        }

        public static bool TryGet(string context, string source, out string result)
        {
            result = null;
            if (string.IsNullOrEmpty(source)) return false;
            if (Items.TryGetValue(Key(context, source), out result)) return true;

            string template;
            List<string> numbers;
            if (PlaceholderGuard.TryTemplateNumbers(source, out template, out numbers))
            {
                string templatedResult;
                if (Items.TryGetValue(Key(context, template), out templatedResult))
                {
                    result = PlaceholderGuard.RestoreNumbers(templatedResult, numbers);
                    return true;
                }
            }
            return false;
        }

        public static bool IsKnownTranslation(string text)
        {
            return !string.IsNullOrEmpty(text) && TranslatedValues.ContainsKey(text);
        }

        public static void Load(string model)
        {
            Items.Clear();
            TranslatedValues.Clear();
            PermanentFailed.Clear();
            string dir = System.IO.Path.Combine(Paths.ConfigPath, "ValheimAutoTranslator");
            Directory.CreateDirectory(dir);
            string safeModel = Regex.Replace(model ?? "default", @"[^a-zA-Z0-9._-]", "_");
            if (safeModel.Length > 80) safeModel = safeModel.Substring(0, 80);
            path = System.IO.Path.Combine(dir, "cache-" + Prompt.PromptVersion + "-" + safeModel + ".tsv");
            failedPath = System.IO.Path.Combine(dir, "failed-" + safeModel + ".tsv");
            if (File.Exists(failedPath))
            {
                try
                {
                    foreach (string line in File.ReadLines(failedPath, Encoding.UTF8))
                    {
                        string[] parts = line.Split(new[] { '\t' }, 3);
                        if (parts.Length >= 2)
                        {
                            try
                            {
                                string c = Decode(parts[0]);
                                string s = Decode(parts[1]);
                                if (s.Length > 0) PermanentFailed[Key(c, s)] = 0;
                            }
                            catch (FormatException) { }
                        }
                    }
                }
                catch { }
            }
            if (!File.Exists(path)) return;
            foreach (string line in File.ReadLines(path, Encoding.UTF8))
            {
                string[] parts = line.Split(new[] { '\t' }, 3);
                if (parts.Length != 3) continue;
                try
                {
                    string context = Decode(parts[0]);
                    string source = Decode(parts[1]);
                    string result = Decode(parts[2]);
                    if (source.Length > 0 && result.Length > 0)
                    {
                        Items[Key(context, source)] = result;
                        TranslatedValues[result] = 0;
                    }
                }
                catch (FormatException) { }
            }
        }

        public static bool Put(string context, string source, string result)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(result) || source == result) return false;
            string key = Key(context, source);
            string line = Encode(context) + "\t" + Encode(source) + "\t" + Encode(result) + Environment.NewLine;
            lock (FileLock)
            {
                if (Items.ContainsKey(key)) return true;
                // Publish only after the append succeeds, so a disk error can be retried.
                File.AppendAllText(path, line, Encoding.UTF8);
                Items[key] = result;
                TranslatedValues[result] = 0;
                return true;
            }
        }

        private static string Encode(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? ""));
        }

        private static string Decode(string value)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }
    }
}
