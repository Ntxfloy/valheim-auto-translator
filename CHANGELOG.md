# Changelog

## 0.1.3

- Detect unchanged model responses before writing to the cache, and give the model a clearer retry hint.
- Report the real rejection reason in diagnostic logs.

## 0.1.2

- Fix delayed refresh, repeated UI requests, cache persistence, and worker queue limits.
- Start a separate `valheim-2` cache for this prompt and validation version.

## 0.1.1

- Initial public preview for Valheim with localization, UI, and message interception.
