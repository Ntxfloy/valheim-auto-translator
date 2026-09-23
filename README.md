# Valheim Auto Translator

Client-side automatic Russian translation for Valheim mods. It translates English text that reaches Valheim's localization system or UI, using an OpenAI-compatible endpoint, and saves accepted translations to a local cache. This is an **experimental preview**.

## Requirements

- Valheim with BepInExPack Valheim 5.4.2350 or a compatible newer version.
- Russian selected in the game's language settings.
- A running OpenAI-compatible translation endpoint. The default is `http://127.0.0.1:8317/v1/chat/completions`; the mod does **not** bundle an AI model or proxy.

The default model name is `gemini-3.8-flash-high`. Change `Model` in the generated config if your endpoint uses another model. Set `ApiKey` there only if your endpoint requires one. The mod sends text to the endpoint you configure; choose an endpoint you trust. API keys and caches are never included in the package.

## Install and configure

Install with r2modman or Thunderstore Mod Manager. If you previously imported a local version, remove or disable that copy so BepInEx does not load the plugin twice. Launch the game once, then edit `BepInEx/config/ntxfloy.valheimautotranslator.cfg` in your profile if needed. Restart the game after changing the model or endpoint.

Translations are cached at `BepInEx/config/ValheimAutoTranslator/cache-valheim-2-MODEL.tsv`. The cache is separate for each model. Already accepted translations are reused on later launches. To start over, close the game and remove the appropriate cache file.

The mod translates strings from `Localization.Localize(string)`, `Localization.Translate`, localized UI trees, direct `TMP_Text.text` and `UnityEngine.UI.Text.text` updates, and `MessageHud.ShowMessage`. Player chat is excluded. Some text that never reaches these paths may remain untranslated. While a translation is pending or the endpoint is unavailable, the original text remains visible.

## Build from source

Requires the .NET 8 SDK, an installed Valheim game and a BepInEx profile. Game and BepInEx DLLs are referenced locally and are not distributed in this repository.

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1 -GameRoot 'D:\Steam\steamapps\common\Valheim' -Profile "$env:APPDATA\r2modmanPlus-local\Valheim\profiles\Default"
powershell -ExecutionPolicy Bypass -File .\make_package.ps1
```

`build.ps1` also looks in common Steam locations when `-GameRoot` is omitted. The `-Install` flag copies the DLL into the selected profile; use it only while the game is closed. The package script creates the ZIP for Thunderstore.

## Русский

Клиентский автопереводчик английских строк модов Valheim на русский. Нужны выбранный русский язык в игре, BepInEx и запущенный OpenAI-совместимый сервис перевода. Модель по умолчанию — `gemini-3.8-flash-high`, но имя модели и адрес сервиса меняются в `BepInEx/config/ntxfloy.valheimautotranslator.cfg`. Переводы сохраняются локально и повторно используются после перезапуска. Чат игроков не переводится. Это экспериментальная версия: часть строк может остаться на английском.

## License

MIT. See `LICENSE` in the [source repository](https://github.com/Ntxfloy/valheim-auto-translator).
