# NONATION — единый реестр задач и статусов

**Последняя ручная актуализация руководителем:** 24.10.26  
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
| **01 — Unity / WebGL / CI** | [agent/unity-webgl](https://github.com/alexvelikiy88-sketch/nonation/tree/agent/unity-webgl) | **PR #3 Draft; статический и .NET CI успешен, Unity WebGL не собран** | Диагностика исходной ошибки Cloud Build №3; воспроизводимый WebGL export и публичная проверенная ссылка | [Отчёт 01](https://github.com/alexvelikiy88-sketch/nonation/blob/agent/unity-webgl/docs/agents/reports/01_UNITY_WEBGL.md) |
| **02 — Backend / симуляция / сеть** | [agent/backend-simulation](https://github.com/alexvelikiy88-sketch/nonation/tree/agent/backend-simulation) | **PR #1 Draft; доменные/.NET/HTTP тесты успешно пройдены, Unity runtime не проверен** | Стабилизировать доменные инварианты и read-only NPC snapshots; проверяемый контракт и тесты синхронизации | [Отчёт 02](https://github.com/alexvelikiy88-sketch/nonation/blob/agent/backend-simulation/docs/agents/reports/02_BACKEND_SIMULATION.md) |
| **03 — Графика / персонажи / UI** | [agent/graphics-ui](https://github.com/alexvelikiy88-sketch/nonation/tree/agent/graphics-ui) | **PR #2 Draft; есть реальный MIT Rocketbox FBX, импорт/Animator/WebGL не проверены** | Технически реальный, лицензируемый Humanoid/Animator/LOD pipeline; убрать главный визуальный барьер без деградации WebGL | [Отчёт 03](https://github.com/alexvelikiy88-sketch/nonation/blob/agent/graphics-ui/docs/agents/reports/03_GRAPHICS_UI.md) |
| **MANAGER — контроль / интеграция** | `main` | **Integration Guard, Cross-Branch CI и реестр активны** | Проверять доказательства, PR и CI; объединять без конфликтов и поддерживать эту сводку | [Руководитель](NONATION_MANAGER.md) |

**Повторная проверка 23.10.26 (CHAT 04):** `main=e3f91d34a7176cf453710ef1548c904ec7202be8`; `agent/unity-webgl=0541a84ec06d2e7e9736b21521dedd229d4e3b92` (4 коммита на момент проверки); `agent/backend-simulation=7b0d965c261a744ca521cf27741091200c722df0` (2 коммита); `agent/graphics-ui=9bcb9b66dc4fd4e7e528f21e56fd84a57d3777b0` (2 коммита). В Backend появился исправляющий коммит `99d6a2471ae556cbc0bbd360ef52bd3ea669e0e1`: `TotalKnownMoney()` теперь суммирует деньги в `long`; добавлен `WideLedgerTests`. Проверен GitHub diff, но запуск новых тестов НЕ подтверждён. [PR #1](https://github.com/alexvelikiy88-sketch/nonation/pull/1) открыт как Draft и получил ревью руководителя; merge заблокирован отсутствием проверок на SHA PR. Исторический [CI №54](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40001657334) относится к `f909e47` и не подтверждает новый код. Управляющий создал и после успешного CI влил [PR #4](https://github.com/alexvelikiy88-sketch/nonation/pull/4), merge SHA `9b02e406d4fc87acbcd0aff3811635fd41d5bb85`: отдельная `pull_request` проверка Python preflight, .NET 8 и HTTP smoke. [PR #3](https://github.com/alexvelikiy88-sketch/nonation/pull/3) Unity (4 коммита) включил PR CI и усилил WebGL artifact validation; последний source-validation на `0541a84` SUCCESS, WebGL/Cloud/Deploy SKIPPED; draft не принят. [PR #2](https://github.com/alexvelikiy88-sketch/nonation/pull/2) Graphics (2 коммита) добавил защиту против статических Humanoid prefabs; draft не принят до проверки Unity. Новая графика не опубликована.

**Текущие промпты:** [01 UNITY](docs/agents/CHAT_01_UNITY_WEBGL.md) · [02 BACKEND](docs/agents/CHAT_02_BACKEND_SIMULATION.md) · [03 GRAPHICS](docs/agents/CHAT_03_GRAPHICS_UI.md).

## Новый независимый контур CHAT 04 — Integration Guard

- [PR #5](https://github.com/alexvelikiy88-sketch/nonation/pull/5) принят: Python `Tools/pr_integration_guard.py` и 14 unit-тестов `Tools/tests/test_pr_integration_guard.py`. SHA merge `bfd363e6efee01e14f6ac6e5985f8ff8c5790d20`. [CI #40006821605](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40006821605) **SUCCESS**: новые 14/14 тестов, Python, .NET, HTTP smoke.
- [PR #6](https://github.com/alexvelikiy88-sketch/nonation/pull/6) принят: `.github/workflows/integration-guard.yml` запускает guard из **актуального доверенного `main`**; diff берётся от `git merge-base` (устаревший PR event base SHA не используется для классификации чужих изменений) при каждом PR в `main`, включая documentation-only. SHA merge `b902057a5dea510f470b024a0573f5961aed4e40`. [Integration Guard #40007063307](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40007063307) **SUCCESS**; [PR-validation #40007063505](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40007063505) **SUCCESS**.
- Проверки: зоны владения файлами из `AGENTS.md`, чужие отчёты/TASKS, критические удаления и переносы, признаки секретов, подтверждение прав на добавляемые бинарные ассеты, исчезновение публичных C# API (warning). Это статический контроль изменений, **не** Unity/WebGL build и не полная проверка лицензий.
- [PR #7](https://github.com/alexvelikiy88-sketch/nonation/pull/7) — закреплены правила в `AGENTS.md` и отчёт по #5/#6; документационный PR прошёл [Integration Guard #40007504626](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40007504626), merge SHA `c0fc9dd3cf7be48b6f5d2044f00e156b4fabc860`.
- [PR #8](https://github.com/alexvelikiy88-sketch/nonation/pull/8) — исправлен запуск валидатора на старых PR со stale `pull_request.base.sha` (из доверенного текущего `main`); оба CI SUCCESS ([guard #40007687776](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40007687776), [PR validation #40007687791](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40007687791)); merge `e2cfca4e394cc3969be597ba1ef02a46d088cbfb`.
- [PR #9](https://github.com/alexvelikiy88-sketch/nonation/pull/9) — устранено ложное обвинение агентов в правке `TASKS.md` после синхронизации с `main`: сравнение `git merge-base main HEAD_PR`, а не исторического base SHA; добавлен 15-й тест с настоящим Git merge. [guard #40008374732](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40008374732) и [PR validation #40008374698](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40008374698) SUCCESS. Merge `bfd331cd61ea49ee7f05e25d9b2415ce44c42f08`.
- **QA наблюдение:** ранние красные Guard-checks CHAT 01/02 с `TASKS.md` были ложными (выявлена причина, исправлена в #8/#9); review-разъяснения оставлены в PR #1 и PR #3. Graphics #2 добавил Rocketbox FBX/PNG; Guard проверяет наличие явного evidence в PR, но реальная Unity компиляция, импорт, PBR/LOD и юридическая полнота материалов остаются непроверенными.
- **Защита `main` подтверждена 24.10.26:** Ruleset `NONATION — Main Protection` (ID `25749583`), `enforcement=active`, `main protected=true`, required `Ownership and change integrity`, PR и review thread resolution, запреты branch deletion и force-push. `.NET Source preflight` не Required из-за paths-фильтра. Сохраняется обязанность менеджера проверять тесты и лицензии до merge.
- Три агентских PR остаются на самостоятельном ревью по фактам GitHub; успешные source-validation не доказывают браузерный запуск. Публичная WebGL-игра не подтверждена.
## Независимый Unity ↔ Backend spectator contract gate

- [PR #11](https://github.com/alexvelikiy88-sketch/nonation/pull/11) принят (merge SHA `053715e1bfd41835034d74d437104261b40c3bea`): `Tools/tests/test_client_server_contracts.py`, **5 новых статических** контрактных тестов, без изменения игровых файлов агентов.
- [CI #40009026193](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40009026193) **SUCCESS**: 30 Python tests, .NET 8 build/tests и реальный HTTP smoke; [Integration Guard #40009026209](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40009026209) **SUCCESS**.
- Проверяет единый источник публичного DTO в Unity/.NET, совместимость GET `/api/v1/spectator/scene`, field serialization, CORS, browser HTTPS/offline по умолчанию, read-only API и отсутствие денежных полей в spectator DTO.
- Это только source-contract validation + HTTP smoke .NET. **Unity Editor / IL2CPP / WebGL runtime не проверены.**
## Интеграционная сверка CHAT 04 — 24.10.26

- **[PR #13](https://github.com/alexvelikiy88-sketch/nonation/pull/13) принят** в `main`, merge SHA `9e76840a4432821ce3cfaaffb41afea44cf8d184`. Добавлен `.github/workflows/cross-branch-integration.yml`: три локальных merge веток на GitHub runner без push/изменения рабочих веток. Запуск при PR и изменениях кода в `main`.
- **[CI #40012146794](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40012146794) SUCCESS:** объединены `agent/unity-webgl=71e8d9b8be8dcc8a084846fe27cbc4fb7869595c`, `agent/backend-simulation=89050fc6d098577e28fcf55de729274067749f83`, `agent/graphics-ui=b20c4234111092615f62f567ee4d4322853bca0d`. Конфликтов нет; Unity source preflight — 29 C# файлов, 0 ошибок; 48 Python tests и 6 Rocketbox synthetic tests — PASS; .NET domain/server build и HTTP/CORS/NPC/persistence smoke — PASS.
- **[CI #40012357312](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40012357312) SUCCESS:** новый workflow выполнялся также после merge в `main`. Сведения о SHA каждого агента всегда брать из конкретного CI, а не переносить результаты на будущие коммиты.
- **Не проверено:** Unity Editor C#, настоящий IL2CPP/WebGL export, GPU/анимации/FPS, HTTPS runtime и браузерная загрузка игры. Все три исполнительных PR остаются Draft до независимого ревью и подтверждения Unity.
- **Следующие ворота:** CHAT 01 — законный Unity Cloud log/build/browser proof; CHAT 02 — .NET correctness и совместимость spectator v2 c Unity; CHAT 03 — реальные текстуры/Avatar/Animator/LOD и право распространения; CHAT 04 — интеграционные конфликт-тесты, PR review и выпуск.

## Приоритетные контрольные точки

| ID | Критерий завершения | Состояние | Владелец |
|---|---|---|---|
| G0 | Исходники и .NET проходят CI без ошибок | **Source/.NET/HTTP PASS на виртуальном объединении трёх веток — CI #40012146794; не Unity compile** | Manager |
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

| 23.10.26 | Manager создал и влил PR #5 (`bfd363e`): 14/14 Guard тестов и .NET/HTTP CI SUCCESS; PR #6 (`b902057`) включает Guard для всех PR, два workflow SUCCESS. Остаётся блокер Branch Rules (main unprotected). | [PR #5](https://github.com/alexvelikiy88-sketch/nonation/pull/5) · [PR #6](https://github.com/alexvelikiy88-sketch/nonation/pull/6) |

| 23.10.26 | CHAT 04 исправил собственный Guard после независимого анализа CI: PR #7, #8, #9 приняты; 15-й регрессионный тест и оба CI на #9 SUCCESS. Устаревшие event base SHA больше не приписывают чужие файлы агентам. Merge SHA `bfd331cd61ea49ee7f05e25d9b2415ce44c42f08`. | [PR #7](https://github.com/alexvelikiy88-sketch/nonation/pull/7) · [PR #8](https://github.com/alexvelikiy88-sketch/nonation/pull/8) · [PR #9](https://github.com/alexvelikiy88-sketch/nonation/pull/9) |

| 23.10.26 | Независимые интеграционные контракты Unity↔Backend: PR #11 / merge `053715e1`, 5 новых статических тестов, 30 Python tests и .NET/HTTP SUCCESS; Unity WebGL пока не подтверждён. | [PR #11](https://github.com/alexvelikiy88-sketch/nonation/pull/11) · [CI](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40009026193) |

| 24.10.26 | GitHub Ruleset #25749583 активен: main protected, PR+Guard required, no force-push/deletion. | [Ruleset](https://github.com/alexvelikiy88-sketch/nonation/settings/rules/25749583) |
| 24.10.26 | Принят manager PR #13 / SHA `9e76840`: временная интеграция всех трёх веток без конфликтов; 48 Python, 6 графических тестов и .NET/HTTP PASS; Unity WebGL не проверен. | [PR #13](https://github.com/alexvelikiy88-sketch/nonation/pull/13) · [CI](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40012146794) |

**Правило:** Manager после каждого принятого PR добавляет дату, PR, проверенный SHA, результат CI и изменённые контрольные точки. Никогда не ставить «готово» по словам исполнителя без проверок.
