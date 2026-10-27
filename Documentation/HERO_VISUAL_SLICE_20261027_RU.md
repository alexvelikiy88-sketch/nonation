# NONATION — художественный срез с настоящими 3D-моделями

Дата: 27.10.26. Статус: исходники в отдельном PR, фактические Unity WebGL и визуальная приёмка ещё не пройдены.

## Единая цель
Утверждённая композиция — солнечная площадь, кафе, продуктовый магазин и набережная. Художественный уровень: https://www.behance.net/gallery/142989785/3D-illustration-for-an-interactive-table . Никаких подложек чужого проекта, Unity примитивов вместо финальных моделей и фейковых скриншотов.

## Состав
- Архитектура: существующие на main настоящие Quaternius CC0 FBX для магазина, кафе, ратуши; оригинальные коллизии и бизнес-точки сохранены. Исходный атлас Quaternius 32×32, поэтому фактическая художественная пригодность этих фасадов по-прежнему требует проверки. Низкое качество подлежит замене более подходящими real meshes.
- Озеленение: creator-uploaded Quaternius Stylized Nature MegaKit Standard CC0 https://opengameart.org/content/stylized-nature-megakit . Архив 104088529 байт, SHA256 298f6732b872e4cf7b30e6e7abf9641c7f6dc6b326df37ac089533ed7e3d58c9. 21 член архива с точными SHA256 — Tools/hero_nature_manifest.json. Проверка GitHub Actions #40560057679 и #40560360249. Шесть исходных glTF с настоящими mesh/UV/PNG repack into GLB без синтеза вершин. В сцене 49 текстурированных экземпляров: 7 деревьев, 12 цветочных кустов, 18 групп цветов, 12 групп травы.
- Три различных реальных человека: Microsoft MIT Rocketbox Male_Adult_12 player (старый source), новые Male_Adult_16 и Female_Adult_16 FBX. Source SHA Microsoft 0943055db6ec570bcef9f2c8b41c9e5467c808f9, оригинальное MIT уведомление в Unity Resources, модель и 9 исходных TGA pinned по Git blob SHA. 1024 TGA получаются с реальной фильтрацией и нормализацией normal textures. Unity Humanoid Avatar + SkinnedMeshRenderer + Bindpose + реальный Quaternius CC0 Idle/Walk/Run Controller. NPC двигаются существующим MirMotionDriver и по действующей городской экономике; никаких постановочных жителей.
- Камера V: магазин (20,-20), центральная площадь (20,20), кафе (56,20) в одном реальном Unity 16:9 кадре. 40° pitch, 52° yaw, distance 66, FOV 42. Свет: тёплое направленное солнце, мягкие тени и MSAA, sky/equator/ground ambient Trilight. Дальняя большая симуляция сохраняется за кадром.

## Строгая приёмка
1. Source/Ownership/.NET PASS — не означает успех Unity WebGL.
2. Unity Cloud actual compilation/import: оригинальные 3D meshes с source texture, не Cube/Sphere, ни повреждённого Avatar/skin/animation, ни рендерного shader fallback для лиц, ни скрытого proxy.
3. Chromium на exact source SHA должен показать NONATION_PREMIUM_NATURE_READY с шестью источниками, три разных skinned источника, реальный WebGL player, 39 NPC и корректное сохранение/reload. Сохранить unedited showcase/waterfront raw screenshot и JSON с диагностикой.
4. Художественная приёмка только при соответствии Behance по реальным кадрам, визуально качественным зданиям, освещению, плотности и анатомии. Измерить FPS, draw calls, LOD и вес сборки. До явного решения владельца VISUAL GATE OPEN.
5. Не запускать параллельно платные Cloud сборки пока предыдущая #19 остаётся queued. Релиз через GitHub PR с доказываемым SHA.

## Лицензии
Quaternius creator CC0-1.0 OpenGameArt Nature Megakit Standard; Microsoft Rocketbox MIT original repository with pinned source commit; CC0 Quaternius animation; уже имеющиеся CC0 архитектурные источники. Нет контента GTA, неразрешённых Asset Store моделей и материалов Behance.
