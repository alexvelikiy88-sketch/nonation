# NONATION — Unity 6 / браузерный городской симулятор

**Обновление 24.10.26.** Это живой Unity/C# проект. Первая настоящая Unity WebGL-сборка уже успешно прошла — [Build №4](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40015140424), исходный SHA `cb46b496...`, архив `nonation-agent01-preview`. Она относится к Unity-ветке без поздних изменений Backend и Graphics. Объединённый исходный срез принят в `main` через [PR #19](https://github.com/alexvelikiy88-sketch/nonation/pull/19) (merge `a75602df4e...`). Его Unity Cloud сборка запущена отдельно: [CI #40020804542](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40020804542). Старый Cloud export уже проходил, но публикация Pages упала из-за `Get Pages site failed: Not Found`; требуется включить [Settings → Pages](https://github.com/alexvelikiy88-sketch/nonation/settings/pages) → Source = `GitHub Actions`. Браузерный запуск пока не подтверждён.

## Комплектация GitHub

Unity Editor `6000.0.58f2`, сцена `Assets/MIR/Scenes/MIR_PlayableDistrict.unity` c фиксированными Build Settings и GUID, C# для 39 NPC, игрока, потребностей, работы, рынка, кафе, склада, производства, грузоперевозок, процедурного города и транспорта. Общие алгоритмы экономического мира используются также в `.NET 8`. В отличие от исторических полных MIR V10/V11 архивов, текущая публичная кодовая база **не реализует** 5 000 NPC, все 60 товарных цепочек, все 36 профессий и MMO-сервер.

**Модели:** в `Art/Rocketbox` имеются оригинальный Microsoft Rocketbox MIT FBX, 6 проверенных TGA 1024 и лицензия. Скрипт создания скелетных LOD есть, но его реальное выполнение Blender и проверка Unity Humanoid/Animator/LODGroup пока отсутствуют. Не называть эту заготовку финальным NPC. Сомнительные Honda (CC BY-NC-SA), Maid/GIGN/Pinkdress и другие сторонние модели не включать без проверки прав.

## Проверка локально в Unity

1. Открыть корень репозитория в Unity Hub, редактор **6000.0.58f2** с Web Build Support.
2. Дождаться импорта Packages, Quaternius FBX и компиляции Editor/C#.
3. Проверить `Assets/MIR/Scenes/MIR_PlayableDistrict.unity` и включение сцены в Build Settings.
4. Запустить Play, проверить управление, NPC, покупки, грузовик и сохранение.
5. Для воспроизводимого WebGL использовать `MIR.Editor.MirCloudBuild.Build` при уже выбранном WebGL target. Не менять target внутри batch `-executeMethod`. Проверить `Build/WebGL/index.html`, `Build/*.wasm`, `*.data`, `*.js`.

## Управление прототипом

| Клавиша | Действие |
|---|---|
| WASD / стрелки | Ходить / управлять машиной |
| Shift | Бежать |
| E | Взаимодействие: работа, покупка, отдых |
| F | Войти или выйти из автомобиля |
| R | Съесть товар из инвентаря |
| Tab | Режим камеры |
| ПКМ + мышь / колесо | Поворот и масштабирование камеры |
| HUD | Заказы, пауза, сохранение |

## Backend и сетевой режим

`Server/Nonation.Server` хранит отдельный авторитетный мир с финансовыми инвариантами, сохранением и ограниченной историей операций. API `GET /api/v1/spectator/scene` возвращает только публичные данные NPC; чтение в Unity опционально. Для тестирования с внешним сервером нужны отдельный HTTPS-хостинг backend и точный CORS origin. **Без авторизации не добавлять writable endpoints**, не смешивать локальный Unity state и серверные транзакции.

## CI и публикация

Проверки: `NONATION Integration Guard`, `Pull Request Validation`, `Cross-Branch Integration`, `MIR Unity WebGL`. Обычная зелёная Python/.NET проверка **не равна** WebGL-сборке; Unity Cloud Build №4 подтвердил именно player export с отдельным SHA. Для публикации необходимы ещё проверенная интегрированная WebGL-сборка, GitHub Pages по HTTPS и Chromium smoke со сценой, NPC, WASD и сохранением/перезагрузкой.

Документы: [README](README.md) · [TASKS](TASKS.md) · [DEPLOYMENT](DEPLOYMENT_STATUS_RU.md) · [Архитектура](Documentation/ARCHITECTURE_RU.md) · [Лицензии](Documentation/LICENSES.md) · [Аудит](Documentation/INTEGRATION_REPORT_RU.md).
