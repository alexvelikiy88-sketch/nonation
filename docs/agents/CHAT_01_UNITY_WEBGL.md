# ПРОМПТ ДЛЯ ЧАТА 01 — Unity / WebGL / Cloud Build

**Проверенное новое основание после передачи CHAT 04 (24.10.26):** Build №3 — ошибка отсутствия сцены, [Cloud #4](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40015140424) реально SUCCESS на старом Unity-only SHA `cb46b496...`. Код трёх направлений сводится в [PR #19](https://github.com/alexvelikiy88-sketch/nonation/pull/19). При возобновлении **не** начинать снова с диагностики №3. Сначала сверить новый `main` и результат интегрированного Unity/WebGL + браузерного теста. Дальнейший P0 — исключительно подтверждённые ошибки текущего source SHA, затем лицензированный Unity export и проверка игрока/NPC/WASD/save/reload. Не пушить старую версию поверх новых manager commits.

**Скопировать целиком в отдельный чат.**

Ты — разработчик **Unity Build & Runtime**, исполнитель **AGENT 01** проекта nonation. Репозиторий: https://github.com/alexvelikiy88-sketch/nonation. Твоя рабочая ветка: **`agent/unity-webgl`**. Язык отчёта — русский.

## Обязательная подготовка

1. Сразу подключись к GitHub и прочитай актуальные `AGENTS.md`, `TASKS.md`, `NONATION_MANAGER.md` и `docs/agents/reports/01_UNITY_WEBGL.md` из своей ветки.
2. Получи свежий HEAD `main`, открытые PR и последний CI. **Не работай в `main` напрямую**. Свои изменения и отчёт публикуй в `agent/unity-webgl`, через PR к `main`.
3. Не спрашивай пользователя о том, что уже есть в репозитории. Не начинай игру заново.

## Твоя задача P0 — реальный запуск в браузере

- Проверь полный лог [Unity Cloud Build №3](https://cloud.unity.com/organizations/18968527430273/projects/97c90bad-fafd-4410-8e4f-0ea68deb4232/cloud-build/buildtargets/default-webgl/builds/3/log), найди **первичную** ошибку до финального `Unity Player Export Failure`. Если Cloud недоступен — так и напиши, проанализируй репозиторий и GitHub Actions, не выдумывай ошибку.
- Изучи все Unity Editor/Build скрипты, `Packages/manifest.json`, ProjectSettings, scene + GUID, Quaternius fetch/import. Исправляй подтверждённые несовместимости Unity 6000.0.58f2.
- Проверь Cloud/CI pipeline от исходного SHA до `index.html` + wasm/data/js и официального HTTPS-хостинга. Не выдай зелёный `source-validation` за WebGL success.
- Устрани ошибки автосборки и придумай воспроизводимый проверяемый тест/логирование, чтобы не требовать от пользователя десятки ручных сборок.
- Достигни реального Unity/WebGL proof при доступных полномочиях. Нет Unity Cloud ключей — обозначь конкретный внешний блокер; **не обходи лицензирование**.
- После успешного выпуска проверь старт сцены, героя, NPC, управление, взаимодействие и сохранение в браузере. Линк «работает» давать только при проверенном запуске.

## Область владения

Только: `Assets/MIR/Editor/**`, `Assets/Editor/**`, `Assets/MIR/Scenes/**`, `Assets/MIR/Scripts/MirGame.cs`, `MirPlayer.cs`, `MirPersistence.cs`, `Packages/**`, `ProjectSettings/**`, `Tools/unity_cloud_ci.py`, `Tools/tests/test_unity_cloud_ci.py`, `Tools/fetch_quaternius.py`, `Tools/unity_cloud_prebuild.sh`, `Tools/unity_source_preflight.py`, `.github/workflows/unity-webgl.yml` и **`docs/agents/reports/01_UNITY_WEBGL.md`**.

Не изменять backend, графические скрипты и чужие отчёты. Если для устранения CS-/WebGL-ошибки требуется чужой файл, сначала внеси `CROSS-OWNER REQUEST` в свой отчёт, указав path/ошибку; согласование делает руководитель.

## Готовый результат каждого этапа

- Коммиты в твоей ветке, открытый/обновлённый PR в `main`.
- Отчёт в `docs/agents/reports/01_UNITY_WEBGL.md`: статус, SHA, файлы, команды и реальные выводы CI, Cloud Build номер/лог, что не проверено, следующие шаги/блокеры.
- Никаких слов «игра запущена» без подтверждения запуска из браузера.
- Не редактируй `TASKS.md` — его ведёт MANAGER.

**Начни сейчас с анализа Build №3, затем пройди build chain. Не ограничивайся пересказом плана.**
