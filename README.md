# nonation — MIR Dockside · Unity 6

**Исходный Unity-проект браузерного симулятора города** (alpha / vertical slice). Это не запущенная онлайн-игра и не сборка WebGL.

## Что уже находится в GitHub

- 21 C#-модуль под `Assets/MIR/Scripts` и `Assets/MIR/Editor`: городской район, управление жителем, 39 автономных NPC, экономика, магазин/кафе, машина, грузовой рейс, камера и интерфейс.
- `Packages/manifest.json`, `ProjectSettings` под **Unity 6000.0.30f1**.
- Каталоги `products_60.json`, `professions_36.json` и документация: в Unity alpha эти каталоги ещё **не полностью реализованы как игровые системы**.
- `Tools/fetch_quaternius.py`: скачивает **CC0** Quaternius Universal Animation Library 1 и 2 (Standard) с OpenGameArt и сверяет SHA-256 трёх оригинальных FBX.
- `.github/workflows/unity-webgl.yml`: ручная облачная сборка через GameCI Unity Builder v4 с публикацией ZIP-артефакта после успеха.

**Изначальные модели Maid, Girl, Floral, Casual, GIGN, Pinkdress, Freya и Honda в публичный репозиторий НЕ загружены:** их права на распространение не проверены; Honda имеет лицензию CC BY-NC-SA и не может быть частью коммерческой WebGL-публикации. При отсутствии модели код использует упрощённую процедурную замену, чтобы избежать некорректных T-поз.

## Сборка онлайн без установки Unity на ПК

1. Подключите репозиторий `alexvelikiy88-sketch/nonation` в Unity Cloud Build Automation, выберите `main`, Unity `6000.0.30f1` и WebGL. В конфигурации сборки обеспечьте вызов `MIR.Editor.MirCloudBuild.Build`.
2. Либо используйте [GitHub Actions — MIR Unity WebGL](../../actions/workflows/unity-webgl.yml). Для GameCI потребуется активация Unity через **Actions Secrets** (не размещайте секреты в коде): [GameCI Activation](https://game.ci/docs/github/activation/).
3. При успехе получите готовые WebGL-файлы в разделе Artifacts и разместите их на HTTPS-хостинге. **Публичной игровой ссылки пока нет.**

## Важно о проверках

- C# и игровые правила ранее прошли только статическую проверку структуры: Unity Editor в рабочей среде недоступен.
- Веб-сборка, реальная анимация Humanoid, производительность GPU и долговечность сохранений Unity **ещё не проверены**.
- Временный локальный мир Unity не является переносом всего MIR V10/V11: нет сервера 24/7, синхронизации нескольких игроков или подтверждённой поддержки 5 000 NPC.
- `Assets/MIR/Scenes/MIR_PlayableDistrict.unity` генерируется редакторским скриптом `MIR.Editor.MirUnitySetup` / `MirCloudBuild`.

Полная документация: [README_RU.md](README_RU.md), [GITHUB_SETUP_RU.md](GITHUB_SETUP_RU.md) и [LICENSES.md](Documentation/LICENSES.md).
