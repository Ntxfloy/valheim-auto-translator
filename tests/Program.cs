using System;
using System.Collections.Generic;
using System.IO;
using ValheimAutoTranslator;

static class Program
{
    static void Main()
    {
        string source = "<b>YOU ARE NOT WORTHY! DEFEAT $1!<br></b>";
        Dictionary<int, string> map;
        string masked = PlaceholderGuard.MaskPlaceholders(source, out map);
        Check(map.Count == 4, "all Valheim placeholders and tags are masked");
        string response = masked.Replace("YOU ARE NOT WORTHY! DEFEAT", "ТЫ НЕДОСТОИН! ПОБЕДИ");
        string translated = PlaceholderGuard.UnmaskPlaceholders(response, map);
        string reason;
        Check(PlaceholderGuard.Validate(source, translated, out reason), "valid translated template: " + reason);
        Check(!PlaceholderGuard.Validate(source, translated.Replace("$1", "Эйктюр"), out reason), "lost $1 rejected");
        Check(!PlaceholderGuard.Validate(source, source, out reason) && reason.Contains("исходную строку"), "unchanged response rejected with clear reason");
        Check(PlaceholderGuard.NeedsTranslation("Вы отправили 1 изображения YOU ARE NOT WORTHY!"), "mixed text detected");
        Check(!PlaceholderGuard.NeedsTranslation("ЭЙКТЮР"), "vanilla Russian name skipped");
        Check(PlaceholderGuard.NeedsTranslation("Bronze sword -> Iron sword"), "Valheim upgrade text with arrow translated");
        Check(!PlaceholderGuard.Validate("Bronze sword -> Iron sword", "Бронзовый меч — железный меч", out reason), "arrow syntax preserved");
        Check(TextSafety.IsDemoChangelog("FIRST\n* Wopdasd\n* FLopr line 2\n* Line 3"), "vanilla demo changelog identified");
        Check(TextSafety.IsDemoChangelog("* FLopr строка 2\n* Строка 3"), "translated demo changelog identified");
        Check(!TextSafety.IsDemoChangelog("Welcome to RtDMMO!\nChoose a Demigod"), "real changelog retained");
        Check(TextSafety.ContainsDigit("Wood 34/50"), "changing counter delayed");
        Check(!TextSafety.ContainsDigit("Choose a Demigod"), "static text not delayed");

        string temp = Path.Combine(Path.GetTempPath(), "ValheimAutoTranslator-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        BepInEx.Paths.ConfigPath = temp;
        TranslationCache.Load("test-model");
        Check(TranslationCache.Put("ui", "Welcome to RtDMMO!", "Добро пожаловать в RtDMMO!"), "cache write");
        Check(TranslationCache.IsKnownTranslation("Добро пожаловать в RtDMMO!"), "translated output recognized");
        TranslationCache.Load("test-model");
        Check(TranslationCache.IsKnownTranslation("Добро пожаловать в RtDMMO!"), "translated output recognized after restart");
        string cached;
        Check(TranslationCache.TryGet("ui", "Welcome to RtDMMO!", out cached) && cached == "Добро пожаловать в RtDMMO!", "existing cache preserved");
        File.Delete(TranslationCache.PathOnDisk);
        Directory.Delete(Path.Combine(temp, "ValheimAutoTranslator"));
        Directory.Delete(temp);
        Console.WriteLine("Guard tests passed");
    }

    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
