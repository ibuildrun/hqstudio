# Архитектура HQ Studio

## Обзор системы

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

## Компоненты

### HQStudio.API (Backend)

**Технологии:** ASP.NET Core 8.0, Entity Framework Core, JWT

**Структура:**
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

**Паттерны:**
- Repository через EF Core DbContext
- JWT Bearer аутентификация
- Rate Limiting для защиты от DDoS

### HQStudio.Web (Frontend)

**Технологии:** Next.js 14, React 18, TypeScript, Tailwind CSS

**Структура:**
```
HQStudio.Web/
├── app/             # Next.js App Router
├── components/      # React компоненты
├── lib/
│   ├── constants.ts # Конфигурация
│   ├── store.tsx    # React Context
│   └── types.ts     # TypeScript типы
└── public/          # Статические файлы
```

**Паттерны:**
- Server Components для SEO
- Client Components для интерактивности
- React Context для состояния

### HQStudio.Desktop (CRM)

**Технологии:** .NET 8.0 WPF, MVVM

**Структура:**
```
HQStudio.Desktop/
├── Views/           # XAML views
├── ViewModels/      # MVVM ViewModels
├── Models/          # Data models
├── Services/        # Business logic
├── Converters/      # XAML converters
└── Styles/          # Resource dictionaries
```

**Паттерны:**
- MVVM (Model-View-ViewModel)
- RelayCommand для команд
- Offline-first с синхронизацией

## База данных

### Схема

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

## Безопасность

### Аутентификация
- JWT токены с 24-часовым сроком
- BCrypt для хеширования паролей
- Refresh токены (планируется)

### Авторизация
- Role-based access control (RBAC)
- Роли: Admin, Editor, Manager

### Защита
- Rate Limiting
- CORS политики
- Input validation
- SQL injection protection (EF Core)

## Деплой

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

| Workflow | Файл | Триггер | Назначение |
|----------|------|---------|------------|
| CI | `ci.yml` | Push/PR to main, develop | Тесты API, Web, Desktop + Codecov |
| Release | `release.yml` | Push to main | Semantic versioning, CHANGELOG, GitHub Release, Docker images |
| Pages | `pages.yml` | Push to main | Deploy Web на GitHub Pages |
| CodeQL | `codeql.yml` | Push/PR + Weekly | Анализ безопасности C# и JS/TS |
| Dependabot Auto-merge | `dependabot-automerge.yml` | Dependabot PR | Auto-merge patch/minor updates |

### Артефакты релиза

При каждом релизе автоматически создаются:
- **Docker images** в GitHub Container Registry:
  - `ghcr.io/randomu3/hqstudio/api:X.Y.Z`
  - `ghcr.io/randomu3/hqstudio/web:X.Y.Z`
- **Desktop ZIP** с self-contained exe
- **CHANGELOG.md** с описанием изменений
- **GitHub Release** с release notes

## Мониторинг

- `/api/health` — health check endpoint
- Swagger UI — API документация (`/swagger`)
- GitHub Actions — CI/CD статус
- Codecov — покрытие кода тестами
- CodeQL — security alerts

## Дополнительная документация

- [Git-интеграция и CI/CD](GIT-INTEGRATION.md) — полная документация по автоматизации
- [API документация](API.md) — описание REST endpoints
