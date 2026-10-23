# NONATION / отчёт 02 — Backend/Simulation

- **Последнее обновление:** 23.10.26
- **Статус:** IN PROGRESS — три кодовых коммита; проверки новых тестов требуют .NET 8
- **Ветка:** `agent/backend-simulation`
- **Промпт:** [Инструкция исполнителя](../CHAT_02_BACKEND_SIMULATION.md)
- **Задача P0:** Проверить серверные и финансовые инварианты, read-only API и синхронизацию NPC.
- **База до старта:** `f909e47ce764911281eee2b97f9b6568a83779d6`
- **PR:** [#1 — Backend invariants and spectator contract (draft)](https://github.com/alexvelikiy88-sketch/nonation/pull/1)
- **Собственные коммиты:** `99d6a2471ae556cbc0bbd360ef52bd3ea669e0e1` (широкий ledger), `0d1f65968b21200e53be7ab8be6aad71e78303b3` (spectator v2), `ddad0b1979b3c135f86244ad9f16d3ad912a8ea6` (загрузка/authority tests)
- **Достоверные тесты по собственной работе:** написаны регрессионные сценарии для экономики, snapshots и реального WorldAuthority; исполнение новых тестов и HTTP smoke не подтверждено (нет .NET SDK)
- **Блокеры:** отсутствует .NET SDK и доступ из контейнера к github.com; PR #1 содержит непроверенные компилятором изменения, не merge до CI

## Журнал работы

| Дата (дд.мм.гг) | Что реально изменено | SHA / PR | Проверки | Что НЕ проверено |
|---|---|---|---|---|
| 23.10.26 | Создана карточка отчётности, не работа исполнительного чата | — | — | Вся задача P0 |
| 23.10.26 | Исправлено переполнение `TotalKnownMoney()` при сумме валидных счетов > Int32.MaxValue; добавлен `WideLedgerTests` с проверкой сохранения суммы и idempotency после пересоздания `MirEconomy` | `99d6a2471ae556cbc0bbd360ef52bd3ea669e0e1` | Статический аудит типов; тесты НЕ запускались | .NET compilation, execution, HTTP smoke, Unity C#, WebGL/browser |
| 23.10.26 | Spectator DTO v2: серверная последовательность `sequence`, строгий порядок, UUID сессии, после рестарта новая сессия, запрет повторов/старого сервера, ограничение 256 NPC/256 KiB; добавлены тесты cursor и HTTP smoke assertions | `0d1f65968b21200e53be7ab8be6aad71e78303b3` / [PR #1](https://github.com/alexvelikiy88-sketch/nonation/pull/1) | Проверено по исходникам, что клиент меняет только позиции NPC и время просмотра; HTTP smoke не выполнялся | .NET execution, Unity serialization, WebGL, CORS smoke |
| 23.10.26 | Загрузка `WorldAuthority.Load`: явный отказ при null-списках и null-элементах; подключены real-authority tests: NPC координаты, деньги, Save/Load, ротация serverId, некорректный JSON | `ddad0b1979b3c135f86244ad9f16d3ad912a8ea6` / [PR #1](https://github.com/alexvelikiy88-sketch/nonation/pull/1) | Проведён статический анализ вызовов и проектов `.csproj`; тесты записаны, но не запускались | .NET build/test, HTTP process restart, Unity C#, WebGL/browser |

## Последнее проверенное состояние

Работа ведётся только в ветке `agent/backend-simulation`, PR [#1](https://github.com/alexvelikiy88-sketch/nonation/pull/1) оставлен draft. На момент входа `main` = `e3f91d34a7176cf453710ef1548c904ec7202be8` (относительно baseline изменился только `TASKS.md`), пересечений по изменённым файлам нет. Последний найденный CI [#54](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40001657334) относится к `f909e47`: source-validation success, WebGL/cloud/deploy skipped. Новые коммиты этим запуском **не проверены**.

### Аудит тестов и найденные дефекты

В `Server/Nonation.Tests/Program.cs` проверяются магазин, еда, зарплата/cooldown, движение NPC, реальные запасы/производственный tick, логистика и возврат escrow, запрет повторных транзакций, сериализация публичного снимка. Найдены и исправлены в отдельных коммитах:
- `TotalKnownMoney()` суммировал счета в `int`, что переполнялось при корректных балансах; теперь `long` и регрессионный тест.
- Клиент ранее допускал откат игрового времени до минуты и не отличал повторный ответ: `MirSceneSnapshot` версия 2 с `sequence`, `MirSceneSnapshotCursor` и bounded payload. Новый клиент **не совместим** со старым v1 endpoint, сервер и Unity-клиент должны быть обновлены согласованно.
- `WorldAuthority.Load` допускал явные `null`-списки, поскольку `candidate.citizens?.Count < 1` не ловит `null`; добавлена проверка и сценарии сохранения/перезапуска на настоящем классе `WorldAuthority`.

### Изменённые файлы (полный список PR)

1. `Assets/MIR/Scripts/MirEconomy.cs`
2. `Assets/MIR/Scripts/MirSceneSnapshot.cs`
3. `Assets/MIR/Scripts/MirRemoteWorldSync.cs`
4. `Server/Nonation.Server/WorldAuthority.cs`
5. `Server/Nonation.Tests/Program.cs`
6. `Server/Nonation.Tests/Nonation.Tests.csproj`
7. `Tools/smoke_server.py`
8. `docs/agents/reports/02_BACKEND_SIMULATION.md`

### Тесты и ограничения

**Фактически сделано:** аудит исходников GitHub, сверка разрешённых путей, чтение тестов и CI, статическая проверка последовательности и контрактов. **Не запускалось:** `dotnet build`, `dotnet run --project Server/Nonation.Tests/Nonation.Tests.csproj`, настоящий HTTP smoke и серверный рестарт, Unity C# / Editor, IL2CPP, WebGL/browser. В среде нет .NET SDK, выход в GitHub из локального контейнера недоступен; нет оснований объявлять экономический инвариант и прогресс NPC доказанными на новой ветке до исполнения тестов.

**Ограничение защиты payload:** сервер отдаёт не более 256 публичных NPC; Unity проверяет предел 256 KiB **после получения**, перед JSON-десериализацией. Это не ограничение сетевой памяти во время загрузки; на production ingress нужен отдельный предел ответа. Старые v1-клиенты несовместимы с v2. Экономический snapshot по-прежнему не доступен публично, API только GET.

## CROSS-OWNER REQUEST

Нет. Если нужна правка чужого файла: `[путь] — [зачем] — [точный требуемый интерфейс] — [ссылка на ошибку]`.

## Следующее конкретное действие

В доступной среде с .NET 8 выполнить `dotnet run --project Server/Nonation.Tests/Nonation.Tests.csproj --configuration Release`, `dotnet build Server/Nonation.Server/Nonation.Server.csproj --configuration Release`, запустить сервер на `127.0.0.1:5180` с разрешённым origin и `python3 Tools/smoke_server.py`. Проверить рестарт процесса с тем же `NONATION_WORLD_FILE`, затем дать интегратору результаты до merge PR #1. Следующий технический блокер — отсутствие реального CI на агентной ветке и Unity build.

## Правила заполнения

Писать только выполненные факты. Приложить ссылки на коммиты, PR, CI, реальные Cloud Build номера и выходные артефакты. Не стирать старые строки журнала. При конфликте с `main` — запросить управляющего интегратора. Не изменять `TASKS.md`.
