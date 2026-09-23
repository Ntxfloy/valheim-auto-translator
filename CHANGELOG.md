# Changelog

## 0.1.5 — тестовая ветка, не опубликована

- `Localization.Localize(string)` возвращён на Prefix: перевод выполняется по исходному шаблону, а токены вида `$enemy_eikthyr` и `$1` маскируются через `\$[a-zA-Z0-9_]+` в `PlaceholderGuard`. Штатная подстановка Valheim выполняется уже по переведённому тексту.
- Шаблонизация чисел для динамических строк (`Wood 34/50` -> `Wood {0}/{1}`): шаблон переводится один раз и сохраняется в кэш, избавляя от повторных запросов при смене значений и раздувания базы кэша.
- Добавлен список окончательных отказов `failed.tsv`: безнадёжные строки не опрашиваются заново при каждом перезапуске игры.
- Добавлен счётчик ошибок в `MenuChangelogRepair`: при непредвиденных изменениях в игре модуль безопасно отключается после 3 сбоев без спама в лог.
- Восстанавливается текст меню CustomMainMenu при возврате в лобби; демонстрационный текст `FLopr` отфильтрован.
- `IsKnownTranslation` исключает повторный перехват собственных переводов.
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
