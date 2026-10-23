# NONATION / отчёт 01 — Unity/WebGL

- **Дата:** 23.10.26
- **Статус:** IN PROGRESS / P0 EXTERNAL BLOCKER — CI, Cloud-диагностика и браузерный proof-тест подготовлены; лицензированная Unity-сборка и реальный WebGL запуск не подтверждены
- **Ветка:** `agent/unity-webgl`
- **База:** `f52d21a2b0e470bb79664f8be0e6fd0251b82596`
- **Последний кодовый SHA перед актуализацией:** `b5ea79a703e9755e56decdb2a6057887f1a0364b`
- **PR:** [#3 — Unity/WebGL](https://github.com/alexvelikiy88-sketch/nonation/pull/3), Draft, НЕ merge самим.

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


## Этап 2 — обновление 23.10.26 (актуальные изменения после PR #3)

### Что исправлено и чем проверено

| Коммит | Область | Реальный результат |
|---|---|---|
| [`9bbbb62`](https://github.com/alexvelikiy88-sketch/nonation/commit/9bbbb621667a089313aed74d604b0ec75f78bfa1) | `Assets/MIR/Editor/MirCloudBuild.cs` | Убрано переключение WebGL target **во время** batch `-executeMethod`; задан fail-fast код `NONATION_BUILD_TARGET_MISMATCH`, логируются Unity version, BuildReport result/errors/warnings/size, проверяются index/Build/wasm/data/js |
| `67ad216`, `9470de6`, `30e5ff9` | `MirGame.cs`, `MirPlayer.cs`, `MirPersistence.cs` | Единичные runtime-маркеры `NONATION_RUNTIME_READY`, `NONATION_PLAYER_MOVED`, `NONATION_SAVE_WRITTEN`, `NONATION_SAVE_RESTORED` для проверки **реальной** сцены, движения и сохранения в браузере |
| `0e76b31`, `be22979` | `Tools/unity_cloud_ci.py` | `--verify-hosted DIR HTTPS_URL`: HTTP HEAD всех опубликованных файлов, HTTPS и отсутствия HTML вместо wasm/data/js; проверка не притворяется runtime-тестом |
| `ce10603`, `cfcb5ab`, `fce2e6e` | `Tools/tests/test_unity_cloud_ci.py` | Offline-тесты HTTPS-проверки, первичных Cloud ошибок, редактирования секретов; устранена ошибка fixture с literal `\\n` |
| `a954d01`, `d087ff3`, `755c8bb` | `Tools/unity_cloud_ci.py` | Реализован read-only `--diagnose-build 3`: получение лога Unity Build Automation v2 по API, обработка HTTP 303 и поиск первой диагностической ошибки до терминального Export Failure. В будущих Cloud-failure сначала извлекается лог, без автоматического пересоздания билда |
| `f98984f`, `721bdfd`, `305ab0d`, `b5ea79a` | `.github/workflows/unity-webgl.yml` | Облачная сборка только для SHA ветки `main`, проверка GitHub Pages по HTTPS, Chromium после deployment (сцена → герой → WASD → save → reload → restore), read-only workflow_dispatch диагностирование существующего Build, раздельная concurrency для main и PR |

Источник для ограничения target switching: [Unity Editor CLI / -buildTarget](https://docs.unity.com/en-us/engine/6000.0/manual/unity-editor/command-line-arguments/editor). Это **подтверждённый риск в прежнем коде**, а не доказанная причина исторического Build №3 (полного лога нет).

### Подтверждённые внешние проверки

- [CI PR validation №26](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40006107870): **SUCCESS**. Job `Source preflight and .NET 8`: этапы `Unity source and scene preflight`, `Unit-test Unity Cloud API helper`, .NET simulation, compile backend и HTTP persistence smoke — все SUCCESS. Это **не Unity C# compile**, не WebGL.
- [CI Unity №55–57](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40004387662): source-validation SUCCESS; `webgl`, `cloud-webgl`, `deploy` SKIPPED. Последующие запуски на ветке выполняли проверки источников без Unity artifact.
- `main` на момент последней синхронизации: `4eefb0cae4f91bc7b3a3854715112a3b9a378727` (новые изменения менеджера: `.github/workflows/pr-validation.yml`, `TASKS.md`; мы их не редактировали). Кодовая ветка AGENT 01 основана на `f52d21a` и требует интеграционной проверки относительно `main`.
- По Build Automation API доступен documented `GET .../builds/3/log`, однако без авторизованного Cloud service account полные строки №3 получить не удалось. **Нельзя называть первичную ошибку**.

### Проверяемые команды и правила доказательств

```sh
python3 Tools/unity_source_preflight.py
python3 -m unittest discover -s Tools/tests -p 'test_*.py' -v
python3 Tools/unity_cloud_ci.py --diagnose-build 3
python3 Tools/unity_cloud_ci.py --verify-dir build/WebGL
python3 Tools/unity_cloud_ci.py --verify-hosted build/WebGL https://<published-pages-host>/
```

- `--diagnose-build 3` **не запускает** Unity; для чтения требует `UNITY_SA_KEY_ID` и `UNITY_SA_SECRET_KEY` в защищённых GitHub Secrets. Никакие credentials в код/отчёт не вносились.
- `--verify-hosted` проверяет статические HTTP(S) ответы, но только `browser-smoke` может подтвердить WebGL-загрузку сцены, ввод и persistence. Этот job не выполнялся: Unity/WebGL build отсутствует.
- Браузерный тест имеет ограниченное время выполнения; если Chromium runtime падает или пропущен маркер, job FAIL, а не фиктивный SUCCESS. После реального билда отдельно нужны проверки взаимодействия с NPC/магазином/машиной и ошибок GPU на целевых браузерах.
- GameCI/UBA обязаны стартовать с WebGL target уже выбранным. Сборки других веток через Cloud, настроенный на `main`, сознательно запрещены, чтобы не принять артефакт чужого SHA.

### Блокеры / следующий конкретный шаг

1. Для исторического Unity Cloud Build №3 нужен авторизованный доступ к полному логу; первопричина остаётся UNKNOWN.
2. Последнее фактически подтверждённое состояние Unity Cloud/GameCI: соответствующие секреты в GitHub Actions отсутствуют, WebGL jobs SKIPPED. Требуется конфигурация штатных service-account secrets **в GitHub UI**, без публикации в чат, либо законная GameCI-активация.
3. После появления учётных данных: read-only диагностика #3; затем **одна** сборка Unity 6000.0.58f2; проверка BuildReport/исходного SHA; HTTPS artifact verification; Chromium runtime smoke; только после успеха публичный URL можно отмечать как работающий.
4. Собственная ветка сохраняется, PR #3 остаётся Draft, менеджеру передаётся на review; `TASKS.md` не менялся.

### CROSS-OWNER REQUEST — этап 2

Новых подтверждённых CS/WebGL ошибок в чужих файлах нет. Графику AGENT 03, backend AGENT 02, чужие отчёты и `main` не трогали.
