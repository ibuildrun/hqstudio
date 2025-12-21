# Project Structure

## Repository Layout
```
/
├── HQStudio.Web/           # Next.js web application
├── HQStudio.Desktop/       # WPF desktop application
├── HQStudio.Desktop.Tests/ # Desktop unit tests
├── HQStudio.API/           # ASP.NET Core API (shared backend)
├── HQStudio.API.Tests/     # API integration tests
├── docs/                   # Documentation (API.md, ARCHITECTURE.md)
├── .github/
│   ├── workflows/          # CI/CD pipelines
│   ├── ISSUE_TEMPLATE/     # Bug/feature templates
│   ├── dependabot.yml      # Auto-update dependencies
│   └── pull_request_template.md
├── .kiro/steering/         # AI assistant context
├── CHANGELOG.md            # Auto-generated changelog
├── CONTRIBUTING.md         # Contribution guide
├── SECURITY.md             # Security policy
├── LICENSE                 # MIT License
└── .editorconfig           # Code style settings
```

---

## Web Application (HQStudio site)

```
HQStudio site/
├── app/                    # Next.js App Router
│   ├── layout.tsx          # Root layout with SEO metadata
│   ├── page.tsx            # Server-rendered entry point
│   ├── ClientPage.tsx      # Client-side page component
│   ├── globals.css         # Global styles
│   ├── sitemap.ts          # Dynamic sitemap generation
│   └── error.tsx           # Error boundaries
├── components/             # React components (flat structure)
│   ├── Admin/              # Admin panel components
│   │   ├── AdminPanel.tsx
│   │   └── SectionPreview.tsx
│   ├── Hero.tsx            # Landing sections
│   ├── Services.tsx
│   ├── Contact.tsx
│   └── ...
├── lib/                    # Utilities and shared code
│   ├── constants.ts        # App constants and default data
│   ├── store.tsx           # React Context state management
│   └── types.ts            # TypeScript type definitions
├── public/                 # Static assets
├── scripts/                # Deployment scripts (tuna tunneling)
└── tuna/                   # Tuna CLI configuration
```

### Component Conventions
- One component per file
- `'use client'` directive for interactive components
- Components use `useAdmin()` hook for shared state
- Framer Motion for animations

---

## Desktop Application (HQStudio exe)

```
HQStudio exe/
├── App.xaml(.cs)           # Application entry point
├── Views/                  # XAML views
│   ├── MainWindow.xaml     # Main application window
│   ├── LoginWindow.xaml    # Authentication
│   ├── DashboardView.xaml  # Analytics dashboard
│   ├── ClientsView.xaml    # Client management
│   ├── OrdersView.xaml     # Order tracking
│   ├── ServicesView.xaml   # Service catalog
│   ├── StaffView.xaml      # Employee management
│   ├── SettingsView.xaml   # App settings
│   └── Dialogs/            # Modal dialogs
├── ViewModels/             # MVVM ViewModels
│   ├── BaseViewModel.cs    # INotifyPropertyChanged base + RelayCommand
│   ├── MainViewModel.cs
│   └── [Feature]ViewModel.cs
├── Models/                 # Data models
│   ├── Client.cs
│   ├── Order.cs
│   ├── Service.cs
│   └── User.cs
├── Services/               # Business logic services
│   ├── DataService.cs      # Data persistence
│   ├── SettingsService.cs  # App configuration
│   └── ThemeService.cs     # UI theming
├── Converters/             # XAML value converters
└── Styles/                 # XAML resource dictionaries
```

### MVVM Conventions
- Views bind to corresponding ViewModels
- `BaseViewModel` provides `SetProperty<T>` and `RelayCommand`
- Services injected into ViewModels


---

## Backend API (HQStudio.API)

```
HQStudio.API/
├── Controllers/            # REST API endpoints
│   ├── AuthController.cs       # JWT authentication
│   ├── ClientsController.cs    # Client management
│   ├── OrdersController.cs     # Order tracking
│   ├── ServicesController.cs   # Service catalog
│   ├── CallbacksController.cs  # Callback requests
│   ├── SubscriptionsController.cs  # Newsletter
│   ├── SiteContentController.cs    # CMS content
│   ├── UsersController.cs      # User management
│   └── DashboardController.cs  # Analytics
├── Models/                 # Entity models
├── DTOs/                   # Data transfer objects
├── Data/
│   ├── AppDbContext.cs     # EF Core DbContext
│   └── DbSeeder.cs         # Initial data seeding
├── Services/
│   └── JwtService.cs       # JWT token generation
├── appsettings.json        # Configuration
└── Program.cs              # App entry point
```

### API Conventions
- RESTful endpoints under `/api/`
- JWT Bearer authentication
- Role-based authorization (Admin, Editor, Manager)
- SQLite database (hqstudio.db)


---

## Tests (HQStudio.API.Tests)

```
HQStudio.API.Tests/
├── TestWebApplicationFactory.cs  # Test server setup with InMemory DB
├── AuthControllerTests.cs        # Authentication tests
├── ServicesControllerTests.cs    # Services CRUD tests
├── CallbacksControllerTests.cs   # Callback requests tests
├── SubscriptionsControllerTests.cs # Newsletter tests
├── SiteContentControllerTests.cs # CMS content tests
└── DashboardControllerTests.cs   # Dashboard stats tests
```

### Testing Conventions
- xUnit test framework
- FluentAssertions for assertions
- InMemory database for isolation
- WebApplicationFactory for integration tests
