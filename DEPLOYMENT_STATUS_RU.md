# NONATION — проверенное состояние WebGL и публикации

**Дата:** 24.10.26. **Публичный запуск игры пока не подтверждён.**

## Сборки Unity Cloud

| Build | Статус | Доказательства |
|---|---|---|
| №1 | FAIL | Ошибка C# `CS0103` в `MirCity.cs`, исправлена в последующих исходниках |
| №2 | FAIL | Ошибка `CS0117` в `MirUnitySetup.cs`, исправлена |
| №3 | FAIL | Исторический SHA `784a3b56...` не содержал зарегистрированной сцены; полный лог 7 929 строк, строка 7845: `There were no scenes configured to build!`, а `Unity player export failed!` лишь последствие |
| **№4** | **SUCCESS** | Реальный Unity WebGL export **Unity-ветки** `cb46b4960dd8bcd255880f569254dfe9876ea14b`, архив `nonation-agent01-preview` проверен и опубликован в [Actions #40015140424](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40015140424). Игра по Pages из него не проверена |

## Объединение трёх направлений

Интегратор подготовил [PR #19](https://github.com/alexvelikiy88-sketch/nonation/pull/19), базовый 4-parent Git commit `52d3bd8a37b5fa507789db6d476e29cd54a6300f`. Точные три агентские SHA и manifest изменений — [INTEGRATION_REPORT_RU.md](Documentation/INTEGRATION_REPORT_RU.md). Оригинальные агенты и их отчёты не перезаписывались; manager-authored записи помечены отдельно.

**GitHub Actions на исходном объединённом SHA:**

- [Integration Guard #40019467544](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40019467544): SUCCESS.
- [PR Validation #40019467542](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40019467542): SUCCESS, Python tests + .NET 8 domain/server + HTTP/CORS/spectator smoke.
- [Cross-Branch #40019467534](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40019467534): SUCCESS, совместные исходники.
- `MIR Unity WebGL` на PR: **source-validation SUCCESS**, а ``webgl/cloud-webgl/deploy/browser-smoke` пропущены. Это **не подтверждает Unity компиляцию интегрированного кода**.

## Последовательность реального запуска

1. Финальный PR проходит Integration Guard, Python/.NET и review, без опасных ассетов.
2. Официальная Unity Cloud Build Automation либо лицензированный GameCI экспортирует **тот же source SHA**, который собирается. Проверка `lastBuiltRevision` обязательна, нельзя выдавать `requestedRevision` за фактическую сборку.
3. Проверить непустые `index.html` и `Build/*.wasm`, `*.data`, `*.js`; при Pages без custom Content-Encoding выгружать несжатые файлы.
4. `actions/upload-pages-artifact` → `actions/deploy-pages`; проверить HTTPS и Content-Type всех файлов.
5. `browser-smoke` (Chromium) проверяет `NONATION_RUNTIME_READY`, сцену, Canvas, NPC, `NONATION_PLAYER_MOVED`, `NONATION_SAVE_WRITTEN` и `NONATION_SAVE_RESTORED` после reload, а также критические ошибки Console.

**Предполагаемый URL:** `https://alexvelikiy88-sketch.github.io/nonation/` — **не заявлять как работающую игру**, пока пункт 5 не прошёл на опубликованной версии.

## Доступ и ограничения

Unity Cloud target: organisation `18968527430273`, project `97c90bad-fafd-4410-8e4f-0ea68deb4232`, target `default-webgl`, Editor `6000.0.58f2`, scene `Assets/MIR/Scenes/MIR_PlayableDistrict.unity`. Service-account secrets `UNITY_SA_KEY_ID` / `UNITY_SA_SECRET_KEY` задаются через защищённые GitHub Actions Secrets, не печатаются и не коммитятся. GameCI activation — альтернативный легальный механизм.

Пока не доказаны интегрированная Unity Editor C#/IL2CPP компиляция, работоспособность игры в браузере, Avatar/Animator реального Rocketbox, FPS/draw calls, производство общего MMO-сервера 24/7. Нельзя запускать несколько конкурирующих Cloud сборок без выяснения причин сбоев.
