# HQ Studio

[![CI](https://github.com/randomu3/hqstudio/actions/workflows/ci.yml/badge.svg)](https://github.com/randomu3/hqstudio/actions/workflows/ci.yml)
[![Release](https://github.com/randomu3/hqstudio/actions/workflows/release.yml/badge.svg)](https://github.com/randomu3/hqstudio/actions/workflows/release.yml)
[![codecov](https://codecov.io/gh/randomu3/hqstudio/graph/badge.svg)](https://codecov.io/gh/randomu3/hqstudio)
[![GitHub release](https://img.shields.io/github/v/release/randomu3/hqstudio)](https://github.com/randomu3/hqstudio/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Комплексное решение для автотюнинг студии: веб-сайт, API и десктопное CRM приложение.

🌐 **[Демо сайт](https://randomu3.github.io/hqstudio/)** | 📦 **[Релизы](https://github.com/randomu3/hqstudio/releases)** | 📖 **[API Docs](docs/API.md)** | 🏛️ **[Архитектура](docs/ARCHITECTURE.md)**

## 🏗️ Структура проекта

```
├── HQStudio.API/          # ASP.NET Core 8.0 Backend
├── HQStudio.API.Tests/    # API Integration Tests
├── HQStudio.Web/          # Next.js 14 Frontend
├── HQStudio.Desktop/      # WPF Desktop Application
├── HQStudio.Desktop.Tests/# Desktop Unit Tests
└── docker-compose.yml     # Production Docker setup
```

## 🚀 Быстрый старт

### Требования
- .NET 8.0 SDK
- Node.js 20+
- Docker (опционально)

### Локальная разработка

```bash
# Клонировать репозиторий
git clone https://github.com/randomu3/hqstudio.git
cd hqstudio

# Скопировать env файлы
cp .env.example .env

# Запустить API
cd HQStudio.API
dotnet run

# Запустить Web (в другом терминале)
cd HQStudio.Web
npm install
npm run dev

# Запустить Desktop (Windows)
cd HQStudio.Desktop
dotnet run
```

### Docker (разработка с hot-reload)

```bash
docker-compose -f docker-compose.dev.yml up --build
```

### Docker (production)

```bash
docker-compose up --build -d
```

## 🧪 Тесты

```bash
# API тесты
dotnet test HQStudio.API.Tests

# Web тесты
cd HQStudio.Web && npm test

# Desktop тесты
dotnet test HQStudio.Desktop.Tests
```

## 📦 Технологии

### Backend (API)
- ASP.NET Core 8.0
- Entity Framework Core
- PostgreSQL / SQLite
- JWT Authentication
- Swagger/OpenAPI

### Frontend (Web)
- Next.js 14 (App Router)
- React 18
- TypeScript
- Tailwind CSS
- Framer Motion

### Desktop
- .NET 8.0 WPF
- MVVM Pattern
- Material Design

## 🔐 Переменные окружения

См. `.env.example` для полного списка переменных.

## 📝 Лицензия

MIT License — см. [LICENSE](LICENSE)

## 🤝 Вклад в проект

См. [CONTRIBUTING.md](CONTRIBUTING.md) для информации о том, как внести вклад.

## 🔒 Безопасность

См. [SECURITY.md](SECURITY.md) для политики безопасности.
