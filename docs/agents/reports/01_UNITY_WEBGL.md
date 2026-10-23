# NONATION / отчёт 01 — Unity/WebGL

- **Дата:** 23.10.26
- **Статус:** IN PROGRESS / P0 BLOCKED — проверки кода и артефактов улучшены, Unity export не подтверждён
- **Ветка:** `agent/unity-webgl`
- **База:** `f52d21a2b0e470bb79664f8be0e6fd0251b82596`
- **Последний кодовый SHA перед этим отчётом:** `ee4c3a6705c6d41080d941c49aa40c7c561c685a`
- **PR:** см. PR из `agent/unity-webgl` в `main` (создаётся после коммита отчёта).

## Фактически проверено

1. Прочитаны `AGENTS.md`, `TASKS.md`, `NONATION_MANAGER.md` и данный отчёт в рабочей ветке. Исходный `main` и `agent/unity-webgl` совпадали на `f52d21a`, открытых PR не было.
2. Unity Cloud Build №3 (https://cloud.unity.com/organizations/18968527430273/projects/97c90bad-fafd-4410-8e4f-0ea68deb4232/cloud-build/buildtargets/default-webgl/builds/3/log): полный лог извлечь не удалось; документирована лишь конечная ошибка `Unity Player Export Failure`. **Первопричина не установлена**.
3. GitHub Actions [№54](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40001657334), SHA `f909e47ce764911281eee2b97f9b6568a83779d6`: `source-validation=success`; `webgl=skipped`, `cloud-webgl=skipped`, `deploy=skipped`. Реальный job-log содержит notices об отсутствии `UNITY_SA_KEY_ID`/`UNITY_SA_SECRET_KEY` и GameCI activation secrets. Артефакты сборки отсутствуют. Quaternius CC0 проверка SHA-256 прошла.
4. Изучены Editor hooks, `MirUnitySetup`, `MirCloudBuild`, импорт Humanoid, `Packages/manifest.json`, ProjectVersion `6000.0.58f2`, scene, scene meta и `EditorBuildSettings`. GUID `d57a17afca7d4fdb9252ba1ea864e3df` и bootstrap `a8847aa4651b4595ad3504b7755d83e2` совпадают; статические проверки не доказывают, что Unity импортирует сцену и компилирует код.
5. Отдельно сверены игровые `MirGame`, `MirPlayer`, `MirPersistence`, `MirFollowCamera`. Камера получает `MainCamera` в `MirFollowCamera.Init()` — дефект назначения тега **не подтверждён**, менять код камеры не требуется.

## Изменения в своей ветке

| Коммит | Файл | Что изменено |
|---|---|---|
| `761252f` | `Tools/unity_cloud_ci.py` | Cloud success теперь требует точного SHA; добавлена команда `--verify-dir` для полного `index.html` + wasm/data/js и отказ от сжатых файлов для GitHub Pages |
| `6bc751e` | `.github/workflows/unity-webgl.yml` | Включены CI для PR в main и ветки agent/unity-webgl; публикация Pages разрешена только из main; проверка GameCI использует общий валидатор |
| `ee4c3a6` | `Tools/tests/test_unity_cloud_ci.py` | Добавлены регрессии на отсутствие каждого обязательного WebGL-файла, пустой HTML и Brotli |

## CI, команды, реальные свидетельства

- Baseline: [GitHub Actions №54](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40001657334) — SUCCESS **только source-validation**.
- Новый CI: [№55](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40004260659) — инициирован коммитом `6bc751e`; при последней проверке in_progress.
- Новый CI: [№56](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40004284432) — инициирован SHA `ee4c3a6`; при последней проверке pending.
- Доступные команды CI: `python3 Tools/unity_source_preflight.py`; `python3 -m unittest discover -s Tools/tests -p 'test_*.py' -v`; `python3 Tools/fetch_quaternius.py`; `python3 Tools/unity_cloud_ci.py --verify-dir build/WebGL`.
- Локальный `git clone` в текущем runtime не выполнен из-за `Could not resolve host: github.com`, поэтому результаты локального запуска этих тестов **не заявляются**. Коммиты выполнены GitHub API.

## Не проверено / блокеры

- Нет доступа к полному Unity Cloud Build №3 логу, неизвестна первичная ошибка до `Unity Player Export Failure`.
- В GitHub Actions №54 необходимые Unity Cloud и GameCI secrets отсутствовали; после коммитов доступность credentials и исход Unity WebGL job не подтверждены.
- Отсутствуют Unity Editor compilation, IL2CPP, полноценный WebGL artifact, проверенное HTTPS-развёртывание и браузерный runtime (сцена, NPC, ввод, взаимодействие, сохранение, Console).
- Новый валидатор проверяет только структуру статических артефактов, **не фактическую загрузку Unity**.
- Unity Cloud Build trigger может собирать branch `main`; исходный SHA обязан совпадать с SHA workflow, иначе публикация блокируется. Для рабочих PR нужен отдельно настроенный Cloud target либо лицензированный GameCI.
- Требуется реальный Cloud build с доступными логами и credentials; лицензирование не обходить.

## CROSS-OWNER REQUEST

Нет доказанной ошибки в файлах чужого владельца. Следующая Unity CS-ошибка (если возникнет) должна быть скопирована с точным путём и строкой сюда до запроса менеджеру.

## Следующий шаг

Дождаться результатов новых GitHub Actions и исправить конкретные failures. Затем выполнить настоящий Unity 6000.0.58f2 WebGL build, проверить артефакты и HTTPS, выполнить браузерный smoke-test перед заявлением о работающей игре. `TASKS.md` не редактировался.
