# NONATION — центральные задачи, доказательства и приёмка

**Актуализация CHAT 04: 27.10.26**  
**Каноническая ветка:** `main`, защищённая GitHub Ruleset №25749583.  
**Все три направления объединены в `main`:** [PR #19](https://github.com/alexvelikiy88-sketch/nonation/pull/19) MERGED, merge SHA `a75602df4ef56e95709a8472eba62d879795c977`. История и исходные SHA трёх агентов сохранены.  
**Предыдущая полная история:** [архив реестра](Documentation/STATUS_HISTORY_23_10_26.md). Для новых решений использовать этот файл и актуальный GitHub, а не устаревшие строки архива.

## Приоритет VIS / 27.10.26 — компактный, детальный stylized 3D hero-block

**Одобренная художественная планка:** [Behance — 3D illustration for an interactive table](https://www.behance.net/gallery/142989785/3D-illustration-for-an-interactive-table). Это *уточнение доступной стилистики*, а не новое право использовать оригинальные 3D-ассеты/материалы. Главная композиционная тема остаётся «Солнечный городской квартал у набережной» ([VISUAL_CANON_RU.md](Documentation/VISUAL_CANON_RU.md)).

**Подтверждённый игровой baseline** — `main@5bf9f17c2fa2463d019bc0685d5b2e4f2f9411d2`, [GitHub Actions #40521592293](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40521592293): Unity Cloud WebGL **SUCCESS**, GitHub Pages **SUCCESS**, Chromium **SUCCESS**. [Исходный browser proof #12742039803](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40521592293/artifacts/12742039803) содержит необработанные игровые V/B скриншоты, JSON и 1280×720 fullscreen proof. В консоли `NONATION_MODULAR_CITY_READY buildings=12 meshes=17 vertices=86000`, `NONATION_HERO_QUARTER_READY plaza=1 waterfront=1 streets=1`, 39 NPC, 13 Kenney деревьев, 3 машины, 2 светофора. Движение W, сохранение и reload PASS. `NONATION_SHADER_FALLBACK` сохраняется, но не фатален.

**Выявленные действительные пробелы по raw screenshot и JSON:** большая доля кадра — пустая дорожная сетка, малый размер основного района и низкая контрастность окружения. `realHumanPilot: null` на initial **и** reload: первый Rocketbox Avatar/Animator **не доказан**; людей не принимать как готовые skinned Humanoid, капсульные fallback не выдавать за финал. Ни 30 FPS, ни качество лица/одежды не подтверждены.

**Следующий узкий графический PR:** менеджерская ветка `manager/stylized-city-diorama-20261027`: концентрированный hero V-ракурс на площади/магазине/кафе, отдельно приближённый B-ракурс набережной; согласованная зелёно-каменная палитра; две **настоящие 3D-клумбы с цветами**, две материальные группы товара/поддонов у магазина (без нового gameplay collider и ложных NPC); для всех уличных материалов переход с упрощённого vertex Lambert шейдера на Built-in Standard Surface Shader с тенями/metallic/roughness. Chromium должен подтвердить `NONATION_STYLIZED_STREET_READY gardens=2 deliveries=2` при первичной загрузке и после reload. Это **предлагаемые правки**, не подтверждённое Unity/WebGL изображение; **VISUAL GATE остаётся OPEN**.

**Следующий художественный блокер после нового кадра:** источник 3–6 неповторяющихся анимированных skinned людей и полноценная проверка Avatar/LOD/skin в WebGL. Предпочитать разрешённые MIT/CC0 модели (Microsoft Rocketbox и Quaternius CC0), исходники платных Asset Store/Fbx с неизвестными правами не публиковать. Только затем расширять мир; PBR фасады из [PR #46](https://github.com/alexvelikiy88-sketch/nonation/pull/46) включены в текущую рабочую сборку.

**Правило:** меньше игровая hero-зона, но завершённый уровень качества; full 5×5 simulation/grid может оставаться для экономики и QA, однако художественный 3/4 кадр не должен показывать большинство пустых кварталов. Экономику, home/business IDs, native colliders, серверный API, движение и сохранения сохранять. Один исходный PR + один Unity Cloud build + реальные Chromium screenshots, без двух параллельных оплачиваемых сборок.

## P0 / 25.10.26 — Unity Cloud #12 PASS, Kenney Runtime FAIL (подтверждено)

- **Игра и исходный SHA:** `39b189a00815f7db4ba64c88fae9d801a65e3f7a`. На [Unity WebGL run #40194385409](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40194385409) source-validation **SUCCESS**, Unity Cloud Build #12 **SUCCESS** с `lastBuiltRevision` того же игрового SHA, реальный Unity/WebGL архив `12599509414` и GitHub Pages deploy/HTTPS **SUCCESS**.
- **Chromium Runtime FAIL:** в том же run браузер выдал `NONATION_RUNTIME_READY scene=MIR_PlayableDistrict player=player-001 npcs=39 vehicles=4 citizens=40`, затем многократные `NONATION_KENNEY_IMPORT_FAILED: city-kit-commercial/building-e Object reference not set to an instance of an object` для разных зданий/деревьев. `NONATION_KENNEY_CITY_READY` **отсутствует**. Сохранён Chrome proof artifact `12599329915` с raw failure screenshot. Unity/WebGL и Pages PASS **не равны** загрузке 3D моделей.
- **Обоснованная рабочая гипотеза для минимального исправления:** в [glTFast 6.0.0](https://github.com/needle-mirror/com.unity.cloud.gltfast/tree/b05d842446989f0521b1e2283c7e93121d785b29) `BuiltInMaterialGenerator` получает шейдеры через `Shader.Find("glTF/...")` и может возвращать `null`, если Unity WebGL удалил динамически запрашиваемые shader dependencies. В логах игры также фиксируется `NONATION_SHADER_FALLBACK`; последняя ошибка — NRE при `InstantiateMainSceneAsync`, полный стек ранее не выводился. **Это правдоподобная причина, но не доказанная первопричина до следующего Chromium proof.**
- **Коррекция в отдельном manager PR:** prebuild `MirKenneyShaderRetention` создаёт реальные Material references на три оригинальных пакетных shader в `Resources`, отклоняет отсутствующие исходники, добавлена runtime-проверка material/shader и подробный glTF ConsoleLogger + exception stack trace. Не копируются закрытые Unity Asset Store файлы, экономика и POI не изменяются. После Source/.NET/Ownership PASS требуется **одна** Unity Cloud WebGL сборка и доказанный Kenney runtime в Chromium.
- **Бесплатные city-паки:** по прямому предложению владельца проверены карточки Unity Asset Store, список и условия в [UNITY_ASSET_STORE_FREE_CITY_SELECTION_RU.md](Documentation/UNITY_ASSET_STORE_FREE_CITY_SELECTION_RU.md). Кандидаты: Marpa Stylized City (FREE, проба зданий), Paulina Sroka Low poly City Assets (FREE, малые формы), Draftpunk Free Low Poly Simple Urban City (FREE, дальний план); One toon building (FREE) пока не совместим с Built-in, City Voxel отклонён по стилю. Все **не импортированы**, лицензия Standard Asset Store EULA не означает CC0 или право публикации исходных FBX/Prefab в открытом Git.
- [PR #38](https://github.com/alexvelikiy88-sketch/nonation/pull/38) (waterfront) остаётся **DRAFT**; не объединять, пока игровой Kenney GLB не подтверждён. [Graphics PR #26](https://github.com/alexvelikiy88-sketch/nonation/pull/26) имеет открытый конфликт `MirCity.cs`. **VISUAL GATE OPEN.**

## Актуальные следующие работы / 25.10.26 — вертикальный срез, управляемые gate

**План производства:** [Documentation/VERTICAL_SLICE_EXECUTION_PLAN_RU.md](Documentation/VERTICAL_SLICE_EXECUTION_PLAN_RU.md). Эталон и критерии качества остаются в [VISUAL_CANON_RU.md](Documentation/VISUAL_CANON_RU.md).

- **P0 / main `39b189a00815f7db4ba64c88fae9d801a65e3f7a`:** [Unity Cloud run #40194385409](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40194385409) — `source-validation SUCCESS`, Unity Cloud ещё `in_progress` на момент ревизии; нет нового WebGL Player/Pages/Chromium доказательства. Никакие предыдущие успехи нельзя переносить на этот SHA.
- **P1-A / Waterfront:** [PR #38](https://github.com/alexvelikiy88-sketch/nonation/pull/38), head `c4c740e397c1c8268aadd97837f66a805fb85da5`, **DRAFT / НЕ MERGED**. Добавлен самостоятельный 3D-модуль набережной от Graphics AGENT 03 и единственный вызов в `MirCity.Build()`, сохранены Kenney/POI/NPC/Collider. **Integration Guard, source/.NET PASS** на PR head. Реальная компиляция Unity, WebGL сцена, FPS и видимый waterfront **НЕ ПРОВЕРЕНЫ**. Не объединять до подтверждения P0.
- **P1-B / Graphics PR #26:** `agent/graphics-ui@35d73180e22d7487fb65045bab64dd92feb9098d` конфликтует с main в `Assets/MIR/Scripts/MirCity.cs`; cross-branch FAIL. Модули делить на совместимые PR по матрице в плане. Сначала набережная #38, затем улицы/площадь, после этого фасады — строго один источник 12 зданий на слот. Текстуры/shader/LFS проверять до включения.
- **P2 / Humanoid:** один действительный Rocketbox/Quaternius SkinnedMeshRenderer, валидные Avatar/Animator Idle/Walk/Run и LOD в Unity WebGL. Зафиксированный дефект графического prebuild `Prepare(false)` без проверки результата необходимо устранить в изолированном визуальном PR; `VIS-03` не закрывать по наличию FBX.
- **P3–P5:** проверить действующие NPC и экономические POI, затем материалы/камеру/HUD, снимки без монтажа и измеренные FPS/p50/p95/дроуколлы/память. Финальная `VIS-07` требует явной приёмки владельца; в настоящий момент **OPEN**.

**Следующее действие:** узнать финальный исход Unity Cloud #40194385409. Если FAIL — первичный Unity log и минимальный PR; если PASS — Pages + Chromium текущего SHA, настоящие `NONATION_KENNEY_CITY_READY buildings=12 trees=13 vehicles=3 props=2` до/после reload, отсутствие фатальных Collider/Shader/glTF ошибок и raw screenshot. Только после подтверждения можно принимать PR #38. Не запускать дублирующий Cloud Build для обновления документации.

## Последняя проверка нового WebGL-релиза / 25.10.26 — GLB runtime BLOCKED

- **Точный `main` SHA:** `faf1bdbdcfaa1493cf9798b24fa4cc58464a6093`.
- **[Unity/WebGL #40190193106](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40190193106):** `source-validation` **PASS**, Unity Cloud WebGL **PASS**, GitHub Pages + HTTPS **PASS**. Cloud archive artifact `12597753213`; `github-pages` artifact `12598875494`. Это подтверждение сборки/размещения файлов, **не** полноценной сцены с новыми GLB.
- **Chromium runtime FAIL** на том же `main`: `NONATION_RUNTIME_READY scene=MIR_PlayableDistrict player=player-001 npcs=39 vehicles=4 citizens=40`, но `NONATION_KENNEY_CITY_ERROR: Could not determine default MaterialGenerator (render pipeline BuiltIn)`. `NONATION_KENNEY_CITY_READY` отсутствует; 3D Kenney не загружен в игровой кадр. Истинный proof artifact `12598396433` содержит JSON/скриншот ошибки, **не** кадр успешного релиза.
- **Проверенное объяснение:** glTFast в IL2CPP/WebGL не выбрал материалы для фактически активного Built-In pipeline. Установленный пакет URP не означает, что URP выбран как активный pipeline. Старые игровые коллайдеры, перемещение, сохранение и рендер после успешного GLB-импорта данным smoke **не проверены** — браузер упал до следующих стадий.
- **[PR #35](https://github.com/alexvelikiy88-sketch/nonation/pull/35)** предлагает адресное исправление: явный `BuiltInMaterialGenerator` для Built-In, строгая проверка количества GLB на старте и после reload, быстрый fail при `NONATION_KENNEY_*ERROR` и статические/Node регрессии. Это **не закрывает** runtime gate до нового Unity/WebGL/Chromium испытания точного SHA.
- **[Graphics PR #26](https://github.com/alexvelikiy88-sketch/nonation/pull/26)** по-прежнему конфликтует в `Assets/MIR/Scripts/MirCity.cs`; Cross-Branch FAIL независим от ошибки material generator. Не сливать его целиком без разрешения конфликта и WebGL-проверки.

**P0 следующий шаг:** принять исправление PR #35 после обязательного PR Guard и всех относящихся к нему тестов, произвести **один** новый SHA-проверенный Unity Cloud релиз и подтвердить реальную GLB-сцену в Chromium. `VIS-02/VIS-03/VIS-06/VIS-07` остаются OPEN.

## P0 / 25.10.26 — Unity Cloud #11 compiler blocker (проверено)

- [Unity WebGL run #40192802666](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40192802666), Unity Cloud Build #11 на `main@1e1f134e2c16b762760efbddf0bf469aa2120864` — **FAILED**; Source Validation PASS, Unity Player export и GitHub Pages **SKIPPED**, Chromium по этому SHA **не проверен**.
- **Первопричина из полного UBA журнала:** `Assets/MIR/Art/Kenney/MirKenneyCityArt.cs(189,47): error CS0246: 'BuiltInMaterialGenerator' could not be found`. Не подменять её сообщениями Unity AssemblyUpdater / `System.Reflection.Emit` или исходным Runtime `NONATION_KENNEY_CITY_ERROR` на предыдущем SHA.
- Исходный [Unity glTFast **6.0.0** package snapshot](https://github.com/needle-mirror/com.unity.cloud.gltfast/tree/b05d842446989f0521b1e2283c7e93121d785b29) доказывает: `glTFast.asmdef` устанавливает `USING_URP` при наличии пакета URP, а `BuiltInMaterialGenerator.cs` исключает этот класс из non-Editor Player при `USING_URP`. Между тем браузер [#40190193106](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40190193106) подтвердил, что активный Render Pipeline **BuiltIn**.
- **Исправление предложено в отдельном manager PR:** удалить пока не настроенную зависимость `com.unity.render-pipelines.universal` из `Packages/manifest.json`, сохранить фактически используемый Built-in pipeline, glTFast 6.0.0 и реальные mesh/material loading. Отдельный source regression test запрещает возвращать эту конфигурацию без полноценного SRP migration.
- После source/.NET/ownership CI нужен **один** реальный Unity Cloud Build на точном SHA и браузерная проверка 12 зданий, 13 деревьев, 3 автомобилей, 2 светофоров, движения, Collider, сохранения и reload. Художественная приёмка **OPEN**. Возвращать URP только отдельной проверенной миграцией с настоящим URP Pipeline Asset, renderer data и shader variants.

## Новая визуальная реализация / 25.10.26 — подтверждённые факты

- **[PR #32](https://github.com/alexvelikiy88-sketch/nonation/pull/32) MERGED**, исходный release SHA `b70ada3663c0d2ffab02479ff604c51f3b685437`: встроен fetcher **14 оригинальных Kenney CC0 GLB** с проверкой конкретных Git SHA и размеров. Код реального 3D-переодевания 12 зданий, 13 деревьев, трёх автомобилей и двух светофоров находится в `Assets/MIR/Art/Kenney/MirKenneyCityArt.cs`. Игровая экономика/POI/коллизии не заменены; при невозможности импорта сохраняется старый визуальный fallback.
- **Unity Cloud и Pages для PR #32 — PASS:** [MIR Unity WebGL #40188505283](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40188505283) на SHA `b70ada3663c0d2ffab02479ff604c51f3b685437`: `source-validation SUCCESS`, `Unity Cloud WebGL SUCCESS`, `Publish Unity WebGL to GitHub Pages SUCCESS`. Текущий публичный адрес: **https://alexvelikiy88-sketch.github.io/nonation/**.
- **НО браузерная проверка этого релиза была SKIPPED.** Unity WebGL не доказал, что новые GLB действительно появились в игровом кадре; успешный export/Pages недостаточен для `VIS-02` и `VIS-06`.
- **[PR #33](https://github.com/alexvelikiy88-sketch/nonation/pull/33) MERGED** в `main@4252ac19e9def4dde75d5392b10b70b9597728ee`: сохранение нативных Box/Capsule/Sphere Collider для IL2CPP, отказ по Unity console warnings о отсутствующих компонентах, фикс пропуска браузерного теста из-за неявного `success()`, обязательные счётчики `NONATION_KENNEY_CITY_READY` и необработанный screenshot + runtime JSON в GitHub artifacts. **PR Guard + source/.NET validation PASS** на `7c4471f727a50a32f6db9b83cbd56bbdba75b1bb`. Реальная компиляция/браузер этого нового SHA проверяется отдельно.
- **Не скрывать открытый конфликт другого направления:** [Cross-Branch #40188505402](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40188505402) FAIL из-за content conflict в `MirCity.cs` с открытым Graphics [PR #26](https://github.com/alexvelikiy88-sketch/nonation/pull/26), а не из-за формата Kenney GLB. Руководитель оставил точный `CROSS-OWNER` handoff в комментариях PR #26; смешивать несовместимые два генератора без явного разрешения конфликта нельзя.
- **Визуальная приёмка НЕ ПРОЙДЕНА:** Kenney — облегчённая игровая графика ниже утверждённого более реалистичного концепта; полноценные Humanoid вместо шарнирных диагностических NPC пока не подтверждены. `VIS-02`, `VIS-03`, `VIS-06`, `VIS-07` остаются открытыми.

**Следующие блокирующие доказательства:** Unity Cloud компиляция на `4252ac...`, GitHub Pages deployment на этом же SHA, unedited Chromium screenshot с положительными счётчиками моделей и отсутствием IL2CPP collider/shader ошибок, затем реальные GPU/FPS замеры и согласование графики с владельцем.

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

## P0-RELEASE — проверенный Unity/WebGL baseline, 24.10.26

**Актуальная проверка имеет приоритет над историческими статусами ниже.** `main@12b3d4a56ad420a63150ecad8d4113c8b806cb89` отличается от игрового `55051008dee22e3a44d867a6191e7ab3ba6e83b4` только двумя документационными коммитами (Git compare; `Assets`, `Server`, `Packages`, `ProjectSettings` не менялись).

- **Unity Cloud #8 — PASS:** [Actions #40024399013](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40024399013) — `Unity Cloud WebGL` SUCCESS; опубликован `nonation-unity-cloud-webgl` artifact ID `12533916450` (9 645 106 bytes), проверены `index.html`, несжатые `.wasm/.data/.js`. Скрипт `Tools/unity_cloud_ci.py::poll_build` отказывает в публикации без `lastBuiltRevision == 55051008dee22e3a44d867a6191e7ab3ba6e83b4`; job завершился SUCCESS.
- **Pages/HTTPS — PASS:** тот же run #40024399013, job `Publish Unity WebGL to GitHub Pages` SUCCESS и `Verify all deployed Unity files over HTTPS` PASS, `github-pages` artifact ID `12533712104`. Это статическое подтверждение WebGL файлов, не художественная приёмка.
- **Настоящий Chromium artifact smoke — PARTIAL, runtime FAIL:** [#40028408620](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40028408620) общий SUCCESS, реальный браузерный job исполнялся на игровом артефакте `55051008...`; присутствуют `NONATION_RUNTIME_READY scene=MIR_PlayableDistrict player=player-001 npcs=39 vehicles=4 citizens=40`, Canvas, W movement, save/reload. Однако в логе сотни `Can't add component because class 'CapsuleCollider' doesn't exist!` и ошибки SphereCollider. Старый тест не классифицировал их как fatal. **Полноценный Chromium gameplay PASS отсутствует** до исправленного Unity IL2CPP и smoke без ошибок.
- **Исправление предложено, не проверено Unity:** [manager PR #30](https://github.com/alexvelikiy88-sketch/nonation/pull/30), SHA `9fa6e45547dd0c85b0f8de2b9cf391d7301028ca`, сохраняет collider-типы через `link.xml` и усиливает browser guard. Guard #40087240292 / PR Validation #40087240266 / Cross-Branch #40087240260 **PASS**, MIR Unity WebGL #40087240328 — source-validation **PASS**, Unity/Cloud/Pages/Chromium **SKIPPED**.
- **Художественный VISUAL GATE — BLOCKED:** graphics [PR #26](https://github.com/alexvelikiy88-sketch/nonation/pull/26), SHA `35d73180e22d7487fb65045bab64dd92feb9098d`, Guard/Source/.NET/Cross-Branch PASS, но реальные Unity/WebGL/Chromium jobs SKIPPED ([#40079307152](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40079307152)). Нет raw day/evening кадров, готового Humanoid/Animator/LOD proof, FPS/p50/p95/draw calls/triangles/memory и явного художественного одобрения владельца.

**Блокер:** одна обоснованная Unity-сборка исправленного collider-кода, Chromium smoke без runtime ошибок; отдельно необходима доказанная игра с новым кварталом и ручная художественная приёмка.

## Исторический P0 — ранние Pages/Shader/Unity compile дефекты (снимок до Cloud #8)

- **Все три агента объединены** в защищённую `main` через PR #19, merge SHA `a75602df4ef56e95709a8472eba62d879795c977`. Проверки source/.NET и совместной интеграции на новом `main` завершились успешно: [CI #40020804529](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40020804529).
- **GitHub Pages включён и публикация прошла:** [Actions #40019259462, attempt 3](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40019259462) завершился **SUCCESS** с `Reported success!` и URL `https://alexvelikiy88-sketch.github.io/nonation/`. Развёрнут старый SHA `47a6424ea7b8a55db82db27b3133a98c3160e139`; это **не** текущая интегрированная версия.
- **Настройка Pages закрыта:** пользователь выставил Source=`GitHub Actions`, повторно запущен только failed deployment job без Cloud rebuild; `actions/configure-pages` и `actions/deploy-pages` оба SUCCESS. Повторной ручной настройки не требуется.
- **Точный игровой дефект:** [реальный Chromium artifact smoke #40022712096](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40022712096) на агентском Cloud #4 архиве завершился **FAILURE**: Unity вывел `InvalidOperationException: Missing runtime shader`, не сообщил `NONATION_RUNTIME_READY`, хотя `NONATION_SAVE_WRITTEN citizens=40` работал. [PR #22](https://github.com/alexvelikiy88-sketch/nonation/pull/22), merge SHA `4366103dd439cfda4b65223608f84688084ba998`, добавил Shader asset под Resources и безопасный fallback при Shader stripping; статический CI PASS. **Исправленная Unity-компиляция и Chromium runtime на этом новом SHA ещё не подтверждены**. Новый [MIR Unity WebGL #40023751528](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40023751528) — отдельный билд/деплой.
- Встроенная Chromium среда этого разговора блокирует навигацию и localhost политикой `ERR_BLOCKED_BY_ADMINISTRATOR` даже для `file://`; это **не** ошибка игры. Поэтому официальный Pages browser-smoke является решающим доказательством runtime.

### Пробная замена процедурной графики на бесплатный CC0 город (25.10.26)

- **Выбран реальный пакет для программной интеграции:** Kenney City Kit Commercial + Roads + Nature + Car, 14 неизменяемых GLB-файлов в [manifest](Tools/kenney_city_manifest.json), авторские CC0-источники и [документация интеграции](Documentation/FREE_CITY_PACK_RU.md).
- Добавлены загрузчик с проверкой хешей, glTFast prefab/runtime интегратор и визуальная замена 12 существующих зданий, 13 деревьев, трёх автомобилей и двух светофоров **без удаления игровых Collider, POI, движения или экономики**. На этапе написания зафиксированы только исходники, **не подтверждён WebGL-кадр или Animator**.
- Статус **IN PROGRESS**, не DONE: требуется успешный Unity Cloud/WebGL экспорт и браузерный скриншот с доказательством `NONATION_KENNEY_CITY_READY buildings=12 trees=13 vehicles=3 props=2`, без критических ошибок; затем сравнение качества и решение по стилю.
- **Synty POLYGON City и Characters не импортированы.** Отдельно найти/импортировать настоящего CC0 скелетного персонажа с проверкой Avatar, skin/animations/LOD; не подменять его статичной GLB-фигурой.
- Текущий стилизованный Kenney городской стиль **сам по себе не соответствует автоматически** реалистичному художественному эталону; VIS-07 остаётся блокирующей задачей.

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
| G1-B | WebGL export полностью интегрированного игрового SHA | **PASS** Unity Cloud #8 на `55051008...`, #40024399013; не относится к новому graphics/manager коду | CHAT 04 + 01 |
| G1-C | HTTPS Pages + браузерная сцена/NPC/WASD/save/reload | **PARTIAL:** Pages PASS на `55051008...`; Chromium #40028408620 сцена/Canvas/39 NPC/W/save/reload PASS, но Collider Console runtime **FAIL** | CHAT 04 |
| G2 | Игровой цикл: движение, NPC, работа, покупки, транспорт, сохранение | **PARTIAL:** движение W и save/reload в Chromium PASS; реальная NPC POI/торговля/транспорт, коллизии не проверены | CHAT 01 + 02 |
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
