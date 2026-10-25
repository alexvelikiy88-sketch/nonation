# NONATION — план реализации одного качественного 3D-квартала

**Дата:** 25.10.26
**Владелец:** CHAT 04 / Integration Manager
**Источник истины:** актуальные GitHub main, PR, Actions, [TASKS.md](../TASKS.md), [VISUAL_CANON_RU.md](VISUAL_CANON_RU.md).
**Визуальная цель:** утверждённое изображение «Солнечный городской квартал у набережной», 1672 × 941, SHA-256 `aa761600852a53231ef4fee15fda43ced35dfe40f614bed569811307bc7151a8`. Оригинальный бинарный PNG в Git ещё не закреплён.

## 1. Что реально подтверждено на момент фиксации

- **main:** `39b189a00815f7db4ba64c88fae9d801a65e3f7a`. Через PR #37 удалена неактивная URP-зависимость, мешавшая компиляции glTFast Built-in; этот конкретный Unity Player/WebGL ещё **не признан успешным**.
- **P0 Unity run:** [#40194385409](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40194385409) — source-validation **PASS**, Unity Cloud ещё **in progress** при проверке документа. Не запускать вторую сборку параллельно. Состояние документа — снимок, перед следующим действием повторно читать run.
- **Ранее доказанные ошибки:** [#40190193106](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40190193106) — в Chromium ошибка glTFast `Could not determine default MaterialGenerator (render pipeline BuiltIn)`; [#40192802666](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40192802666) — Unity Cloud #11 `CS0246 BuiltInMaterialGenerator`. Устранялись через PR #35 и #37; эффект в WebGL пока не доказан.
- **На этапе source:** 14 Kenney CC0 GLB по закреплённому manifest, 12 слотов зданий, 13 деревьев, 3 графические автомобильные модели и 2 светофора; 39 NPC и 1 player, базовая экономика и сохранения. Количество загруженных в последнем браузерном билде Kenney-моделей **не подтверждено**.
- **Графическая интеграция:** [PR #38](https://github.com/alexvelikiy88-sketch/nonation/pull/38) в Draft — отдельные геометрические меши набережной поверх существующего мира без переписывания бизнес-логики; source/ownership CI PASS, Unity WebGL не проверен. [PR #26](https://github.com/alexvelikiy88-sketch/nonation/pull/26) — Draft, есть конфликт `MirCity.cs`; его нельзя объединять целиком.

## 2. Порядок производства и технические gate

| Этап | Следующая конкретная работа | Проходной результат | Владелец |
|---|---|---|---|
| **P0-A. Unity compiler** | Исследовать финал #40194385409; при отказе получить первичный C# / Shader / Export error и исправить отдельным PR | Настоящий Unity 6000.0.58f2 WebGL export на точном release SHA; архив index/js/wasm/data | 01 + 04 |
| **P0-B. Kenney runtime** | Проверить Pages и Chromium именно для этого SHA; glTF материалы, Collider, Shader, CORS, размеры и реальные Renderer | `NONATION_RUNTIME_READY`; `NONATION_KENNEY_CITY_READY buildings=12 trees=13 vehicles=3 props=2` на старте и после reload; без fatal Console ошибок | 01 + 03 + 04 |
| **P0-C. Gameplay regression** | Убедиться, что после 3D-декора работают ввод, ходьба, NPC, транспорт, работа/покупки и offline save/load | Не только W + reload: отдельно зафиксированы успешные POI/магазин/работа/машина, проверены коллизии | 01 + 02 |
| **P1-A. Waterfront** | Разобрать [PR #38](https://github.com/alexvelikiy88-sketch/nonation/pull/38); проверить совместимость набережной и cafe-deck с Kenney, затем ограниченный merge | Подтверждённая WebGL 3D-набережная с доступным променадом, без дублей и поломок маршрутов | 03 + 01 + 04 |
| **P1-B. Квартал** | После успешной P0 провести отдельные совместимые PR для площади/улиц, затем фасадов; без двух параллельных генераторов одних 12 зданий | Кафе, магазин, площадь, перекрёсток, дома, набережная — цельная реальная 3D-композиция | 03 + 01 + 04 |
| **P2. Первый человек** | Rocketbox/Quaternius: один rigged Humanoid, импорт текстур, Avatar, Animator и Idle/Walk/Run, LOD | Настоящий SkinnedMeshRenderer, валидный Humanoid Avatar, анимации без T-pose, скриншот/маркер в Chromium | 03 + 01 |
| **P3. Жизнь квартала** | Привязать существующие NPC/бизнесы к видимым POI и дверям без дублирования экономики | Работа, покупки, отдых, погрузка и транспорт реально отражаются в симуляции и кадре | 02 + 01 |
| **P4. Визуал/UI/performance** | Совместить камеру, дневные/вечерние материалы, свет, HUD, реальные визуальные ассеты и LOD | Сравнение с эталоном, raw 16:9 screenshots, measured p50/p95, FPS, draw calls, tris, memory; цель 30+ FPS на согласованном desktop | 03 + 01 + 04 |
| **P5. Визуальная приёмка** | Отдельная оценка владельца после технических gate | Нет видимых blockout/capsule fallback; рубрика канона ≥85/100 и **явное одобрение владельца** | 04 + владелец |
| **P6. Общий мир** | Только после качественного vertical slice: публичный authoritative 24/7 сервер, auth и multiplayer | Одна общая постоянная локация; синхронизация без локального дублирования состояния | 02 + 01 + 04 |

**Не запускать новый платный Unity Cloud Build**, пока текущий выполняется или пока source/static/ownership проверки не зелёные. Избегать развёртывания неготового графического PR поверх уже проверенного игрового baseline.

## 3. Матрица разборки конфликтующего Graphics PR #26

| Модуль в agent/graphics-ui | Решение для интеграции | Причина и следующий контроль |
|---|---|---|
| `MirWaterfrontQuarter.cs` | **Изолировать; перенесён в Draft PR #38** | Самостоятельные 3D-меши, вода, набережная, BoxCollider. Проверить реальный viewport, край карты и WebGL. Не импортировать второй раз. |
| `MirQuarterStreetLife.cs` | **Отдельный PR после waterfront** | Велосипеды, кафе, переходы, озеленение. Проверить дубли старых перекрёстков, машин и дерева; право на временный fallback. |
| `MirCivicSquare.cs` | **Адаптировать вместо legacy `MarketSquare()`** | Не рендерить два фонтана/две площади в одних координатах; сохранить игровой проходимый Collider и пути NPC. |
| `MirArchitectureQuarter.cs` | **Не принимать целиком без режима выбора арт-источника** | Кастомные фасады занимают те же 12 Kenney слотов. Нужно строго по одному Rendered source на slot (Kenney / modular), а не перекрывающиеся Mesh и Collider; проверка входов и POI. |
| `ArchitecturalLit.shader` и 8 scan-JPG | **Карантин до asset QA** | Проверить LFS pointers, реальное наличие текстур в WebGL, совместимость Built-in Shader, лицензию ambientCG/CC0 и performance. |
| `MirRocketboxPilotBuild.cs`, `MirHumanFactory.cs` | **Отдельный VIS-03 PR на одного человека** | В текущем PR #26 выявлено, что `Prepare(false)` в prebuild не проверяет bool. Visual-release должен быть fail-closed, technical-alpha fallback допустим явно отдельно. |
| `MirFollowCamera.cs` | **Точечный PR** | Фиксированная 3/4 камера для честного сравнения; сохранить свободную игровую камеру и управление. |
| `MirHUD.cs` | **После стабилизации P1/P2** | Нельзя выдавать косметический HUD за улучшение геометрии. Проверить ввод, масштаб и отсутствие перекрытий. |
| `MirCity.cs` | **Manager-интеграция по функциям, не blind merge** | Сохранить Kenney, POI, экономику, Physics и ветку отката, устранить только конкретный конфликт. |
| `docs/agents/reports/03_GRAPHICS_UI.md` | **Не изменять менеджером** | Личный отчёт AGENT 03; источник проверять по SHA, diff и CI. |

## 4. Обязательные доказательства к каждому игровому PR

1. Точный **branch + commit SHA + parent baseline**, лицензии/исходные URLs/хеши ресурсов, разрешения на cross-owner изменения.
2. **Source / .NET / Ownership PASS**. Unity C# compile и WebGL Export показывать отдельными статусами, не подменять ими Chromium.
3. Скриншот **без обработки из реального Unity WebGL**, доказанные runtime-маркеры новых Mesh/Humanoid и проверка Console. При неудаче сохранять raw failure screenshot и JSON.
4. Действующие NPC/POI/транспорт и сохранение после reload, сравнение до/после с постоянной 3/4 камерой.
5. Для серьёзных визуальных изменений — измеренная производительность на названном компьютере и несовпадения с каноном.
6. `TASKS.md` обновляет только CHAT 04 **после фактического подтверждения**, дата дд.мм.гг.

## 5. Правило остановки и отката

- **Unity compile FAIL:** не объявлять Pages или WebGL; извлечь первичную C# / shader ошибку из UBA и исправить в минимальном PR.
- **WebGL PASS, Kenney error или нулевые счётчики:** не принимать визуальный выпуск; расследовать glTF download/import, материалы и Resources, сохранить работающий legacy fallback.
- **Chromium PASS, художественный результат слабый:** это только техническая alpha; `VISUAL GATE` остаётся OPEN.
- **Graphics/Backend конфликт:** не переписывать другого агента и не обходить защиту `main`; интегрировать через manager PR с `INTEGRATION-EXCEPTION: <path>`.

**Ближайший технический шаг:** проверить итог Unity Cloud [#40194385409](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40194385409). Пока он не дал реальных WebGL/Chromium доказательств, [PR #38](https://github.com/alexvelikiy88-sketch/nonation/pull/38) оставлять Draft.
