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
        private static readonly object FileLock = new object();
        private static string path;

        public static string PathOnDisk { get { return path; } }
        public static int Count { get { return Items.Count; } }

        private static string Key(string context, string source)
        {
            return context + "\t" + source;
        }

        public static bool TryGet(string context, string source, out string result)
        {
            return Items.TryGetValue(Key(context, source), out result);
        }

        public static void Load(string model)
        {
            Items.Clear();
            string dir = System.IO.Path.Combine(Paths.ConfigPath, "ValheimAutoTranslator");
            Directory.CreateDirectory(dir);
            string safeModel = Regex.Replace(model ?? "default", @"[^a-zA-Z0-9._-]", "_");
            if (safeModel.Length > 80) safeModel = safeModel.Substring(0, 80);
            path = System.IO.Path.Combine(dir, "cache-" + Prompt.PromptVersion + "-" + safeModel + ".tsv");
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
                        Items[Key(context, source)] = result;
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
