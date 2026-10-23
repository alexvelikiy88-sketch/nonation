# NONATION — каноническая архитектура Unity / .NET

**Актуализация 24.10.26.** База — [интеграционный PR #19](https://github.com/alexvelikiy88-sketch/nonation/pull/19). Это вертикальный игровой срез, **не** завершённая MMO.

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
