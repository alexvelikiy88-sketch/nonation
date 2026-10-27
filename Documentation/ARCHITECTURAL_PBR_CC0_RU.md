# NONATION — Built-in PBR для первого архитектурного квартала

**Ревизия:** 25.10.26. **Статус:** PR / исходники, **НЕ подтверждённый WebGL визуальный gate**.

## 1. Источники и законное использование

В основе фасадных материалов — публичные CC0-текстуры [ambientCG](https://ambientcg.com), разрешённые для коммерческого использования и распространения в проекте (см. [правила CC0](https://ambientcg.com/license) и [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/)). Источники:

- [Bricks097](https://ambientcg.com/view?id=Bricks097): `Bricks097_Color.jpg`, `Bricks097_NormalGL.jpg`, `Bricks097_Roughness.jpg`.
- [Plaster007](https://ambientcg.com/view?id=Plaster007): `Plaster007_ColorEven.jpg`, `Plaster007_NormalSmoothGL.jpg`, `Plaster007_Roughness.jpg`.
- [Planks023A](https://ambientcg.com/view?id=Planks023A): `Planks023A_Color.jpg`, `Planks023A_Roughness.jpg`. Отдельной real normal-карты в текущем проверенном наборе нет: **не синтезируем фальшивую**.
- `ColorEven` и `NormalSmoothGL` — изменённые варианты CC0 scan от AGENT 03 (в Git, исходный blob проверяется), не выдавать их за оригинальные байты ambientCG.

**Точное происхождение байтов:** `agent/graphics-ui@35d73180e22d7487fb65045bab64dd92feb9098d`, прежние JPG blobs сохранены **без перекодирования** в `Assets/MIR/Art/Architecture/Resources/Nonation/Architecture/Scans`. `Tools/tests/test_architecture_pbr_cc0.py` проверяет Git blob SHA1 каждого из восьми JPG и размер. Данный PR добавляет ровно пять ранее отсутствующих карт (normal/roughness); три albedo уже находились в `main`.

Здесь **нет Unity Asset Store Standard EULA файлов**, Shader Graph из платных пакетов или коммерческих prefabs. Сам шейдер `NONATION/ArchitecturalLit` — собственный Cg/HLSL Unity Built-in Surface Shader.

## 2. Реальная графика вместо одноцветных материалов

- `ArchitecturalLit.shader`: взаимодействие солнечного света и фактических tangent-space normal-карт, roughness от ambientCG с переводом `smoothness = 1 - roughness`. Если карта отсутствует, материал честно использует заданный smoothness.
- `MirArchitectureQuarter.cs`: albedo, normal и roughness повторяются с **одним UV-масштабом 0,62 × 0,62**. `mesh.RecalculateTangents()` уже присутствует; геометрия витрин, балконов, входов, крыши и игровые footprint/коллизии **не изменяются**.
- `MirArchitectureScanImport.cs`: реальная normal-карта импортируется как `TextureImporterType.NormalMap`, roughness как **linear / non-sRGB**, с mipmaps для отдалённых зданий, repeat-текстурой и trilinear-фильтром. Программное вторичное gamma correction удалено, чтобы не искажать карты.
- Фасады, прежде повторявшие цветовую albedo текстуру без рельефа, смогут показывать кирпичные швы/штукатурку/отражённый свет в едином солнечном освещении без дополнительной геометрии и без дублирования здания.

## 3. Проверки и ограничения

**Статически:** шейдер/Материал/Importer/8 исходных blob связываются автоматически, проверяется нормализованный UV и import конфигурация; владельцы ассетов и права указаны.

**Обязательный Unity gate:** реальная компиляция Unity 6000.0.58f2 Built-in/WebGL, отсутствие shader variant stripping/розовых материалов, правильные normal maps без инверсии зелёного канала и roughness без «пластикового» блеска. Сравнивать снимки до/после при одинаковом солнечном свете и камере `V`.

**Обязательный графический gate:** actual 16:9 Chromium screenshot с фасадами, отдельная фотопроверка цветовой непрерывности кафе, магазина, площади и набережной. Текущий headless Chromium с SwiftShader не является эталоном реального GPU-профиля. FPS/frametime/draw calls/texture memory публиковать только по настоящим измерениям.

**Открыто:** люди всё ещё нуждаются в качественных анимированных моделях, плотность квартала недостаточна, URP миграцию без отдельной подготовки не выполнять. Даже успешная сборка шейдера **не закрывает VIS-07** без явного одобрения владельца.
