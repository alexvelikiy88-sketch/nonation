# nonation: GitHub + Unity 6 WebGL

Repo: https://github.com/alexvelikiy88-sketch/nonation (публичный).

Основной репозиторий включает C#-исходники, настройки Unity и общедоступные данные.
Модели с непроверенными лицензиями в него НЕ загружайте: Maid, Floral, Casual, Girl, GIGN,
Pinkdress, Freya. Honda Civic BTCC 2023 по CC BY-NC-SA несовместима с коммерческим релизом.

На GitHub Actions подготовлена ручная облачная WebGL-сборка
`.github/workflows/unity-webgl.yml` через game-ci/unity-builder@v4.
При отсутствии лицензии сборка не будет завершена: настройте GitHub Actions secrets
в Settings > Secrets and variables > Actions, по документации https://game.ci/docs/github/activation/.
Никогда не размещайте логин, пароль, сертификат лицензии в исходном коде.

После успешной сборки zip с WebGL будет доступен в GitHub Actions > Run > Artifacts.
Публикация на веб-хостинге/Unity Play ещё НЕ настроена.
В сборке через GitHub Actions клипы Quaternius CC0 автоматически загружаются и проходят SHA256.

Для Unity Cloud Build Automation подключите GitHub repo `alexvelikiy88-sketch/nonation`, ветку `main`,
редактор `6000.0.30f1`, целевую платформу WebGL и prebuild/scene generation через `MIR.Editor.MirCloudBuild`.

Экспорт Unity Editor/WebGL НЕ проверен в ChatGPT; исходная версия была только static audited.

## Unity Cloud Build Automation (без Unity на ПК)

- Source Control → Git → GitHub: `https://github.com/alexvelikiy88-sketch/nonation.git`, branch `main` (public).
- Configure target: WebGL, Editor 6000.0.30f1.
- Advanced → Pre-build script: `Tools/unity_cloud_prebuild.sh` (скачивает CC0 Quaternius со сверкой SHA-256).
- Advanced → Pre-export method: `NonationCloudBuildHook.PreExport` (создаёт сцену при импорте в редактор).
- Configure scene build to `Assets/MIR/Scenes/MIR_PlayableDistrict.unity` if Unity Cloud asks; it is created in pre-export.
- Build → проверить Console и выдать URL только при успешном статусе.

В текущем сеансе нет доступа к управлению Unity Cloud, поэтому сборка не была запущена.
