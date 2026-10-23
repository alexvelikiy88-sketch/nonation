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
| **01 — Unity / WebGL / CI** | [agent/unity-webgl](https://github.com/alexvelikiy88-sketch/nonation/tree/agent/unity-webgl) | **Ожидает запуска чата** | Диагностика исходной ошибки Cloud Build №3; воспроизводимый WebGL export и публичная проверенная ссылка | [Отчёт 01](https://github.com/alexvelikiy88-sketch/nonation/blob/agent/unity-webgl/docs/agents/reports/01_UNITY_WEBGL.md) |
| **02 — Backend / симуляция / сеть** | [agent/backend-simulation](https://github.com/alexvelikiy88-sketch/nonation/tree/agent/backend-simulation) | **Ожидает запуска чата** | Стабилизировать доменные инварианты и read-only NPC snapshots; проверяемый контракт и тесты синхронизации | [Отчёт 02](https://github.com/alexvelikiy88-sketch/nonation/blob/agent/backend-simulation/docs/agents/reports/02_BACKEND_SIMULATION.md) |
| **03 — Графика / персонажи / UI** | [agent/graphics-ui](https://github.com/alexvelikiy88-sketch/nonation/tree/agent/graphics-ui) | **Ожидает запуска чата** | Технически реальный, лицензируемый Humanoid/Animator/LOD pipeline; убрать главный визуальный барьер без деградации WebGL | [Отчёт 03](https://github.com/alexvelikiy88-sketch/nonation/blob/agent/graphics-ui/docs/agents/reports/03_GRAPHICS_UI.md) |
| **MANAGER — контроль / интеграция** | `main` | **Система заведена** | Проверять доказательства, PR и CI; объединять без конфликтов и поддерживать эту сводку | [Руководитель](NONATION_MANAGER.md) |

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

**Правило:** Manager после каждого принятого PR добавляет дату, PR, проверенный SHA, результат CI и изменённые контрольные точки. Никогда не ставить «готово» по словам исполнителя без проверок.
