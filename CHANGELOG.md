## [1.14.0](https://github.com/randomu3/hqstudio/compare/v1.13.1...v1.14.0) (2025-12-24)


### 🚀 Новые возможности

* **desktop:** выбор заказа кликом, кастомные диалоги вместо MessageBox ([10309a4](https://github.com/randomu3/hqstudio/commit/10309a4c9fea3f697f8580848dd54205213d437f))
* **desktop:** добавлен зелёный индикатор выбранного заказа слева ([e34d60d](https://github.com/randomu3/hqstudio/commit/e34d60d3ee17250387505837425113759c077aee))
* **desktop:** добавлена валидация ввода для полей цены, телефона и гос. номера ([6fd17d8](https://github.com/randomu3/hqstudio/commit/6fd17d8d565ce790843a2a92bb7dc0a4538c9304))
* **desktop:** добавлена пагинация на страницу Услуги ([82215a7](https://github.com/randomu3/hqstudio/commit/82215a75fcbe56003b593bbb8250daa589d8a8d5))


### 🐛 Исправления

* **api:** возвращён PostgreSQL как БД по умолчанию для разработки ([ede21f8](https://github.com/randomu3/hqstudio/commit/ede21f8a4216fc550bfcd291b1abf7f5f939d733))
* **desktop:** fixed emoji icons visibility for theme support ([1547eb1](https://github.com/randomu3/hqstudio/commit/1547eb172a402079193697aed6d9939f75779bd8))
* **desktop:** fixed loader and empty state overlap - show empty state only when not loading ([1f410e7](https://github.com/randomu3/hqstudio/commit/1f410e7fd3c7e84f6bf52aeddcd7ba38fddacbb2))
* **desktop:** improved theme contrast - replaced hardcoded colors with dynamic resources ([97c453a](https://github.com/randomu3/hqstudio/commit/97c453abab33a00cbb3fe6cd0267ccfa2803c48f))
* **desktop:** адаптированы окна входа и загрузки для светлой темы ([d2f52e7](https://github.com/randomu3/hqstudio/commit/d2f52e75411091b345aac52aafc3168ebd0ccb4d))
* **desktop:** восстановлен оригинальный UI заказов с CardHoverable стилем ([51f1776](https://github.com/randomu3/hqstudio/commit/51f177660097301dfaf7b3a019e02dd24e0538a9))
* **desktop:** добавлены алиасы SecondaryButton и ButtonPrimary для светлой темы ([c70fcbb](https://github.com/randomu3/hqstudio/commit/c70fcbb4a906b8c84e1cb4495ca11c464b4dd854))
* **desktop:** исправлен индикатор выбранного заказа через code-behind ([c8bbd36](https://github.com/randomu3/hqstudio/commit/c8bbd367ef4d984a87f6ecb6909435065b394890))
* **desktop:** исправлен контраст кнопок SecondaryButton - белый фон и TextBlock с явным цветом ([df97456](https://github.com/randomu3/hqstudio/commit/df97456a924b5ac405ef7bb4a939d0080dbdc1e6))
* **desktop:** исправлен контраст текста в кнопках светлой темы через TextElement.Foreground ([2126a87](https://github.com/randomu3/hqstudio/commit/2126a878d266b9cd94a76a0e77fcd664dd5a3acc))
* **desktop:** исправлен стиль диалога редактирования пользователя ([ed23c02](https://github.com/randomu3/hqstudio/commit/ed23c0271b94ed091a2f2393b487f2b205cbc309))
* **desktop:** исправлены стили кнопок в Buttons.xaml - используют DynamicResource для поддержки тем ([1228141](https://github.com/randomu3/hqstudio/commit/1228141e179468426f20e4291d58b103ea924507))
* **desktop:** разделительные линии между заказами, исправлено обновление статуса на API ([541453e](https://github.com/randomu3/hqstudio/commit/541453e876b49c259fcf6d4f08c260cbb68a1d80))
* **desktop:** создан полноценный стиль SecondaryButton для светлой темы с явным цветом текста ([6ca6437](https://github.com/randomu3/hqstudio/commit/6ca64370c761a76eb8510613ad86b972a89e5a12))
* **desktop:** улучшен контраст кнопок и иконок в светлой теме ([ec29ae7](https://github.com/randomu3/hqstudio/commit/ec29ae7adc17dc4ee98ce48381fa5ae483807e51))
* **desktop:** улучшен контраст светлой темы ([2b4665f](https://github.com/randomu3/hqstudio/commit/2b4665f8cbf3a927275620052a319a8d610bc790))
* **desktop:** улучшены кнопки Печать и Завершить - добавлены сообщения и подтверждения ([12f9eda](https://github.com/randomu3/hqstudio/commit/12f9eda3c9f284ee677e602e22a6c5b25d672c83))
* **docker:** improved healthchecks - use /api/health endpoint, add proper depends_on conditions ([78db28b](https://github.com/randomu3/hqstudio/commit/78db28b3e9009cdd254fcf549d0e074e415d7d53))


### ♻️ Рефакторинг

* **desktop:** унифицирована кнопка Обновить во всех Views ([ae68fcd](https://github.com/randomu3/hqstudio/commit/ae68fcd2f3872aa6e47ca088fc915ecc0f2e9445))

## [1.14.0](https://github.com/randomu3/hqstudio/compare/v1.13.1...v1.14.0) (2025-12-24)


### 🚀 Новые возможности

* **desktop:** выбор заказа кликом, кастомные диалоги вместо MessageBox ([10309a4](https://github.com/randomu3/hqstudio/commit/10309a4c9fea3f697f8580848dd54205213d437f))
* **desktop:** добавлен зелёный индикатор выбранного заказа слева ([e34d60d](https://github.com/randomu3/hqstudio/commit/e34d60d3ee17250387505837425113759c077aee))
* **desktop:** добавлена валидация ввода для полей цены, телефона и гос. номера ([6fd17d8](https://github.com/randomu3/hqstudio/commit/6fd17d8d565ce790843a2a92bb7dc0a4538c9304))
* **desktop:** добавлена пагинация на страницу Услуги ([82215a7](https://github.com/randomu3/hqstudio/commit/82215a75fcbe56003b593bbb8250daa589d8a8d5))


### 🐛 Исправления

* **api:** возвращён PostgreSQL как БД по умолчанию для разработки ([ede21f8](https://github.com/randomu3/hqstudio/commit/ede21f8a4216fc550bfcd291b1abf7f5f939d733))
* **desktop:** fixed emoji icons visibility for theme support ([1547eb1](https://github.com/randomu3/hqstudio/commit/1547eb172a402079193697aed6d9939f75779bd8))
* **desktop:** fixed loader and empty state overlap - show empty state only when not loading ([1f410e7](https://github.com/randomu3/hqstudio/commit/1f410e7fd3c7e84f6bf52aeddcd7ba38fddacbb2))
* **desktop:** improved theme contrast - replaced hardcoded colors with dynamic resources ([97c453a](https://github.com/randomu3/hqstudio/commit/97c453abab33a00cbb3fe6cd0267ccfa2803c48f))
* **desktop:** адаптированы окна входа и загрузки для светлой темы ([d2f52e7](https://github.com/randomu3/hqstudio/commit/d2f52e75411091b345aac52aafc3168ebd0ccb4d))
* **desktop:** восстановлен оригинальный UI заказов с CardHoverable стилем ([51f1776](https://github.com/randomu3/hqstudio/commit/51f177660097301dfaf7b3a019e02dd24e0538a9))
* **desktop:** добавлены алиасы SecondaryButton и ButtonPrimary для светлой темы ([c70fcbb](https://github.com/randomu3/hqstudio/commit/c70fcbb4a906b8c84e1cb4495ca11c464b4dd854))
* **desktop:** исправлен индикатор выбранного заказа через code-behind ([c8bbd36](https://github.com/randomu3/hqstudio/commit/c8bbd367ef4d984a87f6ecb6909435065b394890))
* **desktop:** исправлен контраст кнопок SecondaryButton - белый фон и TextBlock с явным цветом ([df97456](https://github.com/randomu3/hqstudio/commit/df97456a924b5ac405ef7bb4a939d0080dbdc1e6))
* **desktop:** исправлен контраст текста в кнопках светлой темы через TextElement.Foreground ([2126a87](https://github.com/randomu3/hqstudio/commit/2126a878d266b9cd94a76a0e77fcd664dd5a3acc))
* **desktop:** исправлен стиль диалога редактирования пользователя ([ed23c02](https://github.com/randomu3/hqstudio/commit/ed23c0271b94ed091a2f2393b487f2b205cbc309))
* **desktop:** исправлены стили кнопок в Buttons.xaml - используют DynamicResource для поддержки тем ([1228141](https://github.com/randomu3/hqstudio/commit/1228141e179468426f20e4291d58b103ea924507))
* **desktop:** разделительные линии между заказами, исправлено обновление статуса на API ([541453e](https://github.com/randomu3/hqstudio/commit/541453e876b49c259fcf6d4f08c260cbb68a1d80))
* **desktop:** создан полноценный стиль SecondaryButton для светлой темы с явным цветом текста ([6ca6437](https://github.com/randomu3/hqstudio/commit/6ca64370c761a76eb8510613ad86b972a89e5a12))
* **desktop:** улучшен контраст кнопок и иконок в светлой теме ([ec29ae7](https://github.com/randomu3/hqstudio/commit/ec29ae7adc17dc4ee98ce48381fa5ae483807e51))
* **desktop:** улучшен контраст светлой темы ([2b4665f](https://github.com/randomu3/hqstudio/commit/2b4665f8cbf3a927275620052a319a8d610bc790))
* **desktop:** улучшены кнопки Печать и Завершить - добавлены сообщения и подтверждения ([12f9eda](https://github.com/randomu3/hqstudio/commit/12f9eda3c9f284ee677e602e22a6c5b25d672c83))
* **docker:** improved healthchecks - use /api/health endpoint, add proper depends_on conditions ([78db28b](https://github.com/randomu3/hqstudio/commit/78db28b3e9009cdd254fcf549d0e074e415d7d53))


### ♻️ Рефакторинг

* **desktop:** унифицирована кнопка Обновить во всех Views ([ae68fcd](https://github.com/randomu3/hqstudio/commit/ae68fcd2f3872aa6e47ca088fc915ecc0f2e9445))

## [1.14.0](https://github.com/randomu3/hqstudio/compare/v1.13.1...v1.14.0) (2025-12-23)


### 🚀 Новые возможности

* **desktop:** выбор заказа кликом, кастомные диалоги вместо MessageBox ([10309a4](https://github.com/randomu3/hqstudio/commit/10309a4c9fea3f697f8580848dd54205213d437f))
* **desktop:** добавлен зелёный индикатор выбранного заказа слева ([e34d60d](https://github.com/randomu3/hqstudio/commit/e34d60d3ee17250387505837425113759c077aee))
* **desktop:** добавлена валидация ввода для полей цены, телефона и гос. номера ([6fd17d8](https://github.com/randomu3/hqstudio/commit/6fd17d8d565ce790843a2a92bb7dc0a4538c9304))


### 🐛 Исправления

* **api:** возвращён PostgreSQL как БД по умолчанию для разработки ([ede21f8](https://github.com/randomu3/hqstudio/commit/ede21f8a4216fc550bfcd291b1abf7f5f939d733))
* **desktop:** ���������� ��������� emoji ������ ��� ��������� ��� ([1547eb1](https://github.com/randomu3/hqstudio/commit/1547eb172a402079193697aed6d9939f75779bd8))
* **desktop:** ���������� ��������� ������� � ������� ��������� ([1f410e7](https://github.com/randomu3/hqstudio/commit/1f410e7fd3c7e84f6bf52aeddcd7ba38fddacbb2))
* **desktop:** ������� �������� ���� - �������� ������� ����� �� ������������ ������� ([97c453a](https://github.com/randomu3/hqstudio/commit/97c453abab33a00cbb3fe6cd0267ccfa2803c48f))
* **desktop:** восстановлен оригинальный UI заказов с CardHoverable стилем ([51f1776](https://github.com/randomu3/hqstudio/commit/51f177660097301dfaf7b3a019e02dd24e0538a9))
* **desktop:** добавлены алиасы SecondaryButton и ButtonPrimary для светлой темы ([c70fcbb](https://github.com/randomu3/hqstudio/commit/c70fcbb4a906b8c84e1cb4495ca11c464b4dd854))
* **desktop:** исправлен индикатор выбранного заказа через code-behind ([c8bbd36](https://github.com/randomu3/hqstudio/commit/c8bbd367ef4d984a87f6ecb6909435065b394890))
* **desktop:** исправлен контраст кнопок SecondaryButton - белый фон и TextBlock с явным цветом ([df97456](https://github.com/randomu3/hqstudio/commit/df97456a924b5ac405ef7bb4a939d0080dbdc1e6))
* **desktop:** исправлен контраст текста в кнопках светлой темы через TextElement.Foreground ([2126a87](https://github.com/randomu3/hqstudio/commit/2126a878d266b9cd94a76a0e77fcd664dd5a3acc))
* **desktop:** исправлены стили кнопок в Buttons.xaml - используют DynamicResource для поддержки тем ([1228141](https://github.com/randomu3/hqstudio/commit/1228141e179468426f20e4291d58b103ea924507))
* **desktop:** разделительные линии между заказами, исправлено обновление статуса на API ([541453e](https://github.com/randomu3/hqstudio/commit/541453e876b49c259fcf6d4f08c260cbb68a1d80))
* **desktop:** создан полноценный стиль SecondaryButton для светлой темы с явным цветом текста ([6ca6437](https://github.com/randomu3/hqstudio/commit/6ca64370c761a76eb8510613ad86b972a89e5a12))
* **desktop:** улучшен контраст кнопок и иконок в светлой теме ([ec29ae7](https://github.com/randomu3/hqstudio/commit/ec29ae7adc17dc4ee98ce48381fa5ae483807e51))
* **desktop:** улучшены кнопки Печать и Завершить - добавлены сообщения и подтверждения ([12f9eda](https://github.com/randomu3/hqstudio/commit/12f9eda3c9f284ee677e602e22a6c5b25d672c83))
* **docker:** �������� healthcheck - ������������ /api/health endpoint ([78db28b](https://github.com/randomu3/hqstudio/commit/78db28b3e9009cdd254fcf549d0e074e415d7d53))

## [1.14.0](https://github.com/randomu3/hqstudio/compare/v1.13.1...v1.14.0) (2025-12-23)


### 🚀 Новые возможности

* **desktop:** выбор заказа кликом, кастомные диалоги вместо MessageBox ([10309a4](https://github.com/randomu3/hqstudio/commit/10309a4c9fea3f697f8580848dd54205213d437f))
* **desktop:** добавлен зелёный индикатор выбранного заказа слева ([e34d60d](https://github.com/randomu3/hqstudio/commit/e34d60d3ee17250387505837425113759c077aee))
* **desktop:** добавлена валидация ввода для полей цены, телефона и гос. номера ([6fd17d8](https://github.com/randomu3/hqstudio/commit/6fd17d8d565ce790843a2a92bb7dc0a4538c9304))


### 🐛 Исправления

* **api:** возвращён PostgreSQL как БД по умолчанию для разработки ([ede21f8](https://github.com/randomu3/hqstudio/commit/ede21f8a4216fc550bfcd291b1abf7f5f939d733))
* **desktop:** ���������� ��������� emoji ������ ��� ��������� ��� ([1547eb1](https://github.com/randomu3/hqstudio/commit/1547eb172a402079193697aed6d9939f75779bd8))
* **desktop:** ���������� ��������� ������� � ������� ��������� ([1f410e7](https://github.com/randomu3/hqstudio/commit/1f410e7fd3c7e84f6bf52aeddcd7ba38fddacbb2))
* **desktop:** ������� �������� ���� - �������� ������� ����� �� ������������ ������� ([97c453a](https://github.com/randomu3/hqstudio/commit/97c453abab33a00cbb3fe6cd0267ccfa2803c48f))
* **desktop:** восстановлен оригинальный UI заказов с CardHoverable стилем ([51f1776](https://github.com/randomu3/hqstudio/commit/51f177660097301dfaf7b3a019e02dd24e0538a9))
* **desktop:** добавлены алиасы SecondaryButton и ButtonPrimary для светлой темы ([c70fcbb](https://github.com/randomu3/hqstudio/commit/c70fcbb4a906b8c84e1cb4495ca11c464b4dd854))
* **desktop:** исправлен контраст кнопок SecondaryButton - белый фон и TextBlock с явным цветом ([df97456](https://github.com/randomu3/hqstudio/commit/df97456a924b5ac405ef7bb4a939d0080dbdc1e6))
* **desktop:** исправлен контраст текста в кнопках светлой темы через TextElement.Foreground ([2126a87](https://github.com/randomu3/hqstudio/commit/2126a878d266b9cd94a76a0e77fcd664dd5a3acc))
* **desktop:** исправлены стили кнопок в Buttons.xaml - используют DynamicResource для поддержки тем ([1228141](https://github.com/randomu3/hqstudio/commit/1228141e179468426f20e4291d58b103ea924507))
* **desktop:** разделительные линии между заказами, исправлено обновление статуса на API ([541453e](https://github.com/randomu3/hqstudio/commit/541453e876b49c259fcf6d4f08c260cbb68a1d80))
* **desktop:** создан полноценный стиль SecondaryButton для светлой темы с явным цветом текста ([6ca6437](https://github.com/randomu3/hqstudio/commit/6ca64370c761a76eb8510613ad86b972a89e5a12))
* **desktop:** улучшен контраст кнопок и иконок в светлой теме ([ec29ae7](https://github.com/randomu3/hqstudio/commit/ec29ae7adc17dc4ee98ce48381fa5ae483807e51))
* **desktop:** улучшены кнопки Печать и Завершить - добавлены сообщения и подтверждения ([12f9eda](https://github.com/randomu3/hqstudio/commit/12f9eda3c9f284ee677e602e22a6c5b25d672c83))
* **docker:** �������� healthcheck - ������������ /api/health endpoint ([78db28b](https://github.com/randomu3/hqstudio/commit/78db28b3e9009cdd254fcf549d0e074e415d7d53))

## [1.14.0](https://github.com/randomu3/hqstudio/compare/v1.13.1...v1.14.0) (2025-12-23)


### 🚀 Новые возможности

* **desktop:** выбор заказа кликом, кастомные диалоги вместо MessageBox ([10309a4](https://github.com/randomu3/hqstudio/commit/10309a4c9fea3f697f8580848dd54205213d437f))
* **desktop:** добавлена валидация ввода для полей цены, телефона и гос. номера ([6fd17d8](https://github.com/randomu3/hqstudio/commit/6fd17d8d565ce790843a2a92bb7dc0a4538c9304))


### 🐛 Исправления

* **api:** возвращён PostgreSQL как БД по умолчанию для разработки ([ede21f8](https://github.com/randomu3/hqstudio/commit/ede21f8a4216fc550bfcd291b1abf7f5f939d733))
* **desktop:** ���������� ��������� emoji ������ ��� ��������� ��� ([1547eb1](https://github.com/randomu3/hqstudio/commit/1547eb172a402079193697aed6d9939f75779bd8))
* **desktop:** ���������� ��������� ������� � ������� ��������� ([1f410e7](https://github.com/randomu3/hqstudio/commit/1f410e7fd3c7e84f6bf52aeddcd7ba38fddacbb2))
* **desktop:** ������� �������� ���� - �������� ������� ����� �� ������������ ������� ([97c453a](https://github.com/randomu3/hqstudio/commit/97c453abab33a00cbb3fe6cd0267ccfa2803c48f))
* **desktop:** восстановлен оригинальный UI заказов с CardHoverable стилем ([51f1776](https://github.com/randomu3/hqstudio/commit/51f177660097301dfaf7b3a019e02dd24e0538a9))
* **desktop:** добавлены алиасы SecondaryButton и ButtonPrimary для светлой темы ([c70fcbb](https://github.com/randomu3/hqstudio/commit/c70fcbb4a906b8c84e1cb4495ca11c464b4dd854))
* **desktop:** исправлен контраст кнопок SecondaryButton - белый фон и TextBlock с явным цветом ([df97456](https://github.com/randomu3/hqstudio/commit/df97456a924b5ac405ef7bb4a939d0080dbdc1e6))
* **desktop:** исправлен контраст текста в кнопках светлой темы через TextElement.Foreground ([2126a87](https://github.com/randomu3/hqstudio/commit/2126a878d266b9cd94a76a0e77fcd664dd5a3acc))
* **desktop:** исправлены стили кнопок в Buttons.xaml - используют DynamicResource для поддержки тем ([1228141](https://github.com/randomu3/hqstudio/commit/1228141e179468426f20e4291d58b103ea924507))
* **desktop:** разделительные линии между заказами, исправлено обновление статуса на API ([541453e](https://github.com/randomu3/hqstudio/commit/541453e876b49c259fcf6d4f08c260cbb68a1d80))
* **desktop:** создан полноценный стиль SecondaryButton для светлой темы с явным цветом текста ([6ca6437](https://github.com/randomu3/hqstudio/commit/6ca64370c761a76eb8510613ad86b972a89e5a12))
* **desktop:** улучшен контраст кнопок и иконок в светлой теме ([ec29ae7](https://github.com/randomu3/hqstudio/commit/ec29ae7adc17dc4ee98ce48381fa5ae483807e51))
* **desktop:** улучшены кнопки Печать и Завершить - добавлены сообщения и подтверждения ([12f9eda](https://github.com/randomu3/hqstudio/commit/12f9eda3c9f284ee677e602e22a6c5b25d672c83))
* **docker:** �������� healthcheck - ������������ /api/health endpoint ([78db28b](https://github.com/randomu3/hqstudio/commit/78db28b3e9009cdd254fcf549d0e074e415d7d53))

## [1.14.0](https://github.com/randomu3/hqstudio/compare/v1.13.1...v1.14.0) (2025-12-23)


### 🚀 Новые возможности

* **desktop:** выбор заказа кликом, кастомные диалоги вместо MessageBox ([10309a4](https://github.com/randomu3/hqstudio/commit/10309a4c9fea3f697f8580848dd54205213d437f))
* **desktop:** добавлена валидация ввода для полей цены, телефона и гос. номера ([6fd17d8](https://github.com/randomu3/hqstudio/commit/6fd17d8d565ce790843a2a92bb7dc0a4538c9304))


### 🐛 Исправления

* **api:** возвращён PostgreSQL как БД по умолчанию для разработки ([ede21f8](https://github.com/randomu3/hqstudio/commit/ede21f8a4216fc550bfcd291b1abf7f5f939d733))
* **desktop:** ���������� ��������� emoji ������ ��� ��������� ��� ([1547eb1](https://github.com/randomu3/hqstudio/commit/1547eb172a402079193697aed6d9939f75779bd8))
* **desktop:** ���������� ��������� ������� � ������� ��������� ([1f410e7](https://github.com/randomu3/hqstudio/commit/1f410e7fd3c7e84f6bf52aeddcd7ba38fddacbb2))
* **desktop:** ������� �������� ���� - �������� ������� ����� �� ������������ ������� ([97c453a](https://github.com/randomu3/hqstudio/commit/97c453abab33a00cbb3fe6cd0267ccfa2803c48f))
* **desktop:** добавлены алиасы SecondaryButton и ButtonPrimary для светлой темы ([c70fcbb](https://github.com/randomu3/hqstudio/commit/c70fcbb4a906b8c84e1cb4495ca11c464b4dd854))
* **desktop:** исправлен контраст кнопок SecondaryButton - белый фон и TextBlock с явным цветом ([df97456](https://github.com/randomu3/hqstudio/commit/df97456a924b5ac405ef7bb4a939d0080dbdc1e6))
* **desktop:** исправлен контраст текста в кнопках светлой темы через TextElement.Foreground ([2126a87](https://github.com/randomu3/hqstudio/commit/2126a878d266b9cd94a76a0e77fcd664dd5a3acc))
* **desktop:** исправлены стили кнопок в Buttons.xaml - используют DynamicResource для поддержки тем ([1228141](https://github.com/randomu3/hqstudio/commit/1228141e179468426f20e4291d58b103ea924507))
* **desktop:** разделительные линии между заказами, исправлено обновление статуса на API ([541453e](https://github.com/randomu3/hqstudio/commit/541453e876b49c259fcf6d4f08c260cbb68a1d80))
* **desktop:** создан полноценный стиль SecondaryButton для светлой темы с явным цветом текста ([6ca6437](https://github.com/randomu3/hqstudio/commit/6ca64370c761a76eb8510613ad86b972a89e5a12))
* **desktop:** улучшен контраст кнопок и иконок в светлой теме ([ec29ae7](https://github.com/randomu3/hqstudio/commit/ec29ae7adc17dc4ee98ce48381fa5ae483807e51))
* **desktop:** улучшены кнопки Печать и Завершить - добавлены сообщения и подтверждения ([12f9eda](https://github.com/randomu3/hqstudio/commit/12f9eda3c9f284ee677e602e22a6c5b25d672c83))
* **docker:** �������� healthcheck - ������������ /api/health endpoint ([78db28b](https://github.com/randomu3/hqstudio/commit/78db28b3e9009cdd254fcf549d0e074e415d7d53))

## [1.14.0](https://github.com/randomu3/hqstudio/compare/v1.13.1...v1.14.0) (2025-12-23)


### 🚀 Новые возможности

* **desktop:** выбор заказа кликом, кастомные диалоги вместо MessageBox ([10309a4](https://github.com/randomu3/hqstudio/commit/10309a4c9fea3f697f8580848dd54205213d437f))
* **desktop:** добавлена валидация ввода для полей цены, телефона и гос. номера ([6fd17d8](https://github.com/randomu3/hqstudio/commit/6fd17d8d565ce790843a2a92bb7dc0a4538c9304))


### 🐛 Исправления

* **api:** возвращён PostgreSQL как БД по умолчанию для разработки ([ede21f8](https://github.com/randomu3/hqstudio/commit/ede21f8a4216fc550bfcd291b1abf7f5f939d733))
* **desktop:** ���������� ��������� emoji ������ ��� ��������� ��� ([1547eb1](https://github.com/randomu3/hqstudio/commit/1547eb172a402079193697aed6d9939f75779bd8))
* **desktop:** ���������� ��������� ������� � ������� ��������� ([1f410e7](https://github.com/randomu3/hqstudio/commit/1f410e7fd3c7e84f6bf52aeddcd7ba38fddacbb2))
* **desktop:** ������� �������� ���� - �������� ������� ����� �� ������������ ������� ([97c453a](https://github.com/randomu3/hqstudio/commit/97c453abab33a00cbb3fe6cd0267ccfa2803c48f))
* **desktop:** добавлены алиасы SecondaryButton и ButtonPrimary для светлой темы ([c70fcbb](https://github.com/randomu3/hqstudio/commit/c70fcbb4a906b8c84e1cb4495ca11c464b4dd854))
* **desktop:** исправлен контраст кнопок SecondaryButton - белый фон и TextBlock с явным цветом ([df97456](https://github.com/randomu3/hqstudio/commit/df97456a924b5ac405ef7bb4a939d0080dbdc1e6))
* **desktop:** исправлен контраст текста в кнопках светлой темы через TextElement.Foreground ([2126a87](https://github.com/randomu3/hqstudio/commit/2126a878d266b9cd94a76a0e77fcd664dd5a3acc))
* **desktop:** исправлены стили кнопок в Buttons.xaml - используют DynamicResource для поддержки тем ([1228141](https://github.com/randomu3/hqstudio/commit/1228141e179468426f20e4291d58b103ea924507))
* **desktop:** создан полноценный стиль SecondaryButton для светлой темы с явным цветом текста ([6ca6437](https://github.com/randomu3/hqstudio/commit/6ca64370c761a76eb8510613ad86b972a89e5a12))
* **desktop:** улучшен контраст кнопок и иконок в светлой теме ([ec29ae7](https://github.com/randomu3/hqstudio/commit/ec29ae7adc17dc4ee98ce48381fa5ae483807e51))
* **desktop:** улучшены кнопки Печать и Завершить - добавлены сообщения и подтверждения ([12f9eda](https://github.com/randomu3/hqstudio/commit/12f9eda3c9f284ee677e602e22a6c5b25d672c83))
* **docker:** �������� healthcheck - ������������ /api/health endpoint ([78db28b](https://github.com/randomu3/hqstudio/commit/78db28b3e9009cdd254fcf549d0e074e415d7d53))

## [1.14.0](https://github.com/randomu3/hqstudio/compare/v1.13.1...v1.14.0) (2025-12-23)


### 🚀 Новые возможности

* **desktop:** добавлена валидация ввода для полей цены, телефона и гос. номера ([6fd17d8](https://github.com/randomu3/hqstudio/commit/6fd17d8d565ce790843a2a92bb7dc0a4538c9304))


### 🐛 Исправления

* **api:** возвращён PostgreSQL как БД по умолчанию для разработки ([ede21f8](https://github.com/randomu3/hqstudio/commit/ede21f8a4216fc550bfcd291b1abf7f5f939d733))
* **desktop:** ���������� ��������� emoji ������ ��� ��������� ��� ([1547eb1](https://github.com/randomu3/hqstudio/commit/1547eb172a402079193697aed6d9939f75779bd8))
* **desktop:** ���������� ��������� ������� � ������� ��������� ([1f410e7](https://github.com/randomu3/hqstudio/commit/1f410e7fd3c7e84f6bf52aeddcd7ba38fddacbb2))
* **desktop:** ������� �������� ���� - �������� ������� ����� �� ������������ ������� ([97c453a](https://github.com/randomu3/hqstudio/commit/97c453abab33a00cbb3fe6cd0267ccfa2803c48f))
* **desktop:** добавлены алиасы SecondaryButton и ButtonPrimary для светлой темы ([c70fcbb](https://github.com/randomu3/hqstudio/commit/c70fcbb4a906b8c84e1cb4495ca11c464b4dd854))
* **desktop:** исправлен контраст кнопок SecondaryButton - белый фон и TextBlock с явным цветом ([df97456](https://github.com/randomu3/hqstudio/commit/df97456a924b5ac405ef7bb4a939d0080dbdc1e6))
* **desktop:** исправлен контраст текста в кнопках светлой темы через TextElement.Foreground ([2126a87](https://github.com/randomu3/hqstudio/commit/2126a878d266b9cd94a76a0e77fcd664dd5a3acc))
* **desktop:** исправлены стили кнопок в Buttons.xaml - используют DynamicResource для поддержки тем ([1228141](https://github.com/randomu3/hqstudio/commit/1228141e179468426f20e4291d58b103ea924507))
* **desktop:** создан полноценный стиль SecondaryButton для светлой темы с явным цветом текста ([6ca6437](https://github.com/randomu3/hqstudio/commit/6ca64370c761a76eb8510613ad86b972a89e5a12))
* **desktop:** улучшен контраст кнопок и иконок в светлой теме ([ec29ae7](https://github.com/randomu3/hqstudio/commit/ec29ae7adc17dc4ee98ce48381fa5ae483807e51))
* **desktop:** улучшены кнопки Печать и Завершить - добавлены сообщения и подтверждения ([12f9eda](https://github.com/randomu3/hqstudio/commit/12f9eda3c9f284ee677e602e22a6c5b25d672c83))
* **docker:** �������� healthcheck - ������������ /api/health endpoint ([78db28b](https://github.com/randomu3/hqstudio/commit/78db28b3e9009cdd254fcf549d0e074e415d7d53))

## [1.14.0](https://github.com/randomu3/hqstudio/compare/v1.13.1...v1.14.0) (2025-12-23)


### 🚀 Новые возможности

* **desktop:** добавлена валидация ввода для полей цены, телефона и гос. номера ([6fd17d8](https://github.com/randomu3/hqstudio/commit/6fd17d8d565ce790843a2a92bb7dc0a4538c9304))


### 🐛 Исправления

* **api:** возвращён PostgreSQL как БД по умолчанию для разработки ([ede21f8](https://github.com/randomu3/hqstudio/commit/ede21f8a4216fc550bfcd291b1abf7f5f939d733))
* **desktop:** ���������� ��������� emoji ������ ��� ��������� ��� ([1547eb1](https://github.com/randomu3/hqstudio/commit/1547eb172a402079193697aed6d9939f75779bd8))
* **desktop:** ���������� ��������� ������� � ������� ��������� ([1f410e7](https://github.com/randomu3/hqstudio/commit/1f410e7fd3c7e84f6bf52aeddcd7ba38fddacbb2))
* **desktop:** ������� �������� ���� - �������� ������� ����� �� ������������ ������� ([97c453a](https://github.com/randomu3/hqstudio/commit/97c453abab33a00cbb3fe6cd0267ccfa2803c48f))
* **desktop:** добавлены алиасы SecondaryButton и ButtonPrimary для светлой темы ([c70fcbb](https://github.com/randomu3/hqstudio/commit/c70fcbb4a906b8c84e1cb4495ca11c464b4dd854))
* **desktop:** исправлен контраст кнопок SecondaryButton - белый фон и TextBlock с явным цветом ([df97456](https://github.com/randomu3/hqstudio/commit/df97456a924b5ac405ef7bb4a939d0080dbdc1e6))
* **desktop:** исправлен контраст текста в кнопках светлой темы через TextElement.Foreground ([2126a87](https://github.com/randomu3/hqstudio/commit/2126a878d266b9cd94a76a0e77fcd664dd5a3acc))
* **desktop:** исправлены стили кнопок в Buttons.xaml - используют DynamicResource для поддержки тем ([1228141](https://github.com/randomu3/hqstudio/commit/1228141e179468426f20e4291d58b103ea924507))
* **desktop:** создан полноценный стиль SecondaryButton для светлой темы с явным цветом текста ([6ca6437](https://github.com/randomu3/hqstudio/commit/6ca64370c761a76eb8510613ad86b972a89e5a12))
* **desktop:** улучшен контраст кнопок и иконок в светлой теме ([ec29ae7](https://github.com/randomu3/hqstudio/commit/ec29ae7adc17dc4ee98ce48381fa5ae483807e51))
* **docker:** �������� healthcheck - ������������ /api/health endpoint ([78db28b](https://github.com/randomu3/hqstudio/commit/78db28b3e9009cdd254fcf549d0e074e415d7d53))

## [1.14.0](https://github.com/randomu3/hqstudio/compare/v1.13.1...v1.14.0) (2025-12-23)


### 🚀 Новые возможности

* **desktop:** добавлена валидация ввода для полей цены, телефона и гос. номера ([6fd17d8](https://github.com/randomu3/hqstudio/commit/6fd17d8d565ce790843a2a92bb7dc0a4538c9304))


### 🐛 Исправления

* **api:** возвращён PostgreSQL как БД по умолчанию для разработки ([ede21f8](https://github.com/randomu3/hqstudio/commit/ede21f8a4216fc550bfcd291b1abf7f5f939d733))
* **desktop:** ���������� ��������� emoji ������ ��� ��������� ��� ([1547eb1](https://github.com/randomu3/hqstudio/commit/1547eb172a402079193697aed6d9939f75779bd8))
* **desktop:** ���������� ��������� ������� � ������� ��������� ([1f410e7](https://github.com/randomu3/hqstudio/commit/1f410e7fd3c7e84f6bf52aeddcd7ba38fddacbb2))
* **desktop:** ������� �������� ���� - �������� ������� ����� �� ������������ ������� ([97c453a](https://github.com/randomu3/hqstudio/commit/97c453abab33a00cbb3fe6cd0267ccfa2803c48f))
* **desktop:** добавлены алиасы SecondaryButton и ButtonPrimary для светлой темы ([c70fcbb](https://github.com/randomu3/hqstudio/commit/c70fcbb4a906b8c84e1cb4495ca11c464b4dd854))
* **desktop:** исправлен контраст кнопок SecondaryButton - белый фон и TextBlock с явным цветом ([df97456](https://github.com/randomu3/hqstudio/commit/df97456a924b5ac405ef7bb4a939d0080dbdc1e6))
* **desktop:** исправлен контраст текста в кнопках светлой темы через TextElement.Foreground ([2126a87](https://github.com/randomu3/hqstudio/commit/2126a878d266b9cd94a76a0e77fcd664dd5a3acc))
* **desktop:** создан полноценный стиль SecondaryButton для светлой темы с явным цветом текста ([6ca6437](https://github.com/randomu3/hqstudio/commit/6ca64370c761a76eb8510613ad86b972a89e5a12))
* **desktop:** улучшен контраст кнопок и иконок в светлой теме ([ec29ae7](https://github.com/randomu3/hqstudio/commit/ec29ae7adc17dc4ee98ce48381fa5ae483807e51))
* **docker:** �������� healthcheck - ������������ /api/health endpoint ([78db28b](https://github.com/randomu3/hqstudio/commit/78db28b3e9009cdd254fcf549d0e074e415d7d53))

## [1.14.0](https://github.com/randomu3/hqstudio/compare/v1.13.1...v1.14.0) (2025-12-23)


### 🚀 Новые возможности

* **desktop:** добавлена валидация ввода для полей цены, телефона и гос. номера ([6fd17d8](https://github.com/randomu3/hqstudio/commit/6fd17d8d565ce790843a2a92bb7dc0a4538c9304))


### 🐛 Исправления

* **api:** возвращён PostgreSQL как БД по умолчанию для разработки ([ede21f8](https://github.com/randomu3/hqstudio/commit/ede21f8a4216fc550bfcd291b1abf7f5f939d733))
* **desktop:** ���������� ��������� emoji ������ ��� ��������� ��� ([1547eb1](https://github.com/randomu3/hqstudio/commit/1547eb172a402079193697aed6d9939f75779bd8))
* **desktop:** ���������� ��������� ������� � ������� ��������� ([1f410e7](https://github.com/randomu3/hqstudio/commit/1f410e7fd3c7e84f6bf52aeddcd7ba38fddacbb2))
* **desktop:** ������� �������� ���� - �������� ������� ����� �� ������������ ������� ([97c453a](https://github.com/randomu3/hqstudio/commit/97c453abab33a00cbb3fe6cd0267ccfa2803c48f))
* **desktop:** добавлены алиасы SecondaryButton и ButtonPrimary для светлой темы ([c70fcbb](https://github.com/randomu3/hqstudio/commit/c70fcbb4a906b8c84e1cb4495ca11c464b4dd854))
* **desktop:** исправлен контраст текста в кнопках светлой темы через TextElement.Foreground ([2126a87](https://github.com/randomu3/hqstudio/commit/2126a878d266b9cd94a76a0e77fcd664dd5a3acc))
* **desktop:** создан полноценный стиль SecondaryButton для светлой темы с явным цветом текста ([6ca6437](https://github.com/randomu3/hqstudio/commit/6ca64370c761a76eb8510613ad86b972a89e5a12))
* **desktop:** улучшен контраст кнопок и иконок в светлой теме ([ec29ae7](https://github.com/randomu3/hqstudio/commit/ec29ae7adc17dc4ee98ce48381fa5ae483807e51))
* **docker:** �������� healthcheck - ������������ /api/health endpoint ([78db28b](https://github.com/randomu3/hqstudio/commit/78db28b3e9009cdd254fcf549d0e074e415d7d53))

## [1.14.0](https://github.com/randomu3/hqstudio/compare/v1.13.1...v1.14.0) (2025-12-23)


### 🚀 Новые возможности

* **desktop:** добавлена валидация ввода для полей цены, телефона и гос. номера ([6fd17d8](https://github.com/randomu3/hqstudio/commit/6fd17d8d565ce790843a2a92bb7dc0a4538c9304))


### 🐛 Исправления

* **api:** возвращён PostgreSQL как БД по умолчанию для разработки ([ede21f8](https://github.com/randomu3/hqstudio/commit/ede21f8a4216fc550bfcd291b1abf7f5f939d733))
* **desktop:** ���������� ��������� emoji ������ ��� ��������� ��� ([1547eb1](https://github.com/randomu3/hqstudio/commit/1547eb172a402079193697aed6d9939f75779bd8))
* **desktop:** ���������� ��������� ������� � ������� ��������� ([1f410e7](https://github.com/randomu3/hqstudio/commit/1f410e7fd3c7e84f6bf52aeddcd7ba38fddacbb2))
* **desktop:** ������� �������� ���� - �������� ������� ����� �� ������������ ������� ([97c453a](https://github.com/randomu3/hqstudio/commit/97c453abab33a00cbb3fe6cd0267ccfa2803c48f))
* **desktop:** добавлены алиасы SecondaryButton и ButtonPrimary для светлой темы ([c70fcbb](https://github.com/randomu3/hqstudio/commit/c70fcbb4a906b8c84e1cb4495ca11c464b4dd854))
* **desktop:** исправлен контраст текста в кнопках светлой темы через TextElement.Foreground ([2126a87](https://github.com/randomu3/hqstudio/commit/2126a878d266b9cd94a76a0e77fcd664dd5a3acc))
* **desktop:** улучшен контраст кнопок и иконок в светлой теме ([ec29ae7](https://github.com/randomu3/hqstudio/commit/ec29ae7adc17dc4ee98ce48381fa5ae483807e51))
* **docker:** �������� healthcheck - ������������ /api/health endpoint ([78db28b](https://github.com/randomu3/hqstudio/commit/78db28b3e9009cdd254fcf549d0e074e415d7d53))

## [1.14.0](https://github.com/randomu3/hqstudio/compare/v1.13.1...v1.14.0) (2025-12-23)


### 🚀 Новые возможности

* **desktop:** добавлена валидация ввода для полей цены, телефона и гос. номера ([6fd17d8](https://github.com/randomu3/hqstudio/commit/6fd17d8d565ce790843a2a92bb7dc0a4538c9304))


### 🐛 Исправления

* **api:** возвращён PostgreSQL как БД по умолчанию для разработки ([ede21f8](https://github.com/randomu3/hqstudio/commit/ede21f8a4216fc550bfcd291b1abf7f5f939d733))
* **desktop:** ���������� ��������� emoji ������ ��� ��������� ��� ([1547eb1](https://github.com/randomu3/hqstudio/commit/1547eb172a402079193697aed6d9939f75779bd8))
* **desktop:** ���������� ��������� ������� � ������� ��������� ([1f410e7](https://github.com/randomu3/hqstudio/commit/1f410e7fd3c7e84f6bf52aeddcd7ba38fddacbb2))
* **desktop:** ������� �������� ���� - �������� ������� ����� �� ������������ ������� ([97c453a](https://github.com/randomu3/hqstudio/commit/97c453abab33a00cbb3fe6cd0267ccfa2803c48f))
* **desktop:** добавлены алиасы SecondaryButton и ButtonPrimary для светлой темы ([c70fcbb](https://github.com/randomu3/hqstudio/commit/c70fcbb4a906b8c84e1cb4495ca11c464b4dd854))
* **desktop:** улучшен контраст кнопок и иконок в светлой теме ([ec29ae7](https://github.com/randomu3/hqstudio/commit/ec29ae7adc17dc4ee98ce48381fa5ae483807e51))
* **docker:** �������� healthcheck - ������������ /api/health endpoint ([78db28b](https://github.com/randomu3/hqstudio/commit/78db28b3e9009cdd254fcf549d0e074e415d7d53))

## [1.14.0](https://github.com/randomu3/hqstudio/compare/v1.13.1...v1.14.0) (2025-12-23)


### 🚀 Новые возможности

* **desktop:** добавлена валидация ввода для полей цены, телефона и гос. номера ([6fd17d8](https://github.com/randomu3/hqstudio/commit/6fd17d8d565ce790843a2a92bb7dc0a4538c9304))


### 🐛 Исправления

* **api:** возвращён PostgreSQL как БД по умолчанию для разработки ([ede21f8](https://github.com/randomu3/hqstudio/commit/ede21f8a4216fc550bfcd291b1abf7f5f939d733))
* **desktop:** ���������� ��������� emoji ������ ��� ��������� ��� ([1547eb1](https://github.com/randomu3/hqstudio/commit/1547eb172a402079193697aed6d9939f75779bd8))
* **desktop:** ���������� ��������� ������� � ������� ��������� ([1f410e7](https://github.com/randomu3/hqstudio/commit/1f410e7fd3c7e84f6bf52aeddcd7ba38fddacbb2))
* **desktop:** ������� �������� ���� - �������� ������� ����� �� ������������ ������� ([97c453a](https://github.com/randomu3/hqstudio/commit/97c453abab33a00cbb3fe6cd0267ccfa2803c48f))
* **docker:** �������� healthcheck - ������������ /api/health endpoint ([78db28b](https://github.com/randomu3/hqstudio/commit/78db28b3e9009cdd254fcf549d0e074e415d7d53))

## [1.14.0](https://github.com/randomu3/hqstudio/compare/v1.13.1...v1.14.0) (2025-12-23)


### 🚀 Новые возможности

* **desktop:** добавлена валидация ввода для полей цены, телефона и гос. номера ([6fd17d8](https://github.com/randomu3/hqstudio/commit/6fd17d8d565ce790843a2a92bb7dc0a4538c9304))


### 🐛 Исправления

* **desktop:** ���������� ��������� emoji ������ ��� ��������� ��� ([1547eb1](https://github.com/randomu3/hqstudio/commit/1547eb172a402079193697aed6d9939f75779bd8))
* **desktop:** ���������� ��������� ������� � ������� ��������� ([1f410e7](https://github.com/randomu3/hqstudio/commit/1f410e7fd3c7e84f6bf52aeddcd7ba38fddacbb2))
* **desktop:** ������� �������� ���� - �������� ������� ����� �� ������������ ������� ([97c453a](https://github.com/randomu3/hqstudio/commit/97c453abab33a00cbb3fe6cd0267ccfa2803c48f))
* **docker:** �������� healthcheck - ������������ /api/health endpoint ([78db28b](https://github.com/randomu3/hqstudio/commit/78db28b3e9009cdd254fcf549d0e074e415d7d53))

## [1.14.0](https://github.com/randomu3/hqstudio/compare/v1.13.1...v1.14.0) (2025-12-23)


### 🚀 Новые возможности

* **desktop:** добавлена валидация ввода для полей цены, телефона и гос. номера ([6fd17d8](https://github.com/randomu3/hqstudio/commit/6fd17d8d565ce790843a2a92bb7dc0a4538c9304))


### 🐛 Исправления

* **desktop:** исправлена видимость emoji иконок для поддержки тем ([1547eb1](https://github.com/randomu3/hqstudio/commit/1547eb172a402079193697aed6d9939f75779bd8))
* **desktop:** исправлено наложение лоадера и пустого состояния ([1f410e7](https://github.com/randomu3/hqstudio/commit/1f410e7fd3c7e84f6bf52aeddcd7ba38fddacbb2))
* **desktop:** улучшен контраст тем - заменены жёсткие цвета на динамические ресурсы ([97c453a](https://github.com/randomu3/hqstudio/commit/97c453abab33a00cbb3fe6cd0267ccfa2803c48f))
* **docker:** улучшены healthcheck - используется /api/health endpoint, добавлены depends_on условия ([78db28b](https://github.com/randomu3/hqstudio/commit/78db28b3e9009cdd254fcf549d0e074e415d7d53))

## [1.13.1](https://github.com/randomu3/hqstudio/compare/v1.13.0...v1.13.1) (2025-12-23)


### 🚀 Новые возможности
## [1.13.1](https://github.com/randomu3/hqstudio/compare/v1.13.0...v1.13.1) (2025-12-23)


### 🐛 Исправления

* **desktop:** исправлена маршрутизация редактирования услуг через ViewModel API вместо локального DataService ([e6087de](https://github.com/randomu3/hqstudio/commit/e6087de441d548afd94dd8555fc20626c041cb44))

## [1.13.0](https://github.com/randomu3/hqstudio/compare/v1.12.0...v1.13.0) (2025-12-23)


### 🚀 Новые возможности

* **desktop:** добавлен выбор иконок с умными рекомендациями для услуг ([66d04f1](https://github.com/randomu3/hqstudio/commit/66d04f1868fe2c1a49649e16d471d22ff6c25ffe))
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

# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

> 📝 Этот файл автоматически обновляется при релизе через semantic-release

## [1.0.0](https://github.com/randomu3/hqstudio/releases/tag/v1.0.0) (2025-12-20)

### 🚀 Новые возможности

* добавлен CI/CD и подготовка к релизу ([249d0cc](https://github.com/randomu3/hqstudio/commit/249d0ccc9368ec5e49ba0d877b5bdd9212ea63ce))
* добавлены тесты, клавиатурная навигация и иконки ([042fb1e](https://github.com/randomu3/hqstudio/commit/042fb1e13bcd423cb1ccad3541598fdd79959bb6))

### � Инфраструктура

* Monorepo структура проекта (API, Web, Desktop)
* JWT аутентификация
* Docker поддержка (dev + prod)
* PostgreSQL для production, SQLite для разработки
* CI/CD с GitHub Actions
* Conventional Commits + автоматический changelog
