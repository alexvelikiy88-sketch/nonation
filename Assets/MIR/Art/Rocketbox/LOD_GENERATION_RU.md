# NONATION — Rocketbox Male_Adult_12, LOD1–LOD3

Дата: 24.10.26. Владелец AGENT 03. **Pipeline реализован в коде, Blender/Unity еще НЕ выполнялись.**

## Источник и лицензия

- [Microsoft Rocketbox](https://github.com/microsoft/Microsoft-Rocketbox/tree/0943055db6ec570bcef9f2c8b41c9e5467c808f9/Assets/Avatars/Adults/Male_Adult_12), pinned upstream commit `0943055db6ec570bcef9f2c8b41c9e5467c808f9`.
- Source FBX `Male_Adult_12.fbx`, Git blob SHA-1 `15d4f8501d5703600cb3d8a94765a3de1029aaf6`; исходник в `Art/Rocketbox` **не изменять**.
- Исходный copyright/permission notice: [`LICENSE.md`](LICENSE.md), **MIT**, Microsoft 2020. Это же условие действует для созданных производных LOD FBX.
- Исходные 6 MIT TGA, оптимизированные до 1024×1024, имеют [воспроизводимый SHA-256 manifest](Textures/textures_manifest.json).

## Что фактически есть и чего нет

Исходный FBX 7400 имеет ровно **одну** `Geometry Mesh` (`m007_hipoly_81_bones`), 80 `LimbNode`, 80 `Skin Cluster`, два материала. Упоминания `midpoly/lowpoly/ultralowpoly` внутри бинарника **не означают** наличие еще трёх геометрий. Новые LOD в текущем Git НЕ созданы. Названия скрипта и планируемых файлов не являются готовыми моделями.

## Генератор AGENT 03

Реализован [`build_skinned_lods.py`](build_skinned_lods.py) (Blender Python API 3.6+, желательно закрепить версию 4.5). Скрипт:

1. Проверяет точный original Git blob FBX и наличие оригинальной MIT лицензии.
2. Проверяет все шесть TGA, размеры 1024×1024, суммы SHA-256 из manifest.
3. Импортирует FBX без сохранения сцен и проверяет **один Mesh**, **один Armature**, UV, две группы материалов и наличие весов на каждой вершине, привязанных к bone names.
4. Для LOD1 / LOD2 / LOD3 делает независимые копии исходной меш-сетки и применяет Blender Decimate `COLLAPSE` с целевыми отношениями `0.72 / 0.48 / 0.28`; Armature modifier и vertex groups не должны исчезнуть.
5. Требует строгого сокращения triangle count, сохранения skin groups, UV, двух материальных слотов и числа костей. Экспортирует отдельные `.fbx` с исходным скелетом, без baked animation.
6. **Повторно импортирует** каждый exported FBX, проверяет число треугольников и костей. При ошибках не публикует готовый набор; при успехе записывает 3 FBX и `lod_manifest.json` с Blender version, фактическими vertex/triangle counts, SHA-256, reimport check.
7. Не редактирует `Resources`, Unity сцены, `Packages` и `Assets/MIR/Editor/**`.

Пример команды в окружении с установленным Blender:

```bash
blender --background --factory-startup --python Assets/MIR/Art/Rocketbox/build_skinned_lods.py -- \
  --input Assets/MIR/Art/Rocketbox/Male_Adult_12.fbx \
  --output Assets/MIR/Art/Rocketbox/GeneratedLODs \
  --ratios 0.72 0.48 0.28
```

Тесты **без Blender** (валидация ratio, MIT/license, source blob, checksum texture manifest, fail-closed):

```bash
python3 -m unittest discover -s Assets/MIR/Art/Rocketbox -p 'test_build_skinned_lods.py' -v
```

**Не заявлять PASS, пока фактически не запущены.** Код API сверялся с официальной документацией Blender, но ни импорт, ни decimation, ни экспорт пока не тестировались реальной версией Blender. Может потребоваться адаптация к структуре FBX.

## Порядок исполнения и границы владения

- AGENT 03 владеет исходниками и будущими generated meshes под `Assets/MIR/Art/Rocketbox/**`.
- Для однократной автоматической сборки на GitHub hosted runner нужен **Manager-owned** `.github/workflows/**` workflow (или разрешённый компьютер с Blender). AGENT 03 не пишет этот workflow без менеджера; запрос фиксируется в отчёте и issue.
- AGENT 01 владеет `Assets/MIR/Editor/**`, импортом FBX/TextureImporter, контроллером/Avatar, `LODGroup`, prefab и сценами. Отдельно проверить настоящую T-pose / skin deformation / retarget на всех LOD, швы UV и пропадание весов, Idle→Walk→Run и переходы.
- Менеджер сверяет PR, license evidence и реальный WebGL FPS/draw calls после сборки. Пока отсутствует Unity WebGL Player build, **никакой LOD не считается готовым для production**.

## Критерии отказа (fail closed)

- Неизвестная лицензия, иной оригинальный FBX, отсутствующая/битая текстура.
- FBX импортировался не как один скелет + один skinned Mesh.
- Любой LOD: пропавшие UV, материал, bone weights, Armature, нулевая геометрия, неубывающий triangle count.
- Экспортированные FBX не реимпортируются с тем же количеством костей/треугольников.
- В Unity Avatar invalid, T-pose, deformation/texture artifacts, нарушенные foot placement/skin weights — до устранения **не использовать** LOD в игре.
