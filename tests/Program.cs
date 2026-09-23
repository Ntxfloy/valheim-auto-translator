using System;
using System.Collections.Generic;
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
        Console.WriteLine("Guard tests passed");
    }

    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
