# Changelog

## 0.1.5 — тестовая ветка, не опубликована

- Восстанавливается текст меню CustomMainMenu, если после выхода из мира Valheim показывает свой демонстрационный changelog вместо `changelog.txt`.
- Демонстрационный текст `FLopr` больше не переводится и не добавляется в кэш; уже сохранённые переводы сохраняются.
- Результаты перевода не отправляются в модель повторно, даже если в них остались английские имена.
- Перевод применяется после штатной локализации Valheim, до подстановки аргументов; исходная строка для игры не меняется.
- Интерфейсные строки с числами ставятся в очередь только после секунды без изменений.
- В безголовом режиме игры переводчик не запускает рабочие потоки.

## 0.1.4

- Исправлено обновление интерфейса: готовые переводы применяются к видимому тексту без принудительной перелокализации всего окна. Это предотвращает подмену динамического содержимого служебным текстом Valheim.

## 0.1.3

- Detect unchanged model responses before writing to the cache, and give the model a clearer retry hint.
- Report the real rejection reason in diagnostic logs.

## 0.1.2

- Fix delayed refresh, repeated UI requests, cache persistence, and worker queue limits.
- Start a separate `valheim-2` cache for this prompt and validation version.

## 0.1.1

- Initial public preview for Valheim with localization, UI, and message interception.
