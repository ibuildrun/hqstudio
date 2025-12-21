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
