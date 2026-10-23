# NONATION / отчёт 03 — Graphics/UI

- **Последнее обновление:** 23.10.26
- **Статус:** IN PROGRESS — исходный реалистичный Rocketbox MIT FBX получен и проверен по Git blob; готовые prefab, текстуры и WebGL пока отсутствуют
- **Ветка:** `agent/graphics-ui`
- **Промпт:** [Инструкция исполнителя](../CHAT_03_GRAPHICS_UI.md)
- **Исходная база main/ветки при входе:** `f52d21a2b0e470bb79664f8be0e6fd0251b82596` (ветки идентичны; открытых PR не было)
- **Собственный коммит патча:** [`3f2e733d`](https://github.com/alexvelikiy88-sketch/nonation/commit/3f2e733d1c9c1dc178d3e9a1660355c06ee931b6)
- **PR:** [#2 Graphics — Draft](https://github.com/alexvelikiy88-sketch/nonation/pull/2), без merge; Integration Guard требует asset-license evidence и актуальную базу main

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

**Текстуры ещё отсутствуют в GitHub:** оригинальный FBX ссылается на 6 TGA Microsoft `m007_body/head_{color,normal,specular}.tga`, исходные размеры по GitHub tree — по 12 582 956 байт, ~72 МиБ вместе. Не добавляем тяжёлые TGA в git без WebGL оптимизации.

Коммиты [`77fdec16`](https://github.com/alexvelikiy88-sketch/nonation/commit/77fdec1675108b0dce4a2157059e0f72bf95d762) и [`1a36c4d4`](https://github.com/alexvelikiy88-sketch/nonation/commit/1a36c4d455fb42623c6def9e1cd7ce57268fbfa9) добавили `fetch_textures.py` (immutable HTTPS URL, upstream Git SHA-1 и проверка размера, 32-bit TGA downsample, сохранение локально) и `test_fetch_textures.py`.

**Достоверные локальные тесты скрипта:** `python3 -m unittest discover -s /mnt/data/rocketbox_work -p 'test_*.py' -v` — **4/4 PASS** (synthetic TGA, SHA known vector, downsizing BGRA/alpha/orientation, fail-closed на повреждённый/неподдерживаемый TGA). `--dry-run` выводит шесть pinned hash; оба GitHub Python blob SHA побайтно совпали с прошедшими эти тесты локальными файлами. **Не подтверждены:** реальные скачивания шести TGA (сетевой backend не проверен), реальная конверсия этих TGA, Unity Material assignment, текстурные артефакты и WebGL performance. Новая фиксация `PILOT_RU.md` и обновление главного контракта `CHARACTER_PILOT_RU.md`: `c51e6c3`, `c29e1ca`.

**Ключевой принцип:** файл FBX в `Art/`, а не в `Resources/` или игровой сцене; пока `MirHumanFactory` не активирует новую модель и существующая WebGL-цепочка её не включает. Не выдавать «файл в Git» за работающего/анимированного персонажа.

### Новая CI-проверка и владение

Параллельно Manager добавил `NONATION Integration Guard`: [run 40007885901](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40007885901) до синхронизации показал два явных требования: `ASSET-LICENSE-EVIDENCE:` в PR (для FBX и PNG) и отсутствие устаревшей версии `TASKS.md` при прямом сравнении PR base с head. **Исправлено:** соответствующее точное доказательство Microsoft MIT добавлено в текст [PR #2](https://github.com/alexvelikiy88-sketch/nonation/pull/2), ветка `main` безопасно влита в `agent/graphics-ui` коммитом [`063a26d2`](https://github.com/alexvelikiy88-sketch/nonation/commit/063a26d2a5c24db502baafa51e0686ec1a5ad164). Сравнение до слияния показало **0 общих изменённых путей**, новый diff `agent/graphics-ui→main` содержит **только 10 разрешённых файлов AGENT 03**, `TASKS.md` и чужие файлы не менялись нами. Результат нового Integration Guard на последнем SHA требуется перепроверить до review.

**CI исходников:** [run 40007822540](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40007822540) = source/.NET checks SUCCESS на коммите `c51e6c3` (не настоящая Unity сборка). Реальный Unity WebGL Player export, standalone Unity C# compilation, Animator import/retarget, UI/город, draw calls/FPS **НЕ проверены**.

### Обновлённый CROSS-OWNER REQUEST

- **AGENT 01 — `Assets/MIR/Editor/MirAssetImport.cs`, `MirUnitySetup.cs` и при необходимости собственный новый Editor script:** для `Assets/MIR/Art/Rocketbox/Male_Adult_12.fbx` настроить ModelImporter Humanoid с проверкой `Avatar.isValid`/`isHuman`, `SkinnedMeshRenderer`/boneWeights/bindposes, материалы и шесть локально полученных TGA с проверкой альфа/normal map, использовать Quaternius CC0 для Idle/Walk/Run через retarget, создать `Resources/MIR/PilotHuman01.prefab` только после Editor QA; выделить 4 реальные LOD меша и настроить `LODGroup`. Пока текстур нет и реальные clip links не проверены, не подключать prefab к release WebGL.
- **AGENT 02 / Manager — `Assets/MIR/Scripts/MirState.cs` и world default look:** лишь **после** принятого Unity prefab и проходящего WebGL smoke-test согласовать opt-in отображение `PilotHuman01` (например `MirCitizen.look`), не менять данные мира в этом PR.

## Журнал

| Дата | Работа | Коммит / PR | Проверки / ограничения |
|---|---|---|---|
| 23.10.26 | Начальная карточка исполнительного чата | — | Работа не была начата |
| 23.10.26 | Инвентаризация GitHub и safety gate персонажа | [3f2e733d](https://github.com/alexvelikiy88-sketch/nonation/commit/3f2e733d1c9c1dc178d3e9a1660355c06ee931b6) | Анализ исходников; Unity/WebGL не проверены |
| 23.10.26 | Перенастройка locomotion, caching материала, runtime-диагностика, официальный аудит лицензии, контракт пилота | `60937d8`, `fa4a64f`, `b5f510d`, `a3d508b` / PR #2 | Статический анализ; Unity/WebGL, FPS, импорт модели НЕ проверены |
| 23.10.26 | MIT Microsoft Rocketbox исходный FBX/PNG, license provenance, проверяемый fetch текстур и 4 синтетических offline теста | `269d77e`, `77fdec1`, `1a36c4d`, `c51e6c3`, `c29e1ca`, merge `063a26d`; PR #2 Draft | FBX blob идентичен upstream, 4/4 Python unittest PASS; текстуры/Unity/WebGL не проверены |

**CI-контроль 23.10.26:** PR #2 HEAD `91888444` — [run 40005479208](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40005479208), source/.NET success; Unity WebGL remains unverified.

## CROSS-OWNER REQUEST

1. **AGENT 01:** `Assets/MIR/Editor/MirUnitySetup.cs` — текущий `Get(all,"Idle_Loop",...,"A_TPose")` может использовать T-pose как idle; убрать T-pose в качестве fallback и отказывать в создании locomotion при неполных Idle/Walk/Run. Также необходим Editor audit созданных Avatar/skin/weights/материалов/LOD и связь с фактическим WebGL билдом; меняет AGENT 01 или Manager.
2. **AGENT 01:** `Assets/MIR/Editor/MirAssetImport.cs` и `MirHumanoidFromGltf.cs` — при появлении лицензированного реалистичного ассета определить повторяемый importer/Avatar/prefab/LOD pipeline с явными проверками. Не импортировать непроверенные модели в `Resources`.

3. **AGENT 01:** `Assets/MIR/Editor/MirUnitySetup.cs`, `Assets/MIR/Editor/MirAssetImport.cs` — по `Assets/MIR/Art/Rocketbox/PILOT_RU.md` подготовить Editor validation для реально полученного **MIT Rocketbox `Male_Adult_12.fbx`** (Avatar/weights/materials/4 LOD/clip assignment). Не подменять CC0 MakeHuman альтернативой текущий пилот без отдельной задачи.

## Следующее конкретное действие

После успешной Unity WebGL базовой сборки: выбрать один документированно лицензируемый humanoid asset, сохранить исходник/скан лицензии и контрольную сумму, проверить в Unity Editor valid Avatar, skinned weights, Idle/Walk/Run и transitions, отсутствие T-pose и missing textures; затем замерить WebGL FPS/draw calls/полигоны на конкретной платформе. Отдельным PR заняться городом и UI, не смешивая с исправлением Cloud Build.
