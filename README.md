# HQ Studio

[![CI](https://github.com/randomu3/hqstudio/actions/workflows/ci.yml/badge.svg)](https://github.com/randomu3/hqstudio/actions/workflows/ci.yml)
[![Release](https://github.com/randomu3/hqstudio/actions/workflows/release.yml/badge.svg)](https://github.com/randomu3/hqstudio/actions/workflows/release.yml)
[![codecov](https://codecov.io/gh/randomu3/hqstudio/graph/badge.svg)](https://codecov.io/gh/randomu3/hqstudio)
[![GitHub release](https://img.shields.io/github/v/release/randomu3/hqstudio)](https://github.com/randomu3/hqstudio/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A comprehensive solution for an auto tuning studio: website, API, and desktop CRM application.

**[Demo Site](https://randomu3.github.io/hqstudio/)** | **[Releases](https://github.com/randomu3/hqstudio/releases)** | **[API Docs](docs/API.md)** | **[Architecture](docs/ARCHITECTURE.md)** | **[Git & CI/CD](docs/GIT-INTEGRATION.md)**

## Project Structure

```
├── HQStudio.API/          # ASP.NET Core 8.0 Backend
├── HQStudio.API.Tests/    # API Integration Tests
├── HQStudio.Web/          # Next.js 14 Frontend
├── HQStudio.Desktop/      # WPF Desktop Application
├── HQStudio.Desktop.Tests/# Desktop Unit Tests
└── docker-compose.yml     # Production Docker setup
```

## Desktop Application Screenshots

<details>
<summary>Show screenshots</summary>

### Login Window
![Login](docs/screenshots/01-login.png)

### Dashboard (Dark Theme)
![Dashboard Dark](docs/screenshots/02-dashboard.png)

### Orders
![Orders](docs/screenshots/03-orders.png)

### Clients
![Clients](docs/screenshots/04-clients.png)

### Services
![Services](docs/screenshots/05-services.png)

### Staff
![Staff](docs/screenshots/06-staff.png)

### Settings
![Settings](docs/screenshots/07-settings.png)

### Dashboard (Light Theme)
![Dashboard Light](docs/screenshots/08-dashboard-light.png)

</details>

## Quick Start

### Requirements
- .NET 8.0 SDK
- Node.js 20+
- Docker (optional)

### Local Development

```bash
# Clone the repository
git clone https://github.com/randomu3/hqstudio.git
cd hqstudio

# Copy env files
cp .env.example .env

# Run API
cd HQStudio.API
dotnet run

# Run Web (in another terminal)
cd HQStudio.Web
npm install
npm run dev

# Run Desktop (Windows)
cd HQStudio.Desktop
dotnet run
```

### Docker (development with hot-reload)

```bash
docker-compose -f docker-compose.dev.yml up --build
```

### Docker (production)

```bash
docker-compose up --build -d
```

## Tests

```bash
# API tests
dotnet test HQStudio.API.Tests

# Web tests
cd HQStudio.Web && npm test

# Desktop tests
dotnet test HQStudio.Desktop.Tests
```

## CI/CD

The project uses a fully automated CI/CD pipeline:

| Workflow | Purpose |
|----------|---------|
| **CI** | API, Web, Desktop tests + Codecov |
| **Release** | Semantic versioning, CHANGELOG, Docker images |
| **Pages** | Deploy Web to GitHub Pages |
| **CodeQL** | Security analysis |
| **Dependabot** | Auto-update dependencies |

### Conventional Commits

All commits must follow the format:
```
feat(api): add new feature
fix(web): fix bug
docs: update documentation
```

More details: [CONTRIBUTING.md](CONTRIBUTING.md) | [Git & CI/CD](docs/GIT-INTEGRATION.md)

## Technologies

### Backend (API)
- ASP.NET Core 8.0
- Entity Framework Core
- PostgreSQL / SQLite
- JWT Authentication
- Swagger/OpenAPI

### Frontend (Web)
- Next.js 14 (App Router)
- React 18
- TypeScript
- Tailwind CSS
- Framer Motion

### Desktop
- .NET 8.0 WPF
- MVVM Pattern
- Material Design

## Environment Variables

See `.env.example` for the full list of variables.

## License

MIT License — see [LICENSE](LICENSE)

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for information on how to contribute.

## Security

See [SECURITY.md](SECURITY.md) for the security policy.
