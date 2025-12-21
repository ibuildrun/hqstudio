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
