# NONATION — VIS-03: настоящие люди вместо процедурных заглушек

**Дата:** 24.10.26 · AGENT 03 / Graphics · **Приоритет P0: люди раньше фасадов.**

## Непосредственный визуальный эталон

В Источниках проекта закреплён официальный `Солнечный городской квартал у набережной.png` (оригинальный SHA-256 `aa761600852a53231ef4fee15fda43ced35dfe40f614bed569811307bc7151a8`). Минимальный критерий: **анатомически правдоподобные объёмные люди** с настоящей скелетной моделью, кожей/одеждой, корректным Idle/Walk/Run и без T-поз и капсул в принятом WebGL кадре. Сравнивать оригинальное изображение из Источников со свежим реальным скриншотом игры, а не с текстовой спецификацией.

## Источники в GitHub

- **3D модель:** реальный оригинальный Microsoft Rocketbox `Male_Adult_12.fbx`, `Assets/MIR/Art/Rocketbox/Male_Adult_12.fbx`, 487776 байт, Git blob SHA-1 `15d4f8501d5703600cb3d8a94765a3de1029aaf6`. FBX имеет 1 настоящую Skinned Geometry, 80 skin clusters/80 LimbNodes и две material zones: `m007_body`, `m007_head`. Это не ProceduralHuman.
- **Текстуры:** шесть проверенных 1024×1024 оригинальных MIT-производных body/head albedo/normal/specular TGA, `Assets/MIR/Art/Rocketbox/Textures/`, шесть SHA-256 pinned в `Editor/MirRocketboxPilotBuild.cs` и в `Textures/textures_manifest.json`. SHA источника, размера и каждой текстуры проверяются **при подготовке Unity prefab**, любые изменения блокируют.
- **Лицензия:** Microsoft Rocketbox, MIT, Copyright (c) 2020 Microsoft; оригинальная `LICENSE.md` скопирована из официального upstream commit `0943055db6ec570bcef9f2c8b41c9e5467c808f9`, Git blob `9bcfb3ece5301a55d3a41bbd00a029ab27d61d13`.
- **Анимации:** Quaternius Universal Animation Library 1/2, CC0. При реальной Unity-сборке `Tools/fetch_quaternius.py` скачивает/проверяет FBX по SHA-256, `MirAssetImport` (AGENT01) импортирует Quaternius как Humanoid. Rocketbox-builder ищет строго содержательные `Idle_Loop`, `Walk_Loop` / `Walk_Formal_Loop`, `Jog_Fwd_Loop` / `Sprint_Loop`, **НЕ** берёт произвольный первый клип или `TPose`.

## Реальный генератор prefab в Unity Editor

[**`Assets/MIR/Art/Rocketbox/Editor/MirRocketboxPilotBuild.cs`**](Editor/MirRocketboxPilotBuild.cs) — отдельный код только в разрешённом каталоге AGENT 03. Он:
1. Привязывает оригинальный ModelImporter к `Human` + `CreateFromThisModel`, оставляет реальный скелет, векторные веса и оригинальные слоты двух материалов.
2. Настраивает importer TGA: обычный albedo sRGB, normal maps с корректным типом и без повторного получения нормалей из альбедо, specular linear, 1024+ mipmaps.
3. Проверяет `Avatar.isValid && Avatar.isHuman`, **один реальный SkinnedMeshRenderer**, не меньше 40 доступных костей, согласованные bindposes и **имена обоих source-material slots**. Ни одной подмены generic статическим мешем.
4. Создаёт два настоящих Unity `.mat` c URP/Lit (fallback Standard): body/head, 3 карты на материал; без редакторского фейкового скриншота.
5. Создаёт настоящий `AnimatorController` с параметром float `Speed` и `Locomotion` (BlendTree строго `Idle=0`, `Walk=.42`, `Run=1`) из CC0 FBX. Root Motion отключён, перемещение управляется существующим NPC/player кодом.
6. Сохраняет `Assets/MIR/Art/Rocketbox/Resources/MIR/RocketboxPilot01.prefab`. Unity автоматически включает `Resources` prefab с моделью, материалами и Animator в WebGL player, если все проверки прошли.
7. Если любой источник/rig/клип/skin/material невалиден — **prefab удаляется/не создаётся**, лог `NONATION_ROCKETBOX_BLOCKED`, существующий технический fallback остаётся только аварийной диагностикой. Успех логирует `NONATION_ROCKETBOX_READY` с фактическими `vertices/bones/humanAvatar`. На первом действительном появлении в игре `MirHumanFactory` пишет `NONATION_REAL_HUMAN_RUNTIME_OK` с настоящим количеством вершин/костей — проверяемая сигнатура для Chromium smoke.

