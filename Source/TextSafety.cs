using System;

namespace ValheimAutoTranslator
{
    internal static class TextSafety
    {
        // Valheim's serialized changelog sample is sometimes restored when returning to the menu.
        internal static bool IsDemoChangelog(string text)
        {
            return !string.IsNullOrEmpty(text) &&
                text.IndexOf("FLopr", StringComparison.OrdinalIgnoreCase) >= 0 &&
                (text.IndexOf("Wopdasd", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 text.IndexOf("строка 3", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 text.IndexOf("Line 3", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        internal static bool ContainsDigit(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            for (int i = 0; i < text.Length; i++)
                if (char.IsDigit(text[i])) return true;
            return false;
        }

        internal static bool ContainsCyrillic(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            for (int i = 0; i < text.Length; i++)
                if (text[i] >= '\u0400' && text[i] <= '\u052F') return true;
            return false;
        }
    }
}
