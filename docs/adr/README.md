# Architecture Decision Records (ADR)

Этот каталог содержит записи архитектурных решений (ADR) для проекта HQ Studio.

## Что такое ADR?

ADR — это документ, фиксирующий важное архитектурное решение вместе с его контекстом и последствиями.

## Формат

Каждый ADR следует шаблону:
- **Статус**: Proposed / Accepted / Deprecated / Superseded
- **Контекст**: Описание проблемы или ситуации
- **Решение**: Принятое решение
- **Последствия**: Положительные и отрицательные эффекты

## Список ADR

| № | Название | Статус | Дата |
|---|----------|--------|------|
| [001](001-monorepo-structure.md) | Монорепозиторий | Accepted | 2024-01 |
| [002](002-api-technology.md) | ASP.NET Core для API | Accepted | 2024-01 |
| [003](003-web-framework.md) | Next.js для Web | Accepted | 2024-01 |
| [004](004-desktop-framework.md) | WPF для Desktop | Accepted | 2024-01 |
| [005](005-database-choice.md) | PostgreSQL/SQLite для БД | Accepted | 2024-02 |
| [006](006-authentication.md) | JWT аутентификация | Accepted | 2024-02 |
| [007](007-ci-cd-pipeline.md) | GitHub Actions CI/CD | Accepted | 2024-03 |
| [008](008-semantic-release.md) | Semantic Release | Accepted | 2024-03 |
| [009](009-testing-strategy.md) | Стратегия тестирования | Accepted | 2024-04 |
| [010](010-offline-first-desktop.md) | Offline-first для Desktop | Accepted | 2024-06 |

## Создание нового ADR

```bash
# Скопировать шаблон
cp docs/adr/template.md docs/adr/NNN-title.md

# Заполнить и закоммитить
git add docs/adr/NNN-title.md
git commit -m "docs(adr): добавлен ADR-NNN title"
```
