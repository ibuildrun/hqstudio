# ADR-002: ASP.NET Core для API

**Дата**: 2024-01-15
**Статус**: Accepted

## Контекст

Необходимо было выбрать технологию для Backend API. Рассматривались варианты:

1. **ASP.NET Core** — C#, кроссплатформенный
2. **Node.js (Express/Fastify)** — JavaScript/TypeScript
3. **Python (FastAPI/Django)** — Python
4. **Go (Gin/Echo)** — Go

Ключевые требования:
- Высокая производительность
- Типизация
- Хорошая поддержка ORM
- Интеграция с Desktop (WPF)

## Решение

Выбран **ASP.NET Core 8.0** с Entity Framework Core.

Стек:
- ASP.NET Core 8.0 — веб-фреймворк
- Entity Framework Core 8.0 — ORM
- PostgreSQL (production) / SQLite (development)
- JWT Bearer — аутентификация
- Swagger/OpenAPI — документация API

## Последствия

### Положительные

- **Единый язык**: C# для API и Desktop (WPF)
- **Производительность**: один из самых быстрых веб-фреймворков
- **Типизация**: строгая типизация на этапе компиляции
- **Entity Framework**: мощный ORM с миграциями
- **Swagger**: автоматическая документация API
- **Долгосрочная поддержка**: LTS версии от Microsoft

### Отрицательные

- **Размер runtime**: больше чем Node.js или Go
- **Порог входа**: C# сложнее JavaScript для новичков
- **Docker образы**: больше размер из-за .NET runtime

### Нейтральные

- Требуется .NET SDK для разработки
- Хостинг на Linux/Windows/Docker

## Связанные решения

- [ADR-004](004-desktop-framework.md) — WPF для Desktop (тот же язык)
- [ADR-005](005-database-choice.md) — Выбор базы данных
- [ADR-006](006-authentication.md) — JWT аутентификация
