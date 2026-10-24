# NONATION — центральные задачи, доказательства и приёмка

**Актуализация CHAT 04: 24.10.26**  
**Каноническая ветка:** `main`, защищённая GitHub Ruleset №25749583.  
**Все три направления объединены в `main`:** [PR #19](https://github.com/alexvelikiy88-sketch/nonation/pull/19) MERGED, merge SHA `a75602df4ef56e95709a8472eba62d879795c977`. История и исходные SHA трёх агентов сохранены.  
**Предыдущая полная история:** [архив реестра](Documentation/STATUS_HISTORY_23_10_26.md). Для новых решений использовать этот файл и актуальный GitHub, а не устаревшие строки архива.

## ПЕРВЫЙ ПРИОРИТЕТ ПРОЕКТА — VISUAL GATE / утверждённый изометрический квартал

**Директива владельца 24.10.26:** результат **реальной работающей Unity/WebGL-игры** в следующем вертикальном срезе должен быть **не слабее утверждённого концепт-арта** по общему визуальному качеству, плотности, освещению, анатомии людей, деталям и ощущению живого города. Современный AA-квартал — обязательная цель, а не необязательное «полировать потом». [Полный канон и приёмка](Documentation/VISUAL_CANON_RU.md).

**Эталон:** «Солнечный городской квартал у набережной», 1672×941 px, SHA-256 `aa761600852a53231ef4fee15fda43ced35dfe40f614bed569811307bc7151a8`. Оригинальный PNG утверждён в обсуждении; каноническая спецификация и его контрольный хеш сохранены в MD. Физическое копирование бинарного PNG в GitHub — отдельная **ещё не закрытая** задача, не считать изображение уже загруженным в репозиторий.

Текущая доступная WebGL alpha с процедурами/капсулами считается **техническим прототипом**, даже если Unity/WebGL/HTTPS/управление/сохранения проходят smoke-test. **Публикация alpha не означает закрытия VISUAL GATE.** Не начинать новый Unity-проект и не сломать работающую сборку ради графики.

| ID | Поставляемый результат | Статус | Ответственный |
|---|---|---|---|
| VIS-00 | Утверждённый художественный канон, однозначный SHA исходного PNG, отдельный исходный PNG в GitHub | **MD зафиксирован; бинарный PNG ещё не закреплён** | CHAT 04 |
| VIS-01 | Каноническая 3/4 камера, план первого квартала (площадь, кафе, магазин, перекрёсток, набережная), реальные игровые проходы/входы | **OPEN, игровой результат не проверен** | CHAT 01 + CHAT 03 |
| VIS-02 | Модульные здания, фасады/витрины, дороги/бордюры, вода, деревья, мебель, PBR и свет по одному художественному стилю | **OPEN; текущие кубические здания неприемлемы** | CHAT 03 |
| VIS-03 | Первый реальный лицензируемый skinned Humanoid с валидным Avatar/Animator Idle/Walk/Run, реальными LOD/текстурами и анимацией в WebGL; затем вариативность NPC | **OPEN; Rocketbox исходники ≠ готовая Unity-модель** | CHAT 03 + CHAT 01 |
| VIS-04 | Жители действительно ходят в магазин/кафе, работают, общаются, отдыхают и используют транспорт; активность привязана к существующей экономике | **OPEN; требуется демонстрация действующих POI** | CHAT 02 + CHAT 01 |
| VIS-05 | Игровой HUD, управление и камеры без debug-вида, адаптация UI и читаемость на обзорной сцене | **OPEN** | CHAT 03 + CHAT 01 |
| VIS-06 | WebGL 16:9 скрин **из настоящего браузера**, геймплей-видео, измеренные p50/p95 frame time, FPS, draw calls, triangles, memory на названной машине | **OPEN; метрики не выдавать за измеренные** | CHAT 01 + CHAT 03 |
| VIS-07 | Художественная приёмка квартала: сравнительные кадры с одинаковой камеры, рубрика канона (ориентир ≥85/100), отсутствие плейсхолдеров и **явное одобрение владельца** | **BLOCKING / НЕ ПРОЙДЕНО** | CHAT 04 + владелец |

### Порядок выполнения

1. Сохранить текущую рабочую `main`/GitHub Pages сборку как baseline и регрессионный Chromium smoke.
2. Проработать **первый квартал целиком**: layout/камера → реальные здания/улицы/растения → один Humanoid + анимации → NPC-POI/транспорт → материалы/освещение/UI.
3. Для каждого шага небольшие PR в зонах ответственности, лицензии с исходниками, WebGL-скриншот с совпадающей камерой и замеры. Нет WebGL-доказательства — не отмечать DONE.
4. Перед расширением города пройти **VISUAL GATE** с владельцем; запрещено назвать AA-качество достигнутым по красивому offline-рендеру либо только на основании кода.

**Приоритеты на данный момент:** `P0-VISUAL` (новый качественный квартал), `P0-RELEASE` (не ломать уже достигнутый WebGL baseline), `P1` (живые функции квартала), `P2` (масштабирование и LOD), `P3` (MMO). Подробная архитектура — [VISUAL_CANON_RU.md](Documentation/VISUAL_CANON_RU.md).

## P0 — Pages опубликован, WebGL runtime нуждается в исправленной сборке

- **Все три агента объединены** в защищённую `main` через PR #19, merge SHA `a75602df4ef56e95709a8472eba62d879795c977`. Проверки source/.NET и совместной интеграции на новом `main` завершились успешно: [CI #40020804529](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40020804529).
- **GitHub Pages включён и публикация прошла:** [Actions #40019259462, attempt 3](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40019259462) завершился **SUCCESS** с `Reported success!` и URL `https://alexvelikiy88-sketch.github.io/nonation/`. Развёрнут старый SHA `47a6424ea7b8a55db82db27b3133a98c3160e139`; это **не** текущая интегрированная версия.
- **Настройка Pages закрыта:** пользователь выставил Source=`GitHub Actions`, повторно запущен только failed deployment job без Cloud rebuild; `actions/configure-pages` и `actions/deploy-pages` оба SUCCESS. Повторной ручной настройки не требуется.
- **Точный игровой дефект:** [реальный Chromium artifact smoke #40022712096](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40022712096) на агентском Cloud #4 архиве завершился **FAILURE**: Unity вывел `InvalidOperationException: Missing runtime shader`, не сообщил `NONATION_RUNTIME_READY`, хотя `NONATION_SAVE_WRITTEN citizens=40` работал. [PR #22](https://github.com/alexvelikiy88-sketch/nonation/pull/22), merge SHA `4366103dd439cfda4b65223608f84688084ba998`, добавил Shader asset под Resources и безопасный fallback при Shader stripping; статический CI PASS. **Исправленная Unity-компиляция и Chromium runtime на этом новом SHA ещё не подтверждены**. Новый [MIR Unity WebGL #40023751528](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40023751528) — отдельный билд/деплой.
- Встроенная Chromium среда этого разговора блокирует навигацию и localhost политикой `ERR_BLOCKED_BY_ADMINISTRATOR` даже для `file://`; это **не** ошибка игры. Поэтому официальный Pages browser-smoke является решающим доказательством runtime.

## P0 — подтверждённая ошибка Unity-компиляции после объединения, исправлена в Git

- [Unity Cloud Build №6 / Actions #40020804542](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40020804542) **FAIL** для объединённого игрового SHA `a75602df4ef56e95709a8472eba62d879795c977`: `Assets/MIR/Editor/MirIntegrityAudit.cs(39,24): error CS0266: Cannot implicitly convert type 'long' to 'int'`. Изменение Backend расширило `TotalKnownMoney()` до `long`, а Editor-аудит остался `int`.
- Исправлен тип переменной аудита на `long`, добавлен source regression test, [PR #24](https://github.com/alexvelikiy88-sketch/nonation/pull/24) **MERGED**, SHA `55051008dee22e3a44d867a6191e7ab3ba6e83b4`. Обязательный Integration Guard, Python/.NET/HTTP и Cross-Branch CI на PR **SUCCESS**. Это не доказательство компиляции Unity.
- Новый Unity Cloud WebGL run [#40024399013](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40024399013) запущен на исправленном `main@55051008`; при фиксации этого статуса исход Unity Player/export/browser ещё не подтверждён. Не повторять сборки без анализа ошибок.
- Публикация старого WebGL на GitHub Pages уже **SUCCESS** (run #40019259462 attempt 3), но реальный Chromium smoke ранней версии **FAIL** (`Missing runtime shader`). [Shader fix #22](https://github.com/alexvelikiy88-sketch/nonation/pull/22) также уже в `main`; нужны совместный Unity export и полноценный Chromium proof после обоих исправлений.

## Текущее состояние

**Unity Cloud Build №4 — реальный успешный WebGL export** на `agent/unity-webgl@cb46b4960dd8bcd255880f569254dfe9876ea14b`. [GitHub Actions #40015140424](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40015140424) подтвердил статус success, фактический SHA и архив `nonation-agent01-preview` с WebGL файлами. **Игра на публичном HTTPS пока не подтверждена.** Это не Unity-компиляция уже объединённых Backend + Graphics.

| ID | Критерий | Статус / проверяемое evidence | Владелец |
|---|---|---|---|
| G0 | Код трёх направлений интегрируется, .NET-тесты и HTTP проходят | **PASS** на первоначальном интеграционном SHA `52d3bd8a`: [Guard](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40019467544), [Python/.NET/HTTP](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40019467542), [Cross-Branch](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40019467534). Поздние SHA проверять отдельно | CHAT 04 |
| G1-A | Настоящая сборка Unity WebGL хотя бы для одной ветки | **PASS** Unity-only Cloud #4 / `cb46b496`; source/runtime арт сохранён | CHAT 01 |
| G1-B | WebGL export **полностью интегрированного** HEAD | **ОЖИДАЕТ РЕАЛЬНОЙ UNITY СБОРКИ**, source/.NET PASS недостаточно | CHAT 04 + 01 |
| G1-C | HTTPS Pages + браузерная сцена/NPC/WASD/save/reload | **ЧАСТИЧНО:** Pages SUCCESS на старом SHA; настоящий Chromium smoke FAIL на Shader, фикс #22 в main, повторный browser proof ожидается | CHAT 04 |
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
