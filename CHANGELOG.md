## [1.21.0](https://github.com/randomu3/hqstudio/compare/v1.20.0...v1.21.0) (2026-10-08)


### Features

* **app:** in-app admin guide, own-domain only, drop Gemini ([06668c5](https://github.com/randomu3/hqstudio/commit/06668c5443fa649bad26e82e0d436c91b75be5dd))

## [1.20.0](https://github.com/randomu3/hqstudio/compare/v1.19.6...v1.20.0) (2026-10-07)


### Features

* **api:** configurable first admin account and proxy headers ([1984748](https://github.com/randomu3/hqstudio/commit/198474826128d49bc6327993aa475d53d6eebc14))
* **desktop:** graphical installer ([5c9cf93](https://github.com/randomu3/hqstudio/commit/5c9cf93d962a0b1a61051334b8aabb0296b56117))
* **desktop:** one-click updates for the app and the site ([97f70cd](https://github.com/randomu3/hqstudio/commit/97f70cd0bd90b404cb605bee72869d5ac651588a))
* **desktop:** own Tuna domain in installer and site keys ([a644675](https://github.com/randomu3/hqstudio/commit/a64467549c836f16244cf48c28a15cf0db6ac8ee))
* **desktop:** own Tuna domain in site keys dialog ([30bacd4](https://github.com/randomu3/hqstudio/commit/30bacd4852ad9b07884a934afcefcaa9d8f3a55b))
* **desktop:** report bugs as GitHub issues ([c73377a](https://github.com/randomu3/hqstudio/commit/c73377ad42aff858c1951a67eb5e738a10922678))
* **desktop:** site manager page and GUI uninstall ([21df27f](https://github.com/randomu3/hqstudio/commit/21df27f0783ff603f4d1aa9d9d6cad97d25db66e))
* **docker:** single-PC stack with nginx proxy and optional Tuna tunnel ([8f019ce](https://github.com/randomu3/hqstudio/commit/8f019ce896fc4ecfa090e1d90df13a2c5dfa45a2))

## [1.19.6](https://github.com/randomu3/hqstudio/compare/v1.19.5...v1.19.6) (2025-12-31)


### Refactoring

* **api:** complete API architecture refactoring ([63dfc38](https://github.com/randomu3/hqstudio/commit/63dfc3816119a6f41a2ffe6b3771ee93beb4e3e5))

## [1.19.5](https://github.com/randomu3/hqstudio/compare/v1.19.4...v1.19.5) (2025-12-27)


### Bug Fixes

* **web:** fix TypeScript error in lucide-icons test ([ac95583](https://github.com/randomu3/hqstudio/commit/ac95583cd55b16f07dbb10d1769dedc16604c149))

## [1.19.4](https://github.com/randomu3/hqstudio/compare/v1.19.3...v1.19.4) (2025-12-27)


### Bug Fixes

* **desktop:** map service price from API to PriceFrom ([7f60303](https://github.com/randomu3/hqstudio/commit/7f60303c27ac9b2db160c41df2f893d2c48177e7))

## [1.19.3](https://github.com/randomu3/hqstudio/compare/v1.19.2...v1.19.3) (2025-12-27)


### Bug Fixes

* **desktop:** load featured services from API on dashboard ([7653e9f](https://github.com/randomu3/hqstudio/commit/7653e9fddf04a661e0c5e9ca69271a12a4e87761))

## [1.19.2](https://github.com/randomu3/hqstudio/compare/v1.19.1...v1.19.2) (2025-12-27)


### Bug Fixes

* **desktop:** fix monthly revenue not displaying on dashboard ([b5b1d34](https://github.com/randomu3/hqstudio/commit/b5b1d345c957a4566a222ea0409f5ccadd9ecb13))

## [1.19.1](https://github.com/randomu3/hqstudio/compare/v1.19.0...v1.19.1) (2025-12-27)


### Bug Fixes

* **api:** fix monthly revenue calculation date comparison ([e30893d](https://github.com/randomu3/hqstudio/commit/e30893da9a0f8f5de13c98ea0ca7c7308627fbd1))

## [1.19.0](https://github.com/randomu3/hqstudio/compare/v1.18.1...v1.19.0) (2025-12-26)


### 🚀 Новые возможности

* **desktop:** add AnalyticsView code-behind ([bf38e45](https://github.com/randomu3/hqstudio/commit/bf38e459a324e44ee2e25f4adf2647ded2e0d70d))

## [1.18.1](https://github.com/randomu3/hqstudio/compare/v1.18.0...v1.18.1) (2025-12-26)


### 🐛 Исправления

* **api:** исправлен расчёт выручки на Dashboard ([cf47b9a](https://github.com/randomu3/hqstudio/commit/cf47b9a2a03deca2b296feee204dad40c585a6d0))

## [1.18.0](https://github.com/randomu3/hqstudio/compare/v1.17.0...v1.18.0) (2025-12-26)


### 🚀 Новые возможности

* **desktop:** добавлен статус Отменен в диалог редактирования заказа ([d67e244](https://github.com/randomu3/hqstudio/commit/d67e244ce1f800ffd6cb29aa3c168059b68f76ad))
* **desktop:** добавлен централизованный класс OrderStatus для управления статусами заказов ([0531ce5](https://github.com/randomu3/hqstudio/commit/0531ce51699a39b74d8a02d2b151b15117b5d362))
* **desktop:** добавлена кнопка отмены заказа и рефакторинг на централизованные статусы ([a81cc5a](https://github.com/randomu3/hqstudio/commit/a81cc5a086482e209eda0395659007690b519313))


### 🐛 Исправления

* **api:** исправлен расчёт выручки для заказов без CompletedAt ([df7a63c](https://github.com/randomu3/hqstudio/commit/df7a63c8f47f5136623a4a6b28be5e653ff6c652))
* **desktop:** исправлен расчёт выручки за месяц и добавлена подсказка ([d462b9b](https://github.com/randomu3/hqstudio/commit/d462b9b866da82253839c3e7298f0eb4df3c9a5e))
* **desktop:** исправлены цвета графиков аналитики и контрастность легенды ([7b9d048](https://github.com/randomu3/hqstudio/commit/7b9d04810b2e248e4b10b2251b2ccd4a973a8a69))

## [1.17.0](https://github.com/randomu3/hqstudio/compare/v1.16.0...v1.17.0) (2025-12-26)


### 🚀 Новые возможности

* **desktop:** добавлен трекер несохранённых изменений ([b34d952](https://github.com/randomu3/hqstudio/commit/b34d952229de46a4e7468d0d96be667e07875148))
* **desktop:** добавлена страница аналитики с графиками выручки и заказов ([a0f8038](https://github.com/randomu3/hqstudio/commit/a0f8038cd2c6d1159266b5736212cbd735dd6fcc))


### 🐛 Исправления

* **deps:** синхронизация package-lock.json с @semantic-release/exec ([b4931f8](https://github.com/randomu3/hqstudio/commit/b4931f85f7fb1140d1813ff935f3a99c4fc791f6))

## [1.16.0](https://github.com/randomu3/hqstudio/compare/v1.15.1...v1.16.0) (2025-12-26)


### 🚀 Новые возможности

* **desktop:** добавлен индикатор Caps Lock на экране входа ([42d281a](https://github.com/randomu3/hqstudio/commit/42d281a31bfc3b423d8287905ef26ecc0736a8c6))
* **desktop:** добавлена история недавно просмотренных элементов ([c15caa4](https://github.com/randomu3/hqstudio/commit/c15caa488c1ff49e05d6e3365e3af367233e3a09))
* **desktop:** добавлено отслеживание несохранённых изменений в диалогах ([0db0415](https://github.com/randomu3/hqstudio/commit/0db04154f267728ba562055dd752b79d666f559f))
* **desktop:** добавлены toast-уведомления для обратной связи ([6a5ff0d](https://github.com/randomu3/hqstudio/commit/6a5ff0d9f177f944b7aeb32490d3fe63f2d472af))
* **desktop:** добавлены глобальные горячие клавиши ([753b602](https://github.com/randomu3/hqstudio/commit/753b60230078c000a412983133907daad02d9302))
* **desktop:** добавлены системные уведомления Windows ([e1587b2](https://github.com/randomu3/hqstudio/commit/e1587b28945aaea47d74891d81909fef60fea086))
* **desktop:** кликабельные блоки статистики на Dashboard с навигацией ([376ce93](https://github.com/randomu3/hqstudio/commit/376ce9384fd11282968f3753efbc21b374246cf7))


### ♻️ Рефакторинг

* **desktop:** интеграция новых сервисов в MainWindow и ViewModels ([20b65d6](https://github.com/randomu3/hqstudio/commit/20b65d6a9254a897f25545d9738068c04ca1dc73))

## [1.15.1](https://github.com/randomu3/hqstudio/compare/v1.15.0...v1.15.1) (2025-12-26)


### 🐛 Исправления

* **desktop:** очистка поля пароля при неверном вводе ([a847fde](https://github.com/randomu3/hqstudio/commit/a847fde62abaffd0f6f15acdcc809125f2aba251))

## [1.15.0](https://github.com/randomu3/hqstudio/compare/v1.14.0...v1.15.0) (2025-12-26)


### 🚀 Новые возможности

* **release:** форсирование релиза 1.15.0 с накопленными изменениями ([14c1611](https://github.com/randomu3/hqstudio/commit/14c1611ad4b69e7de719f840a8adf7c609bcde85))


### 🐛 Исправления

* **tests:** выровнены версии EF Core и Mvc.Testing для совместимости с Dependabot ([56c5c53](https://github.com/randomu3/hqstudio/commit/56c5c53ff94b9830a6418fa68dcb095bd32aecff))
* **web:** заменён нативный select на кастомный в форме заявки ([add8ab7](https://github.com/randomu3/hqstudio/commit/add8ab7ca583bcb5a78716f0deaf195faadafb74))


## [1.14.0](https://github.com/randomu3/hqstudio/compare/v1.13.1...v1.14.0) (2025-12-23)


### 🚀 Новые возможности

* **desktop:** добавлена валидация ввода для полей цены, телефона и госномера ([a9534a6](https://github.com/randomu3/hqstudio/commit/a9534a6c514a11eb8cbf344795e7e3bffb473e31))


## [1.13.1](https://github.com/randomu3/hqstudio/compare/v1.13.0...v1.13.1) (2025-12-23)


### 🐛 Исправления

* **desktop:** редактирование сервисов через ViewModel API вместо локального DataService ([e6087de](https://github.com/randomu3/hqstudio/commit/e6087de441d548afd94dd8555fc20626c041cb44))


## [1.13.0](https://github.com/randomu3/hqstudio/compare/v1.12.0...v1.13.0) (2025-12-23)


### 🚀 Новые возможности

* **desktop:** добавлен выбор иконок с умными рекомендациями для сервисов ([66d04f1](https://github.com/randomu3/hqstudio/commit/66d04f1868fe2c1a49649e16d471d22ff6c25ffe))
* **desktop:** добавлена пагинация, счётчики, экспорт в Excel и синхронизация с API ([b8e4f43](https://github.com/randomu3/hqstudio/commit/b8e4f43cdea99ba3fe4443cfdc5b6e153ec41e12))


## [1.12.0](https://github.com/randomu3/hqstudio/compare/v1.11.0...v1.12.0) (2025-12-23)


### 🚀 Новые возможности

* **api:** улучшен скрипт миграции SQLite->PostgreSQL с поддержкой IDENTITY ([a5773ea](https://github.com/randomu3/hqstudio/commit/a5773ea27c68aa777f989b184c661f9742e75043))


## [1.11.0](https://github.com/randomu3/hqstudio/compare/v1.10.0...v1.11.0) (2025-12-22)


### 🚀 Новые возможности

* **api:** добавлены команды экспорта/импорта данных для миграции SQLite->PostgreSQL ([0c69a2c](https://github.com/randomu3/hqstudio/commit/0c69a2ce8b62e1f91b1c034b167ef0bbbd659e9b))


## [1.10.0](https://github.com/randomu3/hqstudio/compare/v1.9.1...v1.10.0) (2025-12-22)


### 🚀 Новые возможности

* **web:** добавлена загрузка подписок с API в админке ([aa59ccb](https://github.com/randomu3/hqstudio/commit/aa59ccbc2fd550d29c724bbbd092aa05835efcf6))



## [1.9.1](https://github.com/randomu3/hqstudio/compare/v1.9.0...v1.9.1) (2025-12-22)


### 🐛 Исправления

* **desktop:** убраны тестовые данные со страницы входа ([7ce4d61](https://github.com/randomu3/hqstudio/commit/7ce4d613dbecda9798d65166cc1bb75129940014))


## [1.9.0](https://github.com/randomu3/hqstudio/compare/v1.8.0...v1.9.0) (2025-12-22)


### 🚀 Новые возможности

* **api,desktop:** бесшовный доступ Desktop к API без авторизации ([c663ed3](https://github.com/randomu3/hqstudio/commit/c663ed314b2e6d3db5519652c499b68639de4522))
* **api,desktop:** управление сотрудниками из API с онлайн-статусом ([15695b6](https://github.com/randomu3/hqstudio/commit/15695b6511c4d883ed2deafa75537130dc77ea0f))
* **api:** добавлен endpoint очистки заказов без клиентов ([d4ec25d](https://github.com/randomu3/hqstudio/commit/d4ec25d3dc6c0ac81bb7b551e1dc1b3465b13d53))
* **api:** добавлена команда очистки базы данных ([0706bb7](https://github.com/randomu3/hqstudio/commit/0706bb7ae3d39f0688d1d74c018876aa4a716f55))
* **api:** добавлены тестовые данные журнала ответственности, фильтр по источнику Web на сайте ([72c9645](https://github.com/randomu3/hqstudio/commit/72c96451917667e614c7281a610a7e7769b08ca2))
* **api:** улучшены контроллеры заявок и клиентов ([00dddd7](https://github.com/randomu3/hqstudio/commit/00dddd7b6ce321d7bf037e79c535007ce1a583c4))
* **desktop:** автоматическое создание сессии для онлайн-статуса ([92e0dca](https://github.com/randomu3/hqstudio/commit/92e0dca056a2e55eb9b871ef1545e8458b4e7ccd))
* **desktop:** добавлен кастомный диалог подтверждения ([10221e6](https://github.com/randomu3/hqstudio/commit/10221e63629ee8ee4e7d39423beeef13b42b33bc))
* **desktop:** добавлен конвертер видимости для пагинации ([d4245a6](https://github.com/randomu3/hqstudio/commit/d4245a6f8889b0301dd90d0056f0eb0b6883eaf3))
* **desktop:** добавлен пакет System.Drawing.Common для печати ([63b644c](https://github.com/randomu3/hqstudio/commit/63b644c27f458788e509a83f8f5c79800ad37d4d))
* **desktop:** добавлен сервис синхронизации данных ([41f8abf](https://github.com/randomu3/hqstudio/commit/41f8abf201b3c32cd48a682455b8794f3783189b))
* **desktop:** добавлена фильтрация, печать и экспорт заказов ([8f66974](https://github.com/randomu3/hqstudio/commit/8f669748c089e4226546d817d0d57b972e3a0d10))
* **desktop:** добавлены диалоги деталей заявки и создания клиента ([485840e](https://github.com/randomu3/hqstudio/commit/485840e381d060e6cf14436a5e0f0ca53e13c1ec))
* **desktop:** добавлены сервисы печати и экспорта в Excel ([c7b5d4c](https://github.com/randomu3/hqstudio/commit/c7b5d4ccf297b7b5ae2311ce97e4a9b3a5e66712))
* **desktop:** добавлены тёмные стили DatePicker и Calendar ([7a39433](https://github.com/randomu3/hqstudio/commit/7a394336aab83d43500a2a3cf071591b45d1aed1))
* **desktop:** улучшена панель детализации заявок ([685d67e](https://github.com/randomu3/hqstudio/commit/685d67e814bc4ab7f0c99856bc47507e039914a0))
* **desktop:** улучшена работа с заявками и создание заказов из заявок ([8e5fc02](https://github.com/randomu3/hqstudio/commit/8e5fc024a3373e2412dfe16bd0ded48f58799ea1))
* **desktop:** улучшены диалоги редактирования заказов и клиентов ([b1b5c22](https://github.com/randomu3/hqstudio/commit/b1b5c221d5717a9296e986c0e2bdf7c6c10b4765))
* автоматическая версионность из semantic-release ([0870b24](https://github.com/randomu3/hqstudio/commit/0870b247bdc830c0fea17c56886ed83190df3d8e))
* добавлено форматирование телефонных номеров ([a3b4443](https://github.com/randomu3/hqstudio/commit/a3b44439cbf79a50325c88cc8b337e0908539b7a))


### 🐛 Исправления

* **api:** исправлены падающие тесты UsersController и ActivityLogController ([49cfa87](https://github.com/randomu3/hqstudio/commit/49cfa872e726ea746d2377893a00aa0b6557446b))
* **desktop:** исправлено отображение версии 1.8.0 ([9c56790](https://github.com/randomu3/hqstudio/commit/9c56790822f3224b4724c0b9cdb43907b49cfff4))
* **desktop:** сессия создаётся после входа, а не при запуске ([bdf25ed](https://github.com/randomu3/hqstudio/commit/bdf25ed6e9b13c664ac47089294e26d2891a3741))
* **desktop:** убрано лишнее сообщение о пустых заявках ([6879e81](https://github.com/randomu3/hqstudio/commit/6879e81cb995b275365f1d43db85a75253c35590))
* **desktop:** улучшена отладка загрузки заявок в десктоп приложении ([ee92e00](https://github.com/randomu3/hqstudio/commit/ee92e00c0d4cdfe52936c1a301abc9cac6194937))
* **web:** исправлена авторизация - корректное декодирование JWT Base64Url токенов ([1a3659b](https://github.com/randomu3/hqstudio/commit/1a3659bfded8d8186dd7c338017f4f6f87c53631))


### ♻️ Рефакторинг

* **api:** улучшена авторизация и управление пользователями ([40cb539](https://github.com/randomu3/hqstudio/commit/40cb53968efebb84007383afe9ae64ed0acc5ae1))
* **desktop:** обновлён запуск приложения и views ([9e0f576](https://github.com/randomu3/hqstudio/commit/9e0f576c42862a57d1ff0bd42e58c35edd45eed0))
* **desktop:** улучшены view models ([25ed803](https://github.com/randomu3/hqstudio/commit/25ed803308ce11c1addfb261ab235e117325e5da))


## [1.8.0](https://github.com/randomu3/hqstudio/compare/v1.7.0...v1.8.0) (2025-12-21)


### 🚀 Новые возможности

* **desktop:** добавлена вкладка 'Заявки с сайта' ([b4feb96](https://github.com/randomu3/hqstudio/commit/b4feb96462df777e2cf74b73d64eb81678266795))


## [1.7.0](https://github.com/randomu3/hqstudio/compare/v1.6.0...v1.7.0) (2025-12-21)


### 🚀 Новые возможности

* soft delete для заказов ([17eb26e](https://github.com/randomu3/hqstudio/commit/17eb26e0d72568a69e9e83ffad8290533e2a6c93))


## [1.6.0](https://github.com/randomu3/hqstudio/compare/v1.5.2...v1.6.0) (2025-12-21)


### 🚀 Новые возможности

* **desktop:** расширена UI библиотека компонентов ([eed7201](https://github.com/randomu3/hqstudio/commit/eed7201d345b6c97976384adc676ce3db2bfe3e2))


## [1.5.2](https://github.com/randomu3/hqstudio/compare/v1.5.1...v1.5.2) (2025-12-21)


### 🐛 Исправления

* **web:** исправлен маппинг enum'ов для заявок ([04d82fa](https://github.com/randomu3/hqstudio/commit/04d82fa3c77019678f1f982df16b8858f4f3c39d))


## [1.5.1](https://github.com/randomu3/hqstudio/compare/v1.5.0...v1.5.1) (2025-12-21)


### 🐛 Исправления

* **desktop:** исправлена загрузка журнала ответственности ([6f1afb3](https://github.com/randomu3/hqstudio/commit/6f1afb38dfc0500684148da65de86d1deb6e35c9))


## [1.5.0](https://github.com/randomu3/hqstudio/compare/v1.4.0...v1.5.0) (2025-12-21)


### 🚀 Новые возможности

* **web:** автообновление заявок при добавлении через конфигуратор ([f17d9ca](https://github.com/randomu3/hqstudio/commit/f17d9cae2720b418e3662222dd2aed617102b1ba))


## [1.4.0](https://github.com/randomu3/hqstudio/compare/v1.3.0...v1.4.0) (2025-12-21)


### 🚀 Новые возможности

* **web:** добавлена кнопка обновления и автообновление заявок ([2e98d46](https://github.com/randomu3/hqstudio/commit/2e98d469ab50d2373eb382ca4e1136ac96a6cef0))


## [1.3.0](https://github.com/randomu3/hqstudio/compare/v1.2.1...v1.3.0) (2025-12-21)


### 🚀 Новые возможности

* **web:** добавлена отправка заявки в конфигураторе ([056e511](https://github.com/randomu3/hqstudio/commit/056e511e913851b02fda47e63f45f1f6b10ffbe2))


## [1.2.1](https://github.com/randomu3/hqstudio/compare/v1.2.0...v1.2.1) (2025-12-21)


### 🐛 Исправления

* **web:** заменена битая ссылка на Unsplash изображение ([cb92dd3](https://github.com/randomu3/hqstudio/commit/cb92dd33a1bb93953acc30d9da58ed2b0ae776b7))
* **web:** исправлены ошибки PWA ([4b15d1d](https://github.com/randomu3/hqstudio/commit/4b15d1d11f2c7d7df78153d515fe2821dbd45fd1))


## [1.2.0](https://github.com/randomu3/hqstudio/compare/v1.1.4...v1.2.0) (2025-12-21)


### 🚀 Новые возможности

* добавлен журнал ответственности и PWA с уведомлениями ([dc0f025](https://github.com/randomu3/hqstudio/commit/dc0f0252d6af025ad4d1116c1ef163f710be3c61))


## [1.1.4](https://github.com/randomu3/hqstudio/compare/v1.1.3...v1.1.4) (2025-12-21)


### 🐛 Исправления

* **web:** исправлена версия @vitest/coverage-v8 ([b033604](https://github.com/randomu3/hqstudio/commit/b0336049d15ecb744d46f0ceb75baad7f07d8d2e))


## [1.1.3](https://github.com/randomu3/hqstudio/compare/v1.1.2...v1.1.3) (2025-12-21)


### 🐛 Исправления

* **web:** ослаблены ESLint правила для совместимости ([baf192d](https://github.com/randomu3/hqstudio/commit/baf192db0c428e5c4c6319a90a041bdaa57bb4f7))


## [1.1.2](https://github.com/randomu3/hqstudio/compare/v1.1.1...v1.1.2) (2025-12-21)


### 🐛 Исправления

* **web:** добавлен ESLint в зависимости ([7ed048c](https://github.com/randomu3/hqstudio/commit/7ed048cd663508fa7533f3ab7c587bf756b4feca))


## [1.1.1](https://github.com/randomu3/hqstudio/compare/v1.1.0...v1.1.1) (2025-12-21)


### 🐛 Исправления

* **web:** добавлен ESLint конфиг для CI ([31af835](https://github.com/randomu3/hqstudio/commit/31af83549f5be957825ee2f2d5ac51a423a77af1))


## [1.1.0](https://github.com/randomu3/hqstudio/compare/v1.0.0...v1.1.0) (2025-12-21)


### 🚀 Новые возможности

* **api:** добавлен health check endpoint для мониторинга ([ed4ccca](https://github.com/randomu3/hqstudio/commit/ed4cccab09d27b8b0272bdc6e71d07cf04917724))


### 🐛 Исправления

* **ci:** исправлен конфликт health endpoint и пропуск интеграционных тестов в CI ([35af313](https://github.com/randomu3/hqstudio/commit/35af313e6c04d719327f76daeac81c4740f085f8))


## [1.0.0](https://github.com/randomu3/hqstudio/releases/tag/v1.0.0) (2025-12-20)


### 🚀 Новые возможности

* добавлен CI/CD и подготовка к релизу ([249d0cc](https://github.com/randomu3/hqstudio/commit/249d0ccc9368ec5e49ba0d877b5bdd9212ea63ce))
* добавлены тесты, клавиатурная навигация и иконки ([042fb1e](https://github.com/randomu3/hqstudio/commit/042fb1e13bcd423cb1ccad3541598fdd79959bb6))


### 🏗 Инфраструктура

* Monorepo структура проекта (API, Web, Desktop)
* JWT аутентификация
* Docker поддержка (dev + prod)
* PostgreSQL для production, SQLite для разработки
* CI/CD с GitHub Actions
* Conventional Commits + автоматический changelog
