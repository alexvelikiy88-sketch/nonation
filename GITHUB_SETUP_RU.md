# NONATION — GitHub, Unity Cloud, WebGL и Pages

**Проверено 24.10.26.** Unity 6000.0.58f2, target `default-webgl`. Действующая GitHub Unity service-account авторизация позволила собрать [Unity Cloud Build №4](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40015140424) для Unity SHA `cb46b4960dd8bcd255880f569254dfe9876ea14b`. `index.html`, WebAssembly/data/JS были проверены и сохранены в артефакте `nonation-agent01-preview`. Этим **не** подтверждён запуск совмещённой ветки или работа публичного сайта.

## Репозиторий и правила

- `main` защищена [Ruleset 25749583](https://github.com/alexvelikiy88-sketch/nonation/settings/rules/25749583): только PR, обязательный `Ownership and change integrity`, без force-push/deletion.
- Агентские ветки `agent/unity-webgl`, `agent/backend-simulation`, `agent/graphics-ui` и их личные отчёты — источники оригинальных изменений.
- [Интеграционный PR #19](https://github.com/alexvelikiy88-sketch/nonation/pull/19) **MERGED** в `main`, SHA `a75602df4ef56e95709a8472eba62d879795c977`. История всех трёх агентов сохранена; их отчёты содержат только явно помеченные управленческие дополнения.
- `main` может быть выпущена на Pages только после реальной WebGL-сборки конкретного commit и browser smoke.

## Unity Cloud Build Automation

Организация `18968527430273`, проект `97c90bad-fafd-4410-8e4f-0ea68deb4232`, target `default-webgl`; WebGL, prebuild `Tools/unity_cloud_prebuild.sh`, pre-export `NonationCloudBuildHook.PreExport`. Включённая сцена: `Assets/MIR/Scenes/MIR_PlayableDistrict.unity`, Build Settings: `ProjectSettings/EditorBuildSettings.asset`, GUID `d57a17afca7d4fdb9252ba1ea864e3df`.

Секреты `UNITY_SA_KEY_ID` и `UNITY_SA_SECRET_KEY` задаются только через GitHub Actions Secrets (их значения нигде не публиковать). GameCI требует легальной активации `UNITY_LICENSE` или `UNITY_SERIAL` + учётные данные и является альтернативой Cloud, а не способом обхода лицензии.

Для Cloud CI `Tools/unity_cloud_ci.py` проверяет реальный `lastBuiltRevision == GITHUB_SHA` и архив. Исторический Build №3 упал из-за отсутствия сцены: строка 7845 полного лога. Unity Cloud №4 на исправленном отдельном SHA прошёл. Не запускать одновременно независимые Cloud/ GameCI сборки на одном source revision.

## Обязательное включение GitHub Pages

[GitHub Actions #40019259462](https://github.com/alexvelikiy88-sketch/nonation/actions/runs/40019259462) подтвердил **успешную Unity Cloud сборку** старого `main`, но `Publish Unity WebGL to GitHub Pages` завершился `Get Pages site failed: Not Found`. Это означает, что GitHub Pages не включён/не задан источник GitHub Actions.

**Владелец:** [открой Settings → Pages](https://github.com/alexvelikiy88-sketch/nonation/settings/pages) → `Build and deployment` → `Source` = **GitHub Actions** → сохранить. Это единственное необходимое сейчас ручное действие. Нет необходимости создавать новый GitHub PAT: `configure-pages@v5 enablement:true` требует токен с повышенными правами, а обычный `GITHUB_TOKEN` не подходит.

После включения Pages можно перезапустить только упавший `deploy` job для интегрированной сборки, если её WebGL artifact и SHA подтверждены. Не нужно заново собирать Unity лишь ради повторного `configure-pages`.

## Последовательность выпуска

1. Подтверждение CI `Ownership and change integrity`, source/.NET, интеграционных и лицензионных тестов для итогового PR.
2. Одна успешная Unity Cloud или GameCI **WebGL** сборка итогового `main` SHA с проверкой обязательных файлов.
3. `.github/workflows/unity-webgl.yml` загружает Pages-артефакт, `actions/deploy-pages` публикует сайт по HTTPS. В GitHub Pages Source должен быть выбран **GitHub Actions**.
4. `browser-smoke` запускает Chromium, ждёт `NONATION_RUNTIME_READY`, проверяет canvas/39 NPC, движение `W`, сохранение и восстановление после reload. Если браузер падает, выпуск не считать проверенным.
5. После проверки зафиксировать ссылку и SHA в `DEPLOYMENT_STATUS_RU.md` и `TASKS.md`.

Веб-адрес `https://alexvelikiy88-sketch.github.io/nonation/` — **предполагаемый**, не объявлять рабочим до выполнения пункта 4.

[Статус](DEPLOYMENT_STATUS_RU.md) · [GitHub Actions](https://github.com/alexvelikiy88-sketch/nonation/actions) · [История Unity Cloud](https://cloud.unity.com/organizations/18968527430273/projects/97c90bad-fafd-4410-8e4f-0ea68deb4232/cloud-build/history).
