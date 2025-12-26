# HQ Studio Architecture

## System Overview

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   Web (Next.js) │     │ Desktop (WPF)   │     │  Mobile (TBD)   │
│   Port: 3000    │     │   Windows App   │     │                 │
└────────┬────────┘     └────────┬────────┘     └────────┬────────┘
         │                       │                       │
         └───────────────────────┼───────────────────────┘
                                 │
                    ┌────────────▼────────────┐
                    │     API (ASP.NET)       │
                    │      Port: 5000         │
                    └────────────┬────────────┘
                                 │
                    ┌────────────▼────────────┐
                    │   PostgreSQL / SQLite   │
                    └─────────────────────────┘
```

## Components

### HQStudio.API (Backend)

**Technologies:** ASP.NET Core 8.0, Entity Framework Core, JWT

**Structure:**
```
HQStudio.API/
├── Controllers/     # REST endpoints
├── Models/          # Entity models
├── DTOs/            # Data transfer objects
├── Data/
│   ├── AppDbContext.cs
│   └── DbSeeder.cs
├── Services/
│   └── JwtService.cs
└── Program.cs
```

**Patterns:**
- Repository via EF Core DbContext
- JWT Bearer authentication
- Rate Limiting for DDoS protection

### HQStudio.Web (Frontend)

**Technologies:** Next.js 14, React 18, TypeScript, Tailwind CSS

**Structure:**
```
HQStudio.Web/
├── app/             # Next.js App Router
├── components/      # React components
├── lib/
│   ├── constants.ts # Configuration
│   ├── store.tsx    # React Context
│   └── types.ts     # TypeScript types
└── public/          # Static files
```

**Patterns:**
- Server Components for SEO
- Client Components for interactivity
- React Context for state

### HQStudio.Desktop (CRM)

**Technologies:** .NET 8.0 WPF, MVVM

**Structure:**
```
HQStudio.Desktop/
├── Views/           # XAML views
├── ViewModels/      # MVVM ViewModels
├── Models/          # Data models
├── Services/        # Business logic
├── Converters/      # XAML converters
└── Styles/          # Resource dictionaries
```

**Patterns:**
- MVVM (Model-View-ViewModel)
- RelayCommand for commands
- Offline-first with synchronization

## Database

### Schema

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│    Users     │     │   Clients    │     │   Services   │
├──────────────┤     ├──────────────┤     ├──────────────┤
│ Id           │     │ Id           │     │ Id           │
│ Login        │     │ Name         │     │ Title        │
│ PasswordHash │     │ Phone        │     │ Category     │
│ Name         │     │ CarModel     │     │ Price        │
│ Role         │     │ LicensePlate │     │ Description  │
│ CreatedAt    │     │ CreatedAt    │     │ IsActive     │
└──────────────┘     └──────┬───────┘     └──────────────┘
                           │
                    ┌──────▼───────┐
                    │    Orders    │
                    ├──────────────┤
                    │ Id           │
                    │ ClientId     │
                    │ ServiceIds   │
                    │ Status       │
                    │ TotalPrice   │
                    │ CreatedAt    │
                    └──────────────┘

┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│  Callbacks   │     │Subscriptions │     │ SiteContent  │
├──────────────┤     ├──────────────┤     ├──────────────┤
│ Id           │     │ Id           │     │ Id           │
│ Name         │     │ Email        │     │ Key          │
│ Phone        │     │ CreatedAt    │     │ Value (JSON) │
│ Status       │     │ IsActive     │     │ UpdatedAt    │
│ Source       │     └──────────────┘     └──────────────┘
│ CreatedAt    │
└──────────────┘
```

## Security

### Authentication
- JWT tokens with 24-hour expiration
- BCrypt for password hashing
- Refresh tokens (planned)

### Authorization
- Role-based access control (RBAC)
- Roles: Admin, Editor, Manager

### Protection
- Rate Limiting
- CORS policies
- Input validation
- SQL injection protection (EF Core)

## Deployment

### Development
```bash
docker-compose -f docker-compose.dev.yml up
```

### Production
```bash
docker-compose up -d
```

### CI/CD Pipeline

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
                                   │
                    ┌──────────────▼──────────────┐
                    │   CodeQL Security Analysis  │
                    │   (Weekly + Push/PR)        │
                    └─────────────────────────────┘
```

### GitHub Actions Workflows

| Workflow | File | Trigger | Purpose |
|----------|------|---------|---------|
| CI | `ci.yml` | Push/PR to main, develop | API, Web, Desktop tests + Codecov |
| Release | `release.yml` | Push to main | Semantic versioning, CHANGELOG, GitHub Release, Docker images |
| Pages | `pages.yml` | Push to main | Deploy Web to GitHub Pages |
| CodeQL | `codeql.yml` | Push/PR + Weekly | Security analysis for C# and JS/TS |
| Dependabot Auto-merge | `dependabot-automerge.yml` | Dependabot PR | Auto-merge patch/minor updates |

### Release Artifacts

Each release automatically creates:
- **Docker images** in GitHub Container Registry:
  - `ghcr.io/randomu3/hqstudio/api:X.Y.Z`
  - `ghcr.io/randomu3/hqstudio/web:X.Y.Z`
- **Desktop ZIP** with self-contained exe
- **CHANGELOG.md** with change descriptions
- **GitHub Release** with release notes

## Monitoring

- `/api/health` — health check endpoint
- Swagger UI — API documentation (`/swagger`)
- GitHub Actions — CI/CD status
- Codecov — code test coverage
- CodeQL — security alerts

## Additional Documentation

- [Git Integration & CI/CD](GIT-INTEGRATION.md) — full automation documentation
- [API Documentation](API.md) — REST endpoints description
