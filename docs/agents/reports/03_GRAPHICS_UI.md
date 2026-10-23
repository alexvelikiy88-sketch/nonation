# NONATION / отчёт 03 — Graphics/UI

- **Последнее обновление:** 23.10.26
- **Статус:** IN PROGRESS — аудит лицензий, исправление locomotion и диагностическая готовность импортера (без 3D-ассета)
- **Ветка:** `agent/graphics-ui`
- **Промпт:** [Инструкция исполнителя](../CHAT_03_GRAPHICS_UI.md)
- **Исходная база main/ветки при входе:** `f52d21a2b0e470bb79664f8be0e6fd0251b82596` (ветки идентичны; открытых PR не было)
- **Собственный коммит патча:** [`3f2e733d`](https://github.com/alexvelikiy88-sketch/nonation/commit/3f2e733d1c9c1dc178d3e9a1660355c06ee931b6)
- **PR:** [#2 Graphics — Draft](https://github.com/alexvelikiy88-sketch/nonation/pull/2), без merge

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
- До получения реального CC0 core MakeHuman экспорта невозможно измерить реальные polygons/textures/LOD/weights и устранить «ходящих коробок» по художественным требованиям. Детали и checklist — `CHARACTER_PILOT_RU.md`.

## Журнал

| Дата | Работа | Коммит / PR | Проверки / ограничения |
|---|---|---|---|
| 23.10.26 | Начальная карточка исполнительного чата | — | Работа не была начата |
| 23.10.26 | Инвентаризация GitHub и safety gate персонажа | [3f2e733d](https://github.com/alexvelikiy88-sketch/nonation/commit/3f2e733d1c9c1dc178d3e9a1660355c06ee931b6) | Анализ исходников; Unity/WebGL не проверены |
| 23.10.26 | Перенастройка locomotion, caching материала, runtime-диагностика, официальный аудит лицензии, контракт пилота | `60937d8`, `fa4a64f`, `b5f510d`, `a3d508b` / PR #2 | Статический анализ; Unity/WebGL, FPS, импорт модели НЕ проверены |

**CI-контроль 23.10.26:** PR #2 HEAD `91888444` — [run 40005479208](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40005479208), source/.NET success; Unity WebGL remains unverified.

## CROSS-OWNER REQUEST

1. **AGENT 01:** `Assets/MIR/Editor/MirUnitySetup.cs` — текущий `Get(all,"Idle_Loop",...,"A_TPose")` может использовать T-pose как idle; убрать T-pose в качестве fallback и отказывать в создании locomotion при неполных Idle/Walk/Run. Также необходим Editor audit созданных Avatar/skin/weights/материалов/LOD и связь с фактическим WebGL билдом; меняет AGENT 01 или Manager.
2. **AGENT 01:** `Assets/MIR/Editor/MirAssetImport.cs` и `MirHumanoidFromGltf.cs` — при появлении лицензированного реалистичного ассета определить повторяемый importer/Avatar/prefab/LOD pipeline с явными проверками. Не импортировать непроверенные модели в `Resources`.

3. **AGENT 01:** `Assets/MIR/Editor/MirUnitySetup.cs`, `Assets/MIR/Editor/MirAssetImport.cs` — по контракту `Assets/MIR/Art/CHARACTER_PILOT_RU.md` подготовить отдельный Editor-only validation для реального CC0 MakeHuman FBX (Avatar/weights/materials/real LODs/clip assignment). Результат — только после фактического получения модели; никаких инъекций в build без license manifest.

## Следующее конкретное действие

После успешной Unity WebGL базовой сборки: выбрать один документированно лицензируемый humanoid asset, сохранить исходник/скан лицензии и контрольную сумму, проверить в Unity Editor valid Avatar, skinned weights, Idle/Walk/Run и transitions, отсутствие T-pose и missing textures; затем замерить WebGL FPS/draw calls/полигоны на конкретной платформе. Отдельным PR заняться городом и UI, не смешивая с исправлением Cloud Build.
