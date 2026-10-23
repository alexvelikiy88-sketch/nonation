# nonation — GitHub + Unity Cloud 6 / WebGL

Публичный репозиторий: https://github.com/alexvelikiy88-sketch/nonation

## Актуальное состояние на 23.10.26

Unity 6 версия `6000.0.58f2`. Unity Cloud Build №3 завершился с `Unity Player Export Failure`. GitHub Actions №50 успешно проверил общее .NET-ядро, серверный API и Python preflight, **не** выполняя Unity-компиляцию.

## Конфигурация сборки

- `main`, WebGL, target `default-webgl`.
- `Assets/MIR/Scenes/MIR_PlayableDistrict.unity` и `ProjectSettings/EditorBuildSettings.asset` находятся в Git.
- Unity Cloud pre-build script: `Tools/unity_cloud_prebuild.sh`, pre-export: `NonationCloudBuildHook.PreExport`.
- Для автоматического пути без Unity на ПК используйте Unity service account с разрешением Build Automation и храните пару `UNITY_SA_KEY_ID`/`UNITY_SA_SECRET_KEY` только в GitHub Actions secrets.
- `Tools/unity_cloud_ci.py` (официальный REST API v2) запускает/отслеживает один билд, ждёт результат, проверяет исходный SHA и структуру WebGL, затем GitHub Actions может опубликовать артефакт в Pages.
- Альтернатива — GameCI + официальная Unity activation в GitHub secrets. Если она настроена, CI предпочитает GameCI, чтобы не дублировать облачные сборки.
- В настройках GitHub Pages источник публикации: **GitHub Actions**.

**Ни одна из этих интеграций пока не подтверждает успешного Unity WebGL**. Без service account или GameCI activation source-validation зелёная, но игровые сборки пропускаются.

Исходные модели Maid/Girl/Floral/Casual/GIGN/Pinkdress/Freya/Honda не размещены в публичном репозитории без необходимых прав. Quaternius CC0 загружается со сверкой SHA-256.

Детали: [DEPLOYMENT_STATUS_RU.md](DEPLOYMENT_STATUS_RU.md), [ARCHITECTURE_RU.md](Documentation/ARCHITECTURE_RU.md).
