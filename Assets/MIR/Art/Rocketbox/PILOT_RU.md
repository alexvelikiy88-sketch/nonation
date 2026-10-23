# Microsoft Rocketbox — пилот одного персонажа (source, НЕ prefab)

Дата: 23.10.26. Владелец: AGENT 03.

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

## Обнаруженная структура FBX (статический бинарный просмотр)

Заголовок `Kaydara FBX Binary`; присутствуют `Deformer`, `Skin`, `Cluster`, материалы `m007_body` и `m007_head`, именованные уровни `m007_hipoly_81_bones`, `m007_midpoly_42_bones`, `m007_lowpoly_33_bones`, `m007_ultralowpoly_23_bones`. **Это только идентификаторы бинарного файла, а не Unity QA**: корректность skin weights, кости, humanoid mapping, LOD meshes и retarget в Unity пока не проверялись. Реальные triangles, counts vertices, FPS/draw calls и мимика не измерены.

## Требуемые оригинальные текстуры — отсутствуют в GitHub nonation

Всего **6 TGA** по 12,582,956 байт каждый (исходники ~72 МиБ):
- `Textures/m007_body_color.tga`: `1757150789a5fdcf733394ff21a60512530b4f1a`
- `Textures/m007_body_normal.tga`: `5bc8f98e588d55964e2b6a2d0029778f4a9db9e6`
- `Textures/m007_body_specular.tga`: `611bbd149179a52c6fbded07375904d462e49c19`
- `Textures/m007_head_color.tga`: `e5b23c4f55e499893c2a92ec7e52d0252bee2ae3`
- `Textures/m007_head_normal.tga`: `0a99ce9aa6014e94c3385555f66a14da7599b757`
- `Textures/m007_head_specular.tga`: `81fe377939cf8eaedd5d14dfb5261650de86bf1e`

Все файлы находятся в `Assets/Avatars/Adults/Male_Adult_12/Textures/` того же pinned upstream commit. Без этих файлов **модель нельзя объявить готовой**, потому что оригинальный FBX ссылается на эти имена текстур. Их допускается получить через проверяемый fetch script с уменьшением разрешения только для WebGL бюджета; необходимо измерить качество skin/materials после импорта.

## Статус и этапы проверки

1. [x] Права: Microsoft MIT, лицензия сохранена; оригинальный rigged FBX получен, совпадают Git blob SHA.
2. [ ] Получены и проверены все 6 текстур; ограничены размеры; проверен Material import.
3. [ ] AGENT 01 импортирует FBX Humanoid (`Assets/MIR/Editor/**`) и сопоставляет корректные объекты LOD0–LOD3, clips и Animator. Импорт НЕ МОЖЕТ считаться успешным по одному наличию FBX.
4. [ ] Idle, Walk, Run, Carrying, переходы и skin deformation/foot slide просмотрены в Unity Editor.
5. [ ] Проверены реальные LOD, ошибки материалов и измеренный FPS / draw calls в браузерном WebGL.

**Важное условие:** этот ассет в `Assets/MIR/Art/`, а **не** в `Resources` и не в сцене. Пока он не используется runtime; baseline WebGL не должен зависеть от него. Никаких третьесторонних NC персонажей в проект не добавлялось. `TASKS.md` может изменять только Manager.
