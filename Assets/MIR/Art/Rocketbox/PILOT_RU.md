# Microsoft Rocketbox — пилот одного персонажа (source, НЕ prefab)

Дата создания: 23.10.26. Последний аудит: 24.10.26. Владелец: AGENT 03.

## Источник и права

- Оригинальный репозиторий: https://github.com/microsoft/Microsoft-Rocketbox
- Immutable upstream ref: `0943055db6ec570bcef9f2c8b41c9e5467c808f9` (branch master на дату проверки)
- Модель: [Male_Adult_12.fbx](https://github.com/microsoft/Microsoft-Rocketbox/blob/0943055db6ec570bcef9f2c8b41c9e5467c808f9/Assets/Avatars/Adults/Male_Adult_12/Export/Male_Adult_12.fbx)
- Preview: [Male_Adult_12.png](https://github.com/microsoft/Microsoft-Rocketbox/blob/0943055db6ec570bcef9f2c8b41c9e5467c808f9/Assets/Avatars/Adults/Male_Adult_12/Male_Adult_12.png)
- Лицензия: **MIT**, Copyright (c) 2020 Microsoft. Полный обязательный текст — [LICENSE.md](LICENSE.md) в этой же папке. Обязательное условие MIT: сохранять copyright и permission notice при распространении существенных частей. Коммерческая Unity WebGL-игра разрешена.
- Upstream Git blob SHA-1 `Male_Adult_12.fbx`: `15d4f8501d5703600cb3d8a94765a3de1029aaf6` (487,776 байт).
- Upstream Git blob SHA-1 `Male_Adult_12_preview.png`: `2b4e07161df2786c8cde85952403bc71eaabc031` (506,050 байт).
- Оригинальные байты скопированы побайтово из upstream через GitHub Git blobs API; destination blob SHA **совпадает** с upstream SHA.
- Данный каталог содержит только оригинальный FBX без facial morphs, справочное изображение и документы. Никакого непроверенного third-party контента.

## Проверенная структура FBX (24.10.26)

Бинарный FBX версии **7400**, 487 776 байт: разобрано **3 301 FBX-узел** без ошибок границ. По реальным записям `Objects`: **1 `Geometry` типа `Mesh`, 1 `Model` типа `Mesh`, 80 `Model` типа `LimbNode`, 1 `Deformer` типа `Skin`, 80 `Cluster`, 2 `Material`, 6 `Texture`**. Массив `Geometry.Vertices` содержит 10 149 координат — **3 383 исходные позиции**. Массив `PolygonVertexIndex` содержит **20 166 ссылок на вершины полигонов**, не 20 166 треугольников. Точное число треугольников отдельно измерить после декодирования индексов или в Unity.

**Исправление прежней гипотезы:** в этом FBX присутствует **одна реальная Mesh-геометрия** `m007_hipoly_81_bones`. Строки `m007_midpoly_42_bones`, `m007_lowpoly_33_bones`, `m007_ultralowpoly_23_bones` встречаются в бинарном тексте, однако **дополнительных записей `Geometry Mesh` нет**. Четырёх отдельных сеток LOD в поставленном FBX **нет**, `LODGroup` пока не подтверждён и не готов. Перед его созданием нужны отдельные реальные LOD-геометрии и проверка лицензий.

Статическая структура файла **не доказывает** корректную skin деформацию, Avatar mapping, веса, мимику или WebGL FPS. Upstream `_facial.fbx` существует, но **не импортирован**.

## Текстуры Microsoft — загружены, проверены и оптимизированы (24.10.26)

Всего **6 TGA** по 12,582,956 байт каждый (исходники ~72 МиБ):
- `Textures/m007_body_color.tga`: `1757150789a5fdcf733394ff21a60512530b4f1a`
- `Textures/m007_body_normal.tga`: `5bc8f98e588d55964e2b6a2d0029778f4a9db9e6`
- `Textures/m007_body_specular.tga`: `611bbd149179a52c6fbded07375904d462e49c19`
- `Textures/m007_head_color.tga`: `e5b23c4f55e499893c2a92ec7e52d0252bee2ae3`
- `Textures/m007_head_normal.tga`: `0a99ce9aa6014e94c3385555f66a14da7599b757`
- `Textures/m007_head_specular.tga`: `81fe377939cf8eaedd5d14dfb5261650de86bf1e`

Все шесть исходных файлов находятся в `Assets/Avatars/Adults/Male_Adult_12/Textures/` pinned Microsoft upstream commit. GitHub Actions [40013964977](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40013964977) действительно загрузил все шесть 24-битных TGA (2048×2048), **сверил Git blob SHA-1** с перечисленными выше хешами, уменьшил до 1024×1024 и закоммитил в `Assets/MIR/Art/Rocketbox/Textures/`: коммит [eb58e07f](https://github.com/alexvelikiy88-sketch/nonation/commit/eb58e07f5a1fd97597b71affa1c7a774f3292ebf). Все шесть файлов по **3 145 746 байт**; `textures_manifest.json` содержит источник, размеры, параметры фильтрации и **SHA-256 каждого выходного TGA**. Оригинальные тяжёлые TGA **не** добавлялись в Git. Наличие подготовленных файлов ещё не означает правильный Unity Material import.

## Подготовка текстур для Unity (скрипт в репозитории)

Добавлены `fetch_textures.py` и `test_fetch_textures.py` (оба в данной папке). Используют зафиксированный upstream commit и сверяют байты каждого скачанного TGA с **Git blob SHA-1** и объявленным размером; неверный файл отклоняется, `Resources`/сцены не меняются.

```bash
python3 Assets/MIR/Art/Rocketbox/fetch_textures.py --dry-run
python3 -m unittest discover -s Assets/MIR/Art/Rocketbox -p 'test_*.py' -v
python3 Assets/MIR/Art/Rocketbox/fetch_textures.py --max-edge 1024
```

Скрипт скачивает только 6 оригинальных **24- или 32-битных TGA** из Microsoft Rocketbox; проверяет точные исходные Git blob SHA, сохраняет BGRA/alpha и ориентирование, уменьшает изображения box-average до заданного предела, **повторно нормализует RGB-векторы normal map**. В версии 24.10.26 все шесть исходников проверяются во временном staging-каталоге **до установки**; создаётся `Textures/textures_manifest.json` с SHA-256, размерами и настройками преобразования.

**Реальная загрузка и верификация выполнены:** [GitHub Actions #40013964977](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40013964977) завершился SUCCESS, включая контроль скачанных источников, проверку выходных SHA-256, фиксацию в GitHub и artifact upload. Первоначальный [run #40013648557](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40013648557) выявил несовместимость старого 32-битного TGA parser с оригинальным 24-битным TGA — исправлено коммитами `214254c7` и `e6a8ff6e`. Временный однократный workflow удалён после записи текстур, чтобы не входил в PR. **Не проверено:** Unity Material import, назначение normal/specular maps, skin weights, Avatar и WebGL. Эти действия принадлежат AGENT 01.

**Локальные и GitHub Actions тесты 24.10.26:** 7 Python unittest PASS на синтетических TGA (известный Git SHA-1, BGRA/alpha, ориентация, box-average, normal vector renormalization, отказ при некорректном TGA и при сетевой ошибке на третьем файле без установки неполного набора). Git blob SHA обновлённых Python-файлов **совпадает с протестированными локальными байтами**. Это не Unity QA и не проверка всех реальных Microsoft TGA.

## Статус и этапы проверки

1. [x] Права: Microsoft MIT, лицензия сохранена; оригинальный rigged FBX получен, совпадают Git blob SHA.
2. [x] Получены все 6 MIT-текстур, сверены SHA исходников, подготовлено 1024×1024, сохранены SHA-256 и коммит `eb58e07f`. **[ ] Material import и качество в Unity — не проверены.**
3. [ ] AGENT 01 импортирует FBX Humanoid (`Assets/MIR/Editor/**`), подтверждает единственную поставляемую сетку; отдельно создаёт и проверяет LOD1–LOD3, клипы и Animator. Импорт НЕ МОЖЕТ считаться успешным по одному наличию FBX.
4. [ ] Idle, Walk, Run, Carrying, переходы и skin deformation/foot slide просмотрены в Unity Editor.
5. [ ] Получены/сгенерированы и проверены отдельные LOD-меши (сейчас только один), устранены ошибки материалов, измерен FPS / draw calls в браузерном WebGL.

**Важное условие:** этот ассет в `Assets/MIR/Art/`, а **не** в `Resources` и не в сцене. Пока он не используется runtime; baseline WebGL не должен зависеть от него. Никаких третьесторонних NC персонажей в проект не добавлялось. `TASKS.md` может изменять только Manager.
