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
| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `ci.yml` | Push/PR to main | Run tests for API, Web, Desktop |
| `release.yml` | Push to main | Semantic versioning, CHANGELOG, GitHub Release |
| `pages.yml` | Push to main | Deploy Web to GitHub Pages |
| `codeql.yml` | Push/PR/Weekly | Security analysis |

### Semantic Release
- Conventional Commits format required
- Auto-versioning: `feat:` → minor, `fix:` → patch
- Auto-generates CHANGELOG.md
- Creates GitHub Releases with artifacts

### Dependabot
- Weekly updates for npm (Web)
- Weekly updates for NuGet (API, Desktop)
- Monthly updates for GitHub Actions

### Commit Message Format
```
type(scope): description

Types: feat, fix, docs, style, refactor, perf, test, build, ci, chore
Scopes: api, web, desktop, ci, deps
```

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
- Web: Vitest
- Desktop: xUnit (unit tests only in CI)
