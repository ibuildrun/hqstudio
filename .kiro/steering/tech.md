# Technology Stack

## Web Application (HQStudio.Web)

### Framework & Runtime
- Next.js 14 with App Router
- React 18
- TypeScript 5.4

### Styling
- Tailwind CSS 3.4
- PostCSS with Autoprefixer
- Custom CSS in `globals.css`
- Font: Manrope (Google Fonts)

### Key Libraries
- `framer-motion` - Animations and scroll effects
- `lucide-react` - Icon library
- `@google/generative-ai` - AI integration (Gemini)
- `eslint` + `eslint-config-next` - Linting

### State Management
- React Context API (`lib/store.tsx`)
- localStorage for persistence

### Testing
- Vitest for unit tests
- Tests in `__tests__/` directory

### Deployment
- Docker with multi-stage builds
- GitHub Pages for static export
- Tuna tunneling for public access

### Commands
```bash
# Development
npm run dev

# Production build
npm run build
npm run start

# Linting
npm run lint

# Tests
npm test

# Docker
docker-compose up --build -d              # Production
docker-compose -f docker-compose.dev.yml up --build  # Development
```

---

## Backend API (HQStudio.API)

### Framework
- ASP.NET Core 8.0
- Entity Framework Core 8.0
- PostgreSQL 16 (production) / SQLite (development/tests)

### Key Libraries
- `Npgsql.EntityFrameworkCore.PostgreSQL` - PostgreSQL provider
- `BCrypt.Net-Next` - Password hashing
- `Microsoft.AspNetCore.Authentication.JwtBearer` - JWT auth
- `Swashbuckle.AspNetCore` - Swagger/OpenAPI

### Commands
```bash
# Development
dotnet run

# Build
dotnet build

# Publish
dotnet publish -c Release

# Docker (with PostgreSQL)
docker-compose -f docker-compose.dev.yml up -d
```

### API URL
- Development: `http://localhost:5000`
- Swagger UI: `http://localhost:5000/swagger`

---

## Desktop Application (HQStudio exe)

### Framework
- .NET 8.0 (Windows)
- WPF (Windows Presentation Foundation)

### Architecture
- MVVM pattern
- `Microsoft.Xaml.Behaviors.Wpf` for behaviors

### Build
```bash
# Build
dotnet build

# Run
dotnet run

# Publish
dotnet publish -c Release
```

---

## Environment Variables (Web)
| Variable | Purpose |
|----------|---------|
| `GEMINI_API_KEY` | Google AI API key |
| `TUNA_TOKEN` | Tuna tunnel authentication |
| `TUNA_SUBDOMAIN` | Public subdomain on tuna.am |
| `NEXT_PUBLIC_API_URL` | Backend API URL |

## Environment Variables (API)
| Variable | Purpose |
|----------|---------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL/SQLite connection string |
| `DB_PASSWORD` | PostgreSQL password (Docker) |
| `Jwt__Key` | JWT signing key (min 32 chars) |
| `Jwt__Issuer` | JWT issuer |
| `Jwt__Audience` | JWT audience |


---

## CI/CD & Automation

### GitHub Actions Workflows

| Workflow | Файл | Триггер | Назначение |
|----------|------|---------|------------|
| CI | `ci.yml` | Push/PR to main, develop | Тесты API, Web, Desktop + Codecov upload |
| Release | `release.yml` | Push to main | Semantic versioning, CHANGELOG, GitHub Release, Docker images, Desktop ZIP |
| Pages | `pages.yml` | Push to main | Deploy Web на GitHub Pages |
| CodeQL | `codeql.yml` | Push/PR + Weekly (Mon 6:00 UTC) | Security analysis для C# и JS/TS |
| Dependabot Auto-merge | `dependabot-automerge.yml` | Dependabot PR | Auto-merge patch/minor updates |

### CI Workflow Jobs

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   api-test      │    │   web-test      │    │ desktop-build   │
│   (Ubuntu)      │    │   (Ubuntu)      │    │   (Windows)     │
│                 │    │                 │    │                 │
│ • .NET 8.0      │    │ • Node 20       │    │ • .NET 8.0      │
│ • xUnit tests   │    │ • ESLint        │    │ • Build Release │
│ • Codecov (api) │    │ • TypeScript    │    │ • Unit tests    │
│                 │    │ • Vitest        │    │                 │
│                 │    │ • Codecov (web) │    │                 │
└────────┬────────┘    └────────┬────────┘    └─────────────────┘
         │                      │
         └──────────┬───────────┘
                    ▼
         ┌─────────────────┐
         │  docker-build   │
         │   (Ubuntu)      │
         │                 │
         │ • Build API img │
         │ • Build Web img │
         └─────────────────┘
