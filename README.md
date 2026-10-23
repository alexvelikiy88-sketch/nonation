# nonation — MIR Dockside · Unity 6

**Исходный Unity-проект браузерного симулятора города** (alpha / vertical slice). Это не запущенная онлайн-игра и не сборка WebGL.

## Что уже находится в GitHub

- 24+ Unity C#-модуля: район, NPC, экономика, транспорт, грузовая логистика, камера и интерфейс; общий доменный код также собирается в .NET.
- `Packages/manifest.json`, `ProjectSettings` под **Unity 6000.0.58f2**.
- Каталоги `products_60.json`, `professions_36.json` и документация: в Unity alpha эти каталоги ещё **не полностью реализованы как игровые системы**.
- `Tools/fetch_quaternius.py`: скачивает **CC0** Quaternius Universal Animation Library 1 и 2 (Standard) с OpenGameArt и сверяет SHA-256 трёх оригинальных FBX.
- `.github/workflows/unity-webgl.yml`: CI-проверки, GameCI при наличии активации Unity и опциональная интеграция с Unity Cloud API v2 для получения WebGL и публикации на Pages.

**Изначальные модели Maid, Girl, Floral, Casual, GIGN, Pinkdress, Freya и Honda в публичный репозиторий НЕ загружены:** их права на распространение не проверены; Honda имеет лицензию CC BY-NC-SA и не может быть частью коммерческой WebGL-публикации. При отсутствии модели код использует упрощённую процедурную замену, чтобы избежать некорректных T-поз.

## Текущий блокер и инструкция запуска

**Unity WebGL пока не подтверждён:** сборки Unity Cloud №1 и №2 упали на C#-ошибках, а №3 завершилась `Unity Player Export Failure` (финальная строка 7897; первопричина выше в полном логе пока не установлена). GitHub Actions [№50](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/39994762908) успешно собрал и протестировал общее .NET-ядро и запустил сервер в HTTP smoke-test, но не запускал Unity Editor. Подробнее: [DEPLOYMENT_STATUS_RU.md](DEPLOYMENT_STATUS_RU.md).

## Сборка онлайн без установки Unity на ПК

1. Unity Cloud Build Automation: `main`, Unity `6000.0.58f2`, WebGL. В Advanced settings: pre-build script `Tools/unity_cloud_prebuild.sh` и pre-export `NonationCloudBuildHook.PreExport` (контроллер анимации). Сцена хранится в Git, её больше не требуется создавать перед экспортом.
2. Для последующих автоматических сборок можно один раз включить **Auto-build** в настройках Build Automation (при необходимости потребуется разрешение GitHub Webhooks на токене).
3. Либо используйте [GitHub Actions — MIR Unity WebGL](../../actions/workflows/unity-webgl.yml). Для GameCI потребуется активация Unity через **Actions Secrets** (не размещайте секреты в коде): [GameCI Activation](https://game.ci/docs/github/activation/).
4. Альтернатива GameCI: Unity service account с ключами в GitHub Actions secrets `UNITY_SA_KEY_ID` и `UNITY_SA_SECRET_KEY`. После их настройки CI сможет дождаться успешной Unity Cloud-сборки, проверить её WebGL-артефакты и передать их GitHub Pages. Эта интеграция ещё не проверена с реальными учётными данными. **Публичной игровой ссылки пока нет.**

## Важно о проверках

- C# и игровые правила ранее прошли только статическую проверку структуры: Unity Editor в рабочей среде недоступен.
- Веб-сборка, реальная анимация Humanoid, производительность GPU и долговечность сохранений Unity **ещё не проверены**.
- Временный локальный мир Unity не является переносом всего MIR V10/V11: нет сервера 24/7, синхронизации нескольких игроков или подтверждённой поддержки 5 000 NPC.
- `Assets/MIR/Scenes/MIR_PlayableDistrict.unity` генерируется редакторским скриптом `MIR.Editor.MirUnitySetup` / `MirCloudBuild`.

Полная документация: [README_RU.md](README_RU.md), [GITHUB_SETUP_RU.md](GITHUB_SETUP_RU.md) и [LICENSES.md](Documentation/LICENSES.md).
