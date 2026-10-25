# NONATION — проверенное состояние WebGL и публикации

**Последняя проверка:** 25.10.26. **Unity Cloud #8 и GitHub Pages с интегрированным игровым SHA подтверждены; Chromium доказал частичную работоспособность, но обнаружил массовые runtime Collider-ошибки.** Технический baseline — PARTIAL PASS, полноценный игровой релиз — BLOCKED, художественный релиз — NOT APPROVED.


## Последняя сборка / 25.10.26 — подтверждённый CS0246, публикации нет

- [GitHub Actions #40192802666](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40192802666) (Unity Cloud Build #11), `main@1e1f134e2c16b762760efbddf0bf469aa2120864`: `source-validation` **SUCCESS**, `Unity Cloud WebGL` **FAILURE**, GitHub Pages и Chromium **SKIPPED**. Новая игра не опубликована этим запуском.
- UBA log: `Assets\MIR\Art\Kenney\MirKenneyCityArt.cs(189,47): error CS0246: The type or namespace name 'BuiltInMaterialGenerator' could not be found`.
- Конкретная причина: Unity glTFast 6.0.0 компилирует `BuiltInMaterialGenerator` только при отсутствии `USING_URP` (либо в Editor), а установленный `com.unity.render-pipelines.universal` включал `USING_URP`, хотя игровая сцена использует Built-in Render Pipeline. Доказано исходным [glTFast 6.0.0 snapshot](https://github.com/needle-mirror/com.unity.cloud.gltfast/tree/b05d842446989f0521b1e2283c7e93121d785b29) и актуальным Unity compiler log.
- В отдельном PR предложено удалить **неиспользуемую** зависимость URP и сохранить Built-in shader/material path. Этого **пока недостаточно** для утверждения успешного рендера: обязательны Unity WebGL compile, правильная загрузка Kenney GLB и настоящий Chromium screenshot на одном SHA.
- Последняя подтверждённая опубликованная версия — предшествующий технический билд [#40190193106](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40190193106), где Unity и Pages PASS, но Kenney GLB не загружались из-за MaterialGenerator. Публичный адрес остаётся https://alexvelikiy88-sketch.github.io/nonation/, это **не** подтверждённый графический релиз.

## Актуальный релиз / 25.10.26 — Unity/Pages PASS, Kenney runtime FAIL

- GitHub `main@faf1bdbdcfaa1493cf9798b24fa4cc58464a6093`, [run #40190193106](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40190193106): **Unity Cloud WebGL SUCCESS** (artifact `12597753213`), **Pages HTTPS SUCCESS** (artifact `12598875494`). Адрес публикации: https://alexvelikiy88-sketch.github.io/nonation/.
- **Chromium FAIL**, proof artifact `12598396433`: `NONATION_RUNTIME_READY ... npcs=39 ... vehicles=4 citizens=40` есть; после этого `NONATION_KENNEY_CITY_ERROR: Could not determine default MaterialGenerator (render pipeline BuiltIn)`. `NONATION_KENNEY_CITY_READY` **отсутствует**. Браузер дожидался маркера и завершился ошибкой.
- **Причина доказана журналом:** импорт моделей glTFast останавливается при выборе генератора материалов для активного Built-In pipeline; скомпилированная Unity игра запускается, но вместо новых Kenney-моделей остаётся аварийный legacy-рендер. `NONATION_SHADER_FALLBACK` также присутствует как предупреждение, но главный blocker — material generator.
- **Исправление только предложено:** [PR #35](https://github.com/alexvelikiy88-sketch/nonation/pull/35) вводит явный Built-In glTFast MaterialGenerator, быстрый отказ на ошибках импорта и регрессионное тестирование загрузки/reload; его успешный Unity/WebGL-рендер **пока не подтверждён**. Платную пересборку до оценки PR CI не запускать.
- **VISUAL GATE не пройден.** Скриншот сбойного Chromium не считается доказательством новых 3D, а экономику/сохранения и FPS необходимо проверять в полноценном browser smoke на следующем игровом SHA.

Ниже сохранён исторический подтверждённый baseline 24.10.26.

## Проверенный интегрированный WebGL baseline / CHAT 04, 24.10.26

- **Игровой SHA:** `55051008dee22e3a44d867a6191e7ab3ba6e83b4`. `main@12b3d4a56ad420a63150ecad8d4113c8b806cb89` отличается двумя документационными коммитами; diff не содержит игровых/серверных/Unity configuration изменений.
- **Unity Cloud Build #8:** [GitHub Actions #40024399013](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40024399013), `Unity Cloud WebGL (optional service account)` **SUCCESS**. Проверка `Tools/unity_cloud_ci.py::poll_build` отказывает в публикации при отсутствии или несовпадении **фактического** `lastBuiltRevision` с указанным игровым SHA; успешный путь завершился и выгрузил `nonation-unity-cloud-webgl` artifact **12533916450**, размер 9 645 106 байт. `index.html` и несжатые `.wasm`, `.data`, `.js` успешно проверены. Отдельный GameCI `webgl` job — **SKIPPED**, не путать с завершённой Cloud-компиляцией.
- **Публикация:** в том же run `Publish Unity WebGL to GitHub Pages` **SUCCESS**, `Verify all deployed Unity files over HTTPS` **PASS**, Pages artifact **12533712104**. Unity WebGL доступен по `https://alexvelikiy88-sketch.github.io/nonation/`. Проверка наличия файлов по HTTPS не эквивалентна работающей без ошибок игре.
- **Настоящий Chromium smoke:** [Actions #40028408620](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40028408620) действительно запустил скачанный артефакт этого SHA (не только source validation), браузерный job **SUCCESS**, proof artifact **12533388049**. В логе `NONATION_RUNTIME_READY scene=MIR_PlayableDistrict player=player-001 npcs=39 vehicles=4 citizens=40`; обнаружены Canvas, `NONATION_PLAYER_MOVED`, `NONATION_SAVE_WRITTEN`, `NONATION_SAVE_RESTORED` после reload. **Однако множество console errors**: `Can't add component because class 'CapsuleCollider' doesn't exist!` и `SphereCollider`, прежний smoke не классифицировал их как fatal. Следовательно реальный runtime quality gate — **FAIL**, хотя workflow — **SUCCESS**.
- **Исправление collider stripping в рассмотрении:** [manager PR #30](https://github.com/alexvelikiy88-sketch/nonation/pull/30) на SHA `9fa6e45547dd0c85b0f8de2b9cf391d7301028ca`: `Assets/MIR/link.xml`, усиление browser guard/тестов. Source/.NET/Cross-Branch/Ownership **PASS** ([#40087240266](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40087240266), [#40087240260](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40087240260), [#40087240292](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40087240292)); реальный Unity Cloud/Pages/Chromium по этому PR — **SKIPPED/NOT VERIFIED**. PR Draft.
- **Новая графика отдельна от этого доказательства:** [graphics PR #26](https://github.com/alexvelikiy88-sketch/nonation/pull/26), HEAD `35d73180e22d7487fb65045bab64dd92feb9098d`. [Actions #40079307152](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40079307152) — только source-validation **PASS**, Unity Cloud/WebGL/Pages/Chromium **SKIPPED**. Humanoid Animator/LOD, реальная 3D-сцена, FPS/p50/p95/draw calls/triangles/memory и сходство с утверждённым PNG **NOT VERIFIED**. Художественный выпуск не одобрен владельцем.

**Блокер:** исправленный IL2CPP WebGL runtime без Collider ошибок и повторный Chromium smoke на точном SHA; для нового визуального вертикального среза — отдельный Unity Player/Chromium proof, фактические метрики и явная художественная приёмка владельца. Платную Cloud-сборку без анализа не перезапускать.

## История — завершённая интеграция и первоначальная Pages-настройка (до Cloud #8)

- [PR #19](https://github.com/alexvelikiy88-sketch/nonation/pull/19) принят в `main`, merge SHA `a75602df4ef56e95709a8472eba62d879795c977`; исходные агенты #1/#2/#3 стали `merged` без перезаписывания отчётов.
- [Cross-Branch #40020804529](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40020804529) — **SUCCESS** на совмещённом `main`.
- На старой main-ревизии `47a6424...` Unity Cloud WebGL **SUCCESS**; первый Pages deploy упал на конфигурации, но после выбора Source=`GitHub Actions` повторный **deployment job** [Actions #40019259462, attempt 3](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40019259462) завершился **SUCCESS**, URL `https://alexvelikiy88-sketch.github.io/nonation/`. Опубликован **не новый** объединённый SHA.
- [Интегрированная release-сборка #40020804542](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40020804542) на SHA `a75602df...` запущена отдельно; результат Unity Cloud/Pages/браузера должен быть проверен по финальным jobs.
- **Настройка Pages завершена:** владелец включил Source=`GitHub Actions`; GitHub Actions подтвердил `Confirm Pages configuration` и `Deploy to GitHub Pages` = success. Больше не требовать ручной настройки или нового PAT.
- **Текущий релизный gate:** Unity WebGL build + artifact проверенного SHA `4366103dd439cfda4b65223608f84688084ba998` после shader fix, затем Pages deploy и Chromium smoke; ранний Pages success не достаточен.



## Историческая релизная проверка — 24.10.26 (снимок до Cloud #8)

- Ранняя публикация файлов GitHub Pages: **SUCCESS**, [GitHub Actions #40019259462, attempt 3](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40019259462), URL `https://alexvelikiy88-sketch.github.io/nonation/`, source SHA `47a6424...`.
- Ранний Unity #4 **компилируется**, но реальный [Chromium smoke #40022712096](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40022712096) **FAIL**: `Missing runtime shader`, `NONATION_RUNTIME_READY` отсутствует, хотя `NONATION_SAVE_WRITTEN citizens=40` был. [PR #22](https://github.com/alexvelikiy88-sketch/nonation/pull/22) добавил фактический Shader asset в `Resources` и безопасную проверку `Shader.isSupported`, squash SHA `4366103dd439cfda4b65223608f84688084ba998`. Его Python/.NET/guard CI PASS, WebGL runtime нового SHA отдельно не проверен.
- Интегрированный Unity Cloud Build **№6** в [run #40020804542](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40020804542) **FAILED** на редакторском `CS0266` — `MirIntegrityAudit.cs(39,24)` присваивал `long TotalKnownMoney()` переменной `int`. [PR #24](https://github.com/alexvelikiy88-sketch/nonation/pull/24) исправил на `long` и добавил тесты, merge `55051008dee22e3a44d867a6191e7ab3ba6e83b4`. Предыдущая компиляционная ошибка подтверждена лентой логов, а не предположением.
- [Свежий Unity Cloud/Pages run #40024399013](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40024399013) проверяет объединённый `main@55051008` уже с shader и CS0266 patch. Итог WebGL export, Pages deployment и Browser smoke пока **не подтверждён**. Не заявлять, что игра готова, пока маркеры сцены/NPC/движения/сохранения не пройдут на реальном артефакте.

## Сборки Unity Cloud

| Build | Статус | Доказательства |
|---|---|---|
| №1 | FAIL | Ошибка C# `CS0103` в `MirCity.cs`, исправлена в последующих исходниках |
| №2 | FAIL | Ошибка `CS0117` в `MirUnitySetup.cs`, исправлена |
| №3 | FAIL | Исторический SHA `784a3b56...` не содержал зарегистрированной сцены; полный лог 7 929 строк, строка 7845: `There were no scenes configured to build!`, а `Unity player export failed!` лишь последствие |
| **№4** | **SUCCESS** | Реальный Unity WebGL export **Unity-ветки** `cb46b4960dd8bcd255880f569254dfe9876ea14b`, архив `nonation-agent01-preview` проверен и опубликован в [Actions #40015140424](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40015140424). Игра по Pages из него не проверена |
| **№8** | **SUCCESS** | Реальный интегрированный Unity Cloud WebGL export `55051008dee22e3a44d867a6191e7ab3ba6e83b4`, [#40024399013](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40024399013), artifact 12533916450, Pages PASS. Chromium частично исполняет игру, но Collider-errors: runtime FAIL |

## Объединение трёх направлений

Интегратор подготовил [PR #19](https://github.com/alexvelikiy88-sketch/nonation/pull/19), базовый 4-parent Git commit `52d3bd8a37b5fa507789db6d476e29cd54a6300f`. Точные три агентские SHA и manifest изменений — [INTEGRATION_REPORT_RU.md](Documentation/INTEGRATION_REPORT_RU.md). Оригинальные агенты и их отчёты не перезаписывались; manager-authored записи помечены отдельно.

**GitHub Actions на исходном объединённом SHA:**

- [Integration Guard #40019467544](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40019467544): SUCCESS.
- [PR Validation #40019467542](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40019467542): SUCCESS, Python tests + .NET 8 domain/server + HTTP/CORS/spectator smoke.
- [Cross-Branch #40019467534](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40019467534): SUCCESS, совместные исходники.
- `MIR Unity WebGL` на PR: **source-validation SUCCESS**, а ``webgl/cloud-webgl/deploy/browser-smoke` пропущены. Это **не подтверждает Unity компиляцию интегрированного кода**.

## Последовательность реального запуска

1. Финальный PR проходит Integration Guard, Python/.NET и review, без опасных ассетов.
2. Официальная Unity Cloud Build Automation либо лицензированный GameCI экспортирует **тот же source SHA**, который собирается. Проверка `lastBuiltRevision` обязательна, нельзя выдавать `requestedRevision` за фактическую сборку.
3. Проверить непустые `index.html` и `Build/*.wasm`, `*.data`, `*.js`; при Pages без custom Content-Encoding выгружать несжатые файлы.
4. `actions/upload-pages-artifact` → `actions/deploy-pages`; проверить HTTPS и Content-Type всех файлов.
5. `browser-smoke` (Chromium) проверяет `NONATION_RUNTIME_READY`, сцену, Canvas, NPC, `NONATION_PLAYER_MOVED`, `NONATION_SAVE_WRITTEN` и `NONATION_SAVE_RESTORED` после reload, а также критические ошибки Console.

**Опубликованный URL:** `https://alexvelikiy88-sketch.github.io/nonation/` — актуальная подтверждённая публикация файлов интегрированного игрового SHA `55051008...` из Cloud #8 [#40024399013](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40024399013). **Полный runtime FAIL** из-за отсутствующих Collider типов, несмотря на частичный Chromium gameplay PASS [#40028408620](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40028408620). История ошибок старого shader приведена выше.

## Доступ и ограничения

Unity Cloud target: organisation `18968527430273`, project `97c90bad-fafd-4410-8e4f-0ea68deb4232`, target `default-webgl`, Editor `6000.0.58f2`, scene `Assets/MIR/Scenes/MIR_PlayableDistrict.unity`. Service-account secrets `UNITY_SA_KEY_ID` / `UNITY_SA_SECRET_KEY` задаются через защищённые GitHub Actions Secrets, не печатаются и не коммитятся. GameCI activation — альтернативный легальный механизм.

Интегрированный Unity Editor C#/IL2CPP экспорт доказан **только для игрового SHA `55051008...`** (Cloud #8). Браузерная игра запускается и частично функционирует, однако Collider Console ошибки блокируют полный gameplay PASS; Humanoid/Animator/LOD в новом PR, FPS/draw calls и MMO-сервер 24/7 также не доказаны. Нельзя запускать несколько конкурирующих Cloud-сборок без выяснения причин сбоев.
