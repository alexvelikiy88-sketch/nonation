# nonation — состояние онлайн-запуска

**Онлайн-игры Unity пока нет.** Unity Cloud уже запустил компиляцию, но сборки №1 и №2 остановились на ошибках CS0103 (MirCity.cs) и CS0117 (MirUnitySetup.cs). Обе ошибки исправлены в `main`; успешная WebGL-сборка новой ревизии ещё не подтверждена.

**Текущая проверенная база (23.10.26):** GitHub Actions проверил 22 C#-файла структурным анализатором (0 ошибок), корректность JSON и три настоящих CC0 FBX Quaternius по SHA-256. Эти проверки НЕ заменяют Unity Editor и WebGL-сборку.

## Что действительно проверено

- Unity 6 исходники находятся в `main`: `Assets/`, `Packages/`, `ProjectSettings/`.
- Source validation успешно завершён в [GitHub Actions](https://github.com/alexvelikiy88-sketch/nonation/actions).
- GitHub Actions по-прежнему не запускает Unity Editor: нет UNITY_LICENSE/UNITY_SERIAL и учётных данных. Этот сбой относится только к GitHub Actions; в **Unity Cloud** лицензия активируется и C# уже компилировался.
- Автосборка определена в `.github/workflows/unity-webgl.yml`, публикация — через официальный GitHub Pages deployment action, **только после успешного WebGL**.
- До очередной Unity Cloud-сборки добавлены защита от отсутствующей сцены, автозаполнение Build Settings и фиксированная версия редактора **6000.0.58f2** (исправление безопасности). Сами изменения ещё не проверены Unity Editor.
- WebGL-публикация не зависит от файлов с неясными правами: модели Maid, Girl, Floral, Casual, GIGN, Pinkdress, Freya и Honda исключены из публичной цепочки. Quaternius Standard CC0 скачивается на сборке.

## Вариант A: Unity Cloud Build Automation (не нужно устанавливать Unity на ПК)

Если требуется только браузер, используйте Unity Cloud:

1. Откройте [Unity Cloud](https://cloud.unity.com/) → проект `nonation` → **DevOps → Build Automation → Settings → Source Control**.
2. Подключите GitHub `alexvelikiy88-sketch/nonation`, ветку `main`.
3. **Configurations → New target → WebGL/Web**, Unity Editor `6000.0.58f2`. Дождитесь проверки редактора в списке поддерживаемых версий; при его отсутствии смена версии требует совместимого изменения `ProjectSettings/ProjectVersion.txt`.
4. **Advanced → Pre-build script**: `Tools/unity_cloud_prebuild.sh`; **Pre-export method**: `NonationCloudBuildHook.PreExport`.
5. Для автоматических повторных сборок включите **Auto-build** в настройках цели WebGL. Тогда каждый новый коммит в `main` будет запускать Unity Cloud без ручного нажатия Build. Для GitHub Personal Access Token может потребоваться разрешение управлять **Webhooks**; не отправляйте токен в чат.
6. Запустите новую **Build** один раз, если Auto-build пока не включён. Unity Editor/IL2CPP может выявить новые ошибки, которые невозможно исключить одной статической проверкой.
7. После статуса **Success** получите ZIP WebGL-файлов в Build History. Для публичного URL опубликуйте их на [Unity Play](https://play.unity.com/) или другом HTTPS-хостинге. Сам успех облачной сборки не создаёт автоматически публичную ссылку.

Этот путь использует лицензирование Unity Cloud на его стороне и не требует локального Unity Editor. Для автоматического переноса результата Unity Cloud на GitHub Pages впоследствии понадобится отдельная авторизованная интеграция.

## Вариант B: GitHub Actions + Pages

1. Откройте [Secrets and variables → Actions](https://github.com/alexvelikiy88-sketch/nonation/settings/secrets/actions).
2. Для Unity Personal, согласно [GameCI Activation](https://game.ci/docs/github/activation/), задайте `UNITY_LICENSE` (содержимое активного ULF), `UNITY_EMAIL`, `UNITY_PASSWORD`. Для Pro вместо `UNITY_LICENSE` используйте `UNITY_SERIAL`. **Ничего из этого нельзя отправлять в чат или коммитить в GitHub.**
3. Откройте [Settings → Pages](https://github.com/alexvelikiy88-sketch/nonation/settings/pages) → Source = **GitHub Actions**.
4. Запустите [MIR Unity WebGL → Run workflow](https://github.com/alexvelikiy88-sketch/nonation/actions/workflows/unity-webgl.yml) на `main`. В будущем workflow также запускается автоматически при изменениях игровых исходников.
5. Проверьте `source-validation`, `webgl` и `Publish Unity WebGL to GitHub Pages` — **все должны быть зелёными**. Только после этого можно делиться ссылкой `https://alexvelikiy88-sketch.github.io/nonation/`.

Для Unity Personal GameCI документирует получение лицензии через Unity Hub. Если установка на компьютер неприемлема, предпочтительнее вариант A.

## Важные ограничения

- Unity Cloud №2 подтвердил компиляцию runtime-сборки C#, но editor-сборка остановилась на CS0117. Исправленный код, генерация сцены и Unity 6000.0.58f2 пока не проверены облачной сборкой.
- Имеющийся Unity-проект — самостоятельный playable district / vertical slice, а не полный перенос движка V10/V11.
- При появлении WebGL-ссылки сначала проверьте загрузку сцены, Animator, автомобили, сохранение, мобильный UI и работу браузерных запросов. Только прошедшая проверку ссылка считается рабочей.
