# Git-интеграция и CI/CD

Полная документация по интеграции с Git, автоматизации и CI/CD пайплайнам проекта HQ Studio.

## 📋 Содержание

- [Обзор инфраструктуры](#обзор-инфраструктуры)
- [Conventional Commits](#conventional-commits)
- [Git Hooks (Husky)](#git-hooks-husky)
- [GitHub Actions Workflows](#github-actions-workflows)
- [Semantic Release](#semantic-release)
- [Dependabot](#dependabot)
- [Codecov](#codecov)
- [Issue & PR Templates](#issue--pr-templates)
- [EditorConfig](#editorconfig)
- [Kiro AI Integration](#kiro-ai-integration)

---

## Обзор инфраструктуры

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
         │                         │                         │
         └─────────────────────────┼─────────────────────────┘
                                   ▼
                    ┌─────────────────────────┐
                    │   CodeQL (Weekly +      │
                    │   Push/PR to main)      │
                    │   • C# Analysis         │
                    │   • JS/TS Analysis      │
                    └─────────────────────────┘
```

### Ключевые компоненты

| Компонент | Файл | Назначение |
|-----------|------|------------|
| Commitlint | `commitlint.config.js` | Валидация сообщений коммитов |
| Husky | `.husky/commit-msg` | Git hooks для проверки коммитов |
| Semantic Release | `.releaserc.json` | Автоматическое версионирование |
| Dependabot | `.github/dependabot.yml` | Автообновление зависимостей |
| Codecov | `codecov.yml` | Отслеживание покрытия кода |
| EditorConfig | `.editorconfig` | Единый стиль кода |

---

## Conventional Commits

Проект использует [Conventional Commits](https://www.conventionalcommits.org/) для стандартизации сообщений коммитов.

### Формат

```
<type>(<scope>): <description>

[optional body]

[optional footer(s)]
```

### Типы коммитов

| Тип | Описание | Влияние на версию |
|-----|----------|-------------------|
| `feat` | Новая функциональность | **minor** (1.x.0) |
| `fix` | Исправление бага | **patch** (1.0.x) |
| `perf` | Улучшение производительности | **patch** |
| `refactor` | Рефакторинг кода | **patch** |
| `docs` | Документация | Без релиза |
| `style` | Форматирование | Без релиза |
| `test` | Тесты | Без релиза |
| `build` | Сборка/зависимости | Без релиза |
| `ci` | CI/CD конфигурация | Без релиза |
| `chore` | Прочие изменения | Без релиза |
| `revert` | Откат изменений | Зависит от типа |

### Области (Scopes)

| Scope | Описание |
|-------|----------|
| `api` | HQStudio.API (ASP.NET Core) |
| `web` | HQStudio.Web (Next.js) |
| `desktop` | HQStudio.Desktop (WPF) |
| `tests` | Тесты любого компонента |
| `docker` | Docker конфигурация |
| `ci` | CI/CD пайплайны |
| `deps` | Зависимости |
| `release` | Автоматические релизы |

### Примеры коммитов

```bash
# Новая функция (minor release)
feat(api): добавлен endpoint для экспорта заказов

# Исправление бага (patch release)
fix(web): исправлена ошибка валидации формы обратной связи

# Документация (без релиза)
docs: обновлена документация по API

# Рефакторинг (patch release)
refactor(desktop): оптимизирован DataService для работы с кэшем

# Breaking change (major release)
feat(api)!: изменён формат ответа API

BREAKING CHANGE: поле `status` теперь возвращает enum вместо строки

# Зависимости (без релиза)
chore(deps): обновлены зависимости NuGet
```

### Интерактивный коммит

```bash
# Запуск Commitizen для интерактивного создания коммита
npm run commit
```

---

## Git Hooks (Husky)

### Структура

```
.husky/
├── _/                  # Husky internals
└── commit-msg          # Валидация сообщения коммита
```

### commit-msg hook

Файл `.husky/commit-msg`:
```bash
npx --no -- commitlint --edit $1
```

Этот hook автоматически проверяет каждый коммит на соответствие Conventional Commits.

### Конфигурация Commitlint

Файл `commitlint.config.js`:
```javascript
module.exports = {
  extends: ['@commitlint/config-conventional'],
  rules: {
    'type-enum': [2, 'always', [
      'feat', 'fix', 'docs', 'style', 'refactor',
      'perf', 'test', 'build', 'ci', 'chore', 'revert'
    ]],
    'scope-enum': [1, 'always', [
      'api', 'web', 'desktop', 'tests', 'docker', 'ci', 'deps'
    ]],
    'subject-case': [0],        // Разрешён любой регистр
    'body-max-line-length': [0] // Без ограничения длины body
  }
};
```

### Установка hooks

```bash
# Автоматически при npm install (через prepare script)
npm install

# Или вручную
npx husky install
```

---

## GitHub Actions Workflows

### 1. CI Workflow (`ci.yml`)

**Триггеры:** Push/PR в `main`, `develop`

```yaml
jobs:
  api-test:      # Ubuntu, .NET 8.0
  web-test:      # Ubuntu, Node 20
  desktop-build: # Windows, .NET 8.0
  docker-build:  # Ubuntu (после тестов)
```

**Этапы API Tests:**
1. Checkout кода
2. Setup .NET 8.0
3. Restore dependencies
4. Build проекта
5. Запуск тестов с coverage
6. Upload coverage в Codecov (flag: `api`)
7. Upload test results artifact

**Этапы Web Tests:**
1. Checkout кода
2. Setup Node.js 20
3. npm ci (с кэшированием)
4. ESLint проверка
5. TypeScript type check
6. Vitest с coverage
7. Upload coverage в Codecov (flag: `web`)

**Этапы Desktop Build:**
1. Checkout кода
2. Setup .NET 8.0
3. Restore dependencies
4. Build Release
5. Запуск unit тестов (без Integration)

**Этапы Docker Build:**
1. Build API image
2. Build Web image

### 2. Release Workflow (`release.yml`)

**Триггеры:** Push в `main`, manual dispatch

```yaml
jobs:
  test:     # Прогон всех тестов
  release:  # Semantic Release
  docker:   # Push images в GHCR
  desktop:  # Build и upload ZIP
```

**Semantic Release этапы:**
1. Анализ коммитов с последнего релиза
2. Определение новой версии (semver)
3. Генерация CHANGELOG.md
4. Создание Git tag
5. Создание GitHub Release
6. Push изменений в репозиторий

**Docker этапы (если есть новый релиз):**
1. Login в GitHub Container Registry
2. Build и push API image с тегами:
   - `ghcr.io/randomu3/hqstudio/api:X.Y.Z`
   - `ghcr.io/randomu3/hqstudio/api:latest`
3. Build и push Web image аналогично

**Desktop этапы (если есть новый релиз):**
1. Update версии в .csproj
2. Publish self-contained single-file exe
3. Создание ZIP архива
4. Upload в GitHub Release

### 3. Pages Workflow (`pages.yml`)

**Триггеры:** Push в `main`, manual dispatch

```yaml
jobs:
  build:   # Next.js static export
  deploy:  # GitHub Pages deployment
```

**Этапы:**
1. Checkout кода
2. Setup Node.js 20
3. Configure Pages для Next.js
4. npm ci
5. `npm run build` (static export в `out/`)
6. Upload artifact
7. Deploy в GitHub Pages

**URL:** https://randomu3.github.io/hqstudio/

### 4. CodeQL Workflow (`codeql.yml`)

**Триггеры:** Push/PR в `main`, Weekly (понедельник 6:00 UTC)

```yaml
strategy:
  matrix:
    language: ['csharp', 'javascript-typescript']
```

**Этапы:**
1. Checkout кода
2. Initialize CodeQL с `security-extended` queries
3. Build .NET проектов (для C#)
4. Perform CodeQL Analysis
5. Upload results в Security tab

### 5. Dependabot Auto-merge (`dependabot-automerge.yml`)

**Триггеры:** PR от dependabot[bot]

**Логика:**
- Patch/Minor updates → Auto-merge (squash)
- GitHub Actions updates → Auto-merge (squash)
- Major updates → Требуют ручного review

---

## Semantic Release

### Конфигурация (`.releaserc.json`)

```json
{
  "branches": ["main"],
  "plugins": [
    "@semantic-release/commit-analyzer",
    "@semantic-release/release-notes-generator",
    "@semantic-release/changelog",
    "@semantic-release/git",
    "@semantic-release/github"
  ]
}
```

### Release Rules

| Тип коммита | Релиз |
|-------------|-------|
| `feat` | minor |
| `fix` | patch |
| `perf` | patch |
| `refactor` | patch |
| `docs`, `style`, `chore`, `test`, `build`, `ci` | Без релиза |

### Секции CHANGELOG

| Тип | Секция в CHANGELOG |
|-----|-------------------|
| `feat` | 🚀 Новые возможности |
| `fix` | 🐛 Исправления |
| `perf` | ⚡ Производительность |
| `refactor` | ♻️ Рефакторинг |

### Локальный dry-run

```bash
npm run release:dry
```

---

## Dependabot

### Конфигурация (`.github/dependabot.yml`)

| Ecosystem | Directory | Schedule | Limit |
|-----------|-----------|----------|-------|
| npm | `/HQStudio.Web` | Weekly (Mon) | 5 PRs |
| npm | `/` (root) | Monthly | 3 PRs |
| nuget | `/HQStudio.API` | Weekly (Mon) | 5 PRs |
| nuget | `/HQStudio.Desktop` | Weekly (Mon) | 5 PRs |
| github-actions | `/` | Monthly | - |

### Игнорируемые major updates

- `next` (Next.js)
- `eslint`, `eslint-config-next`
- `vitest`, `@vitest/*`

### Labels

| Label | Описание |
|-------|----------|
| `dependencies` | Все PR от Dependabot |
| `web` | NPM зависимости Web |
| `api` | NuGet зависимости API |
| `desktop` | NuGet зависимости Desktop |
| `ci` | GitHub Actions и root npm |

### Commit prefix

Все коммиты от Dependabot используют prefix `chore(deps)`:
```
chore(deps): bump framer-motion from 11.0.0 to 11.1.0
```

---

## Codecov

### Конфигурация (`codecov.yml`)

```yaml
coverage:
  precision: 2
  range: "60...100"
  status:
    project:
      default:
        target: auto
        threshold: 5%
        informational: true

flags:
  api:
    paths: [HQStudio.API/]
    carryforward: true
  web:
    paths: [HQStudio.Web/lib/]
    carryforward: true
```

### Flags

| Flag | Покрытие | Источник |
|------|----------|----------|
| `api` | HQStudio.API | xUnit + coverlet |
| `web` | HQStudio.Web/lib | Vitest + v8 |

### Badge

```markdown
[![codecov](https://codecov.io/gh/randomu3/hqstudio/graph/badge.svg)](https://codecov.io/gh/randomu3/hqstudio)
```

### Локальный запуск с coverage

```bash
# API
dotnet test HQStudio.API.Tests --collect:"XPlat Code Coverage"

# Web
cd HQStudio.Web && npm test -- --coverage
```

---

## Issue & PR Templates

### Bug Report (`.github/ISSUE_TEMPLATE/bug_report.md`)

Поля:
- Описание бага
- Шаги для воспроизведения
- Ожидаемое поведение
- Скриншоты
- Окружение (компонент, версия, ОС, браузер)

### Feature Request (`.github/ISSUE_TEMPLATE/feature_request.md`)

Поля:
- Проблема
- Предлагаемое решение
- Альтернативы
- Компонент (Web/Desktop/API/Инфраструктура)

### Pull Request (`.github/pull_request_template.md`)

Чеклист:
- [ ] Тип изменений (fix/feat/docs/refactor/test/chore)
- [ ] Связанные Issues
- [ ] Код соответствует стилю
- [ ] Тесты добавлены/обновлены
- [ ] Документация обновлена
- [ ] Все тесты проходят локально
- [ ] Коммиты следуют Conventional Commits

---

## EditorConfig

### Конфигурация (`.editorconfig`)

| Файлы | Indent | Особенности |
|-------|--------|-------------|
| `*.cs` | 4 spaces | .NET naming conventions |
| `*.{ts,tsx,js,jsx}` | 2 spaces | - |
| `*.json` | 2 spaces | - |
| `*.{yml,yaml}` | 2 spaces | - |
| `*.{xml,xaml,csproj}` | 2 spaces | - |
| `*.md` | 2 spaces | Сохранять trailing whitespace |
| `Makefile` | tabs | - |
| `*.sh` | 2 spaces | LF line endings |
| `*.{cmd,bat}` | 2 spaces | CRLF line endings |

### Общие настройки

- Charset: UTF-8
- Line endings: LF (кроме Windows batch)
- Final newline: Yes
- Trim trailing whitespace: Yes (кроме Markdown)

---

## Kiro AI Integration

### Структура Steering Files

```
.kiro/
└── steering/
    ├── conventions.md   # Coding conventions и Git правила
    ├── product.md       # Описание продукта
    ├── structure.md     # Структура проекта
    └── tech.md          # Technology stack и CI/CD
```

### Автоматическое включение

Все steering files включаются автоматически в контекст Kiro при работе с проектом.

### Ключевые правила для Kiro

1. **Коммиты на русском языке** — все сообщения коммитов должны быть на русском
2. **Conventional Commits** — строгое следование формату
3. **Проверка CI после push** — обязательная проверка статуса workflows
4. **Локальные тесты** — запуск тестов перед push

### Команда проверки CI статуса

```powershell
Invoke-RestMethod -Uri "https://api.github.com/repos/randomu3/hqstudio/actions/runs?per_page=5" `
  -Headers @{Accept="application/vnd.github.v3+json"} | `
  Select-Object -ExpandProperty workflow_runs | `
  ForEach-Object { "$($_.name) | $($_.status) | $($_.conclusion)" }
```

---

## Быстрые команды

### Локальная разработка

```bash
# Запуск тестов перед коммитом
dotnet test HQStudio.API.Tests
npm test --prefix HQStudio.Web
dotnet test HQStudio.Desktop.Tests --filter "Category!=Integration"

# Интерактивный коммит
npm run commit

# Dry-run релиза
npm run release:dry
```

### Проверка статуса

```bash
# Статус последних workflow runs
gh run list --limit 5

# Детали конкретного run
gh run view <run-id>

# Логи failed job
gh run view <run-id> --log-failed
```

### Docker

```bash
# Локальная сборка
docker build -t hqstudio-api:local ./HQStudio.API
docker build -t hqstudio-web:local ./HQStudio.Web

# Pull из GHCR
docker pull ghcr.io/randomu3/hqstudio/api:latest
docker pull ghcr.io/randomu3/hqstudio/web:latest
```
