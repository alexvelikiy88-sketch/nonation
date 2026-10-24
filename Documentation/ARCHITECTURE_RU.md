# NONATION — каноническая архитектура Unity / .NET

**Актуализация 24.10.26.** База — [интеграционный PR #19](https://github.com/alexvelikiy88-sketch/nonation/pull/19). Это вертикальный игровой срез, **не** завершённая MMO.

## Новая архитектурная норма: визуальный AA vertical slice

**Владелец утвердил [VISUAL_CANON_RU.md](VISUAL_CANON_RU.md) как обязательный визуальный уровень.** Цель — превратить техническую alpha с процедурными блоками в **действительно играбельный изометрический квартал** с детальной архитектурой, людьми и городской жизнью. На готовой рабочей WebGL-сборке техническая приёмка достигнута, а новая визуальная приёмка пока не пройдена.

### Конструкция нового квартала без перезапуска Unity-проекта

- **`World/Data`:** существующий домен экономики, NPC, потребностей, товаров и грузоперевозок не менять ради внешнего вида. Ввести стабильные POI/идентификаторы дверей, торговых точек, рабочих мест и маршрутов, используемые клиентом и симуляцией.
- **`Scene/Geometry`:** качественный модульный квартал из повторно используемых prefab и читаемых Unity Scene данных (3D здания, витрины, дороги, переходы, площадь, набережная, вода, растения). Текущие `MirCity` procedural primitives допустимы лишь как debug/fallback или вне принятого кадра; недопустимы как итоговая архитектура.
- **`Character/Animation`:** лицензируемые skinned Mesh + проверенный Human Avatar, Animator Idle/Walk/Run и контекстные действия, реальные LOD/материалы, вариативность NPC. `MirHumanFactory` возвращает диагностический proxy только при ошибке импорта; не считать fallback финальным артом.
- **`Camera/UI`:** 3/4 изометрическая/изометрически близкая камера (параметры сохранены), понятный масштаб; HUD интегрирован с объектами мира, без навязчивой отладочной панели.
- **`Render`:** Unity 6 + URP 17 без обязательной смены движка; совместимые WebGL PBR-шейдеры, baked light для статики, контролируемые динамические тени, атласы/instancing/LOD/culling по результатам профайлера.
- **`Release`:** код, работавший раньше, продолжает проходить собственный Chromium smoke и финансовые тесты. Новую графику выпускать только после отдельного VISUAL GATE (сравнительный скрин, игровые действия, анимация, FPS, лицензии, одобрение владельца).

### Точки интеграции и зоны ответственности

| Контракт | CHAT 01 | CHAT 02 | CHAT 03 | CHAT 04 |
|---|---|---|---|---|
| Layout, camera, scene import | Unity сцена, сборка, камера | POI IDs и gameplay события | модели и prefab квартала | интерфейсы, review |
| NPC + среда | настоящие GameObject/Animator и перемещение | экономическое/социальное поведение | Humanoid rig/LOD/анимации | интеграционные тесты |
| Магазин, кафе и городской транспорт | маршруты/взаимодействия клиента | деньги/товары/работа/доставка | реальные интерьеры/машины/UI | сквозная приёмка |
| Графический бюджет | WebGL profiler, build | ограничения алгоритмов NPC | geometry/material/LOD | фиксирует измерения |
| Визуальный критерий | Chromium screenshot, input/save | работающие POI | соответствие концепту | заключение, решение владельца |

**Нельзя** делать графический трюк, который ломает сохранение, хитбоксы, управление и реальные NPC. Только настоящая Unity WebGL сцена может считаться реализацией канона; картинка в UI или Blender render сами по себе не выполняют задачу.

### Технический и художественный выпуск — два разных статуса

- `TECH-BASELINE`: Unity WebGL экспорт и GitHub Pages опубликованы, игра запускается, 39 NPC, движение, сохранения проверены. Это техническая alpha.
- `VISUAL-RELEASE`: квартал из канона реально работает, настоящий Humanoid не в T-позе, камеры/окружение не примитивны, требуемые взаимодействия/оптимизация доказаны, владелец явно одобрил сравнительный обзорный кадр.

Нельзя переводить `VISUAL-RELEASE` в DONE только вследствие успешного `TECH-BASELINE`. Актуальные задачи [VIS-00…VIS-07](../TASKS.md) имеют приоритет над расширением MMO.

## Границы слоёв

| Слой | Исходники | Ответственность |
|---|---|---|
| Domain/Core | `MirState.cs`, `MirEconomy.cs`, `MirSimulationLoop.cs`, `MirCitizenDecision.cs`, `MirWorldGeometry.cs`, `MirHeadlessPopulation.cs` | Целочисленные деньги, запасы, NPC, товары, фиксированный шаг, грузовые заказы. Без UnityEngine; общий код с .NET |
| Backend | `Server/Nonation.Server/WorldAuthority.cs`, `WorldSimulationWorker.cs`, `Program.cs` | Один авторитетный headless-мир, bounded JSON, atomic save, эксклюзивное владение файлом мира, read-only HTTP |
| Unity Client | `MirGame`, `MirPlayer`, `MirNpcAgent`, `MirCity`, `MirVehicle`, `MirHUD`, `MirPersistence` | Управление, представление, локальная игра, камера, UI, визуализация и сохранение |
| Spectator | `MirSceneSnapshot.cs`, `MirRemoteWorldSync.cs` | Общий DTO v2, `serverId/sequence`, проверка порядка, ограничение нагрузки, CORS/HTTPS |
| Graphics | `MirHumanFactory`, `MirMotionDriver`, `MirAnimationCatalog`, `Assets/MIR/Art` | Выбор персонажа, Humanoid guard, анимации, материалы, диагностический fallback |
| Infrastructure | `.github/workflows/*`, `Tools/*` | Source QA, .NET CI, Unity Cloud Build, WebGL artifact, Pages и браузерные тесты |

Экономические правила принадлежат Domain/Core, а не Unity HUD. Клиент без авторизации не имеет полномочий менять серверные деньги и товар.

## Денежные и товарные инварианты

- Балансы счетов — `int`, суммарный `TotalKnownMoney()` — `long`; `Transfer(id, from, to, amount)` отклоняет повторные идентификаторы и переполнение.
- `MirWorldState.NewId` / freight-ID защищены от wraparound и повтора после перезапуска.
- Этапы грузового заказа: `Created → Loading → Driving → Unloading → Returning → Completed`. Escrow/резервы изменяются строго при согласованных переходах; двойная доставка и повторная выплата запрещены.
- Backend JSON сохраняет всю необходимую receipt-историю, иначе после рестарта возникнет повтор перевода. Простой обрезки истории для обхода ограничения 20 000 нет. Следующее архитектурное решение — независимый transactional SQL-ledger с unique receipt ID, checkpoint/replay и проверяемой миграцией.

## Сервер и сохранения

`Nonation.Server` использует .NET 8, локальный bind `127.0.0.1:5180` по умолчанию. `WorldAuthority` сериализует мутации под `gate` и сохранения под `saveGate`. Отдельный lease запрещает двум процессам писать один мир; staging JSON пишется атомарно с flush и заменой. Загрузка отклоняет повреждённые деньги, некорректный escrow, отрицательные остатки, несовместимые идентификаторы и неверную версию. Это **однопроцессная** архитектура — не кластер и не распределённая БД.

## Unity ↔ Backend

`NetworkConfig.json` по умолчанию задаёт пустой `snapshotUrl`: игра работает offline. При явной настройке `https://.../api/v1/spectator/scene` Unity принимает только публичные позиции жителей с `version=2`, `serverId` и `sequence`, отклоняя невалидные или повторные снимки. HTTP разрешён лишь локально в Unity Editor. Серверный `NONATION_CORS_ORIGINS` должен совпадать с внешним origin. Переводы денег, товары, игрок и команды не синхронизируются.

## Unity/WebGL и графика

Unity `6000.0.58f2`, сцена `Assets/MIR/Scenes/MIR_PlayableDistrict.unity`, `ProjectSettings/EditorBuildSettings.asset` (enabled=1, GUID `d57a17afca7d4fdb9252ba1ea864e3df`) и `InputManager.asset`. Исторический Build №3 упал из-за отсутствия сцены; Build №4 **успешно экспортирован только на исходном Unity SHA** `cb46b496...`. Для общей интегрированной версии нужен отдельный экспорт и браузерный smoke.

Microsoft Rocketbox MIT FBX/6 TGA находятся в `Assets/MIR/Art/Rocketbox/`. Скрипт подготовки скелетных LOD присутствует, но Blender/Unity Avatar, Animator, skin weights и LODGroup пока не прошли runtime QA; при отсутствии корректного рига применяется явно диагностический proxy. Непроверенные и NC ассеты в публичную игру не добавлять.

## CI и доказательства

`Integration Guard` проверяет владение и provenance, `Pull Request Validation` — Python/.NET/HTTP, `Cross-Branch Integration` — объединённый source smoke. **Unity source-validation не эквивалентен компиляции Unity.** Публичный выпуск возможен только после успешного Unity WebGL export конкретного SHA, HTTPS Pages deploy, Chromium сцены/NPC/управления/сохранения.

[Сводка интеграции](INTEGRATION_REPORT_RU.md) · [Лицензии](LICENSES.md) · [Деплой](../DEPLOYMENT_STATUS_RU.md).