```

### Release Workflow Jobs

```
test → release → docker (if new release) → desktop (if new release)
```

**Артефакты релиза:**
- Docker images в GHCR: `ghcr.io/randomu3/hqstudio/api:X.Y.Z`, `ghcr.io/randomu3/hqstudio/web:X.Y.Z`
- Desktop ZIP: `HQStudio-Desktop-vX.Y.Z.zip` (self-contained, single-file)
- CHANGELOG.md с release notes
- GitHub Release с описанием изменений

### Semantic Release

Конфигурация в `.releaserc.json`:
- Conventional Commits format required
- Auto-versioning: `feat:` → minor, `fix:` → patch, `perf:` → patch, `refactor:` → patch
- Auto-generates CHANGELOG.md с секциями:
  - 🚀 Новые возможности (feat)
  - 🐛 Исправления (fix)
  - ⚡ Производительность (perf)
  - ♻️ Рефакторинг (refactor)
- Creates GitHub Releases with artifacts

### Git Hooks (Husky)

```
.husky/
└── commit-msg    # Валидация через Commitlint
```

**Commitlint конфигурация** (`commitlint.config.js`):
- Разрешённые типы: feat, fix, docs, style, refactor, perf, test, build, ci, chore, revert
- Рекомендуемые scopes: api, web, desktop, tests, docker, ci, deps, release
- Без ограничений на регистр subject и длину body

### Dependabot

Конфигурация в `.github/dependabot.yml`:

| Ecosystem | Directory | Schedule | Limit | Labels |
|-----------|-----------|----------|-------|--------|
| npm | `/HQStudio.Web` | Weekly (Mon) | 5 PRs | dependencies, web |
| npm | `/` (root) | Monthly | 3 PRs | dependencies, ci |
| nuget | `/HQStudio.API` | Weekly (Mon) | 5 PRs | dependencies, api |
| nuget | `/HQStudio.Desktop` | Weekly (Mon) | 5 PRs | dependencies, desktop |
| github-actions | `/` | Monthly | - | dependencies, ci |

**Auto-merge:** patch/minor updates и GitHub Actions updates автоматически мержатся после прохождения CI.

**Игнорируемые major updates:** Next.js, ESLint, Vitest (требуют ручного review).

### Codecov

Конфигурация в `codecov.yml`:
- Coverage reports uploaded from CI
- Flags: `api` (HQStudio.API/), `web` (HQStudio.Web/lib/)
- Target: auto с threshold 5%
- Carryforward enabled для обоих flags
- Badge в README показывает общий coverage %

### Commit Message Format

```
type(scope): описание на русском языке

[optional body]

[optional footer]
```

**ВАЖНО:** Все коммиты должны быть на русском языке!

Types: feat, fix, docs, style, refactor, perf, test, build, ci, chore, revert
Scopes: api, web, desktop, tests, docker, ci, deps, release

### Проверка CI статуса

После push в main обязательно проверять статус:

```powershell
# PowerShell
Invoke-RestMethod -Uri "https://api.github.com/repos/randomu3/hqstudio/actions/runs?per_page=5" `
  -Headers @{Accept="application/vnd.github.v3+json"} | `
  Select-Object -ExpandProperty workflow_runs | `
  ForEach-Object { "$($_.name) | $($_.status) | $($_.conclusion)" }
```

```bash
# GitHub CLI
gh run list --limit 5
gh run view <run-id> --log-failed  # для просмотра ошибок
```

Все workflows должны быть `success`:
- ✅ CI
- ✅ Release
- ✅ Deploy to GitHub Pages
- ✅ CodeQL Security Analysis

---

## Code Quality

### EditorConfig
- Unified code style across all editors
- 4 spaces for C#, 2 spaces for TS/JS/JSON/YAML
- UTF-8 encoding, LF line endings

### ESLint (Web)
- `next/core-web-vitals` preset
- Warnings for `<img>` usage (prefer `next/image`)

### Testing
- API: xUnit + FluentAssertions + WebApplicationFactory
- Web: Vitest + @vitest/coverage-v8
- Desktop: xUnit (unit tests only in CI, integration tests skipped)
- Coverage tracked via Codecov (~50% target)
