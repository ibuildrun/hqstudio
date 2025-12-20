# Technology Stack

## Web Application (HQStudio site)

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

### State Management
- React Context API (`lib/store.tsx`)
- localStorage for persistence

### Deployment
- Docker with multi-stage builds
- Tuna tunneling for public access (Russian ngrok alternative)

### Commands
```bash
# Development
npm run dev

# Production build
npm run build
npm run start

# Linting
npm run lint

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
