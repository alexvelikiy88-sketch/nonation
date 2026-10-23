# nonation — состояние WebGL и проверки

**24.10.26. Публичная работающая Unity WebGL-сборка не подтверждена.**

### Подтверждено

- Unity Cloud Build №1: `CS0103` в `MirCity.cs`; исправлено.
- Unity Cloud Build №2: `CS0117` в `MirUnitySetup.cs`; исправлено.
- Unity Cloud Build №3: **FAILED**, `Unity Player Export Failure` в конце журнала (строка 7897). Эта строка описывает итог, а не точную первопричину. Детальные предшествующие строки полного Unity-лога ещё не проанализированы.
- [GitHub Actions №50](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/39994762908): **source-validation SUCCESS** — Python preflight, настоящая .NET 8-компиляция, тесты экономики, HTTP smoke-test запущенного сервера и сверка SHA-256 анимационных FBX. WebGL/Pages в этом запуске **SKIPPED** по причине отсутствия GameCI activation.
- Unity-сцена теперь находится непосредственно в Git: `Assets/MIR/Scenes/MIR_PlayableDistrict.unity`. Ссылки на `MirGame` и `EditorBuildSettings` зафиксированы GUID.
- Unity Editor + IL2CPP + WebGL не проверены в текущей среде после архитектурных исправлений. Не считать статические тесты проверкой Unity.

### Один автоматизированный конвейер

`main → GitHub source-validation → Unity WebGL → проверка артефактов → GitHub Pages`

Работают два **альтернативных** способа собрать WebGL (не активировать оба одновременно):

1. **GitHub GameCI:** `UNITY_LICENSE` (или `UNITY_SERIAL`) + `UNITY_EMAIL` и `UNITY_PASSWORD` в GitHub Actions secrets. В этом варианте сам GitHub runner компилирует Unity и публикует WebGL.
2. **Unity Cloud API v2:** сервисный аккаунт Unity с ролью Build Automation и GitHub secrets `UNITY_SA_KEY_ID`, `UNITY_SA_SECRET_KEY`. При отсутствии активного GameCI CI запускает или отслеживает облачный билд `default-webgl`, проверяет SHA исходного коммита и финальный Success, безопасно скачивает WebGL ZIP и передаёт его в GitHub Pages. Код: `Tools/unity_cloud_ci.py`. **Этот путь подготовлен, но ещё не испытан с реальным Unity service account.**

Если в Unity Cloud уже включён Auto-build, скрипт **не запускает вторую сборку** — пытается найти и дождаться сборки того же SHA.

**Важно:** Unity Cloud и GitHub Actions используют разные механизмы лицензирования. Секреты не нужны в репозитории и не должны попадать в чат.

### Проверка Unity Cloud target

- Unity Project: `97c90bad-fafd-4410-8e4f-0ea68deb4232`
- Organization: `18968527430273`
- Target ID: `default-webgl`, GitHub `main`
- Редактор в исходниках: `6000.0.58f2`
- Pre-build script: `Tools/unity_cloud_prebuild.sh`
- Pre-export method: `NonationCloudBuildHook.PreExport`
- Главная сцена: `Assets/MIR/Scenes/MIR_PlayableDistrict.unity`
- [История сборок Unity Cloud](https://cloud.unity.com/organizations/18968527430273/projects/97c90bad-fafd-4410-8e4f-0ea68deb4232/cloud-build/history)

Чтобы GitHub Pages принял артефакт, в репозитории должно быть разрешено деплоить через **GitHub Actions**. Предполагаемый URL `https://alexvelikiy88-sketch.github.io/nonation/` нельзя называть работающим до успешного деплоя и браузерного smoke-test.

### Не завершено

- Авторизованный журнал Unity Cloud Build №3 получен, но первичная ошибка до сих пор не установлена; необходима проверка Build failureDetails/steps и строк с контекстом.
- Реальная компиляция Unity Editor, WebGL/IL2CPP и проверка производительности в браузере.
- Профессиональные Humanoid-персонажи, визуал улиц/машин, адаптивный интерфейс.
- Общее сервер-авторитетное онлайн-состояние: .NET-сервер пока только read-only prototype, Unity-клиент в локальном режиме.

## Подключение Unity к серверу (добавлено после CI №52)

Подготовлены `MirSceneSnapshot`, публичный `/api/v1/spectator/scene`, CORS allowlist и транспорт `MirRemoteWorldSync`. Режим офлайн остаётся значением по умолчанию. Авторитетная серверная симуляция пока передаёт **только NPC-позиции**; игрок и экономика не синхронизированы, сетевые команды не поддерживаются. После коммита нужно выполнить .NET- и HTTP-тесты; Unity Editor/WebGL ещё нельзя считать проверенным.

## Управленческая проверка 24.10.26

- GitHub `main` защищена активным Ruleset `NONATION — Main Protection` (ID `25749583`), обязательная проверка `Ownership and change integrity`; force-push/deletions заблокированы.
- [CI Cross-Branch #40012146794](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40012146794) — SUCCESS: виртуальный Git merge трёх рабочих веток без конфликтов, 48 Python тестов, 6 тестов подготовки Rocketbox, .NET build/domain tests и локальный HTTP/CORS/spectator smoke. Затронутые SHA указаны в журнале CI; код не публиковался.
- Последний отдельно просмотренный [Unity CI #40010115046](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40010115046): `source-validation SUCCESS`, `webgl`, `cloud-webgl`, `deploy`, `browser-smoke` **SKIPPED**. Это не доказательство Unity runtime.
- P0 остаётся: требуется разрешённый доступ к Unity Cloud Build #3 log, затем одна успешная лицензированная Unity WebGL сборка на конкретном SHA, HTTPS-хостинг и запуск в браузере с проверкой Canvas/Console/NPC/input/save.

### Диагностика существующей Unity Cloud сборки — 24.10.26

[GitHub Actions #40013127244](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40013127244) подтвердил корректную авторизацию Unity service-account secrets и read-only доступ к Build №3 (`status=failure`, исходный revision `784a3b56b9d486f458b87cdaf0990920a7d02913`). Извлечено **4 775 строк** журнала без вывода подписанных URL или секретов; автоматический поиск завершился `ROOT CAUSE UNKNOWN`. Этот run **не запускал** нового Unity build: `webgl`, `cloud-webgl`, `deploy` и `browser-smoke` были SKIPPED. Приоритет CHAT 01 — выделить первичную ошибку из существующих логов/failureDetails без повторного build; затем адресный фикс и одно контролируемое WebGL испытание.
