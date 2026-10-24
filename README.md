# NONATION — Unity 6 WebGL · симуляция города

**Статус на 24.10.26:** Unity WebGL **реально скомпилирован** в [Cloud Build №4 / Actions #40015140424](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40015140424) для **только Unity-ветки** `cb46b4960dd8bcd255880f569254dfe9876ea14b`. WebGL-артефакт `nonation-agent01-preview` проверен и сохранён в GitHub Actions. Это ещё **не доказательство работы интегрированной игры в браузере**.

**Интеграция завершена:** все три ветки приняты через [PR #19](https://github.com/alexvelikiy88-sketch/nonation/pull/19). [GitHub Pages](https://alexvelikiy88-sketch.github.io/nonation/) теперь **опубликован** (ранний WebGL SHA `47a6424...`), но [настоящий Chromium smoke #40022712096](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40022712096) выявил `Missing runtime shader` и отсутствие готовности игровой сцены на раннем Unity-артефакте. [PR #22](https://github.com/alexvelikiy88-sketch/nonation/pull/22) исправляет shader retention, merge `4366103dd439cfda4b65223608f84688084ba998`; **реальный повторный Unity WebGL + browser proof ещё ожидается**.

## Новая художественная цель — обязательна к исполнению

**24.10.26 владелец утвердил изометрический AA-эталон** — солнечный городской квартал у набережной с детальными жилыми домами, магазинами, кафе, площадью, автомобилями и настоящими людьми. [Полная спецификация, контрольная сумма изображения и VISUAL GATE](Documentation/VISUAL_CANON_RU.md) · [задачи VIS-00…VIS-07](TASKS.md).

**Состояние:** исходная техническая WebGL alpha уже запускается, но её примитивные здания, процедурные персонажи и HUD **не соответствуют целевой графике**. Следующий приоритет — **один законченный игровой квартал** на существующем Unity/Backend, без перезапуска проекта. Финальный игровой кадр по впечатлению должен быть не хуже согласованного концепта; скрин из браузера и измеренные FPS обязательны. Иллюстрация, макет или Blender-рендер сами по себе не закрывают VISUAL GATE.

Ответственные: CHAT 03 — город/персонажи/материалы/LOD/UI; CHAT 01 — Unity сцена/изометрическая камера/WebGL; CHAT 02 — действия NPC/экономика/POI; CHAT 04 — интеграция и художественная приёмка с владельцем.

## Что есть в исходниках

- Unity `6000.0.58f2`, сцена `Assets/MIR/Scenes/MIR_PlayableDistrict.unity`, управление WASD / Shift / E / F / R / Tab, 39 NPC + игрок, транспорт, базовые магазины, работа, грузоперевозки, камера, HUD и локальное сохранение.
- Общая экономика `Assets/MIR/Scripts/MirState.cs` + `MirEconomy.cs` и симуляция без UnityEngine используются также в .NET 8.
- `Server/Nonation.Server`: один авторитетный headless-мир, защищённое атомарное JSON-сохранение с lease, публичный **read-only** `/api/v1/spectator/scene`. По умолчанию слушает `127.0.0.1:5180`, мультиплеерных команд нет.
- `MirRemoteWorldSync`: опциональные spectator v2 снимки с `serverId/sequence`, HTTPS/CORS и защитой от повторов. `NetworkConfig.json` по умолчанию пуст — игра offline.
- Microsoft Rocketbox `Male_Adult_12.fbx` и шесть TGA с закреплёнными хешами и MIT notice находятся в `Assets/MIR/Art/Rocketbox/`. Есть скрипт LOD, **но готовый Humanoid prefab/Animator/LODGroup не подтверждён и игра использует диагностический процедурный fallback**.
- Анимации Quaternius CC0 загружаются через `Tools/fetch_quaternius.py` с SHA-проверкой. Не использовать в публичном релизе Honda NC, Maid/GIGN/Pinkdress и другие ассеты без подтверждённых прав.

## Проверка и выпуск

1. [Integration Guard](.github/workflows/integration-guard.yml) запрещает случайное затирание чужих путей; `main` защищён GitHub Ruleset.
2. [PR Validation](.github/workflows/pr-validation.yml) выполняет Python source/contract tests, .NET 8 tests/build и HTTP smoke.
3. [Cross-Branch Integration](.github/workflows/cross-branch-integration.yml) временно соединяет все три агентские ветки, проверяет совместимость без push в `main`.
4. [MIR Unity WebGL](.github/workflows/unity-webgl.yml) собирает через официальный Unity Cloud или легальную GameCI activation, проверяет точный SHA и WebGL-артефакт; только затем GitHub Pages и Chromium runtime.
5. **Веб-адрес `https://alexvelikiy88-sketch.github.io/nonation/` считать рабочим лишь при подтверждённом Deploy + Browser smoke.**

[Текущие задачи](TASKS.md) · [Архитектура](Documentation/ARCHITECTURE_RU.md) · [Состояние деплоя](DEPLOYMENT_STATUS_RU.md) · [Аудит интеграции](Documentation/INTEGRATION_REPORT_RU.md) · [Лицензии](Documentation/LICENSES.md) · [Управление агентами](AGENTS.md).

## Границы прототипа

Это **первый вертикальный срез**, а не завершённая многопользовательская игра. Отсутствуют постоянный общий сервер 24/7, полноценные семьи/социальные связи, вся экономика 60 товаров/36 профессий, сотни/тысячи высокореалистичных NPC, боеспособные WebGL LOD и измеренные FPS. Цифры из исходной концепции не являются доказательством их реализации.
