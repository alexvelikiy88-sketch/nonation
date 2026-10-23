# nonation — MIR Dockside · Unity 6

**Исходный Unity-проект браузерного симулятора города** (alpha / vertical slice). Это не запущенная онлайн-игра и не сборка WebGL.

## Что уже находится в GitHub

- 22 C#-модуля под `Assets/MIR/Scripts`, `Assets/MIR/Editor` и `Assets/Editor`: городской район, управление жителем, 39 автономных NPC, экономика, магазин/кафе, машина, грузовой рейс, камера и интерфейс.
- `Packages/manifest.json`, `ProjectSettings` под **Unity 6000.0.58f2**.
- Каталоги `products_60.json`, `professions_36.json` и документация: в Unity alpha эти каталоги ещё **не полностью реализованы как игровые системы**.
- `Tools/fetch_quaternius.py`: скачивает **CC0** Quaternius Universal Animation Library 1 и 2 (Standard) с OpenGameArt и сверяет SHA-256 трёх оригинальных FBX.
- `.github/workflows/unity-webgl.yml`: ручная облачная сборка через GameCI Unity Builder v4 с публикацией ZIP-артефакта после успеха.

**Изначальные модели Maid, Girl, Floral, Casual, GIGN, Pinkdress, Freya и Honda в публичный репозиторий НЕ загружены:** их права на распространение не проверены; Honda имеет лицензию CC BY-NC-SA и не может быть частью коммерческой WebGL-публикации. При отсутствии модели код использует упрощённую процедурную замену, чтобы избежать некорректных T-поз.

## Текущий блокер и инструкция запуска

**Unity WebGL ещё не собран:** Unity Cloud Build #1 и #2 выявили и помогли исправить ошибки CS0103 / CS0117. После исправлений нужна новая сборка Unity Cloud; её успех пока не подтверждён. GitHub Actions source-validation выполняет статические проверки и проверяет SHA-256 трёх CC0 FBX; настоящий Unity Editor в GitHub Actions по-прежнему останавливается из-за отсутствия Unity-лицензии в Actions Secrets. Список проверок и два способа получить онлайн-билд (Unity Cloud без установки или GitHub Actions) — **[DEPLOYMENT_STATUS_RU.md](DEPLOYMENT_STATUS_RU.md)**.

## Сборка онлайн без установки Unity на ПК

1. Подключите репозиторий `alexvelikiy88-sketch/nonation` в Unity Cloud Build Automation, выберите `main`, Unity `6000.0.58f2` и WebGL. В Advanced settings укажите pre-build script `Tools/unity_cloud_prebuild.sh` и pre-export method `NonationCloudBuildHook.PreExport` (для загрузки CC0-анимаций и создания сцены). В GitHub Actions используется `MIR.Editor.MirCloudBuild.Build`.
2. Для последующих автоматических сборок можно один раз включить **Auto-build** в настройках Build Automation (при необходимости потребуется разрешение GitHub Webhooks на токене).
3. Либо используйте [GitHub Actions — MIR Unity WebGL](../../actions/workflows/unity-webgl.yml). Для GameCI потребуется активация Unity через **Actions Secrets** (не размещайте секреты в коде): [GameCI Activation](https://game.ci/docs/github/activation/).
4. При успехе получите готовые WebGL-файлы в разделе Artifacts и разместите их на HTTPS-хостинге. **Публичной игровой ссылки пока нет.**

## Важно о проверках

- C# и игровые правила ранее прошли только статическую проверку структуры: Unity Editor в рабочей среде недоступен.
- Веб-сборка, реальная анимация Humanoid, производительность GPU и долговечность сохранений Unity **ещё не проверены**.
- Временный локальный мир Unity не является переносом всего MIR V10/V11: нет сервера 24/7, синхронизации нескольких игроков или подтверждённой поддержки 5 000 NPC.
- `Assets/MIR/Scenes/MIR_PlayableDistrict.unity` генерируется редакторским скриптом `MIR.Editor.MirUnitySetup` / `MirCloudBuild`.

Полная документация: [README_RU.md](README_RU.md), [GITHUB_SETUP_RU.md](GITHUB_SETUP_RU.md) и [LICENSES.md](Documentation/LICENSES.md).
