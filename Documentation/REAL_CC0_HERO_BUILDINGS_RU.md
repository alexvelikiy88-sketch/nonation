# NONATION — реальные авторские FBX фасады вместо procedural blocks

**Статус 27.10.26:** source/CI experiment, NOT Unity/WebGL/visual acceptance.
**Цель владельца:** небольшая, но полноценно проработанная stylized-realism 3D городская диорама; [Behance ориентир](https://www.behance.net/gallery/142989785/3D-illustration-for-an-interactive-table) вместе с утверждённым солнечным кварталом у воды. Мы не копируем защищённые Behance изображения или исходники GTA.

## Точно подтверждённые авторские исходники

- **Автор:** Quaternius.
- **Оригинальный авторский релиз:** Ultimate Textured Buildings Pack, December 2019.
- **Авторская публикация архива / лицензия:** [OpenGameArt — LowPoly Buildings Pack](https://opengameart.org/content/lowpoly-buildings-pack), 18.06.21, Quaternius, Creative Commons Zero (CC0 1.0).
- **Проверенный архив:** https://opengameart.org/sites/default/files/ultimate_textured_building_pack_by_quaternius.zip
- **Размер:** 29 961 262 байт; **SHA-256:** `ff876502f53f6d4c35104378e8322ec8253fd0ee45c8ec28d9dc81df54b80e85`.
- Проверены собственным бесплатным [GitHub Actions archive-probe #40534353159](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40534353159) и [dimensions/hash probe #40534639784](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40534639784): **421 архивационных записи**, 102 FBX, 102 OBJ, 102 MTL, 102 .blend и 13 PNG. Без программной подделки или замены скачиваемого контента.
- Версии лицензий сайта Quaternius могли меняться; **юридическое основание здесь — конкретная исходная публикация на OpenGameArt под CC0 в 2021**, а не автоматически всё содержимое современного сайта. В проект переносится только восемь явно проверенных файлов этой публикации.

## Интегрируемые объекты

| Исходная модель (FBX) | Авторская геометрия по OBJ | Использование |
|---|---|---|
| `2Story_Wide_2Doors` | 6,8278 × 2,7479 × 2,2553 source units | Один полноразмерный grocery с объёмными дверями и архитектурной деталью |
| `2Story_Sign` | 2,1542 × 2,7573 × 2,1542 | Первая половина кафе / коммерческая вывеска |
| `2Story_Balcony` | 2,1586 × 2,9441 × 2,5322 | Вторая половина кафе с собственным балконом |
| `3Story_Small` | 1,9089 × 3,8485 × 2,0562 | Два самостоятельных блока гражданской постройки |
| `Texture_Light/Red/Grey/Blue.png` | Только оригинальные PNG 32×32 | Проверенный atlas UV материала, без переназначения UV по догадке |

Пять **видимых моделей** из четырёх уникальных FBX образуют **три героических игровых слота**: магазин, кафе и ратуша. Последние девять слотов сохраняют существующие детализированные PBR модульные фасады, а не дублируют их. **Не путать с 12 новыми свободными зданиями:** цель — реальное улучшение центрального кадра, а не ложный счётчик.

## Технический контракт

1. `Tools/quaternius_hero_manifest.json` содержит immutable archive и SHA-256 каждого из восьми файлов; `Tools/fetch_quaternius_hero.py` получает авторский ZIP, сверяет полный SHA, исходные имена, CRC и все восемь payload hashes. **Никакого unsafe extractall / непроверенного fallback URL.**
2. GitHub Actions проверяет источник *до* оплачиваемой сборки. Unity Cloud prebuild повторно получает ровно выбранные бинарные модели в `Assets/MIR/Art/QuaterniusHero/Resources/MIR/QuaterniusHero`, до открытия Unity Editor.
3. Unity импортирует настоящие FBX как `GameObject/MeshFilter/MeshRenderer` (не `Cube`, не PNG-фальшивый фасад), сохраняет оригинальную геометрию и UV. Оригинальные 32px color swatches импортируются без mip bleed.
4. Runtime загружает Unity-ресурсы `Resources.Load<GameObject>` и оригинальные atlas PNG, назначает один совместимый Built-in Lit Shader на все submeshes. Устанавливает **только равномерный масштаб** для сохранения пропорций дверей/этажей и помещает фасады относительно существующего south-entry (отрицательный Z).
5. `MirModernDistrictIntegrator` сначала требует все настоящие MeshFilter и материалы; только после успешной установки вызывает `MirArchitectureQuarter.Build(...,useRealQuaterniusHero:true)`, **отключая генерацию этих 3 procedural facade slots**. Оригинальные 12 legacy BoxCollider и все NPC/home/commercial POI остаются авторитетными.
6. Если хотя бы один FBX/atlas/shader не загружен, происходит честный атомарный откат на предыдущие 12 modular facades. Он сохраняет техническую работоспособность, но не считается визуальным релизом. В Chrome обязательны `NONATION_QUATERNIUS_HERO_READY models=5 slots=3 atlas=4 vertices>=1200` и `NONATION_MODULAR_CITY_READY ... cc0Hero=models` **до и после reload**.
7. При успешной сборке исследовать реальные V/B 16:9 снимки, положение дверей относительно gameplay POI, отсутствие пересечений/фальшивых коллизий и единый баланс стилей фасадов. Даже успешная загрузка оригинальных FBX не означает готовую художественную приёмку.

## Что НЕ объявлено готовым

- Камера, затенение и скейлинг конкретных FBX в реальном Unity WebGL **пока не проверены**. Исходные объекты могли оказаться меньше/больше предсказанного до реального importer; зафиксированного screen proof ещё нет.
- Анимированные 3–6 разных людей и LOD по-прежнему необходимы: реальный Rocketbox пилот блокировался на проверке MIT лицензии, исправление предложено отдельно в PR #49. До реального SkinnedMeshRenderer/Animator в Chrome всё ещё возможен диагностический NPC proxy.
- Цвета Quaternius palette требуют арт-review рядом с настоящими CC0 ambientCG PBR фасадами, чтобы не было визуального разрыва. При заметном конфликте подобрать другие CC0 модели/палитры либо отказаться от этой пары, **не имитировать готовность низкокачественными блоками**.

**Лицензии:** [Creative Commons CC0](https://creativecommons.org/publicdomain/zero/1.0/), [Quaternius OGA авторская страница](https://opengameart.org/content/lowpoly-buildings-pack). Старые Unity Asset Store source prefabs из сторонних лицензий в открытый GitHub не добавляются.