`MirHumanFactory.cs` в `Assets/MIR/Scripts` теперь при создании игрока и NPC загружает **в первую очередь** `MIR/RocketboxPilot01`. Если он невалиден, можно использовать любой другой проверенный Humanoid, и только затем старый диагностический proxy. Никаких изменений в экономике, маршрутах, именных данных персонажей, базовом Unity-проекте.

## Воспроизводимая процедура для AGENT 01 / Manager

**В репозитории сейчас лежит исходный FBX и TGA; серилизованный `.prefab` генерируется при выполнении Unity Editor, и его наличие НЕ подтверждено.**

Открыть project checkout **PR #26 / `agent/graphics-ui`** в настоящем Unity **6000.0.58f2** с WebGL Build Support. Предварительно:

```bash
python3 Tools/fetch_quaternius.py
```

После импорта Unity и скриптов:
- Меню **`NONATION → Graphics → 01 — Build real Rocketbox humanoid`**. При наличии CC0 FBX генерация также выполняется при запуске Editor и перед WebGL player build (`IPreprocessBuildWithReport`, не изменяя AGENT 01 build code).
- Проверить Unity Console: `NONATION_ROCKETBOX_READY ... avatarHuman=True ...` (лог печатает `avatarHuman=True/False` формат Unity C#); при `NONATION_ROCKETBOX_BLOCKED` снять точное первое сообщение, не маскировать.
- Перейти в Scene Play: игрок и NPC должны иметь **настоящий `SkinnedMeshRenderer`**, тело/голову с отдельными материалами, реальные Idle/Walk/Run переходы; в Console должен появиться `NONATION_REAL_HUMAN_RUNTIME_OK`. После движений снять скриншоты крупно, проверка рук/коленей/стоп, направлений взгляда, foot slide/ground contact и отсутствие красно-розового shader.
- Сборка/Chromium preview **именно этого SHA** должна содержать реального человека. Проверить 16:9 из V-камеры с правильной долей в общем кадре, паспорт GPU/browser, FPS/draw calls/tris/memory.
- Возобновлять масштабирование на нескольких настоящих мужчин/женщин и различимую одежду **только после первого доказанного prefab** и Animator. Один Rocketbox, размноженный на 39 NPC, — не готовое разнообразие и **не финальный уровень референса**.

## Достоверный статус

- **Исходники:** реальный Microsoft skinned FBX + 6 MIT текстур доступны в GitHub, весь Editor builder и runtime hook реализованы в `agent/graphics-ui`.
- **Тесты кода/хешей:** `test_build_skinned_lods.py` + `test_fetch_textures.py` + `test_rocketbox_unity_pipeline.py`. Подтверждены 22 Python теста [Cross-Branch CI #40078739947](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40078739947) после исправления излишне буквального теста loader; реальное Unity compilation/Animator/WebGL не проверялось.
- **Критические неизвестные:** Unity Avatar может не пройти auto humanoid mapping Rocketbox Biped без ручной настройки; верность слотов Body/Head, UV, импорт TGA, retarget Quaternius, Unity Shader Compiler, visual skin deformation, FPS **нельзя считать проверенными** до запуска Editor/Chromium.
- **Сверка с закреплённым визуальным референсом из Источников NONATION: ВИЗУАЛЬНОЕ СООТВЕТСТВИЕ НЕ ПРОВЕРЕНО** — нет свежего честного WebGL кадра с действительно анимированным Rocketbox.
- **Cross-owner:** прямые файлы `Assets/MIR/Editor/**`, сцена и Cloud Build остаются у AGENT 01. Должны быть проверены через [issue #17](https://github.com/alexvelikiy88-sketch/nonation/issues/17) и текущий [PR #26](https://github.com/alexvelikiy88-sketch/nonation/pull/26); статус проекта `TASKS.md` обновляет только Manager.
