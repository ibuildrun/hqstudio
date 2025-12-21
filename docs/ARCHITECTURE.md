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
Push to main
    │
    ├── CI: Tests (API, Web, Desktop)
    │
    ├── Release: Semantic versioning
    │   └── CHANGELOG update
    │
    └── Deploy: GitHub Pages (Web)
```

## Мониторинг

- `/api/health` — health check endpoint
- Swagger UI — API документация
- GitHub Actions — CI/CD статус
