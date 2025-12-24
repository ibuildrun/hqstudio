# Git Integration & CI/CD

Полная информация о Git-интеграции, автоматизации и CI/CD пайплайнах проекта.

## Инфраструктура автоматизации

```
┌─────────────────────────────────────────────────────────────────────┐
│                         Git Push to main                            │
└─────────────────────────────────────────────────────────────────────┘
                                   │
         ┌─────────────────────────┼─────────────────────────┐
         │                         │                         │
         ▼                         ▼                         ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   CI Workflow   │    │ Release Workflow│    │ Pages Workflow  │
│                 │    │                 │    │                 │
│ • API Tests     │    │ • Semantic Ver  │    │ • Build Next.js │
│ • Web Tests     │    │ • CHANGELOG     │    │ • Deploy Pages  │
│ • Desktop Build │    │ • GitHub Release│    │                 │
│ • Docker Build  │    │ • Docker Push   │    │                 │
│ • Codecov       │    │ • Desktop ZIP   │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

## Ключевые файлы конфигурации

| Файл | Назначение |
|------|------------|
| `.releaserc.json` | Semantic Release конфигурация |
| `commitlint.config.js` | Правила валидации коммитов |
| `.husky/commit-msg` | Git hook для проверки коммитов |
| `codecov.yml` | Конфигурация покрытия кода |
| `.editorconfig` | Единый стиль кода |
| `.github/dependabot.yml` | Автообновление зависимостей |

## GitHub Actions Workflows

### CI (`ci.yml`)
- **Триггер:** Push/PR to main, develop
- **Jobs:** api-test, web-test, desktop-build, docker-build
- **Артефакты:** Test results, Codecov reports

### Release (`release.yml`)
- **Триггер:** Push to main
- **Jobs:** test → release → docker → desktop
- **Артефакты:** Docker images (GHCR), Desktop ZIP, CHANGELOG, GitHub Release

### Pages (`pages.yml`)
- **Триггер:** Push to main
- **Jobs:** build → deploy
- **URL:** https://randomu3.github.io/hqstudio/

### CodeQL (`codeql.yml`)
- **Триггер:** Push/PR to main + Weekly (Mon 6:00 UTC)
- **Languages:** C#, JavaScript/TypeScript
- **Queries:** security-extended

### Dependabot Auto-merge (`dependabot-automerge.yml`)
- **Триггер:** Dependabot PR
- **Auto-merge:** patch/minor updates, GitHub Actions updates

## Semantic Release

### Release Rules

| Тип коммита | Версия |
|-------------|--------|
| `feat` | minor (1.x.0) |
| `fix`, `perf`, `refactor` | patch (1.0.x) |
| `docs`, `style`, `test`, `build`, `ci`, `chore` | Без релиза |
| `feat!`, `fix!` (breaking) | major (x.0.0) |

### CHANGELOG секции

- 🚀 Новые возможности (feat)
- 🐛 Исправления (fix)
- ⚡ Производительность (perf)
- ♻️ Рефакторинг (refactor)

## Dependabot

### Расписание обновлений

| Ecosystem | Directory | Schedule |
|-----------|-----------|----------|
| npm | `/HQStudio.Web` | Weekly (Mon) |
| npm | `/` (root) | Monthly |
| nuget | `/HQStudio.API` | Weekly (Mon) |
| nuget | `/HQStudio.Desktop` | Weekly (Mon) |
| github-actions | `/` | Monthly |

### Игнорируемые major updates
- Next.js, ESLint, Vitest (требуют ручного review)

## Codecov

### Flags

| Flag | Путь | Источник |
|------|------|----------|
| `api` | HQStudio.API/ | xUnit + coverlet |
| `web` | HQStudio.Web/lib/ | Vitest + v8 |

### Локальный запуск с coverage

```bash
# API
dotnet test HQStudio.API.Tests --collect:"XPlat Code Coverage"

# Web
cd HQStudio.Web && npm test -- --coverage
```

## Быстрые команды

```bash
# Интерактивный коммит
npm run commit

# Dry-run релиза
npm run release:dry

# Проверка CI статуса
gh run list --limit 5

# Просмотр ошибок
gh run view <run-id> --log-failed

# Локальные тесты
dotnet test HQStudio.API.Tests
npm test --prefix HQStudio.Web
dotnet test HQStudio.Desktop.Tests --filter "Category!=Integration"
```

## Документация

- [docs/GIT-INTEGRATION.md](docs/GIT-INTEGRATION.md) — полная техническая документация
- [CONTRIBUTING.md](CONTRIBUTING.md) — руководство по внесению вклада
