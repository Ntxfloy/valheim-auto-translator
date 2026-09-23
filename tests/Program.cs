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

        string numTemplate;
        List<string> numbers;
        Check(PlaceholderGuard.TryTemplateNumbers("Wood 34/50", out numTemplate, out numbers) && numTemplate == "Wood [[GATNUM:0]]/[[GATNUM:1]]" && numbers.Count == 2 && numbers[0] == "34" && numbers[1] == "50", "number templating creates correct template");
        Check(PlaceholderGuard.RestoreNumbers("Дерево [[GATNUM:0]]/[[GATNUM:1]]", numbers) == "Дерево 34/50", "number restoration works");
        Check(PlaceholderGuard.TryTemplateNumbers("Deals {0} damage to 3 enemies", out numTemplate, out numbers) && numTemplate == "Deals {0} damage to [[GATNUM:0]] enemies", "number marker cannot collide with game placeholder");
        Check(PlaceholderGuard.RestoreNumbers("Наносит {0} урона [[GATNUM:0]] врагам", numbers) == "Наносит {0} урона 3 врагам", "real game placeholder survives number restoration");
        Dictionary<int, string> mixedMarkers;
        string mixedMasked = PlaceholderGuard.MaskPlaceholders(numTemplate, out mixedMarkers);
        Check(mixedMarkers.Count == 2 && mixedMarkers[1] == "{0}" && mixedMarkers[2] == "[[GATNUM:0]]", "both real and numeric placeholders are independently protected");
        string mixedTranslated = PlaceholderGuard.UnmaskPlaceholders("Наносит ⟦1⟧ урона ⟦2⟧ врагам", mixedMarkers);
        Check(PlaceholderGuard.Validate(numTemplate, mixedTranslated, out reason), "translated numeric template validates: " + reason);
        Check(PlaceholderGuard.RestoreNumbers(mixedTranslated, numbers) == "Наносит {0} урона 3 врагам", "full template round trip preserves game placeholder");
        Check(!PlaceholderGuard.TryTemplateNumbers("[[GATNUM:0]] 3", out numTemplate, out numbers), "existing private marker is not reused");
        Check(!PlaceholderGuard.TryTemplateNumbers("YOU ARE NOT WORTHY! DEFEAT $1!", out numTemplate, out numbers), "$1 placeholder not broken by number templating");
        Check(!PlaceholderGuard.TryTemplateNumbers("Choose a Demigod", out numTemplate, out numbers), "text without numbers not templated");
        Check(PlaceholderGuard.IsStructuralFailure("плейсхолдеры не совпадают: [] -> []"), "placeholder failure is structural");
        Check(!PlaceholderGuard.IsStructuralFailure("missing response id"), "missing model response is transient");
        Check(!PlaceholderGuard.IsStructuralFailure("модель вернула исходную строку без перевода"), "unchanged model response is transient");

        Dictionary<int, string> tokenMap;
        string tokenSource = "DEFEAT $enemy_eikthyr NOW!";
        string tokenMasked = PlaceholderGuard.MaskPlaceholders(tokenSource, out tokenMap);
        Check(tokenMap.Count == 1 && tokenMap[1] == "$enemy_eikthyr", "$enemy_eikthyr masked properly");
        string tokenRestored = PlaceholderGuard.UnmaskPlaceholders(tokenMasked.Replace("DEFEAT", "ПОБЕДИ").Replace("NOW", "СЕЙЧАС"), tokenMap);
        Check(tokenRestored == "ПОБЕДИ $enemy_eikthyr СЕЙЧАС!", "$enemy_eikthyr restored properly");

        string temp = Path.Combine(Path.GetTempPath(), "ValheimAutoTranslator-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        BepInEx.Paths.ConfigPath = temp;
        TranslationCache.Load("test-model");
        Check(TranslationCache.Put("ui", "Welcome to RtDMMO!", "Добро пожаловать в RtDMMO!"), "cache write");
        Check(TranslationCache.IsKnownTranslation("Добро пожаловать в RtDMMO!"), "translated output recognized");
        Check(TranslationCache.Put("ui", "Wood [[GATNUM:0]]/[[GATNUM:1]]", "Дерево [[GATNUM:0]]/[[GATNUM:1]]"), "templated cache write");
        string templatedCached;
        Check(TranslationCache.TryGet("ui", "Wood 99/100", out templatedCached) && templatedCached == "Дерево 99/100", "templated number dynamic match from cache");
        Check(TranslationCache.IsKnownTranslation("Дерево 99/100"), "materialized numbered translation recognized");
        Check(TranslationCache.Put("ui", "Mage [[GATNUM:0]]", "Маг wood [[GATNUM:0]]"), "mixed-script templated cache write");
        Check(TranslationCache.IsKnownTranslation("Маг wood 34"), "materialized mixed-script translation recognized");
        TranslationCache.Load("test-model");
        Check(TranslationCache.IsKnownTranslation("Добро пожаловать в RtDMMO!"), "translated output recognized after restart");
        Check(TranslationCache.TryGet("ui", "Wood 12/20", out templatedCached) && templatedCached == "Дерево 12/20", "templated number dynamic match after cache reload");
        string cached;
        Check(TranslationCache.TryGet("ui", "Welcome to RtDMMO!", out cached) && cached == "Добро пожаловать в RtDMMO!", "existing cache preserved");

        TranslationCache.AddPermanentFailed("ui", "TransientString", "missing response id");
        Check(!TranslationCache.IsPermanentFailed("ui", "TransientString"), "transient failure is not persisted");
        TranslationCache.AddPermanentFailed("ui", "BrokenString123", "плейсхолдеры не совпадают");
        Check(TranslationCache.IsPermanentFailed("ui", "BrokenString123"), "permanent failure recognized");
        TranslationCache.Load("test-model");
        Check(TranslationCache.IsPermanentFailed("ui", "BrokenString123"), "permanent failure preserved across restart");

        File.Delete(TranslationCache.PathOnDisk);
        string failedFile = Path.Combine(temp, "ValheimAutoTranslator", "failed-test-test-model.tsv");
        if (File.Exists(failedFile)) File.Delete(failedFile);
        Directory.Delete(Path.Combine(temp, "ValheimAutoTranslator"));
        Directory.Delete(temp);
        Console.WriteLine("Guard tests passed");
    }

    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
