# NONATION — происхождение ассетов и лицензии

**Проверка 24.10.26.** Наличие файла в Git ещё не доказывает корректную Unity Material/Avatar-конфигурацию. Лицензия на исходные ассеты проверяется отдельно от качества, скелета и сборки.

## Используемые / подготовленные источники

- **Microsoft Rocketbox**, один исходный `Male_Adult_12.fbx` и `Male_Adult_12_preview.png`: [официальный репозиторий Microsoft](https://github.com/microsoft/Microsoft-Rocketbox), закреплённый upstream commit `0943055db6ec570bcef9f2c8b41c9e5467c808f9`. Лицензия **MIT**, Copyright (c) 2020 Microsoft. Полное условие сохранено рядом с исходниками в `Assets/MIR/Art/Rocketbox/LICENSE.md`. Изменять или удалять copyright/permission notice при публикации нельзя.
- **Rocketbox TGA:** шесть оригинальных body/head color, normal и specular источников приведены к 1024×1024 с пиннингом upstream Git blob SHA-1 и выходным SHA-256; пути и точные хеши — `Assets/MIR/Art/Rocketbox/Textures/textures_manifest.json`. Подтверждены реальные скачивание и проверка исходных байтов в CI; материал/LOD/skin в Unity пока не проверены.
- **Rocketbox LOD script:** `Assets/MIR/Art/Rocketbox/build_skinned_lods.py` — исходник генератора skinned LOD, **не свидетельство о созданных LOD-моделях**. Требуется реальный Blender export и Unity importer проверки.
- **Quaternius Universal Animation Library 1/2 Standard:** лицензия **CC0 1.0** согласно прилагаемым `Assets/MIR/Art/Quaternius/LICENSE_A1.txt` и `LICENSE_A2.txt`; SHA-pinned получение `Tools/fetch_quaternius.py`. Атрибуцию источника разумно сохранять, даже если CC0 не требует её.

## Новый источник: Kenney City Kit / пробная интеграция 25.10.26

- **Kenney City Kit Commercial** (городские здания), **City Kit Roads** (светофоры), **Car Kit** (модели машин), **Nature Kit** (деревья) — оригинальные бесплатные ресурсы автора [Kenney](https://kenney.nl), обозначены на страницах авторских наборов как **Creative Commons Zero (CC0 1.0)**.
- Сборка самодостаточных GLB из исходных паков Kenney: [Hidencod/tge-assets](https://github.com/Hidencod/tge-assets), commit `7be9b573bc74f32ed766b44b3f46550d02ca83b5`. В `Tools/kenney_city_manifest.json` закреплены 14 путей, точные размеры и Git blob SHA-1.
- `Tools/fetch_kenney_city.py` проверяет каждый файл по SHA-1, версии и длине GLB до сохранения, затем glTFast импортирует их через `Assets/MIR/Resources/MIR/Kenney/`. Ресурсы воспроизводимо подтягиваются при сборке, а не загружаются из недоверенных архивов.
- На момент ввода кода действительный Unity WebGL импорт, шейдерная совместимость, FPS и качество игрового кадра **не подтверждены**; см. [FREE_CITY_PACK_RU.md](FREE_CITY_PACK_RU.md). Лицензия на модели не распространяется автоматически на исходный код собиравшего их стороннего агрегатора, который в продукт не включается.
- **Synty POLYGON City/Characters не импортировались** и не выдаются за часть бесплатного набора. Художественная приёмка по исходному реалистичному референсу остаётся отдельной целью.

## Не включать в коммерческую сборку без новой проверки

- `maid_skinned_lod1.glb`, Maid, Floral, Casual, Girl, GIGN, Pinkdress, Freya — происхождение/права на коммерческое распространение из локальных пользовательских архивов **не подтверждены**.
- Honda Civic BTCC 2023 / vecarz [Sketchfab](https://sketchfab.com/3d-models/honda-civic-btcc-2023-wwwvecarzcom-e1e0f3cb5f754838a5313da1e8999334) — **CC BY-NC-SA 4.0**, не годится для коммерческой публикации без дополнительного разрешения. Не часть стандартного build.
- Unity Package Manager/Editor и третьесторонние пакеты регулируются собственными лицензиями, а не лицензией ассета Microsoft.
- Оригинальный C#/проект не получает автоматически открытую лицензию от наличия MIT-файла Rocketbox: лицензия MIT применяется к конкретным upstream файлам, **не ко всему NONATION**.

## Контроль перед релизом

Проверять MIT notice, pinned hashes, содержимое Build scene dependencies/Resources и запрет NC ассетов. `Integration Guard` ловит известные опасные имена и требует `ASSET-LICENSE-EVIDENCE`, но это **не юридическая сертификация** всех материалов. Отдельно проверять факт включения конкретных ассетов в Unity WebGL player.
