# nonation — состояние онлайн-запуска

**Онлайн-игры Unity сейчас нет.** Репозиторий содержит исходники и настроенную цепочку публикации, но последние проверки WebGL остановлены лицензированием.

## Что действительно проверено

- Unity 6 исходники находятся в `main`: `Assets/`, `Packages/`, `ProjectSettings/`.
- Source validation успешно завершён в [GitHub Actions](https://github.com/alexvelikiy88-sketch/nonation/actions).
- Последняя диагностированная неудача: `Missing Unity License File and no Serial was found`. Это означает, что Unity Editor **не начал** компиляцию.
- Автосборка определена в `.github/workflows/unity-webgl.yml`, публикация — через официальный GitHub Pages deployment action, **только после успешного WebGL**.
- WebGL-публикация не зависит от файлов с неясными правами: модели Maid, Girl, Floral, Casual, GIGN, Pinkdress, Freya и Honda исключены из публичной цепочки. Quaternius Standard CC0 скачивается на сборке.

## Вариант A: Unity Cloud Build Automation (не нужно устанавливать Unity на ПК)

Если требуется только браузер, используйте Unity Cloud:

1. Откройте [Unity Cloud](https://cloud.unity.com/) → проект `nonation` → **DevOps → Build Automation → Settings → Source Control**.
2. Подключите GitHub `alexvelikiy88-sketch/nonation`, ветку `main`.
3. **Configurations → New target → WebGL/Web**, Unity Editor `6000.0.30f1`. Дождитесь проверки редактора в списке поддерживаемых версий; при его отсутствии смена версии требует совместимого изменения `ProjectSettings/ProjectVersion.txt`.
4. **Advanced → Pre-build script**: `Tools/unity_cloud_prebuild.sh`; **Pre-export method**: `NonationCloudBuildHook.PreExport`.
5. Запустите **Build**. Это первая настоящая компиляция проекта: она может выявить C# ошибки, не видимые статической проверке.
6. После статуса **Success** получите ZIP WebGL-файлов в Build History. Для публичного URL опубликуйте их на [Unity Play](https://play.unity.com/) или другом HTTPS-хостинге. Сам успех облачной сборки не создаёт автоматически публичную ссылку.

Этот путь использует лицензирование Unity Cloud на его стороне и не требует локального Unity Editor. Для автоматического переноса результата Unity Cloud на GitHub Pages впоследствии понадобится отдельная авторизованная интеграция.

## Вариант B: GitHub Actions + Pages

1. Откройте [Secrets and variables → Actions](https://github.com/alexvelikiy88-sketch/nonation/settings/secrets/actions).
2. Для Unity Personal, согласно [GameCI Activation](https://game.ci/docs/github/activation/), задайте `UNITY_LICENSE` (содержимое активного ULF), `UNITY_EMAIL`, `UNITY_PASSWORD`. Для Pro вместо `UNITY_LICENSE` используйте `UNITY_SERIAL`. **Ничего из этого нельзя отправлять в чат или коммитить в GitHub.**
3. Откройте [Settings → Pages](https://github.com/alexvelikiy88-sketch/nonation/settings/pages) → Source = **GitHub Actions**.
4. Запустите [MIR Unity WebGL → Run workflow](https://github.com/alexvelikiy88-sketch/nonation/actions/workflows/unity-webgl.yml) на `main`. В будущем workflow также запускается автоматически при изменениях игровых исходников.
5. Проверьте `source-validation`, `webgl` и `Publish Unity WebGL to GitHub Pages` — **все должны быть зелёными**. Только после этого можно делиться ссылкой `https://alexvelikiy88-sketch.github.io/nonation/`.

Для Unity Personal GameCI документирует получение лицензии через Unity Hub. Если установка на компьютер неприемлема, предпочтительнее вариант A.

## Важные ограничения

- Unity Editor не проверял C# компиляцию этого проекта. Возможны дополнительные исправления после первой сборки.
- Имеющийся Unity-проект — самостоятельный playable district / vertical slice, а не полный перенос движка V10/V11.
- При появлении WebGL-ссылки сначала проверьте загрузку сцены, Animator, автомобили, сохранение, мобильный UI и работу браузерных запросов. Только прошедшая проверку ссылка считается рабочей.
