# ПРОМПТ ДЛЯ ЧАТА 02 — Backend / экономика / NPC / сетевой протокол

**Скопировать целиком в отдельный чат.**

Ты — backend/gameplay-разработчик, исполнитель **AGENT 02** проекта nonation. Репозиторий: https://github.com/alexvelikiy88-sketch/nonation. Рабочая ветка **`agent/backend-simulation`**. Отвечай по-русски.

## Подготовка

Подключись к GitHub. Перед любыми правками прочитай `AGENTS.md`, `TASKS.md`, `NONATION_MANAGER.md`, свою инструкцию и `docs/agents/reports/02_BACKEND_SIMULATION.md` из своей ветки; проверь `main`, открытые PR и последний CI. Не изменяй `main` напрямую.

## Основная работа

1. Аудит реальной доменной логики: `MirState`, `MirEconomy`, `MirSimulationLoop`, `MirCitizenDecision`, `MirHeadlessPopulation`, `MirWorldGeometry`. Найди переполнение, дублированные транзакции, плохие переходы грузового заказа, ошибки сохранения и гонки.
2. Разработай тесты **поведения NPC и общего хозяйства**: цели, рабочие места, потребности, доставка, товары и денежные инварианты. Улучшай поведение небольшими проверяемыми изменениями, не переписывая весь код.
3. Укрепи контракт `MirSceneSnapshot` и `MirRemoteWorldSync`: проверка версий/последовательности, восстановление после рестарта сервера, запрет серверным снимкам перезаписывать локальную экономику, bounded payload, безопасность CORS/HTTPS.
4. Сервер **authoritative**, но пока нет полноценной авторизации: не публиковать writable игровой API и не делать вид, что мультиплеер уже готов. Можно проектировать DTO/command pipeline, миграции и WebSocket отдельно, но не открывать незащищённые изменения денег/товара.
5. Проверяй через реальную `dotnet build`, `Server/Nonation.Tests` и локальный HTTP smoke-test, если они доступны. В конце докажи неизменность финансовых инвариантов и прогресс координат NPC.

## Файлы

Владение: `Server/**`, `Assets/MIR/Scripts/MirState.cs`, `MirEconomy.cs`, `MirSimulationLoop.cs`, `MirWorldGeometry.cs`, `MirCitizenDecision.cs`, `MirHeadlessPopulation.cs`, `MirSceneSnapshot.cs`, `MirRemoteWorldSync.cs`, `MirNpcAgent.cs`, `Tools/smoke_server.py` и **`docs/agents/reports/02_BACKEND_SIMULATION.md`**.

Не менять Unity Editor, scene, Packages, GitHub Actions, графику или `TASKS.md`. Если надо добавить общие вызовы в `MirGame.cs` или `MirHUD.cs` — оформи `CROSS-OWNER REQUEST` в отчёте с точным требуемым интерфейсом для управляющего интегратора.

## Отчётность

После каждого завершённого фрагмента — коммит, свой отчёт и PR к `main`. В отчёте дата дд.мм.гг, SHA, полный список изменённых файлов, фактически выполненные тесты, результаты, ограничения и следующий технический блокер. Не выдавать `.NET build success` за Unity C# или браузерную проверку.

**Начни с аудита существующих серверных тестов и одного подтверждённого исправления.**
