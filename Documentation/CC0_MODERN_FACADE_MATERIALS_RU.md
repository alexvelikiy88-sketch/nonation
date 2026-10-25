# NONATION — три проверенных albedo-материала для модернизированных фасадов

**Дата:** 25.10.26
**Происхождение:** ambientCG (CC0 1.0), исходные ассеты из внутренней ветки `agent/graphics-ui@35d73180e22d7487fb65045bab64dd92feb9098d`.
**Разрешённое использование:** [ambientCG License](https://ambientcg.com/license) — Creative Commons CC0, коммерческое использование и распространение разрешены без атрибуции; здесь источники указаны для проверяемости.
**Подтверждение:** https://ambientcg.com/view?id=Bricks097 · https://ambientcg.com/view?id=Planks023A · https://ambientcg.com/view?id=Plaster007.

| Реальный файл (1K JPEG или исходная производная) | Git blob SHA-1 | Источник / применение |
|---|---|---|
| `Bricks097_Color.jpg` | `cb777108b6e9e14f7054d2a6d6318f560c57163a` | [Bricks097](https://ambientcg.com/view?id=Bricks097); тёплый кирпич |
| `Planks023A_Color.jpg` | `e5e65712b9250546bceb146352aae4ea9d809691` | [Planks023A](https://ambientcg.com/view?id=Planks023A); тёмное дерево |
| `Plaster007_ColorEven.jpg` | `574d19144d1d0178b5b777eae02998e8da2b2403` | [Plaster007](https://ambientcg.com/view?id=Plaster007); адаптированное светлое минеральное покрытие из ветки AGENT 03 |

Три конкретных Blob заимствованы из этого же репозитория, **не** из Asset Store. Загружены как JPG с проверяемым Git object SHA, а не LFS pointer. Исходные права CC0 не требуют согласия Asset Store и не конфликтуют с правилами приватного импорта Unity Asset Store паков.

## Архитектура отображения

- Материалы используются настоящим `MirArchitectureQuarter` через `Resources.Load<Texture2D>` из `Nonation/Architecture/Scans` в Unity WebGL.
- Shader `NONATION/ArchitecturalLit` — новый проектный Built-in Standard Surface shader; один набор тональных цветов для 12 современных фасадов. Нет зависимости от URP.
- Остальные минералы/бетон пока используют проектные процедурные 256 px tile maps. Этот этап не утверждает уровень исходной фотореалистичной картинки.
- Не импортировать оригинальные приватные Unity Asset Store prefab/JPG/FBX в публичный GitHub. Их использование допускается позже только через лицензированный приватный канал и отдельные style/scale/collider тесты.
- Обязательная приёмка: реальный WebGL screenshot из обзорной камеры, 12 видимых новых фасадов, работа POI, честный `NONATION_MODULAR_CITY_READY` и `NONATION_KENNEY_CITY_READY buildings=0`, FPS и no double colliders. В случае сбоя — резервный Kenney baseline, не художественный PASS.
