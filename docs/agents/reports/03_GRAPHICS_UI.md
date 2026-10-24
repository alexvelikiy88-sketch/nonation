# NONATION / отчёт 03 — Graphics/UI

- **Последнее обновление:** 24.10.26
- **Статус:** IN PROGRESS — Rocketbox FBX, MIT license и шесть текстур 1024×1024 находятся в GitHub; код генерации настоящих skinned LOD подготовлен, но Blender/Unity/WebGL ещё не проверены
- **Ветка:** `agent/graphics-ui`
- **Промпт:** [Инструкция исполнителя](../CHAT_03_GRAPHICS_UI.md)
- **Исходная база main/ветки при входе:** `f52d21a2b0e470bb79664f8be0e6fd0251b82596` (ветки идентичны; открытых PR не было)
- **Собственный коммит патча:** [`3f2e733d`](https://github.com/alexvelikiy88-sketch/nonation/commit/3f2e733d1c9c1dc178d3e9a1660355c06ee931b6)
- **PR:** [#2 Graphics — Draft](https://github.com/alexvelikiy88-sketch/nonation/pull/2), не объединён; source/.NET + ownership проверки ранее проходили, Unity/WebGL результат отсутствует

## Инвентаризация исходников GitHub, 23.10.26

В дереве `main` 74 blob-файла, **0 FBX, 0 GLB/GLTF, 0 PNG/JPG, 0 игровых .prefab, 0 .controller, 0 .anim и 0 игровых Unity .mat**. Единственный каталог в `Assets/MIR/Art/Quaternius/` содержит `LICENSE_A1.txt` и `LICENSE_A2.txt`, заявляющие **CC0 1.0**; бинарные исходники отсутствуют. `Assets/MIR/Resources/MIR` содержит только `NetworkConfig.json`. Это инвентаризация GitHub, а не файлов Unity Cloud, локального диска или содержимого скачиваемых архивов.

`Tools/fetch_quaternius.py` должен скачать `UAL1_Standard_Unity.fbx`, `UAL2_Standard_Unity.fbx`, `Mannequin_F.fbx` с проверкой SHA-256. `MirUnitySetup` (владение AGENT 01) создаёт контроллер, каталог и префаб в Editor из импортированных ресурсов; **эти полученные Unity-артефакты не закоммичены**. Из CC0-лицензии Quaternius не следует, что модель имеет реалистичную внешность или проверенные лицевые blendshapes и LOD.

`MirHumanFactory`: загружает каталог и prefab из Resources, сверяет Humanoid Avatar, масштабирует экземпляр до ~1.72 м, иначе создаёт процедурный `MirFallbackWalker` из примитивов. Это не реалистичный персонаж. `MirMotionDriver`: отключает root motion, выставляет `Speed`, cross-fade к состояниям `Locomotion` и действиям; если состоянием не обладает, возвращается к `Locomotion`. `MirAnimationCatalog` хранит ссылки на клипы; наличие каталога само по себе не гарантирует валидные rig и Animator. Проверки в Unity Editor, реального blend tree, выходного WebGL и движения персонажа в браузере **не производились**.

### Что отсутствует или не подтверждено

| Категория | Результат |
|---|---|
| Настоящие игровые FBX/GLB/GLTF в GitHub | Отсутствуют; отдельно заявлены только CC0-архивы для скачивания |
| Текстуры персонажей, PBR и материалы | Файлов текстур и игровых материалов нет |
| Unity Humanoid Avatar / prefabs | Сгенерированные .asset/.prefab отсутствуют в GitHub |
| Animator Controller / Clips | .controller/.anim отсутствуют в GitHub; в редакторе потенциально генерируются из скачанных FBX |
| Skinned weights / topology / blendshapes | Без импортированного исходника не измерены и не подтверждены |
| LODGroup / LOD meshes | В GitHub готовой структуры LOD для персонажа нет |
| Лицензии внешних персонажей | Maid, Floral, Casual, Girl, GIGN, Pinkdress, Freya — права перераспространения не подтверждены (`Documentation/LICENSES.md`); Honda — CC BY-NC-SA 4.0, в коммерческий WebGL запрещена |
| Draw calls / triangles / memory / FPS | Не измерены: нет Unity Editor или WebGL-профилирования |

### Источник-кандидат для отдельного пилота

**MakeHuman Community / MakeHuman**, проверить конкретную версию генератора и набор экспортируемых ассетов: заявленная модель распространения создаваемых базовых персонажей — CC0, но перед помещением в репозиторий нужны зафиксированные URL/версия/текст лицензии **каждого используемого материала**, экспортированный FBX/GLB и проверка реальных bone weights, skeleton Humanoid, Avatar validity, facial blendshapes (если имеются), Idle/Walk/Run, корректных текстур и создаваемых LOD. Отдельно проверить сторонние прокси/одежду, которые могут иметь иные лицензии. Пока **не приобретён/не импортирован и не сертифицирован** как WebGL-ready персонаж; не включать в сборку как готовый. Альтернатива — CC0 Quaternius как тестовый анимационный/rig источник, но не замена реалистичной модели.

## Выполнено: безопасная правка №1

`Assets/MIR/Scripts/MirHumanFactory.cs` — метод `HasPlayableRig`: принимаем prefab только если имеются валидный `Humanoid Avatar`, `RuntimeAnimatorController`, float-параметр `Speed`, непустые клипы Idle/Walk/Run (не T-pose) и активный `SkinnedMeshRenderer` с ненулевой сеткой. При непрохождении — сохраняется старое поведение `MirFallbackWalker`. Защита предотвращает заведомо статичный импорт из-за отсутствующего контроллера или clip; не гарантирует правильную настройку BlendTree или отсутствие T-pose при неверных клипах. Не добавлены ассеты, runtime-ресурсы или новые зависимости.

**Коммит:** `3f2e733d1c9c1dc178d3e9a1660355c06ee931b6`.
**Проверено:** визуальный анализ C#-исходников, условий возврата, вызовов в фабрике и сохранения fallback; дерево GitHub, лицензии и отсутствие готовых бинарников.
**Не проверено:** C# компилятором Unity, импортом, Animator runtime, реальным FBX/GLB, WebGL export, браузером, GPU/FPS. В GitHub Actions было только `source-validation SUCCESS` на старом SHA `f909e47`; WebGL/cloud-webgl/deploy были SKIPPED. На момент первого патча CI именно этого SHA ещё не было; **впоследствии** отдельный PR-validation workflow выполнил source/.NET проверки на HEAD ветки (см. раздел продолжения), но без Unity/WebGL компиляции.

## Продолжение этапа 1 — 23.10.26

### Верификация лицензируемого первоисточника

Проверены **официальные** материалы MakeHuman Community:
- https://www.makehumancommunity.org/about/license.html — core graphic assets CC0; код самого генератора MakeHuman AGPL/MPFB GPL.
- https://github.com/makehumancommunity/makehuman/blob/master/LICENSE.md — экспорт из собственных базовых ассетов MakeHuman относится к выходным данным пользователя, права на output не заявляются.
- https://www.makehumancommunity.org/makehuman/faq/can_i_sell_models_created_with_makehuman.html — коммерческие и закрытые игровые проекты разрешены, экспорт по CC0.
- Анимации Quaternius: https://quaternius.com/packs/universalanimationlibrary.html и https://quaternius.com/packs/universalanimationlibrary2.html — CC0, включая коммерческое применение.

**Вывод по правам:** базовый MakeHuman/Quaternius pipeline потенциально разрешён для коммерческого WebGL, но **конкретный FBX персонажа пока отсутствует**, сторонняя одежда/skin/hair может иметь иные лицензии. Не проверены реальная геометрия, skinning, мимика, текстуры, LOD, polycount, runtime-совместимость. Статус полноценного игрового персонажа: **BLOCKED**.

Создан [`Assets/MIR/Art/CHARACTER_PILOT_RU.md`](../../../Assets/MIR/Art/CHARACTER_PILOT_RU.md) — входной контракт для одного пилота с обязательными SHA-256/лицензиями, настройкой humanoid import, проверками клипов/skin/LOD, измерениями FPS/draw calls/треугольников и разграничением AGENT 01/03. Это **спецификация, не импорт ассета**.

### Выполненные правки кода

1. **`MirMotionDriver.cs`, [`60937d8`](https://github.com/alexvelikiy88-sketch/nonation/commit/60937d8dbe934423f9fbf3344f1b3263392bc8cc):** исправлено соответствие скоростей и BlendTree: NPC при 1.3–1.7 м/с и игрок при 2.7 м/с получают `Speed=.42` (полный Walk), игрок при 5.5 м/с — `Speed=1.0` (Run). Поддержана опциональная анимация `Locomotion_Carry`, fallback отсутствующих состояний теперь запоминает фактическое состояние; root motion остаётся отключённым. Это калибровка по **исходному коду**, не подтверждённая визуально в Unity.
2. **`MirHumanFactory.cs`, [`fa4a64f`](https://github.com/alexvelikiy88-sketch/nonation/commit/fa4a64f5e44a45fdffe032fa164e9f0c0e9926ee):** процедура поиска prefab больше не требует `QuaterniusCatalog` (лицензированный внешний персонаж может использовать собственный контроллер), добавлен стабильный положительный FNV-variant вместо нестабильного `string.GetHashCode`, выключение отвергнутого экземпляра до `Destroy`, проверка `Locomotion`. Диагностический capsule fallback **не считается готовой графикой**.
3. **`MirHumanFactory.cs`, [`fa4a64f`](https://github.com/alexvelikiy88-sketch/nonation/commit/fa4a64f5e44a45fdffe032fa164e9f0c0e9926ee):** общий набор из **5 материалов палитры** fallback (1 skin, 1 hair, 3 outfits) вместо создания ~3 материалов на каждого NPC. `Material.enableInstancing=true`; **никакого подтверждённого ускорения FPS или сокращения draw calls не заявляется**.
4. **`MirHumanFactory.cs`, [`b5f510d`](https://github.com/alexvelikiy88-sketch/nonation/commit/b5f510d5259464f2c4e6cf9b04322e202d33b1c4):** предупреждения о точной причине отказа prefab (Avatar/Controller/Speed/Locomotion/Idle-Walk-Run/skinned mesh) с dedup один раз на путь + ошибку; отсутствие prefab тоже логируется один раз. Это помогает диагностировать причину появления заглушек, не переполняя Console сообщениями для всех NPC.
5. **[Договор пилотного ассета, `a3d508b`](https://github.com/alexvelikiy88-sketch/nonation/commit/a3d508ba5f1b6bf87cfd4835856a7d10f48fff68):** документация требований одного лицензированного персонажа до любых 10 разновидностей.

**Статическая проверка (НЕ Unity):** после записи перечитаны оба C# файла из GitHub, проверены условия сохранения fallback, наличие Animator/state guard, отключённый root motion, разделение NPC/player скоростей и код sharing материалов. Простая проверка скобок и табличная проверка логики map скорости: `0→0`, `0.65→.21`, `1.3→.42`, `1.7→.42`, `2.7→.42`, `4.1→.71`, `5.5→1.0` — не компилятор и не игровой прогон.

**Не выполнено:** Unity C# compilation, FBX importer, LOD preview, skin/bone weight/retarget QA, браузерный WebGL, GPU measurement. В доступном runtime `git ls-remote` завершился DNS-ошибкой для github.com, C#/.NET/Unity компилятор отсутствует; поэтому тесты проекта локально не запускались. **CI PR #2 на HEAD `91888444f800cca0e09f0715e1985ff841fb440c` подтверждён:** [GitHub Actions run 40005479208](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40005479208) — job `Source preflight and .NET 8` SUCCESS; успешные шаги: Unity **source/scene preflight (не Unity build)**, Python unittest для Cloud helper, .NET tests экономики и симуляции, сборка .NET backend, HTTP/snapshot smoke. **НЕ проверяет Unity C# компилятор, Animator, WebGL, FPS**. AGENT 01 ведёт отдельный [PR #3](https://github.com/alexvelikiy88-sketch/nonation/pull/3) по WebGL CI.

### Блокирующие зависимости

- AGENT 01 должен подтвердить рабочий Unity 6000.0.58f2 WebGL export/Play Mode и взять импорт, Avatar и Animator по `Assets/MIR/Editor/**`.
- Реальный MIT Rocketbox FBX уже находится в GitHub; до импорта оригинальных текстур, Humanoid/Animator/LOD проверки в Unity и WebGL нельзя заменить им диагностический fallback. Детали — `Assets/MIR/Art/Rocketbox/PILOT_RU.md`.

## Продолжение этапа 1 — Microsoft Rocketbox, 23.10.26

### Реальный 3D-исходник в AGENT 03

После сравнения MIT Microsoft Rocketbox с прежней MakeHuman CC0 гипотезой выбран **Microsoft Rocketbox `Male_Adult_12`** — настоящий clothed rigged FBX, в отличие от процедурного `MirFallbackWalker`. Документация и проверяемое происхождение: [`Assets/MIR/Art/Rocketbox/PILOT_RU.md`](../../../Assets/MIR/Art/Rocketbox/PILOT_RU.md), официальный источник https://github.com/microsoft/Microsoft-Rocketbox, pinned upstream commit `0943055db6ec570bcef9f2c8b41c9e5467c808f9`, лицензия [MIT](https://github.com/microsoft/Microsoft-Rocketbox/blob/master/LICENSE.md), © Microsoft 2020 (текст лицензии обязательно сохранять при распространении).

Коммит [`269d77e0`](https://github.com/alexvelikiy88-sketch/nonation/commit/269d77e0e4ebc887d85458d6676341e7dfe4894d) добавил в `Assets/MIR/Art/Rocketbox/`:
- `Male_Adult_12.fbx` **487 776 байт**, Git blob SHA-1 `15d4f8501d5703600cb3d8a94765a3de1029aaf6`, байтовая идентичность с оригиналом Microsoft проверена через совпадающий Git blob SHA;
- `Male_Adult_12_preview.png` **506 050 байт**, SHA `2b4e07161df2786c8cde85952403bc71eaabc031`, идентичен исходному изображению;
- `LICENSE.md` — полный исходный MIT notice, SHA `9bcfb3ece5301a55d3a41bbd00a029ab27d61d13`;
- `PILOT_RU.md` — точное происхождение модели, upstream SHA, перечень отсутствующих текстур, pipeline и ограничения.

**Бинарная предварительная проверка FBX:** заголовок `Kaydara FBX Binary`, внутри найдены `Deformer`, `Skin`, `Cluster`, 2 именованных материала и четыре LOD-подобных именования: `m007_hipoly_81_bones`, `m007_midpoly_42_bones`, `m007_lowpoly_33_bones`, `m007_ultralowpoly_23_bones`. Это **не** сертификация скелета, весов, числа полигонов, настройки LODGroup или анимации: Unity Editor не запускался.

**Позднее уточнение 24.10.26:** найден ровно **один** Geometry Mesh; остальные три LOD-имени не соответствуют отдельным Mesh. См. аудит ниже.

**Текстуры ещё отсутствуют в GitHub:** оригинальный FBX ссылается на 6 TGA Microsoft `m007_body/head_{color,normal,specular}.tga`, исходные размеры по GitHub tree — по 12 582 956 байт, ~72 МиБ вместе. Не добавляем тяжёлые TGA в git без WebGL оптимизации.

Коммиты [`77fdec16`](https://github.com/alexvelikiy88-sketch/nonation/commit/77fdec1675108b0dce4a2157059e0f72bf95d762) и [`1a36c4d4`](https://github.com/alexvelikiy88-sketch/nonation/commit/1a36c4d455fb42623c6def9e1cd7ce57268fbfa9) добавили `fetch_textures.py` (immutable HTTPS URL, upstream Git SHA-1 и проверка размера, 32-bit TGA downsample, сохранение локально) и `test_fetch_textures.py`.

**Достоверные локальные тесты скрипта:** `python3 -m unittest discover -s /mnt/data/rocketbox_work -p 'test_*.py' -v` — **4/4 PASS** (synthetic TGA, SHA known vector, downsizing BGRA/alpha/orientation, fail-closed на повреждённый/неподдерживаемый TGA). `--dry-run` выводит шесть pinned hash; оба GitHub Python blob SHA побайтно совпали с прошедшими эти тесты локальными файлами. **Не подтверждены:** реальные скачивания шести TGA (сетевой backend не проверен), реальная конверсия этих TGA, Unity Material assignment, текстурные артефакты и WebGL performance. Новая фиксация `PILOT_RU.md` и обновление главного контракта `CHARACTER_PILOT_RU.md`: `c51e6c3`, `c29e1ca`.

**Ключевой принцип:** файл FBX в `Art/`, а не в `Resources/` или игровой сцене; пока `MirHumanFactory` не активирует новую модель и существующая WebGL-цепочка её не включает. Не выдавать «файл в Git» за работающего/анимированного персонажа.

### Новая CI-проверка и владение

Параллельно Manager добавил `NONATION Integration Guard`: [run 40007885901](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40007885901) до синхронизации показал два явных требования: `ASSET-LICENSE-EVIDENCE:` в PR (для FBX и PNG) и отсутствие устаревшей версии `TASKS.md` при прямом сравнении PR base с head. **Исправлено:** соответствующее точное доказательство Microsoft MIT добавлено в текст [PR #2](https://github.com/alexvelikiy88-sketch/nonation/pull/2), ветка `main` безопасно влита в `agent/graphics-ui` коммитом [`063a26d2`](https://github.com/alexvelikiy88-sketch/nonation/commit/063a26d2a5c24db502baafa51e0686ec1a5ad164). Сравнение до слияния показало **0 общих изменённых путей**, новый diff `agent/graphics-ui→main` содержит **только 10 разрешённых файлов AGENT 03**, `TASKS.md` и чужие файлы не менялись нами. **Результат перепроверен:** [Integration Guard run 40008579046](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40008579046) = SUCCESS на HEAD `543ef754607b419cac5f0c701ed94b38ab10d31b`; [Pull Request Validation 40008579084](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40008579084) = SUCCESS (source preflight, .NET tests и backend HTTP smoke; не Unity/WebGL). Последующий rerun guard после изменения описания PR тоже запускался; PR остается Draft до настоящего Unity QA.

**CI исходников:** [run 40007822540](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40007822540) = source/.NET checks SUCCESS на коммите `c51e6c3` (не настоящая Unity сборка). Реальный Unity WebGL Player export, standalone Unity C# compilation, Animator import/retarget, UI/город, draw calls/FPS **НЕ проверены**.

### Обновлённый CROSS-OWNER REQUEST

- **AGENT 01 — `Assets/MIR/Editor/MirAssetImport.cs`, `MirUnitySetup.cs` и при необходимости собственный новый Editor script:** для `Assets/MIR/Art/Rocketbox/Male_Adult_12.fbx` настроить ModelImporter Humanoid с проверкой `Avatar.isValid`/`isHuman`, `SkinnedMeshRenderer`/boneWeights/bindposes, материалы и шесть локально полученных TGA с проверкой альфа/normal map, использовать Quaternius CC0 для Idle/Walk/Run через retarget, создать `Resources/MIR/PilotHuman01.prefab` только после Editor QA; выделить 4 реальные LOD меша и настроить `LODGroup`. Пока текстур нет и реальные clip links не проверены, не подключать prefab к release WebGL.
- **AGENT 02 / Manager — `Assets/MIR/Scripts/MirState.cs` и world default look:** лишь **после** принятого Unity prefab и проходящего WebGL smoke-test согласовать opt-in отображение `PilotHuman01` (например `MirCitizen.look`), не менять данные мира в этом PR.

## Инженерная проверка FBX и текстур — 24.10.26

### Новые подтверждённые факты о `Male_Adult_12.fbx`

В отличие от первоначальной проверки текстовых маркеров, бинарный FBX `15d4f8501d5703600cb3d8a94765a3de1029aaf6` реально разобран по узлам FBX **7400**. Прочитаны 3 301 узел без ошибок смещений; в файле **1 Geometry Mesh**, **1 Mesh Model**, **80 LimbNode**, **1 Skin Deformer**, **80 Cluster**, **2 Material** и **6 Texture**. Координатный массив `Vertices` имеет длину **10 149 (3 383 вершины)**; `PolygonVertexIndex` имеет длину **20 166 индексов углов полигонов**, не измеренных triangle count. Это метаданные по бинарному оригиналу, **не Unity-import/LOD/runtime proof**.

**Существенное исправление:** только `m007_hipoly_81_bones` соответствует настоящему `Model Mesh`. Имена `midpoly/lowpoly/ultralowpoly` есть в строках бинарника, **но отдельных Geometry Mesh в этом FBX нет**. Прежнее предположение, что он содержит четыре LOD, **не подтверждено и не должно использоваться в планировании**. Реальные LOD1–LOD3 придётся получить или сгенерировать по отдельной проверяемой процедуре, а затем измерить. Обновлены `Assets/MIR/Art/Rocketbox/PILOT_RU.md` и `Assets/MIR/Art/CHARACTER_PILOT_RU.md`.

### Улучшена надёжность подготовки текстур (AGENT 03)

- [`b5339300`](https://github.com/alexvelikiy88-sketch/nonation/commit/b53393007b60c76a22f9cbc99b56bfdc380893fd) — `fetch_textures.py`: скачивание и SHA-1 проверка исходников остаются привязаны к immutable Microsoft Rocketbox revision. Теперь все 6 TGA **сначала полностью проходят подготовку в staging**, после чего устанавливаются в папку `Textures`. Ошибка скачивания/хеша третьей текстуры не оставляет частично добавленных файлов; ошибка диска во время финального перемещения *не* может гарантировать атомарность всего набора. Дополнительно записывается `textures_manifest.json` с upstream commit, размерами, SHA-256 выходных TGA и настройкой. Box-average для normal maps завершается **нормализацией** вектора, alpha сохранена.
- [`93557f2d`](https://github.com/alexvelikiy88-sketch/nonation/commit/93557f2da01cbf936dd9cd3485950f460c95a57c) — `test_fetch_textures.py`: **6/6 локальных unittest PASS** на синтетических TGA, включая нормализацию normals, отказ при битом TGA и отказ от частичного набора при падении третьего download.
- Контроль Git blob исходников `fetch_textures.py`=`05cf4a55ba5a932fb5d09e6ae43a4522543103df`, `test_fetch_textures.py`=`c8db65d02dd856be8e6e5f3828886ca3351dbdf0`: **совпадают** с байтами локально прошедших 6 тестов. Команды локальной проверки: `cd /mnt/data/rocketbox_work && python -m unittest discover -s . -p 'test_*.py' -v`, `python fetch_textures.py --dry-run`.
- **НЕ проверено:** загрузка *реальных* 6 upstream TGA из текущей среды (DNS `raw.githubusercontent.com` недоступен); нет реальных уменьшенных текстур в GitHub, назначения материалов, Unity импортера, Animator, Humanoid Avatar или WebGL QA.

### Runtime проверка SkinnedMeshRenderer

[`2bd9f049`](https://github.com/alexvelikiy88-sketch/nonation/commit/2bd9f0493bbd20b548f4e7a7c9bc0fd86a011ddb) в `MirHumanFactory.cs` дополняет `HasPlayableRig`: одной `SkinnedMeshRenderer` недостаточно — нужны непустые bones и равное количество bindposes, mesh с вершинами. При отсутствии используется прежний **диагностический** fallback и однократное предупреждение. Runtime API и фактическую совместимость с Unity **ещё не компилировали / не проверяли**; нужен Play Mode тест AGENT 01.

### Незавершённая зависимость

**CROSS-OWNER REQUEST для AGENT 01:** `Assets/MIR/Editor/MirAssetImport.cs`, `MirUnitySetup.cs` и свой Editor QA: на реально импортируемом `Male_Adult_12.fbx` проверить `Avatar.isValid/isHuman`, 80 skin clusters/weights/bindpose mapping, 6 TGA+материалы, `Quaternius` retarget Idle/Walk/Run и absence of T-pose; **не создавать LODGroup из четырёх фиктивных записей**, сначала получить настоящие LOD-меши. Стадия WebGL release остаётся P0 блокером AGENT 01 и Manager. Никаких чужих файлов мы не меняли.

## Реальная установка текстур Rocketbox — 24.10.26

**Пользователь поручил скачать текстуры через GitHub и установить в репозиторий. Исполнено.** Временный workflow `rocketbox-textures-one-shot.yml` запущен на `agent/graphics-ui` через GitHub Actions, а после успешной установки **удалён**, чтобы не оставить чужой workflow в итоговом PR. Ни `main`, ни `TASKS.md`, ни Editor-файлы не менялись.

1. Первый запуск [Actions #40013648557](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40013648557) успешно получил оригинальный файл Microsoft с совпадающим хешем, но обнаружил неверное допущение старого парсера: **исходные TGA имеют 24 бита**, а не 32. Это было реальной, воспроизведённой ошибкой, а не сетевым блокером GitHub runner.
2. Коммит [`214254c7`](https://github.com/alexvelikiy88-sketch/nonation/commit/214254c7a0cd1778813e53ae0dd939758356e1d2) — поддержка **24/32-bit true-color TGA** с корректной длиной пикселя и BGR/BGRA. Коммит [`e6a8ff6e`](https://github.com/alexvelikiy88-sketch/nonation/commit/e6a8ff6e761cd0cee88c9f29014203802a8bdbd8) — регрессия 24-bit BGR и normal-vector.
3. Повторный [GitHub Actions #40013964977](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40013964977) **SUCCESS на всех стадиях**: 7 Python unit tests, загрузка шести оригинальных MIT Microsoft TGA, проверки upstream Git blob SHA-1, уменьшение до **1024×1024**, проверка шести SHA-256 результатов, commit+push и artifact upload.
4. **Ветка `agent/graphics-ui` получила реальный ассетный коммит [`eb58e07f`](https://github.com/alexvelikiy88-sketch/nonation/commit/eb58e07f5a1fd97597b71affa1c7a774f3292ebf)** с шестью `Assets/MIR/Art/Rocketbox/Textures/m007_{body,head}_{color,normal,specular}.tga` по **3 145 746 байт каждый** и [`Textures/textures_manifest.json`](https://github.com/alexvelikiy88-sketch/nonation/blob/agent/graphics-ui/Assets/MIR/Art/Rocketbox/Textures/textures_manifest.json) со всеми SHA-256 и upstream хешами. Суммарно шесть текстур: 18 874 476 байт (~18 МиБ) плюс JSON manifest.
5. Отдельно сверены GitHub tree и содержание manifest: **6 из 6 текстур присутствуют**; `source_blob_sha1` для каждого имени совпадает с pinned Microsoft `0943055db6ec570bcef9f2c8b41c9e5467c808f9`. Изображения записаны в `Art/Rocketbox/Textures`, **не в `Resources` или сцену**.

**Граница готовности:** полноценного Unity `PilotHuman01.prefab`, проверенного Humanoid Avatar, корректных слотов материалов, Animator/retarget, отдельных LOD1–3 и подтверждённого WebGL FPS **по-прежнему нет**. Текстуры теперь доступны для проверки AGENT 01. Следующий CROSS-OWNER REQUEST адресован AGENT 01: правильно настроить ModelImporter/TextureImporter для `Assets/MIR/Art/Rocketbox/Male_Adult_12.fbx` + шести 1024×1024 TGA, создать и проверить material mappings (sRGB для albedo, normal map importer для нормалей, specular workflow по фактическому render pipeline), Humanoid mapping и реальные LOD. Не делать release без реального Unity QA.

## Подготовка настоящих Rocketbox LOD1–LOD3 — 24.10.26

**Решение по качеству:** исходный MIT FBX содержит одну реальную скелетную сетку; дополнительные LOD нельзя объявить готовыми по строкам `midpoly/lowpoly`. По этой причине AGENT 03 подготовил отдельный fail-closed Blender-процесс. Он не изменяет импорт Unity, сцены, Packages или release WebGL.

### Изменения ветки

- [`f4bf6444`](https://github.com/alexvelikiy88-sketch/nonation/commit/f4bf6444bb588e66acd6af24c087d62d276f7cbf) — [`Assets/MIR/Art/Rocketbox/build_skinned_lods.py`](../../../Assets/MIR/Art/Rocketbox/build_skinned_lods.py). Проверяет исходный source Git blob/обязательный MIT notice/шесть SHA-256 текстур; через Blender 3.6+ импортирует **один** skinned Mesh, делает независимые копии и Decimate с целями **0.72 / 0.48 / 0.28**, требует сохранения UV/двух материальных слотов/костей и vertex groups, строгого падения triangle count, экспортирует FBX для каждого LOD и **повторно импортирует** все три файла перед выпуском. Записывает `lod_manifest.json` с source SHA, Blender version, фактическими vertex/triangle counts и output SHA-256. Три реальных LOD-файла **на момент этого отчёта не создавались**.
- [`d376c3ee`](https://github.com/alexvelikiy88-sketch/nonation/commit/d376c3eec52fe248602efcd0ba4613fa80f20979) — [`test_build_skinned_lods.py`](../../../Assets/MIR/Art/Rocketbox/test_build_skinned_lods.py): шесть Python тестов без Blender на pinned source hash, разрешённые descending ratios, наличие/целостность MIT-лицензии и 6 texture blobs, fail-closed на повреждённых данных и отказ выполнять pipeline без Blender.
- [`087b99b7`](https://github.com/alexvelikiy88-sketch/nonation/commit/087b99b7b6d3b4dee9903c115718f0047f22dee6) — [`LOD_GENERATION_RU.md`](../../../Assets/MIR/Art/Rocketbox/LOD_GENERATION_RU.md) с повторяемыми командами, владельцами, критериями отказа, запретом считать pipeline готовой 3D-моделью.
- [`238cfb69`](https://github.com/alexvelikiy88-sketch/nonation/commit/238cfb69f3b5b2900a8f6b545be095861521caf2) — уточнена документация Rocketbox Pilot и ссылка на [issue #17](https://github.com/alexvelikiy88-sketch/nonation/issues/17).

### Дополнительная проверка реальных исходников

- [`e6451343`](https://github.com/alexvelikiy88-sketch/nonation/commit/e6451343aa138300e4f5a0c2b65f162f7c7bba20) — генератор теперь требует *точный* Git blob SHA-1 оригинального `LICENSE.md` от Microsoft и закреплённые 6 `source_blob_sha1` из их immutable repository commit, а не просто наличие похожего текста лицензии и самосогласованный JSON.
- [`c3cea33e`](https://github.com/alexvelikiy88-sketch/nonation/commit/c3cea33eeb1d24c30d5c5351ee3bac0bdf9910bf) и [`9b8409b2`](https://github.com/alexvelikiy88-sketch/nonation/commit/9b8409b257bd1049feb43eb0c3b5580a5ffb7b4d) — расширены/исправлены fixture-based тесты на подмену `source_blob_sha1` и MIT.
- [`74b58497`](https://github.com/alexvelikiy88-sketch/nonation/commit/74b58497f8f02b37aeca4268192c2c715213e622) — отдельный **неподменённый** тест читает *реальные* `Male_Adult_12.fbx`, `LICENSE.md` и шесть TGA из Git checkout и проверяет оригинальный source hash, MIT notice, SHA-256, размер и pinned upstream manifest. Это интеграция проверки исходников, **не** Blender/Unity импорт.
- [Cross-Branch Integration #40018055658](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40018055658) на SHA `74b58497` = **SUCCESS**, ровно **15/15 Python тестов** (8 для LOD/source integrity + 7 для TGA), включая `test_real_repository_assets_match_pinned_microsoft_source` и `test_tampered_manifest_origin_rejected`. Этот workflow также проверил трёхагентную merge-совместимость, .NET и backend HTTP. **Blender export, Unity compilation, Avatar/Animator, WebGL/browser/FPS не выполнялись.**
- Промежуточные CI failures на `e6451343` и `c3cea33e` были **ошибкой synthetic fixture при ужесточении pinned MIT проверки**, устранены в `9b8409b2`, а затем подтверждены final source integrity test. Они не связаны с Unity C# или графикой.

### Достоверная проверка

- [GitHub Actions Cross-Branch Integration #40016593658](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40016593658): **SUCCESS**, в логе запуск `python3 -m unittest discover -s Assets/MIR/Art/Rocketbox -p 'test_*.py' -v`. Подтверждены **6/6 новых offline тестов LOD и 7/7 существующих тестов TGA**; плюс cross-branch merge, Python source checks, .NET build и HTTP smoke. **Только статический Python-контур**, в workflow Blender/Unity/IL2CPP/WebGL не запускались.
- API Blender сверено с официальной документацией, но это **не запуск Blender** и не проверка экспортируемых костей/материалов в Unity. Возможные непригодные упрощения, потерянные skin weights, UV seam artifacts — стоп-факторы и предмет реального Editor QA.
- Новые бинарные LOD отсутствуют в Git; `GeneratedLODs/` — только путь будущего вывода. Никакого FPS/draw calls/triangle counts от генерации **не заявляется**.

### CROSS-OWNER REQUEST

- [**Issue #17 — Manager/AGENT 01**](https://github.com/alexvelikiy88-sketch/nonation/issues/17): менеджеру/владельцу CI — разрешить одноразовый Blender job или другой проверенный Blender runtime (Manager владеет `.github/workflows/**`); результат: 3 реально экспортированных+реимпортированных MIT FBX и `lod_manifest.json` в `agent/graphics-ui` только при PASS, вместе с логом Blender. При FAIL — log+blocker, не создавать фейковые LOD.
- AGENT 01 — исключительно его `Assets/MIR/Editor/**`, `Assets/MIR/Scenes/**`: Unity импорт skinned FBX/6 TGA, Avatar/Animator/true LODGroup и анимационная QA, затем браузерный WebGL proof. AGENT 03 эти файлы не менял.

## Журнал

| Дата | Работа | Коммит / PR | Проверки / ограничения |
|---|---|---|---|
| 23.10.26 | Начальная карточка исполнительного чата | — | Работа не была начата |
| 23.10.26 | Инвентаризация GitHub и safety gate персонажа | [3f2e733d](https://github.com/alexvelikiy88-sketch/nonation/commit/3f2e733d1c9c1dc178d3e9a1660355c06ee931b6) | Анализ исходников; Unity/WebGL не проверены |
| 23.10.26 | Перенастройка locomotion, caching материала, runtime-диагностика, официальный аудит лицензии, контракт пилота | `60937d8`, `fa4a64f`, `b5f510d`, `a3d508b` / PR #2 | Статический анализ; Unity/WebGL, FPS, импорт модели НЕ проверены |
| 23.10.26 | MIT Microsoft Rocketbox исходный FBX/PNG, license provenance, проверяемый fetch текстур и 4 синтетических offline теста | `269d77e`, `77fdec1`, `1a36c4d`, `c51e6c3`, `c29e1ca`, merge `063a26d`; PR #2 Draft | FBX blob идентичен upstream, 4/4 Python unittest PASS; текстуры/Unity/WebGL не проверены |
| 24.10.26 | Установлено фактическое количество FBX Mesh/Skins; staging текстур и SHA-256 manifest, нормализация normal maps; проверка bones/bindposes | `b5339300`, `93557f2d`, `2bd9f049` / PR #2 Draft | 6/6 синтетических unittest PASS и анализ бинарника; Unity, реальные TGA, LOD, FPS не проверены |
| 24.10.26 | Через GitHub Actions действительно загружены и установлены 6 MIT Rocketbox текстур 1024×1024, исправлен 24-bit parser, сохранены upstream SHA и SHA-256 output manifest | `214254c7`, `e6a8ff6e`, `eb58e07f`; [Actions 40013964977](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40013964977) | 7/7 Python unittest, реальный download и hash checks, commit success; Unity/WebGL не проверены |
| 24.10.26 | Реализован Blender-pipeline трёх skinned LOD и шесть offline tests; создан запрос на одноразовый Blender job | `f4bf6444`, `d376c3ee`, `087b99b7`, `238cfb69` / [issue #17](https://github.com/alexvelikiy88-sketch/nonation/issues/17) | [CI 40016593658](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40016593658) 13/13 Python графических тестов PASS; реального Blender export, Unity/LOD визуального QA и WebGL ещё нет |
| 24.10.26 | Пиннинг SHA лицензии и всех source TGA, реальная проверка FBX+6 TGA в CI и защита от подмены manifest | `e6451343`, `c3cea33e`, `9b8409b2`, `74b58497` | [Cross-Branch #40018055658](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40018055658) **15/15 графических Python тестов PASS**, реальные pinned input hashes подтверждены; Blender/Unity/WebGL не проверены |

**CI-контроль 23.10.26:** PR #2 HEAD `91888444` — [run 40005479208](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40005479208), source/.NET success; Unity WebGL remains unverified.

**Контроль исходного PR кода (23.10.26):** SHA `543ef754`, source/.NET [run 40008579084](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40008579084) SUCCESS; ownership + license evidence [run 40008579046](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40008579046) SUCCESS. Ни одна из этих проверок **не является Unity Player экспортом**.

## CROSS-OWNER REQUEST

1. **AGENT 01:** `Assets/MIR/Editor/MirUnitySetup.cs` — текущий `Get(all,"Idle_Loop",...,"A_TPose")` может использовать T-pose как idle; убрать T-pose в качестве fallback и отказывать в создании locomotion при неполных Idle/Walk/Run. Также необходим Editor audit созданных Avatar/skin/weights/материалов/LOD и связь с фактическим WebGL билдом; меняет AGENT 01 или Manager.
2. **AGENT 01:** `Assets/MIR/Editor/MirAssetImport.cs` и `MirHumanoidFromGltf.cs` — при появлении лицензированного реалистичного ассета определить повторяемый importer/Avatar/prefab/LOD pipeline с явными проверками. Не импортировать непроверенные модели в `Resources`.

3. **AGENT 01:** `Assets/MIR/Editor/MirUnitySetup.cs`, `Assets/MIR/Editor/MirAssetImport.cs` — по `Assets/MIR/Art/Rocketbox/PILOT_RU.md` подготовить Editor validation для реально полученного **MIT Rocketbox `Male_Adult_12.fbx`** (Avatar/weights/materials/1 исходный Mesh, отдельное получение действительных LOD и clip assignment). Не подменять CC0 MakeHuman альтернативой текущий пилот без отдельной задачи.

## Следующее конкретное действие

Один лицензированный FBX **и 6 проверенных уменьшенных TGA уже получены и закоммичены**. AGENT 03 подготовил скрипт для настоящих LOD1–LOD3 и тесты, но Blender не запускался. Следующий факт — [issue #17](https://github.com/alexvelikiy88-sketch/nonation/issues/17): Manager запускает проверенный Blender job и публикует только успешно экспортированные FBX/manifest в графической ветке; AGENT 01 проверяет Unity Editor material/skin/boneWeights/Avatar/Idle-Walk-Run и реальные LOD meshes, затем первый WebGL export/браузерные FPS и draw calls. Не смешивать с Cloud Build; проектный статус `TASKS.md` редактирует только Manager.

## INTEGRATION HANDOFF - CHAT 04 / Manager
Author: CHAT 04 / Manager

**Дата:** 24.10.26. **Источник отчёта агента:** `agent/graphics-ui@a6b38f6e53f45d92f9480a2899a360d87ab6f778`, Git blob `38b6e7bb58c99ac27d7b62c8b929a89e4f6c70c3`.

Это **прозрачное дополнение технического руководителя при разовой интеграции**, а не сообщение от лица исполнителя. Весь исходный текст отчёта выше сохранён без правок.

**Сверка результатов руководителем:**
- Исходные графические C# и материалы импортированы с атрибуцией автора; исходный Microsoft Rocketbox FBX, лицензия MIT и 6 TGA 1024 сохранены в Art, но не объявлены готовой игровой моделью.
- `build_skinned_lods.py` — подготовленный pipeline; Blender не запускался, отдельные LOD1–3 и игровой Prefab/Avatar пока не подтверждены.
- Совместный Python/.NET/HTTP CI на PR #19 SUCCESS, но не проверяет skinned weights, импорт в Unity, анимацию, качество материалов и FPS.
- Первое успешное Unity Cloud WebGL свидетельство #4 относится только к Unity-ветке, не доказывает, что Rocketbox персонаж отображается в игре.
- После возобновления CHAT 03: сверить финальный `main`, сделать один реально импортированный Humanoid+Animator/LOD и измерить WebGL FPS, не подменять procedural fallback AAA-графикой.

**Контроль:** [интеграционный PR #19](https://github.com/alexvelikiy88-sketch/nonation/pull/19); главный статус и критерии выпуска — `TASKS.md` и `DEPLOYMENT_STATUS_RU.md`. Не считать этот раздел подтверждением ещё не пройденных Unity/browser проверок.


## ВОЗОБНОВЛЕНИЕ AGENT 03 — VISUAL BENCHMARK / 24.10.26
**Автор:** AGENT 03 — Graphics/UI. Этот новый раздел добавлен после исходного отчёта агента и подписанного `INTEGRATION HANDOFF - CHAT 04 / Manager`; менеджерский текст выше не изменялся.

### Управляющее требование пользователя — ОБЯЗАТЕЛЬНО

**Официальный визуальный референс — файл `Солнечный городской квартал у набережной.png`, закреплённый в «Источниках» проекта NONATION.** Он является **нижней планкой** итогового realtime Unity/WebGL visual quality для **ОДНОГО** городского vertical slice. Это **не** иллюстрация настроения и не loosely-inspired style guide.

Каноническое правило для будущих сеансов: **каждый раз заново открывать этот файл из Источников**, визуально сравнивать его с актуальным Unity/WebGL screenshot сопоставимого ракурса, публиковать таблицу соответствий и ссылку на фактический Unity SHA/build/capture. Не принимать «кубы+процедурные NPC» вместо сцены референсного уровня. Если screenshot нет — результат сравнения **«ВИЗУАЛЬНОЕ СООТВЕТСТВИЕ НЕ ПРОВЕРЕНО»**.

Создан [`Assets/MIR/Art/VERTICAL_SLICE_VISUAL_BENCHMARK_RU.md`](../../../Assets/MIR/Art/VERTICAL_SLICE_VISUAL_BENCHMARK_RU.md): обязательные Environment / Architecture / Waterfront / Characters / Lighting / Materials / UI / Performance-WebGL / Definition of Done, порядок side-by-side приёмки day/evening. Коммит [`7f77873a`](https://github.com/alexvelikiy88-sketch/nonation/commit/7f77873a46a416410bc40635269a77a8b2d98a9d).

### Факты GitHub на старте (НЕ использовать старую ветку)

- История AGENT 03 ранее принята в `main` через [PR #19](https://github.com/alexvelikiy88-sketch/nonation/pull/19); исходная `agent/graphics-ui@a6b38f6e` была строго предком актуальной `main@2f2937b7` (на 98 коммитов позади, **0 ahead**). Графическая ветка безопасно перемещена **fast-forward** к `main@2f2937b7d58c9d5b3450717a71a1c05acd271a54`, без force-push, удаления истории, трогания `TASKS.md` или менеджерского merge-результата.
- Последний опубликованный GitHub Pages билд и Chromium-скриншот для side-by-side этого source-референса в рамках AGENT 03 **не исследовались**. `TASKS.md` фиксирует более ранний browser-smoke `Missing runtime shader` и последующие Shader/editor fixes, но **открытый референс ≠ готовый Unity scene**.
- **ВИЗУАЛЬНОЕ СООТВЕТСТВИЕ НЕ ПРОВЕРЕНО**: сопоставимого актуального реального кадра Unity/WebGL нет в полученных данных этого сеанса. Code/source review не может доказать похожесть сцены на утверждённую картинку.

### Аудит разрывов с изображением из Источников NONATION

| Слой | Что есть в текущем `MirCity.cs` | Что требует benchmark | Текущий gate |
|---|---|---|---|
| Улицы | Сетка дорог, тротуары, зебра-переходы и бордюры, фонари | Правдоподобная городская ткань/ритм, велополосы и объёмные детали | **PARTIAL** |
| Архитектура | Простые корпуса `BlockBuilding`, повторяющиеся фасадные окна | 2–4 различных высококачественных семейства, витрины, балконы, карнизы, интересные крыши | **FAIL** |
| Первые этажи | Старые входы были непрозрачными Cube-дверями | Профессиональные коммерческие витрины и городская активность | **FAIL** (новый partial pass ниже) |
| Пешеходная среда | Примитивные bench/tree/lamp, рыночная площадь | Озеленение разных видов, мебель, ограждения, кафе, уличные сцены | **FAIL** |
| Набережная | Полноценной береговой композиции в исходной реализации нет | Вода, борт, общественная пешеходная набережная и ограждения | **FAIL** |
| NPC | Импортируемый Rocketbox исходник + текстуры в Art, fallback caps | Видимые живые риггированные персонажи, idle/walk/run, вариативность | **NOT VERIFIED** |
| Освещение | `UpdateLighting` меняет ambient, fog, spot-интенсивность | Реалистичный солнечный и вечерний pass, тени, качественные материалы | **NOT VERIFIED** |
| UI/камера | `MirHUD` IMGUI, `MirFollowCamera` isometric/third person | Профессиональный ненавязчивый UI и композиция camera | **NOT VERIFIED** |

Это оценка **исходного кода** относительно просмотренного концепт-арта; **не результат визуальной проверки Unity screenshot**.

### Первый ограниченный environment quality pass — фактический код

[Коммит `cc329e55`](https://github.com/alexvelikiy88-sketch/nonation/commit/cc329e55f783ada4875765a574bb1c375cb191a4): изменён **только разрешённый** `Assets/MIR/Scripts/MirCity.cs`.
- `RetailFront`: для `Grocery store` и `Corner café` на первом этаже добавлены рамные стеклянные панели, простенки, фасадная полоса, козырьки и парные кашпо. Первый этаж в этих двух зданиях больше не рендерит три жилых окна; верхние этажи сохранены.
- `Balcony`: у одного показательного 5-этажного жилого корпуса (`Residential block 2`) добавлены балконные плиты, поручни, стойки, кашпо и зелень только для верхних этажей.
- Все новые элементы — **визуальные**, без коллайдеров, освещения, физики, маршрутов или новых package dependencies; материалы реиспользуют `Mat` с instancing. Остальной город не изменён. **Применены только простые mesh primitives: это НЕ арт-качество референса**, а подготовка модульных фасадов/системы разнообразия перед импортом качественных meshes.

**Проверено статически:** в C#-патче сохранены существующие публичные сигнатуры и прочий `Build`-контракт, новые optional `retail`/`balconies` активируются только у двух коммерческих и одного жилого здания; фигурные скобки сбалансированы; не менялись пути NPC и автомобильные позиции, не добавлялись `Light`, scene/Editor/Packages. **НЕ проверено:** Unity C# compiler, реальный render, коллизии уровней, FPS, draw calls, визуальное совпадение с референсом, WebGL браузер/адаптивность.

### Следующие реальные задачи и cross-owner request

1. Сделать **настоящий** видимый hero-quarter с правдоподобными материалами и высококачественной геометрией вместо размножения Cube; добавить общественную набережную, коммерческие фасады, озеленение и транспортный/пешеходный ритм; не расширять карту, пока первый квартал не получит сравнимый screenshot.
2. [AGENT 01 / MANAGER] Для реального визуального сравнения нужен Chromium screenshot текущего WebGL SHA с фиксированной изометрической камерой, день + вечер, на том же масштабе что референс. Нет возможности объявить pass по одной успешной source/.NET проверке.
3. [AGENT 01 / issue #17](https://github.com/alexvelikiy88-sketch/nonation/issues/17): завершить Humanoid/Animator/LOD и Blender QA Rocketbox, только после этого сравнивать анимацию и NPC density.
4. Все доступные фактические CI и PR-результаты необходимо фиксировать на текущем SHA после создания нового самостоятельного AGENT 03 PR. Старый PR #2 **уже интегрирован** через менеджера; не открывать его заново и не пытаться повторно импортировать старый код в main.

**GATE:** отказ от «готово» до реального side-by-side с закреплённым **проектным Source-файлом** и отдельного доказательства WebGL rendering/performance.


### Подтверждённые автоматические проверки PR #26 — 24.10.26

На исходном SHA `3d70ad2797391671afed492a9cfd2db4a073dc89` (включены и `MirCity.cs`, и benchmark документ, и предыдущая версия этого отчёта):

- [Ownership & Change Integrity #40050786994](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40050786994) — **SUCCESS**.
- [Source preflight + .NET 8 #40050785775](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40050785775) — **SUCCESS**.
- [Cross-Branch Integration #40050785896](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40050785896) — **SUCCESS**.
- [MIR Unity WebGL #40050785811](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40050785811) — общий workflow **SUCCESS только в source-validation**; `webgl`, Cloud preview, deployment и Chromium browser QA **SKIPPED**. **Unity C# компиляция и WebGL rendering этим не доказаны**.

**Сверка с закреплённым визуальным референсом из Источников NONATION** — актуального side-by-side WebGL capture для коммита `3d70ad27` нет, итог **ВИЗУАЛЬНОЕ СООТВЕТСТВИЕ НЕ ПРОВЕРЕНО**. Следующий необходимый артефакт: сравнительный дневной+вечерний Unity screenshot, где реально можно оценить плотность объектов, витрины, дома, waterfront, людей и свет относительно **`Солнечный городской квартал у набережной.png`**.


## AGENT 03 / Quality pass 02 — реальная геометрия набережной / 24.10.26

**Источник истины:** заново открыт прикреплённый в Источниках NONATION исходный `Солнечный городской квартал у набережной.png` **1672×941, SHA-256 `aa761600852a53231ef4fee15fda43ced35dfe40f614bed569811307bc7151a8`** — соответствует `Documentation/VISUAL_CANON_RU.md`. Эталон — нижняя художественная планка для **живого Unity/WebGL кадра**, а не декоративное изображение внутри игры.

### Фактически изменённые AGENT 03 файлы

1. [`Assets/MIR/Art/Waterfront/MirWaterfrontQuarter.cs`](../../../Assets/MIR/Art/Waterfront/MirWaterfrontQuarter.cs), коммиты [`e718f3fa`](https://github.com/alexvelikiy88-sketch/nonation/commit/e718f3fad1b5f5118a38f57bb68010c98d901dd8), [`111486d4`](https://github.com/alexvelikiy88-sketch/nonation/commit/111486d4bc4f7d3805fc728e13c20c70a7df666b), [`77b8207f`](https://github.com/alexvelikiy88-sketch/nonation/commit/77b8207f9432c2d61036a1259785bb35b45b1eb6) — новый **runtime 3D** геометрический модуль набережной. Включает сухой общедоступный настил 188×20 м `z=-108..-128`, воду за границей исходного `ground` `z=-130..-181`, волну как небольшую статичную деформацию водной сетки, наглядную береговую стенку, трубчатые металлические ограждения, велополосу, деревья с объёмными кронами, кустарники/кашпо, деревянные скамьи и два настоящих геометрических кафе-зонта. Ровно **12 групп Renderer по материалам** вместо тысячи отдельных Tile GameObjects; один длинный физический барьер у воды + два walkable BoxCollider. **4 локальных shadowless светильника** привязаны к существующему `MirCity.UpdateLighting` (выкл. днём; вкл. ночью). Сохранены действующие gameplay POI.
2. [`Assets/MIR/Scripts/MirCity.cs`](../../../Assets/MIR/Scripts/MirCity.cs), коммиты [`76364272`](https://github.com/alexvelikiy88-sketch/nonation/commit/76364272c0c53bd536206b7f26e9e3ef7b958f25), [`f975053a`](https://github.com/alexvelikiy88-sketch/nonation/commit/f975053a79aec54e6457e6aeb7f27a3afb75eed6) — один интеграционный вызов `MirWaterfrontQuarter.Build(root,Mat,lamps.Add)`, без изменений API BusinessEntry/HomeEntry, маршрутов NPC и физики транспорта.
3. [`Assets/MIR/Scripts/MirFollowCamera.cs`](../../../Assets/MIR/Scripts/MirFollowCamera.cs), [`2908b8a9`](https://github.com/alexvelikiy88-sketch/nonation/commit/2908b8a94b994f4bcf1eb6e66b1b3798b5cc5b2a) — `V` включит повторяемый фиксированный `DistrictShowcase` без монтажа `focus=(20,1.8,-48)`, `pitch=51`, `yaw=40`, `distance=175`, `FOV=52`; **Tab** вернёт текущие игровые виды. Статическая геометрическая проверка проекции показала plaza, cafe, market и восточную береговую линию в кадре 16:9, **но не доказывает реального Unity рендера**.
4. [`Assets/MIR/Scripts/MirHUD.cs`](../../../Assets/MIR/Scripts/MirHUD.cs), [`d064c31b`](https://github.com/alexvelikiy88-sketch/nonation/commit/d064c31b27a8691563a7a6250c01888d7a7b037c) — подсказка клавиши `V` в существующем GUI, без смены экономических кнопок.
5. [`Assets/MIR/Art/Waterfront/VISUAL_SLICE_RU.md`](../../../Assets/MIR/Art/Waterfront/VISUAL_SLICE_RU.md), [`8b53cfd5`](https://github.com/alexvelikiy88-sketch/nonation/commit/8b53cfd5caa85328c0f835f0baf474899466d22c) — технологическая карта, координаты и обязательный side-by-side со **свежим чтением файла из Источников**, условия WebGL QA.

**Фактические неигровые зависимости не добавлены:** нет новых DLL, сторонних 3D-ассетов с неизвестной лицензией, UnityEditor в runtime, чужих сцен, `Packages`, `MirGame.cs`, изменений `TASKS.md` или Backend. Вода/растительность/патио — новая реальная `Mesh`-геометрия и коллайдеры, не вставленная картинка, но без качественных PBR карт и авторских зданий **это ещё не визуальная приёмка**.

### Сверка с закреплённым визуальным референсом из Источников NONATION

| Категория | Факт исходников / прогресс | Художественный критерий | Итог |
|---|---|---|---|
| Перекрёстки и площадь | Существующая road grid и фонтан остаются | Точность пропорций/плотности относительно PNG | **ЧАСТИЧНО, скрин не получен** |
| Реальная набережная | Добавлены 3D вода, настил, стенка, ограждение, скамейки, деревья | Богатая набережная на переднем плане, реалистичная вода и узнаваемые малые формы | **PARTIAL** |
| Кафе и фасады | Существующие витрины + новое патио с двумя зонтами | Настоящие интерьеры, премиальные фасады, 3–5 архитектурных семейств | **НЕ ДОСТИГНУТО** |
| Персонажи | Rocketbox FBX и TGA в Art, но Unity Avatar/skin/Animator/LOD не подтверждены | Не менее одного проверенного skinned Humanoid и живые NPC без капсул | **НЕ ДОСТИГНУТО** |
| Свет | 4 ночных shadowless светильника + старый ambient/fog/sun | Качественные реалистичные день/вечер тени/материалы | **НЕ ПРОВЕРЕНО** |
| UI | Подсказка V + прежний IMGUI HUD | Современный UI без технодемки | **НЕ ДОСТИГНУТО** |
| Производительность | Поверхности сгруппированы по 12 материалам; нет runtime wave update | Реальный WebGL p50/p95 FPS, draw calls, VRAM | **НЕ ИЗМЕРЕНО** |

**ВИЗУАЛЬНОЕ СООТВЕТСТВИЕ НЕ ПРОВЕРЕНО**: после изменений нет неприкосновенного `Unity WebGL/Chromium` снимка с тем же ракурсом и SHA. Геометрический проверочный код — не визуальная приёмка. Прежние здания ещё примитивны. Финальный `VISUAL GATE` из `VISUAL_CANON_RU.md` остаётся **FAIL/BLOCKING**.

### Верификация и ограничения

- **Статический технический аудит четырёх изменённых/новых C#**: все фигурные скобки уравновешены (с учётом строк и комментариев), сохранены `MirCity.Build/SidewalkRoute/UpdateLighting`, не изменены business ID/POI, фасадные объекты/люди не подменены, нет API `UnityEditor`; корректное winding основных горизонтальных поверхностей и зонтов проверено геометрически; новые светильники регистрируются в ночном переключателе. Это **не** полноценный компилятор Unity и не Unity Play Mode.
- [PR #26 / source-/.NET на SHA `8b53cfd5`](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40051669059) — **SUCCESS**, [Cross-Branch Integration #40051669068](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40051669068) — **SUCCESS**, [Ownership Guard #40051669062](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40051669062) — **SUCCESS**.
- [MIR Unity WebGL #40051669058](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40051669058) — только source-validation SUCCESS; **webgl/Cloud/Pages/Chromium jobs SKIPPED**. Нет Unity C# compilation, реальной сцены, GPU FPS, QA коллайдеров.
- После этих CI тестов был обновлён только отчёт, не production-код. Никаких измеренных frame time, draw calls, triangles, памяти или качества шейдеров не заявляется.

### Cross-owner — запрошено и доступно

[**Issue #28**](https://github.com/alexvelikiy88-sketch/nonation/issues/28) просит AGENT 01/Manager снять настоящий дневной+вечерний 1920×1080 Chromium V-кадр без редактирования с WebGL SHA/URL/GPU/FPS и проверить player/NPC/cafe/market/сохранения, границу воды и viewport. Художественный PASS требует открывать исходный PNG из Источников перед *каждой* проверкой.

**Следующая собственная задача:** заменить оставшиеся примитивные `BlockBuilding` архитектурными фасадами/текстурами, не касаясь POI/Backend; затем отдельный качественный NPC/Animator и HUD pass. Нельзя масштабировать город до художественного утверждения одного квартала.


### Небольшое UI-дополнение — 24.10.26

[Коммит `b7068306`](https://github.com/alexvelikiy88-sketch/nonation/commit/b7068306a7f42b6cf254a76aa1cc8228e7cff104) — `Assets/MIR/Scripts/MirHUD.cs`: введён обратимый **H — компактный HUD**. Полная панель остаётся режимом по умолчанию, в том числе со всеми экономическими действиями; при H рисуется только небольшая плашка NONATION (день, число жителей) и кнопка `Панель` для возврата. Мир, физика, анимация, время и ввод камеры **не выключаются**, поэтому это не обманная «чистая картинка». [Коммит `1dafb673`](https://github.com/alexvelikiy88-sketch/nonation/commit/1dafb673a798b3b3ddd83f72604107531fc85a06) дополняет инструкцию камеры/компактного HUD. Responsive layout, интерфейс игрока на touch и профессиональная UI-приёмка **не проверены**.

**Приёмка по Source-референсу остаётся открытой:** инженерный 3D waterfront + H/V режимы повышают проверяемость и насыщенность, но не заменяют полноценные PBR здания, rigged NPC и живой Chromium screenshot. Реальное качество игры не объявлено достигнутым.


### CI текущего gameplay/art-кода — 24.10.26

После добавления компактного HUD и синхронизации с новым защищённым `main`, исходный C# код и собственная документация находились в `agent/graphics-ui@e6016e802439b5e0aa69ce034a8da2ff10393c10`. GitHub Actions на этом коде:

- [Ownership Guard #40052081842](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40052081842) — **SUCCESS**.
- [Source/.NET #40052081873](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40052081873) — **SUCCESS**.
- [Cross-Branch Integration #40052081982](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40052081982) — **SUCCESS**.
- [MIR Unity WebGL #40052081958](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40052081958) — только **source-validation SUCCESS**, настоящий `webgl`, `Cloud`, `deploy`, `browser smoke` отмечены **SKIPPED**.

Это доказывает владельческие границы и статический/source/.NET контур, **но НЕ доказывает Unity C# compilation, отражения/качества воды, анимаций, действительных draw calls, FPS и соответствия закреплённому в Источниках PNG**. [Issue #28](https://github.com/alexvelikiy88-sketch/nonation/issues/28) — точный запрос на браузерные кадры/метрики AGENT 01/Manager. Следующий результат AGENT 03 должен содержать реальное сравнение, иначе **VISUAL GATE всё ещё не принят**.


## VIS-02 / Architecture Rebuild — фундаментальная замена графики квартала, 24.10.26

### Сверка с закреплённым визуальным референсом из Источников NONATION

Официальный файл `Солнечный городской квартал у набережной.png` из Источников NONATION прочитан непосредственно. Разрешение **1672×941**, канонический **SHA-256 `aa761600852a53231ef4fee15fda43ced35dfe40f614bed569811307bc7151a8`** подтверждён расчётом по оригинальному PNG. Временная копия `(1).png` имеет такие же RGB-пиксели, но иной PNG-хеш из-за RGBA-формата; нормативный источник — оригинал без суффикса. Сравнивать нужно каждый новый **настоящий** Unity WebGL кадр с оригинальным изображением. **Сейчас ВИЗУАЛЬНОЕ СООТВЕТСТВИЕ НЕ ПРОВЕРЕНО**: пригодного Chromium WebGL-кадра на новом SHA нет.

**Почему фундаментальная замена:** старый `MirCity.Build()` создавал 12 зданий через `BlockBuilding` (кубические монолиты с наложенными одинаковыми окнами) и `MarketSquare` (плита + примитивные цилиндры). Эти вызовы в работающем `Build` заменены двумя новыми архитектурными модулями. Старые методы оставлены вне runtime-вызова для безопасного rollback, но новый город уже выбирает новые меши, а не прежние фасадные кубы.

### Рабочие коммиты и ассеты (разрешённые пути AGENT 03)

- [`f305b96e`](https://github.com/alexvelikiy88-sketch/nonation/commit/f305b96ef9c4fb2c4ee81daaf1e4e379b7ff2597) — **`Assets/MIR/Art/Architecture/MirArchitectureQuarter.cs`**, новая авторская система модульной архитектуры. Все **12** прежних зданий сохранены на точных исходных координатах и с прежними внешними `BoxCollider`; четыре архитектурных семейства (кирпич, светлый известняк, минеральная облицовка, дерево), модули реальных открытых передних проёмов/глубинных рам, сегментированные оконные стены, объёмные витрины и мерчандайзинг, скамейки/кашпо у коммерции, балконы, кровельные перголы, террасы/парапеты и 3D-вывески. Не изменяются POI/игровые пути и `MirWorldGeometry`. Набор геометрических элементов объединён в `Mesh` по материалам, **не отдельный рендерер на каждый элемент**.
- [`ef00446e`](https://github.com/alexvelikiy88-sketch/nonation/commit/ef00446ecb0c326f3285db59f846309de4c3c0aa) — `MirCity.cs` больше не вызывает четыре общественных и восемь жилых `BlockBuilding`, вместо них `MirArchitectureQuarter.Build(root)`. Коммерческие `market/cafe/garage/town`, остальные бизнесы, `MirWorldGeometry.HomeEntry`, машины, существующая набережная и сценарии NPC остались без изменений.
- [`5ccdc4ac`](https://github.com/alexvelikiy88-sketch/nonation/commit/5ccdc4ac3fd437eb2455bcc654cc45b937277741) и [`8baedbf7`](https://github.com/alexvelikiy88-sketch/nonation/commit/8baedbf7166b61c76ece9ab5a37c0cbcc18b8c53) — исправлена авторская линейная 3D-типографика вывесок (13 сегментов), обеспечена цветовая текстура кирпича/камня/штукатурки/дерева и корректный резервный tint при отсутствии шейдера. Цветовые карты генерируются один раз в **256×256** с mipmaps, tileable world-space UV; это **собственный ограниченный procedurally-authored albedo**, а не готовые high-res PBR текстуры из внешнего пакета.
- [`19a72db4`](https://github.com/alexvelikiy88-sketch/nonation/commit/19a72db499cc55cf71d6c853f411c84f4b4f81c5), [`10cb390c`](https://github.com/alexvelikiy88-sketch/nonation/commit/10cb390c8b630da6b55d34386f1f270c0687c300), [`ac0519b3`](https://github.com/alexvelikiy88-sketch/nonation/commit/ac0519b3070b8e2d14cd74b389b3d462268e3fed) — **`Assets/MIR/Art/Architecture/Resources/Nonation/Architecture/ArchitecturalLit.shader`**, собственный URP-compatible lit+textured shader с встроенным pipeline fallback, directional diffuse/specular, fog, shadow caster/pass, Resources-backed для гарантированной доступности в player при корректном импорте. **Это облегчённый shader, не фотоподобный PBR/Lit**. Не тестировался через настоящий Unity Shader Compiler; возможны исправления у AGENT 01 при появлении Unity compile/log. `Packages/manifest.json` в `main` указывает URP 17.0.3; Packages и Editor файлы нами не изменялись.
- [`5b8043b4`](https://github.com/alexvelikiy88-sketch/nonation/commit/5b8043b4018afcdcc49bf81a82d7eb61ae395f58), [`f5168f5c`](https://github.com/alexvelikiy88-sketch/nonation/commit/f5168f5c2a2c2f768aa0fe5336a92914cb1e14a8), [`49b75a49`](https://github.com/alexvelikiy88-sketch/nonation/commit/49b75a49e08da086fedd209020f6f720c62efa3f), [`ba9c0d62`](https://github.com/alexvelikiy88-sketch/nonation/commit/ba9c0d62fb5c773be149ab8f45e1c55328161707) — **`Assets/MIR/Art/Architecture/MirCivicSquare.cs`**, прежняя demo-площадь заменена настоящей 3D-геометрией: 27×27 м, 324 плашки мощения в общих мешах, многоуровневый круглый фонтан с водой, непрерывный золотой N-знак, рассадники/деревянные лавки/уличная мебель, два shadowless светильника; два упрощённых физических коллайдера. Старый `MarketSquare()` больше не вызывается.
- [`6bd1ee09`](https://github.com/alexvelikiy88-sketch/nonation/commit/6bd1ee0926e258e9600b0cc11ff480fcb75a97f8) — [`Assets/MIR/Art/Architecture/ARCHITECTURE_REBUILD_RU.md`](../../../Assets/MIR/Art/Architecture/ARCHITECTURE_REBUILD_RU.md), канон сверки, модули, лицензии, блокеры и QA.

### Что прошло проверку

**Статический осмотр нового и существующего C# + shader: 18/18 source-инвариантов выполнены** на промежуточном графическом SHA, включая сбалансированные скобки и круглые скобки (строки/комментарии исключены), ровно один вызов новой архитектуры и новой площади, старые `BlockBuilding()/MarketSquare()` вне `Build`, целостность всех восьми business ID, сохранение home/vehicle entry, отсутствие `UnityEditor` в runtime, наличие реальных Mesh.SetVertices/Triangles, URP shader в owned Resources, existing camera/waterfront, никакой подмены кадра. Это **не C# компилятор Unity**.

- [Cross-Branch Integration #40054683205](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40054683205) — SUCCESS на промежуточном коде c городом/площадью.
- [Source/.NET #40054683157](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40054683157) — SUCCESS.
- [Ownership Guard #40054683226](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40054683226) — SUCCESS.
- В более поздних исходных коммитах были только документы и шейдер/материал-совместимость; актуальные CI на последнем SHA нужно проверять отдельно.
- **WebGL workflow SUCCESS может означать только `source-validation`** — Unity Shader Compiler, активный рендер, scene, Chromium и GPU/FPS не подтверждены этим кодовым этапом.

### VISUAL GATE — реальная оценка по эталону

| Слой | Сейчас в исходниках | Статус художественной приёмки |
|---|---|---|
| 12 зданий и коммерция | Новые отдельные рельефные фасады/витрины/крыши/террасы | **ПРОГРЕСС, НЕ ПРОВЕРЕНО в WebGL** |
| Центр квартала | Полноценная геометрическая plaza и фонтан | **ПРОГРЕСС, НЕ ПРОВЕРЕНО в WebGL** |
| Набережная | Модуль `MirWaterfrontQuarter` остаётся | **НЕ ПРОВЕРЕНО в WebGL** |
| Люди | Rocketbox FBX+MIT текстуры в Art, но не готовый Humanoid Animator/LOD | **НЕ ДОСТИГНУТО** |
| Свет/PBR | Облегчённый URP лит-шейдер, 256×256 authored pattern | **НЕ ДОСТИГНУТО** до реальных материалов/освещения |
| Транспорт/NPC | Исходная игровая логика и консервативный рендер сохранены | **НЕТ живого доказательства** |
| HUD/камера | Предыдущие H/V режимы сохранены | **НЕ ПРОВЕРЕНО в браузере** |
| FPS/draw calls/tris/memory | Нет фактических Unity GPU/браузерных замеров | **НЕ ИЗМЕРЕНО** |

Соблюдено `Documentation/VISUAL_CANON_RU.md`: **новый архитектурный код не означает, что сцена не хуже оригинального референса**. Старые промышленные объекты и части улиц ещё примитивны, персонажи не подтверждены, авторские 256² текстуры — промежуточные; схожесть композиции требуется подтвердить **настоящим кадром Chromium** и явным владельческим review.

### CROSS-OWNER REQUEST / ближайший внешний gate

[Issue #28](https://github.com/alexvelikiy88-sketch/nonation/issues/28) для AGENT 01/Manager требуется дополнить: построить/запустить **preview Unity/WebGL именно текущей графической ветки**, проверить компиляцию `MirArchitectureQuarter.cs`, `MirCivicSquare.cs`, `ArchitecturalLit.shader` и import свойства `Resources`, затем непрепарированные 16:9 скриншоты V day/evening и реальный p50/p95 frame time/FPS/draw calls/triangles/memory. Плюс прочность коллайдеров и gameplay regression. До этих доказательств PR 26 остаётся **Draft**. Файлы AGENT 01/02/Manager, `TASKS.md` и защищённый `main` не изменялись.

**Следующая наша визуальная задача:** после реального кадрового proof — импорт более качественных лицензированных PBR текстур/нормалей, усиление дороги и зелени, затем один действительно риггированный и анимированный humanoid. Только после этого допускается выпуск новых вариантов NPC и расширение карты.
