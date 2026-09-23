using TMPro;
using UnityEngine;

namespace ValheimAutoTranslator
{
    internal static class MenuChangelogRepair
    {
        private static float nextCheck;

        internal static void Tick()
        {
            if (Time.realtimeSinceStartup < nextCheck) return;
            nextCheck = Time.realtimeSinceStartup + 0.5f;

            FejdStartup menu = FejdStartup.instance;
            if (menu == null || menu.m_changeLog == null) return;
            ChangeLog changelog = menu.m_changeLog.GetComponent<ChangeLog>();
            if (changelog == null) return;
            TMP_Text label = changelog.m_textField;
            if (label == null || !TextSafety.IsDemoChangelog(label.text)) return;

            // Use the game's normal changelog path so CustomMainMenu can supply its file.
            string actual = changelog.GetPlatformText();
            if (string.IsNullOrEmpty(actual) || TextSafety.IsDemoChangelog(actual)) return;
            label.text = actual;
            GATLog.Msg("Restored main-menu changelog after scene transition.");
        }
    }
}
