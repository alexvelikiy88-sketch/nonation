# NONATION — единый реестр задач и статусов

**Последняя ручная актуализация руководителем:** 23.10.26  
**Каноническая ветка:** `main`  
**Репозиторий:** https://github.com/alexvelikiy88-sketch/nonation  
**Правила:** [AGENTS.md](AGENTS.md) · **Руководитель:** [NONATION_MANAGER.md](NONATION_MANAGER.md)

> Важно: это сводка **проверенных фактов**. Исполнители обновляют личные отчёты в своих ветках и PR; руководитель сверяет их и обновляет этот файл. Прямое редактирование `TASKS.md` из трёх параллельных чатов запрещено во избежание конфликтов. Для самой свежей незавершённой работы см. отчёты **в ветках** и открытые [Pull Requests](https://github.com/alexvelikiy88-sketch/nonation/pulls).

## Последняя подтверждённая техническая база

- GitHub `main` до создания системы делегирования: `f909e47ce764911281eee2b97f9b6568a83779d6`.
- [GitHub Actions №54](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40001657334): `source-validation = success`; проверены Python preflight, .NET-компиляция, тесты экономики и backend API. `webgl`, `cloud-webgl`, `deploy` = **skipped**.
- Unity Cloud Build №3 = **FAILED**, последняя видимая ошибка `Unity Player Export Failure`, но первопричина по полному логу **не определена**.
- Настоящий Unity WebGL build, загрузка в браузере и публичная рабочая ссылка — **не подтверждены**.
- В репозитории уже есть Unity-сцена, базовая экономика, 39 NPC, минимальный .NET-сервер, read-only spectator API и сетевой transport. Это **прототип**, а не завершённый MMO/городской симулятор.

## Делегирование: три исполнительных чата + руководитель

| Направление | Ветка | Статус отдельного чата | Первый результат (P0) | Живой отчёт в ветке |
|---|---|---|---|---|
| **01 — Unity / WebGL / CI** | [agent/unity-webgl](https://github.com/alexvelikiy88-sketch/nonation/tree/agent/unity-webgl) | **4 коммита, PR #3 Draft; WebGL не собран** | Диагностика исходной ошибки Cloud Build №3; воспроизводимый WebGL export и публичная проверенная ссылка | [Отчёт 01](https://github.com/alexvelikiy88-sketch/nonation/blob/agent/unity-webgl/docs/agents/reports/01_UNITY_WEBGL.md) |
| **02 — Backend / симуляция / сеть** | [agent/backend-simulation](https://github.com/alexvelikiy88-sketch/nonation/tree/agent/backend-simulation) | **Первый фикс опубликован; PR #1 Draft, нет CI на head** | Стабилизировать доменные инварианты и read-only NPC snapshots; проверяемый контракт и тесты синхронизации | [Отчёт 02](https://github.com/alexvelikiy88-sketch/nonation/blob/agent/backend-simulation/docs/agents/reports/02_BACKEND_SIMULATION.md) |
| **03 — Графика / персонажи / UI** | [agent/graphics-ui](https://github.com/alexvelikiy88-sketch/nonation/tree/agent/graphics-ui) | **2 коммита, PR #2 Draft; Unity не проверен** | Технически реальный, лицензируемый Humanoid/Animator/LOD pipeline; убрать главный визуальный барьер без деградации WebGL | [Отчёт 03](https://github.com/alexvelikiy88-sketch/nonation/blob/agent/graphics-ui/docs/agents/reports/03_GRAPHICS_UI.md) |
| **MANAGER — контроль / интеграция** | `main` | **Система заведена** | Проверять доказательства, PR и CI; объединять без конфликтов и поддерживать эту сводку | [Руководитель](NONATION_MANAGER.md) |

**Повторная проверка 23.10.26 (CHAT 04):** `main=e3f91d34a7176cf453710ef1548c904ec7202be8`; `agent/unity-webgl=0541a84ec06d2e7e9736b21521dedd229d4e3b92` (4 коммита на момент проверки); `agent/backend-simulation=7b0d965c261a744ca521cf27741091200c722df0` (2 коммита); `agent/graphics-ui=9bcb9b66dc4fd4e7e528f21e56fd84a57d3777b0` (2 коммита). В Backend появился исправляющий коммит `99d6a2471ae556cbc0bbd360ef52bd3ea669e0e1`: `TotalKnownMoney()` теперь суммирует деньги в `long`; добавлен `WideLedgerTests`. Проверен GitHub diff, но запуск новых тестов НЕ подтверждён. [PR #1](https://github.com/alexvelikiy88-sketch/nonation/pull/1) открыт как Draft и получил ревью руководителя; merge заблокирован отсутствием проверок на SHA PR. Исторический [CI №54](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40001657334) относится к `f909e47` и не подтверждает новый код. Управляющий создал и после успешного CI влил [PR #4](https://github.com/alexvelikiy88-sketch/nonation/pull/4), merge SHA `9b02e406d4fc87acbcd0aff3811635fd41d5bb85`: отдельная `pull_request` проверка Python preflight, .NET 8 и HTTP smoke. [PR #3](https://github.com/alexvelikiy88-sketch/nonation/pull/3) Unity (4 коммита) включил PR CI и усилил WebGL artifact validation; последний source-validation на `0541a84` SUCCESS, WebGL/Cloud/Deploy SKIPPED; draft не принят. [PR #2](https://github.com/alexvelikiy88-sketch/nonation/pull/2) Graphics (2 коммита) добавил защиту против статических Humanoid prefabs; draft не принят до проверки Unity. Новая графика не опубликована.

**Текущие промпты:** [01 UNITY](docs/agents/CHAT_01_UNITY_WEBGL.md) · [02 BACKEND](docs/agents/CHAT_02_BACKEND_SIMULATION.md) · [03 GRAPHICS](docs/agents/CHAT_03_GRAPHICS_UI.md).

## Приоритетные контрольные точки

| ID | Критерий завершения | Состояние | Владелец |
|---|---|---|---|
| G0 | Исходники и .NET проходят CI без ошибок | **Подтверждено на SHA f909e47** | Manager |
| G1 | Unity Cloud или GameCI возвращает реальную WebGL-сборку; страница запускается с публичного HTTPS-URL; проверены загрузка, сцена, управление и Console | **Не подтверждено** | 01 |
| G2 | Offline gameplay: движение, NPC, работа, товары, грузовик, сохранение и перезапуск проверены в браузере | **Не подтверждено** | 01 + 02 |
| G3 | Серверные жители отображаются в Unity на живых снапшотах с корректным CORS/HTTPS; без двойных локальных транзакций | **Код подготовлен, не проверен в Unity** | 02 + 01 |
| G4 | Визуал: не заглушки, корректный риг/скин, анимации, разнообразие людей, LOD, производительность WebGL | **Не подтверждено** | 03 |
| G5 | Сервер-авторитетные игровые действия/авторизация, общий мир 24/7 и долговременное хранение с миграциями | **Не реализовано** | 02, затем Manager |

## Текущие зависимости и риски

- **Build №3:** требуется первичная ошибка из предыдущих строк полного Unity Cloud лога; финальное сообщение не объясняет причину.
- **Unity Cloud CI:** подготовлены `UNITY_SA_KEY_ID` и `UNITY_SA_SECRET_KEY`, но их наличие в GitHub secrets не подтверждено. Секреты нельзя отправлять в чат и коммитить.
- **Графика:** оригинальные пользовательские архивы в публичном GitHub отсутствуют; нельзя считать, что реалистичные FBX уже доступны.
- **Параллельная работа:** общие файлы и `TASKS.md` принадлежат Manager; все пересечения фиксируются в личных отчётах.
- **Развёртывание backend:** сервер в GitHub CI тестируется локально, но не опубликован как production backend.

## Журнал управленческих изменений

| Дата | Решение / факт | Ссылка |
|---|---|---|
| 23.10.26 | Создана система координации на 3 исполнительных чата. Это **не означает**, что отдельные чаты уже запущены. | `NONATION_MANAGER.md` |
| 23.10.26 | Зафиксирована исходная база `f909e47` и CI №54. | [CI №54](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40001657334) |
| 23.10.26 | Первое фактическое изменение CHAT 02: `TotalKnownMoney()` => `long` и тест WideLedgerTests (`99d6a247`); PR #1 проверен статически, не принят без CI на новом SHA. | [PR #1](https://github.com/alexvelikiy88-sketch/nonation/pull/1) |
| 23.10.26 | PR #4: независимый pull_request CI прошёл тесты .NET 8, Python и HTTP smoke; принят merge SHA `9b02e406d4fc87acbcd0aff3811635fd41d5bb85`. Не Unity build. | [PR #4](https://github.com/alexvelikiy88-sketch/nonation/pull/4) |
| 23.10.26 | Проверены обновления исполнителей: Unity PR #3 / SHA `0541a84` (source-validation success, WebGL jobs skipped); Graphics PR #2 / SHA `9bcb9b6` (только исходники, Unity не проверен). Обе PR Draft. | [PR #3](https://github.com/alexvelikiy88-sketch/nonation/pull/3) · [PR #2](https://github.com/alexvelikiy88-sketch/nonation/pull/2) |
| 23.10.26 | Прочитаны отчёты всех трёх веток, сравнения main...agent, PR и Actions; WebGL по-прежнему не подтверждён. | [Ветки](https://github.com/alexvelikiy88-sketch/nonation/branches) · [Actions](https://github.com/alexvelikiy88-sketch/nonation/actions) |

**Правило:** Manager после каждого принятого PR добавляет дату, PR, проверенный SHA, результат CI и изменённые контрольные точки. Никогда не ставить «готово» по словам исполнителя без проверок.
