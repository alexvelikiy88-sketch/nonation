# NONATION / отчёт 02 — Backend/Simulation

- **Последнее обновление:** 23.10.26
- **Статус:** IN PROGRESS — подтверждены .NET-тесты грузовых заказов и конкурентных сохранений; ожидается интеграционный Unity/WebGL review
- **Ветка:** `agent/backend-simulation`
- **Промпт:** [Инструкция исполнителя](../CHAT_02_BACKEND_SIMULATION.md)
- **Задача P0:** Проверить серверные и финансовые инварианты, read-only API и синхронизацию NPC.
- **База до старта:** `f909e47ce764911281eee2b97f9b6568a83779d6`
- **PR:** [#1 — Backend invariants and spectator contract (draft)](https://github.com/alexvelikiy88-sketch/nonation/pull/1)
- **Собственные коммиты:** `99d6a2471ae556cbc0bbd360ef52bd3ea669e0e1` (широкий ledger), `0d1f65968b21200e53be7ab8be6aad71e78303b3` (spectator v2), `ddad0b1979b3c135f86244ad9f16d3ad912a8ea6` (загрузка/authority tests), `5b9182a428e0d748cf0abfb961f15524dd8d82bb` (stock/counter overflow), `6471209060155f6d3a389c27d35c667149bfe81c` (freight lifecycle), `6a396a61f38b74b78c30ac1202f264a75d6b577a` (serial save)
- **Достоверные тесты по собственной работе:** [PR Validation #43](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40007776559) **SUCCESS** на `6a396a61`: .NET 8 доменные и серверные тесты, включая freight и concurrent saves; server build, HTTP/CORS smoke. Unity C#/WebGL не проверялись
- **Блокеры:** нет локального .NET SDK; фактический .NET CI успешен; Unity Editor, WebGL, браузер не проверены. Открыт CROSS-OWNER REQUEST на отделение remote-времени. Integration Guard [#12](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40008195153) **FAIL** из-за ложного определения `TASKS.md` как изменения агента при сравнении устаревшего PR base и head; требуется исправление инструмента Manager. .NET CI при этом SUCCESS

## Журнал работы

| Дата (дд.мм.гг) | Что реально изменено | SHA / PR | Проверки | Что НЕ проверено |
|---|---|---|---|---|
| 23.10.26 | Создана карточка отчётности, не работа исполнительного чата | — | — | Вся задача P0 |
| 23.10.26 | Исправлено переполнение `TotalKnownMoney()` при сумме валидных счетов > Int32.MaxValue; добавлен `WideLedgerTests` с проверкой сохранения суммы и idempotency после пересоздания `MirEconomy` | `99d6a2471ae556cbc0bbd360ef52bd3ea669e0e1` | Статический аудит типов; тесты НЕ запускались | .NET compilation, execution, HTTP smoke, Unity C#, WebGL/browser |
| 23.10.26 | Spectator DTO v2: серверная последовательность `sequence`, строгий порядок, UUID сессии, после рестарта новая сессия, запрет повторов/старого сервера, ограничение 256 NPC/256 KiB; добавлены тесты cursor и HTTP smoke assertions | `0d1f65968b21200e53be7ab8be6aad71e78303b3` / [PR #1](https://github.com/alexvelikiy88-sketch/nonation/pull/1) | Проверено по исходникам, что клиент меняет только позиции NPC и время просмотра; HTTP smoke не выполнялся | .NET execution, Unity serialization, WebGL, CORS smoke |
| 23.10.26 | Загрузка `WorldAuthority.Load`: явный отказ при null-списках и null-элементах; подключены real-authority tests: NPC координаты, деньги, Save/Load, ротация serverId, некорректный JSON | `ddad0b1979b3c135f86244ad9f16d3ad912a8ea6` / [PR #1](https://github.com/alexvelikiy88-sketch/nonation/pull/1) | Проведён статический анализ вызовов и проектов `.csproj`; тесты записаны, но не запускались | .NET build/test, HTTP process restart, Unity C#, WebGL/browser |

| 23.10.26 | Получено подтверждение фактического CI на PR head после кодовых правок: экономика и симуляция, версия/последовательность снимков, сохранение, startup в ASP.NET и read-only HTTP | [PR Validation #15](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40005514411), head `981494a0` | **SUCCESS:** `dotnet run` консольных тестов; `dotnet build` ASP.NET backend; Python HTTP smoke; логи содержат PASS money invariants, NPC movement, restart, read-only, CORS. Сборка с nullable warnings | Unity C#, Editor, WebGL/IL2CPP, browser |

| 23.10.26 | Исправлено переполнение запасов фермы/перерабатывающего цеха (сравнение в `long`) и счётчиков `daysWorked`/`skill` при max-int; граничные `ProductStockOverflowTests` | `5b9182a428e0d748cf0abfb961f15524dd8d82bb` / [PR #1](https://github.com/alexvelikiy88-sketch/nonation/pull/1) | **CI #27 SUCCESS**: PASS по всем шести граничным проверкам, финансам NPC, .NET build и HTTP smoke | Unity C#, Editor, WebGL/browser |

| 23.10.26 | Исправлен некорректный переход `Returning → Completed` при неоплаченном заказе; введены строгие пред/постусловия состояния freight и запрет возврата неверного товара; тестированы перевод, отмена, replay и сериализация `Loading` | [`6471209060`](https://github.com/alexvelikiy88-sketch/nonation/commit/6471209060155f6d3a389c27d35c667149bfe81c) | [PR Validation](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40007526159) **SUCCESS** (freight invariant tests + .NET build/HTTP) | Unity C#, WebGL |
| 23.10.26 | Защищена операция `WorldAuthority.Save()`: `saveGate` охватывает сериализацию и запись/rename, flush-to-disk временного файла, очистка временного файла при сбое; тесты fail-before-replace, orphan .tmp, 24 параллельных сохранений и NPC/денежного состояния | [`6a396a61f3`](https://github.com/alexvelikiy88-sketch/nonation/commit/6a396a61f38b74b78c30ac1202f264a75d6b577a) | [PR Validation #43](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40007776559) **SUCCESS**: все новые `PASS`, .NET build и HTTP smoke | Реальный сбой питания, multi-process locks, Unity, WebGL |

| 23.10.26 | Интеграционный Guard на обновлённом PR #1 обнаружил `TASKS.md` из-за сравнительного диапазона base…head, хотя в собственных коммитах AGENT 02 этот файл не изменялся | [Guard #12](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40008195153) | `14/14` тестов самого Guard прошли; итог **FAIL** в шаге проверки владельца из-за stale base. В `Tools/pr_integration_guard.py` используется `git diff base head`, нужно сравнивать с merge-base. Менеджерский блокер | Unity/WebGL; Guard PASS после фикса Manager |

## Последнее проверенное состояние

**CI:** [NONATION Pull Request Validation #43](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40007776559) — **SUCCESS**, `head_sha=6a396a61f38b74b78c30ac1202f264a75d6b577a`. В журнале верифицированы актуальные тесты freight и atomic save, после этого `dotnet build` и `python3 Tools/smoke_server.py`. По логу `Source preflight and .NET 8` выполнено:
- Unity source/scene **статическая** preflight — success (не Unity Editor).
- `dotnet run --project Server/Nonation.Tests/Nonation.Tests.csproj --configuration Release` — success; реально напечатаны `PASS` для 64-bit ledger, грузового escrow, перемещения NPC, неприкосновенности игрока, покупки еды, spectator v2 replay/restart, `WorldAuthority.Save/Load` и инвариантов денег.
- `dotnet build Server/Nonation.Server/Nonation.Server.csproj --configuration Release --nologo` — `Build succeeded` с nullable warnings (`CS8618/CS8603` и др.), без ошибок сборки.
- Локальный HTTP smoke на запущенном `Nonation.Server.dll` — `PASS: public spectator API, readonly security, CORS and moving server NPCs`. Истинная публичная HTTPS-среда не проверялась.

Работа ведётся в `agent/backend-simulation`; [PR #1](https://github.com/alexvelikiy88-sketch/nonation/pull/1) остаётся Draft до проверки Unity и браузера. `TASKS.md` и `main` исполнитель не редактировал. Последний подтверждённый **кодовый** SHA — `6a396a61`; последующие изменения отчёта не меняют серверный код. **Важно:** [Guard #12](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40008195153) FAIL не свидетельствует о чужой правке агента: код Guard сравнивает весь diff устаревшего PR base с head вместо diff от merge-base.

### Аудит и исправленные дефекты

1. `TotalKnownMoney()` агрегировал общий баланс в `int`; переполнение при корректной сумме счетов устранено переходом на `long`. CI: PASS для суммы выше `Int32.MaxValue`, переводов и защиты от replay.
2. `MirRemoteWorldSync` раньше допускал откат времени и не отличал повторный ответ. Теперь `MirSceneSnapshot` v2 с `sequence`, `MirSceneSnapshotCursor`, лимитом 256 NPC и отбрасыванием invalid/replay. Старый v1 endpoint несовместим: backend и Unity должны обновляться согласованно.
3. `WorldAuthority.Load` допускал явные `null` в списках при использовании `?.Count < 1`; проверки исправлены. CI: PASS по загрузке после рестарта, координатам NPC, денежному инварианту и отклонению испорченных сохранений.
4. `MirEconomy.Work` и производство в `Tick` складывали товары в `int`, что позволяло переполнять stock даже при `capacity=int.MaxValue`; `skill` и `daysWorked` тоже могли перейти в отрицательные значения. `ProductStockOverflowTests` **прошёл в CI #27**: все 6 проверок, включая граничную вместимость, зарплату и сохранение денег.
5. `SetFreightStage()` позволял закрыть `Returning` с `paid=false`, если состояние заказа было несогласованным. Добавлена единая проверка freight-state пред всеми изменениями и тесты: ни двойной платеж, ни скрытая отмена, ни неправильный `productId` не проходят. **CI #43 PASS**.
6. В `WorldAuthority.Save()` две сериализации защищались `gate`, но дисковая запись за блокировкой могла чередоваться и перезаписать новое состояние старым. Введён `saveGate` на время сериализации и финального rename, принудительный flush staging перед rename, очистка `.tmp` при исключении. **CI #43 PASS** по staging-failure, orphan-stage, 24 concurrent Save, восстановлению денег и координат NPC.

### Изменённые файлы (полный список PR)

1. `Assets/MIR/Scripts/MirEconomy.cs`
2. `Assets/MIR/Scripts/MirSceneSnapshot.cs`
3. `Assets/MIR/Scripts/MirRemoteWorldSync.cs`
4. `Server/Nonation.Server/WorldAuthority.cs`
5. `Server/Nonation.Tests/Program.cs`
6. `Server/Nonation.Tests/Nonation.Tests.csproj`
7. `Tools/smoke_server.py`
8. `docs/agents/reports/02_BACKEND_SIMULATION.md`

### Ограничения

- Unity C# компилятор, WebGL/IL2CPP, сцена в браузере и реальный внешний HTTPS ingress не проверялись.
- `saveGate` гарантирует упорядочивание сохранений **в рамках одного процесса`; несколько серверных процессов с общим `NONATION_WORLD_FILE` не поддерживаются. `Flush(true)` и rename снижают риск повреждения, но тесты не имитировали отключение питания/потерю метаданных файловой системы.
- Серверный authoritative API остаётся **read-only для внешних клиентов**, полноценной авторизации и multiplayer writes нет.
- Размер JSON Unity проверяет **после загрузки**, перед десериализацией. Это не ограничение размера принимаемого сетевого буфера; для public ingress нужен отдельный лимит тела ответа.
- `MirRemoteWorldSync` не меняет счета, товары, инвентарь, транзакции или локальный save (режим remote блокирует `Save`), **но пока присваивает `game.World.gameMinutes` для часов UI**. Это мутирует локальное игровое время в памяти, хотя не сохраняется. Полное разделение требует изменения других владельцев (см. запрос ниже).
- В `main` идёт параллельная работа других агентов. Интегратор должен проверить PR diff/конфликты и протестировать клиент v2 перед merge.

## CROSS-OWNER REQUEST

**Разделить часы серверного spectator и локальную экономику**:
- `Assets/MIR/Scripts/MirGame.cs` — владелец AGENT 01: добавить read-only `public float RemoteGameMinutes { get; private set; }` и `public void SetRemoteGameMinutes(float minutes)` с проверкой `RemoteSpectator` и конечного неотрицательного значения; метод **не должен** менять `World.gameMinutes` / `Economy` / сохранение.
- `Assets/MIR/Scripts/MirHUD.cs` — владелец AGENT 03: отображать время по `game.RemoteGameMinutes` при `game.RemoteSpectator`, иначе обычное `game.World.gameMinutes`; вычислять отображаемые день/час/минуту из выбранного значения.
- После готовности интерфейса AGENT 02 в `MirRemoteWorldSync.Accept` заменит `game.World.gameMinutes = snapshot.gameMinutes` на `game.SetRemoteGameMinutes(snapshot.gameMinutes)`.
- Причина: сейчас при просмотре удалённого мира меняется локальный часовой показатель, от которого рассчитываются offline-экономические тики; хотя save заблокирован, лучше не мутировать локальный world вообще.

**Исправить ложную блокировку Integration Guard (MANAGER):** `Tools/pr_integration_guard.py` и при необходимости `.github/workflows/integration-guard.yml` — в `main` код читает `base, head` из PR-event и вызывает `git diff base head`; для давно открытого PR #1 сюда попадает `TASKS.md`, изменённый менеджером в `main` **после** ветвления агента. Требуется определять фактический merge-base и проверять изменения **только** `merge-base → PR head`, добавить регрессионный тест для stale-base PR. AGENT 02 эти файлы не изменяет. Лог: [Guard #12](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40008195153), строка `TASKS.md: owner is manager, not agent/backend-simulation`.

## Следующее конкретное действие

После исправления сравнения диапазона PR в Guard менеджером повторить `Ownership and change integrity` на PR #1. Перед merge интегратор должен проверить Unity C# + WebGL/browser и согласованное обновление spectator v2; доменная и серверная проверка .NET успешно пройдена на SHA `6a396a61`. После CROSS-OWNER REQUEST заменить мутацию `World.gameMinutes` на отдельные удалённые часы. Следующие технические риски: контроль целостности сохранённого доменного состояния при старте и границы многопроцессного persistence.

## Правила заполнения

Писать только выполненные факты. Приложить ссылки на коммиты, PR, CI, реальные Cloud Build номера и выходные артефакты. Не стирать старые строки журнала. При конфликте с `main` — запросить управляющего интегратора. Не изменять `TASKS.md`.
