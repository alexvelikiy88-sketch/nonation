# NONATION — центральные задачи, доказательства и приёмка

**Актуализация CHAT 04: 24.10.26**  
**Каноническая ветка:** `main`, защищённая GitHub Ruleset №25749583.  
**Все три направления объединены в `main`:** [PR #19](https://github.com/alexvelikiy88-sketch/nonation/pull/19) MERGED, merge SHA `a75602df4ef56e95709a8472eba62d879795c977`. История и исходные SHA трёх агентов сохранены.  
**Предыдущая полная история:** [архив реестра](Documentation/STATUS_HISTORY_23_10_26.md). Для новых решений использовать этот файл и актуальный GitHub, а не устаревшие строки архива.

## P0 — публикация: установлен отдельный блокер GitHub Pages

- **Все три агента объединены** в защищённую `main` через PR #19, merge SHA `a75602df4ef56e95709a8472eba62d879795c977`. Проверки source/.NET и совместной интеграции на новом `main` завершились успешно: [CI #40020804529](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40020804529).
- Старый main SHA `47a6424ea7b8a55db82db27b3133a98c3160e139` отдельно **успешно собран Unity Cloud**, но [Actions #40019259462](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40019259462) провалился на `actions/configure-pages@v5`: `Get Pages site failed — Not Found`. Unity экспорт прошёл; GitHub Pages ещё не включён через Settings.
- **Действие владельца:** [GitHub → Settings → Pages](https://github.com/alexvelikiy88-sketch/nonation/settings/pages) → Build and deployment → Source → **GitHub Actions** → Save. Самостоятельное включение `configure-pages enablement:true` требует повышенного PAT/App токена, стандартный `GITHUB_TOKEN` этого не умеет. Никаких токенов в чат или Git.
- **Совмещённый исходный SHA `a75602df...`** проходит отдельный Cloud WebGL job в [Actions #40020804542](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40020804542). До фактического SUCCESS и Deploy/Chromium нельзя утверждать, что публичная игра работает. Если Pages включена после сборки и deployment упадёт, переисполнить только failed deploy job для точного SHA, без лишнего Cloud build.
- Встроенная Chromium среда этого разговора блокирует навигацию и localhost политикой `ERR_BLOCKED_BY_ADMINISTRATOR` даже для `file://`; это **не** ошибка игры. Поэтому официальный Pages browser-smoke является решающим доказательством runtime.

## Текущее состояние

**Unity Cloud Build №4 — реальный успешный WebGL export** на `agent/unity-webgl@cb46b4960dd8bcd255880f569254dfe9876ea14b`. [GitHub Actions #40015140424](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40015140424) подтвердил статус success, фактический SHA и архив `nonation-agent01-preview` с WebGL файлами. **Игра на публичном HTTPS пока не подтверждена.** Это не Unity-компиляция уже объединённых Backend + Graphics.

| ID | Критерий | Статус / проверяемое evidence | Владелец |
|---|---|---|---|
| G0 | Код трёх направлений интегрируется, .NET-тесты и HTTP проходят | **PASS** на первоначальном интеграционном SHA `52d3bd8a`: [Guard](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40019467544), [Python/.NET/HTTP](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40019467542), [Cross-Branch](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40019467534). Поздние SHA проверять отдельно | CHAT 04 |
| G1-A | Настоящая сборка Unity WebGL хотя бы для одной ветки | **PASS** Unity-only Cloud #4 / `cb46b496`; source/runtime арт сохранён | CHAT 01 |
| G1-B | WebGL export **полностью интегрированного** HEAD | **ОЖИДАЕТ РЕАЛЬНОЙ UNITY СБОРКИ**, source/.NET PASS недостаточно | CHAT 04 + 01 |
| G1-C | HTTPS Pages + браузерная сцена/NPC/WASD/save/reload | **НЕ ПОДТВЕРЖДЕНО**; только после настоящего `browser-smoke` считать ссылку работающей | CHAT 04 |
| G2 | Игровой цикл: движение, NPC, работа, покупки, транспорт, сохранение | Код есть, .NET-алгоритмы проверены; **WebGL игровой прогон не доказан** | CHAT 01 + 02 |
| G3 | Unity spectator v2 ↔ .NET backend | `MirSceneSnapshot` и read-only API согласованы по source/HTTP тестам; внешний HTTPS live NPC не проверен | CHAT 02 |
| G4 | Реалистичный Humanoid, skin/Animator, LOD, WebGL FPS | Rocketbox MIT FBX + 6 TGA и LOD script есть; **Unity import/Animator/LOD/FPS не проверены** | CHAT 03 |
| G5 | MMO-сервер 24/7, auth, state changes, SQL ledger | **Не реализовано**, API только публичное чтение | CHAT 02 + 04 |

## Источники агентских работ и ограничения интеграции

| Агент | Зафиксированный SHA исходников | Работа и качество доказательств | Исходный отчёт |
|---|---|---|---|
| **CHAT 01 — Unity / WebGL** | `cb46b4960dd8bcd255880f569254dfe9876ea14b` | Build #3 root cause на строке 7845: сцен не было; сцена зарегистрирована. Cloud Build #4 success на собственном SHA | [01](docs/agents/reports/01_UNITY_WEBGL.md) |
| **CHAT 02 — Backend / NPC** | `1284979a388aa22ec13cc3ab81d16577a26d4762` | Сохранения, эксклюзивная lease, idempotency, экономика, freight, spectator v2; .NET/HTTP CI успешно | [02](docs/agents/reports/02_BACKEND_SIMULATION.md) |
| **CHAT 03 — Graphics / UI** | `a6b38f6e53f45d92f9480a2899a360d87ab6f778` | Microsoft MIT FBX/6 TGA, LOD script и графические C#; нормальный Humanoid/LOD/Animator в Unity ещё не доказан | [03](docs/agents/reports/03_GRAPHICS_UI.md) |
| **CHAT 04 — Integration** | `52d3bd8a37b5fa507789db6d476e29cd54a6300f` (исходная 4-parent сборка) | Импортировано 39 непересекающихся файлов без удаления и без подмены агентских исходников | [интеграционный отчёт](Documentation/INTEGRATION_REPORT_RU.md) |

**Атрибуция:** исходные отчёты агентов импортированы из их Git blobs. Дополнения `INTEGRATION HANDOFF - CHAT 04 / Manager` написаны руководителем и **не выдаются за слова агентов**. На время интеграции владелец приостанавливает исполнительные чаты; не требуется force-push или чистка их веток.

## Ближайшие действия руководителя

1. Довести PR #19 до финального CI по фактическому HEAD, проверить статическую совместимость/лицензии и запустить именно **общий** Unity WebGL export.
2. На случай новой Unity ошибки: получить полный Cloud build log, исправить точную проблему минимальным патчем в интеграционной ветке и повторять сборку только после анализа.
3. После successful WebGL проверить `index.html`, wasm/data/js и HTTPS Pages; запустить Chromium browser smoke и проверить сохранение/управление. Если WebGL виден, но игра не запускается, не объявлять релиз.
4. Зафиксировать deploy/браузерный proof URL + конкретный SHA + run ID здесь и в `DEPLOYMENT_STATUS_RU.md`.
5. Снова распределить задачи трём агентам от **нового main**, не восстановить старые версии по собственным чатам.

## Не закрытые инженерные задачи

- **P0:** интегрированная WebGL сборка + Pages + WebGL Chromium proof.
- **P1:** разделить клиентские и spectator-часы; проверить поведение Unity↔.NET вне локального CI, ретрансляцию без двойной экономики; защитить исторические сохранения.
- **P1:** проектирование durable transactional ledger при исчерпании 20 000 replay receipts — **не** удалять старые записи.
- **P2:** запустить Blender LOD pipeline, получить реальные Mesh+Avatar/Animator+PBR и FPS на WebGL. Настоящий Rocketbox импорт не должен полагаться на фиктивные капсулы.
- **P3:** авторизация, серверные команды, базы данных, масштабирование общего мира 24/7.

## Безопасность / процедура

`AGENTS.md` определяет владельцев путей; `NONATION_MANAGER.md` — полномочия руководителя. `main` защищён: PR, Required `Ownership and change integrity`, review threads; no force pushes/deletions. Любые ассеты требуют source/license evidence, секреты Unity никогда не размещать в Git. Не приравнивать Python/.NET PASS к успешному Unity WebGL/браузерному запуску.
