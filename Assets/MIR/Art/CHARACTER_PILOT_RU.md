# nonation / Character Pilot 01 — воспроизводимый импорт

**Дата:** 23.10.26  
**Владелец документа:** AGENT 03 — Graphics/UI  
**Статус:** спецификация/входной контракт, **НЕ ГОТОВЫЙ 3D-АССЕТ**, не включена в WebGL

## Источники и ограничения

- Геометрия/базовая кожа: **MakeHuman Community**, только core assets, лицензия **CC0 1.0**. Официальные источники: https://www.makehumancommunity.org/about/license.html ; https://github.com/makehumancommunity/makehuman/blob/master/LICENSE.md ; https://www.makehumancommunity.org/makehuman/faq/can_i_sell_models_created_with_makehuman.html. Экспортированные модели из core assets можно использовать в коммерческой игре, включая закрытые/WebGL-проекты. Лицензия программного кода MakeHuman (AGPL) **не переносится автоматически на его экспорт**.
- **Внешние** волосы, одежда, кожи, скелеты и текстуры из сторонних библиотек могут иметь отдельные лицензии. До проверки автора, первичного URL и типа лицензии **не включать** их в исходник или билд.
- Анимация: Quaternius Universal Animation Library (CC0): https://quaternius.com/packs/universalanimationlibrary.html ; UAL 2: https://quaternius.com/packs/universalanimationlibrary2.html. В репозитории лицензии `LICENSE_A1.txt`, `LICENSE_A2.txt`, а скачиваемые FBX проверяются `Tools/fetch_quaternius.py` по SHA-256.
- Лицензии Maid, GIGN, Pinkdress, Floral, Casual, Girl, Freya не подтверждены; Honda CC BY-NC-SA — **не использовать в коммерческой публикации**.

## Необходимый фактический вход

Создать **одного** реального персонажа `PilotHuman01` с уникальным исходным экспортом и записать:

| Поле | Обязательные сведения |
|---|---|
| Исходник | MakeHuman версия/официальный архив, SHA-256, дата скачивания |
| Геометрия | `.fbx` либо импортируемый `.glb`, исходная масштабная единица, число triangles/vertices из импортера |
| Риг | наименование скелета, bind poses, количество костей, веса, Avatar.isValid/isHuman в Unity |
| Материалы | diffuse/albedo, normals, roughness/metallic (если есть), форматы и реальные размеры текстур |
| Анимации | Idle, Walk, Run/Jog/Sprint, выбранные клипы, контроллер, параметры, retarget/loop |
| Мимика | реальные blendshapes и количество, если есть; отсутствие явно отмечать |
| LOD | количество *отличающихся* сеток, triangles по LOD, реальные экраны переключения |
| Лицензия | URL/автор/текст/тип для каждой базовой и внешней составляющей |
| Runtime | Unity 6000.0.58f2, WebGL, целевой браузер/GPU, FPS/draw calls/треугольники (только реально измеренное) |

Без этих сведений **запрещено** помечать импорт как успешный персонаж.

## Пайплайн и зоны ответственности

1. **AGENT 03** собирает и лицензирует исходник; никаких NC/unverified материалов в `Assets/MIR/Resources/`. Исходники пилота и manifest сохраняются в `Assets/MIR/Art/Characters/PilotHuman01/` только после фактического получения и проверки прав.
2. **AGENT 01** (владелец `Assets/MIR/Editor/**`, `Assets/MIR/Scenes/**`) импортирует FBX/GLB как Humanoid, проверяет скелет/weights/Avatar и создаёт `Resources/MIR/PilotHuman01.prefab` с `Animator Controller`. Отдельный `CROSS-OWNER REQUEST` в отчёте 03.
3. В Unity проверить, что `Animator` содержит float `Speed`, состояние `Locomotion` и назначенные Idle/Walk/Run (не T-pose). `MirHumanFactory` проверяет наличие, но **не доказывает качество деформации**.
4. Проверить переходы Idle↔Walk↔Run, развороты NPC, бег игрока и Carrying. Просмотреть плечи, локти, колени, стопы, кожу/текстуры и положение ног на земле, в том числе после 15–30 секунд работы.
5. LOD0→LOD1→LOD2: разные сетки, без всплывающих артефактов; проверка distant NPC и игрока вблизи. Если реальных LOD нет, явно блокировать отметку готовности.
6. Снять профиль в **настоящем WebGL-браузере** при текущей плотности NPC. Приложить исходную платформу, разрешение, FPS, batches/draw calls, tris, memory, скриншоты/видео, консольные предупреждения. Не вычислять и не подставлять показатели без измерения.
7. AGENT 01/Manager проверяет Cloud Build/export, добавляет минимальный smoke-test без несогласованных правок сцен/редактора. AGENT 03 прикладывает результаты и коммиты в `docs/agents/reports/03_GRAPHICS_UI.md`.

## Runtime-защита уже в AGENT 03

- `MirHumanFactory` загружает prefab независимо от наличия QuaterniusCatalog, отбраковывает явно неподготовленные риги, один раз сообщает причину отказа и использует **диагностическую процедурную замену**, не называя её готовым персонажем.
- `MirMotionDriver` отображает `NPC 1.3–1.7 м/с → full walk`, `player walk 2.7 м/с → full walk`, `player run 5.5 м/с → full run` по текущим параметрам кода, а не по замеренному действительному клипу.
- Цифры — входные скорости в исходном C#, **не показатели производительности и не результат Unity QA**.

## Стоп-факторы

До получения проверенного внешнего ассета, доступа к Unity Editor и успешного WebGL экспорта **Character Pilot 01 остаётся BLOCKED**. Не подменять 3D-художественный результат описанием архитектуры, procedural mesh или скриншотом.
