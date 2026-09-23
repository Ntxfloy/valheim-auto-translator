using TMPro;
using UnityEngine;

namespace ValheimAutoTranslator
{
    internal static class MenuChangelogRepair
    {
        private static float nextCheck;
        private static int errorCount;
        private static bool disabled;

        internal static void Tick()
        {
            if (disabled || Time.realtimeSinceStartup < nextCheck) return;
            nextCheck = Time.realtimeSinceStartup + 0.5f;

            try
            {
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
                errorCount = 0;
            }
            catch (System.Exception ex)
            {
                errorCount++;
                if (errorCount >= 3)
                {
                    disabled = true;
                    GATLog.Warn("MenuChangelogRepair disabled after 3 failures: " + ex.Message);
                }
            }
        }
    }
}
