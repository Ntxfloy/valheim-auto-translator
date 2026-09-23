# Valheim Auto Translator

Автоматический перевод английских строк модов Valheim на русский язык. Мод работает на клиенте: перехватывает текст локализации и интерфейса, отправляет его в настроенный OpenAI-совместимый сервис и сохраняет принятые переводы в локальном кэше. **Версия 0.1.5 пока тестовая и не опубликована:** часть строк может остаться на английском.

## Что нужно

- Valheim и BepInExPack Valheim 5.4.2350 или совместимая более новая версия.
- Русский язык в настройках игры.
- Запущенный OpenAI-совместимый сервис перевода. По умолчанию используется `http://127.0.0.1:8317/v1/chat/completions`. Модель и прокси **не входят** в мод.

Модель по умолчанию — `gemini-3.8-flash-high`. В конфиге можно указать другую модель и адрес сервиса. Текст отправляется только на указанный вами адрес; используйте сервис, которому доверяете. Ключ API и кэш переводов в архив мода не входят.

## Установка и настройка

Установите мод через r2modman или Thunderstore Mod Manager. Если раньше добавляли локальный архив, удалите или отключите ту копию, чтобы BepInEx не загрузил две версии плагина. Один раз запустите игру и при необходимости отредактируйте конфиг профиля: `BepInEx/config/ntxfloy.valheimautotranslator.cfg`.

Основные параметры: `Endpoint` — адрес сервиса, `Model` — имя модели, `ApiKey` — ключ, если он требуется вашему сервису. После изменения модели или адреса перезапустите игру. Переводы сохраняются в `BepInEx/config/ValheimAutoTranslator/cache-valheim-2-ИМЯ_МОДЕЛИ.tsv`; для каждой модели используется отдельный кэш. Уже сохранённые строки повторно не запрашиваются. Для полного сброса закройте игру и удалите соответствующий файл кэша.

Тестовая версия 0.1.5 использует тот же кэш, что и 0.1.4. При локальной проверке не удаляйте его: это позволит проверить возврат в главное меню без повторного перевода всех строк.

По всем вопросам настройки пишите в [Telegram-группу ValheimAutoTranslate](https://t.me/+XHS7FxYqGK42YjIy).

## Что переводит

Мод обрабатывает строки `Localization.Localize(string)` и `Localization.Translate`, локализуемые элементы интерфейса, прямые записи в `TMP_Text.text` и `UnityEngine.UI.Text.text`, а также сообщения `MessageHud.ShowMessage`. Чат игроков не переводится. Пока перевод готовится или сервис недоступен, на экране остаётся исходный текст. Текст, который не проходит через эти пути, тоже может остаться без перевода.

## Сборка из исходников

Нужны .NET 8 SDK, установленный Valheim и профиль с BepInEx. DLL игры и BepInEx берутся с вашего компьютера и не публикуются в репозитории.

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1 -GameRoot 'D:\Steam\steamapps\common\Valheim' -Profile "$env:APPDATA\r2modmanPlus-local\Valheim\profiles\Default"
powershell -ExecutionPolicy Bypass -File .\make_package.ps1
```

Если `-GameRoot` не указан, скрипт ищет игру в распространённых папках Steam. Параметр `-Install` копирует DLL в выбранный профиль; запускайте его только при закрытой игре.

## English description

Valheim Auto Translator is an experimental client-side mod that translates English text from Valheim mods into Russian through an OpenAI-compatible endpoint. It caches accepted translations locally and reuses them on later launches.

Requirements: Valheim with BepInExPack Valheim 5.4.2350, Russian selected in game, and a running translation endpoint. The default endpoint is `http://127.0.0.1:8317/v1/chat/completions`, and the default model name is `gemini-3.8-flash-high`; both are configurable in `BepInEx/config/ntxfloy.valheimautotranslator.cfg`. No model, proxy, API key, or translation cache is bundled. The mod sends game text to the endpoint you configure.

Install through r2modman or Thunderstore Mod Manager. Disable any older locally imported copy first. The mod covers Valheim localization calls, UI text, and game messages; player chat is excluded. Text outside these paths may remain untranslated, and original text is shown while translation is pending or the endpoint is unavailable. For configuration help, join the [ValheimAutoTranslate Telegram group](https://t.me/+XHS7FxYqGK42YjIy).

## Лицензия / License

MIT. См. [LICENSE в репозитории](https://github.com/Ntxfloy/valheim-auto-translator/blob/main/LICENSE).
