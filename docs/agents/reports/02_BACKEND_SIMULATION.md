# NONATION / отчёт 02 — Backend/Simulation

- **Последнее обновление:** 24.10.26
- **Статус:** IN PROGRESS — проверены строгая загрузка сохранений и защита идентификаторов от переполнения; Unity/WebGL остаётся интеграционным блокером
- **Ветка:** `agent/backend-simulation`
- **Промпт:** [Инструкция исполнителя](../CHAT_02_BACKEND_SIMULATION.md)
- **Задача P0:** Проверить серверные и финансовые инварианты, read-only API и синхронизацию NPC.
- **База до старта:** `f909e47ce764911281eee2b97f9b6568a83779d6`
- **PR:** [#1 — Backend invariants and spectator contract (draft)](https://github.com/alexvelikiy88-sketch/nonation/pull/1)
- **Собственные коммиты:** `99d6a2471ae556cbc0bbd360ef52bd3ea669e0e1` (широкий ledger), `0d1f65968b21200e53be7ab8be6aad71e78303b3` (spectator v2), `ddad0b1979b3c135f86244ad9f16d3ad912a8ea6` (загрузка/authority tests), `5b9182a428e0d748cf0abfb961f15524dd8d82bb` (stock/counter overflow), `6471209060155f6d3a389c27d35c667149bfe81c` (freight lifecycle), `6a396a61f38b74b78c30ac1202f264a75d6b577a` (serial save), `1884236bd1239f328810a74415631297eec19a82` (saved-world validation), `0a53eb178a81ff111d577dc095ba44fc53763988` (ID exhaustion)
- **Достоверные тесты по собственной работе:** [PR Validation #66](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40010216758) **SUCCESS** на кодовом SHA `0a53eb17`: все доменные тесты, в том числе 6 новых граничных проверок ID, NPC, money invariants; .NET server build и HTTP smoke; [Integration Guard #42](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40010216734) **SUCCESS**. Unity/WebGL не проверялись
- **Блокеры:** .NET SDK локально отсутствует, но CI #66 подтверждает сборку/тесты/HTTP; Unity C#, WebGL и браузерный прогон не проверены. Не выполнено разделение remote-часов (AGENT 01 + 03). Многопроцессное сохранение не поддерживается; реальное долговременное сетевое развёртывание не проверено.

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

| 24.10.26 | Добавлена строгая проверка сохранённого доменного мира в `WorldAuthority.Load`: неотрицательные счета и товары, уникальность счётных идентификаторов, транзакций и заказов, корректный escrow/state по freight, не более одного активного заказа. При нарушении сервер не запускает неконсистентную экономику и не перезаписывает JSON. 13 негативных кейсов и 3 позитивные проверки `SavedWorldIntegrityTests`, включая успешное возобновление нормальной поставки | Код и тесты в текущем PR #1, SHA этого коммита см. историю ветки | Статический аудит, CI для нового SHA ожидается | Unity/WebGL, внешняя HTTPS-среда, power loss |

| 24.10.26 | Проверка финансово некорректных сохранений `WorldAuthority.Load`, отказ без порчи канонического JSON и восстановление допустимой поставки | `1884236bd1239f328810a74415631297eec19a82` | [CI #61](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40009669218) SUCCESS: все `SavedWorldIntegrityTests`, NPC movement, money invariant, .NET build и HTTP; [Guard](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40009669212) SUCCESS | Unity Editor/WebGL/browser |
| 24.10.26 | Защита счётчиков `transactionSequence` и `jobSequence` от Int32 overflow: отказ в новых автогенерируемых операциях без расходования средств и запасов | `0a53eb178a81ff111d577dc095ba44fc53763988` | [CI #66](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40010216758) SUCCESS, 6 PASS по ID exhaustion, NPC/money invariants, .NET build и HTTP; [Guard #42](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40010216734) SUCCESS | Unity C#, WebGL, браузер |

## Последнее проверенное состояние

**CI:** [NONATION Pull Request Validation #66](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40010216758) — **SUCCESS** на последнем кодовом SHA `0a53eb178a81ff111d577dc095ba44fc53763988`. Проверены также saved-world integrity, freight, concurrent saves и ID sequence exhaustion; [Integration Guard #42](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40010216734) — SUCCESS. По логу `Source preflight and .NET 8` выполнено:
- Unity source/scene **статическая** preflight — success (не Unity Editor).
- `dotnet run --project Server/Nonation.Tests/Nonation.Tests.csproj --configuration Release` — success; реально напечатаны `PASS` для 64-bit ledger, грузового escrow, перемещения NPC, неприкосновенности игрока, покупки еды, spectator v2 replay/restart, `WorldAuthority.Save/Load` и инвариантов денег.
- `dotnet build Server/Nonation.Server/Nonation.Server.csproj --configuration Release --nologo` — `Build succeeded` с nullable warnings (`CS8618/CS8603` и др.), без ошибок сборки.
- Локальный HTTP smoke на запущенном `Nonation.Server.dll` — `PASS: public spectator API, readonly security, CORS and moving server NPCs`. Истинная публичная HTTPS-среда не проверялась.

Работа ведётся в `agent/backend-simulation`; [PR #1](https://github.com/alexvelikiy88-sketch/nonation/pull/1) остаётся Draft до проверки Unity и браузера. `TASKS.md` и `main` исполнитель не редактировал. Последний подтверждённый **кодовый** SHA — `0a53eb17`; последующие изменения отчёта не меняют серверный код. Менеджер исправил ложный Guard FAIL для устаревшей базы PR: текущий Guard #42 успешно проверил зоны владения.

### Аудит и исправленные дефекты

1. `TotalKnownMoney()` агрегировал общий баланс в `int`; переполнение при корректной сумме счетов устранено переходом на `long`. CI: PASS для суммы выше `Int32.MaxValue`, переводов и защиты от replay.
2. `MirRemoteWorldSync` раньше допускал откат времени и не отличал повторный ответ. Теперь `MirSceneSnapshot` v2 с `sequence`, `MirSceneSnapshotCursor`, лимитом 256 NPC и отбрасыванием invalid/replay. Старый v1 endpoint несовместим: backend и Unity должны обновляться согласованно.
3. `WorldAuthority.Load` допускал явные `null` в списках при использовании `?.Count < 1`; проверки исправлены. CI: PASS по загрузке после рестарта, координатам NPC, денежному инварианту и отклонению испорченных сохранений.
4. `MirEconomy.Work` и производство в `Tick` складывали товары в `int`, что позволяло переполнять stock даже при `capacity=int.MaxValue`; `skill` и `daysWorked` тоже могли перейти в отрицательные значения. `ProductStockOverflowTests` **прошёл в CI #27**: все 6 проверок, включая граничную вместимость, зарплату и сохранение денег.
5. `SetFreightStage()` позволял закрыть `Returning` с `paid=false`, если состояние заказа было несогласованным. Добавлена единая проверка freight-state пред всеми изменениями и тесты: ни двойной платеж, ни скрытая отмена, ни неправильный `productId` не проходят. **CI #43 PASS**.
6. В `WorldAuthority.Save()` две сериализации защищались `gate`, но дисковая запись за блокировкой могла чередоваться и перезаписать новое состояние старым. Введён `saveGate` на время сериализации и финального rename, принудительный flush staging перед rename, очистка `.tmp` при исключении. **CI #43 PASS** по staging-failure, orphan-stage, 24 concurrent Save, восстановлению денег и координат NPC.

7. `WorldAuthority.Load` теперь отказывает при негативных остатках или счетах, дубликатах аккаунтов/транзакций/грузов, несогласованном escrow и более чем одной активной доставке; не повреждает исходный JSON. CI #61/66 PASS. Нормальная поставка после рестарта проходит.
8. `transactionSequence` и `jobSequence` теперь не переполняются при верхней границе Int32; `NewId` безопасно возвращает пустой ID при исчерпании, `Transfer` отклоняет его, `CreateRestock` отклоняет создание до резерва товара. CI #66 PASS.

### Изменённые файлы (полный список PR)

1. `Assets/MIR/Scripts/MirState.cs`
2. `Assets/MIR/Scripts/MirEconomy.cs`
3. `Assets/MIR/Scripts/MirSceneSnapshot.cs`
4. `Assets/MIR/Scripts/MirRemoteWorldSync.cs`
5. `Server/Nonation.Server/WorldAuthority.cs`
6. `Server/Nonation.Tests/Program.cs`
7. `Server/Nonation.Tests/Nonation.Tests.csproj`
8. `Tools/smoke_server.py`
9. `docs/agents/reports/02_BACKEND_SIMULATION.md`

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

**Integration Guard (MANAGER) — закрыто:** старый [Guard #12](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40008195153) ложно учитывал `TASKS.md` из старого PR base. Менеджер перешёл на `merge-base`; [Guard #42](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40010216734) SUCCESS для текущей ветки, посторонних изменений нет.

## Следующий технический блок — 24.10.26

Граничные тесты загрузки сохранения и исчерпания ID прошли в [.NET CI #66](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40010216758). Следующая задача AGENT 02: ограничить риск двух процессов, пишущих один `NONATION_WORLD_FILE` (с проверяемой моделью владения файлом и release при завершении), и дополнительно исследовать ограничение входного размера сохранения до десериализации. Не изменять HTTP API в сторону unauthenticated writes.

**Перед merge:** интегратор проводит Unity Editor компиляцию, реальный WebGL/browser и совместимость DTO spectator v2; другой владелец предоставляет `RemoteGameMinutes` и обновление HUD (CROSS-OWNER REQUEST выше). Серверный .NET success не доказывает Unity runtime.

## Правила заполнения

Писать только выполненные факты. Приложить ссылки на коммиты, PR, CI, реальные Cloud Build номера и выходные артефакты. Не стирать старые строки журнала. При конфликте с `main` — запросить управляющего интегратора. Не изменять `TASKS.md`.
