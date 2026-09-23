using System;
using System.Collections.Generic;
using System.Text;

namespace ValheimAutoTranslator
{
    public static class Prompt
    {
        public const string PromptVersion = "valheim-2";
        public const string System =
            "Ты переводчик модов игры Valheim на русский язык. Ответь только JSON-объектом {\"id\":\"перевод\"}. " +
            "Переводи только значения, ключи не меняй. Сохраняй все маркеры ⟦1⟧, ⟦2⟧ ровно по одному разу, " +
            "а также HTML/TMP-теги, переносы строк и регистр. Не добавляй пояснения. " +
            "Кириллические фрагменты исходника сохраняй дословно. " +
            "Для коротких элементов интерфейса используй краткий естественный перевод. " +
            "Имена боссов: Eikthyr — Эйктюр, The Elder — Древний, Bonemass — Масса костей, Moder — Моудер, Yagluth — Яглут.";

        public static string BuildUserMessage(string context, Dictionary<string, string> items,
            Dictionary<string, Dictionary<int, string>> requiredMarkers = null,
            Dictionary<string, string> retryHints = null)
        {
            var sb = new StringBuilder();
            sb.Append("{\"context\":\"").Append(MiniJson.Escape(context)).Append("\",\"items\":{");
            bool first = true;
            foreach (var kv in items)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append('"').Append(MiniJson.Escape(kv.Key)).Append("\":\"")
                  .Append(MiniJson.Escape(kv.Value)).Append('"');
            }
            sb.Append('}');
            if (requiredMarkers != null && requiredMarkers.Count > 0)
            {
                sb.Append(",\"required_markers\":{");
                first = true;
                foreach (var kv in requiredMarkers)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append('"').Append(MiniJson.Escape(kv.Key)).Append("\":\"");
                    foreach (int n in kv.Value.Keys) sb.Append('⟦').Append(n).Append('⟧').Append(' ');
                    sb.Append('"');
                }
                sb.Append('}');
            }
            if (retryHints != null && retryHints.Count > 0)
            {
                sb.Append(",\"retry_hints\":{");
                first = true;
                foreach (var kv in retryHints)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append('"').Append(MiniJson.Escape(kv.Key)).Append("\":\"")
                      .Append(MiniJson.Escape(kv.Value)).Append('"');
                }
                sb.Append('}');
            }
            sb.Append('}');
            return sb.ToString();
        }
    }
}
