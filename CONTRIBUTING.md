# Contributing to HQ Studio

A guide for contributing to the HQ Studio project.

## Table of Contents

- [Conventional Commits](#-conventional-commits)
- [Git Hooks](#-git-hooks)
- [How to Make Commits](#-how-to-make-commits)
- [Automatic Release](#-automatic-release)
- [Testing](#-testing)
- [Pull Request](#-pull-request)
- [CI/CD Pipeline](#-cicd-pipeline)
- [Checking CI Status](#-checking-ci-status)

---

## Conventional Commits

We use [Conventional Commits](https://www.conventionalcommits.org/) to standardize commit messages.

### Commit Format

```
<type>(<scope>): <description>

[optional body]

[optional footer(s)]
```

### Commit Types

| Type | Description | Version Impact |
|------|-------------|----------------|
| `feat` | New feature | **minor** (1.x.0) |
| `fix` | Bug fix | **patch** (1.0.x) |
| `perf` | Performance improvement | **patch** |
| `refactor` | Code refactoring | **patch** |
| `docs` | Documentation | No release |
| `style` | Formatting (no code change) | No release |
| `test` | Adding/changing tests | No release |
| `build` | Build/dependencies | No release |
| `ci` | CI/CD configuration | No release |
| `chore` | Miscellaneous changes | No release |
| `revert` | Revert changes | Depends on type |

### Scopes

| Scope | Description |
|-------|-------------|
| `api` | HQStudio.API (ASP.NET Core backend) |
| `web` | HQStudio.Web (Next.js frontend) |
| `desktop` | HQStudio.Desktop (WPF application) |
| `tests` | Tests for any component |
| `docker` | Docker configuration |
| `ci` | CI/CD pipelines |
| `deps` | Dependencies |
| `release` | Automatic releases |

### Examples

```bash
# New feature (creates minor release)
feat(api): add order export endpoint

# Bug fix (creates patch release)
fix(web): fix contact form validation error

# Documentation (no release)
docs: update API documentation

# Refactoring (creates patch release)
refactor(desktop): optimize DataService for caching

# Breaking change (creates major release)
feat(api)!: change API response format

BREAKING CHANGE: `status` field now returns enum instead of string

# Dependencies (no release)
chore(deps): update NuGet dependencies
```

---

## Git Hooks

The project uses [Husky](https://typicode.github.io/husky/) for automatic commit validation.

### Installed Hooks

| Hook | Action |
|------|--------|
| `commit-msg` | Message format validation via Commitlint |

### Commitlint Configuration

The `commitlint.config.js` file defines:
- Allowed commit types
- Recommended scopes
- Formatting rules

### Installing Hooks

```bash
# Automatically on npm install
npm install

# Or manually
npx husky install
```

---

## How to Make Commits

### Option 1: Interactive Mode (recommended)

```bash
npm run commit
```

Commitizen will guide you through creating a proper commit.

### Option 2: Manual

```bash
git commit -m "feat(api): add JWT authentication"
```

### Option 3: VS Code / IDE

Use the standard commit interface, but follow the Conventional Commits format.

---

## Automatic Release

When pushing to `main`, [Semantic Release](https://semantic-release.gitbook.io/) automatically runs:

### Release Process

1. All tests run (API, Web, Desktop)
2. Commits since last release are analyzed
3. New version determined by semver
4. CHANGELOG.md generated/updated
5. Git tag created
6. GitHub Release created
7. Docker images built and pushed to GHCR
8. Desktop application built (ZIP)

### Release Artifacts

- `ghcr.io/randomu3/hqstudio/api:X.Y.Z` — API Docker image
- `ghcr.io/randomu3/hqstudio/web:X.Y.Z` — Web Docker image
- `HQStudio-Desktop-vX.Y.Z.zip` — Windows application

### Local Dry-run

```bash
npm run release:dry
```

---

## Testing

### Before committing, always run tests:

```bash
# API tests (xUnit + FluentAssertions)
dotnet test HQStudio.API.Tests

# Web tests (Vitest)
cd HQStudio.Web && npm test

# Desktop tests (xUnit, without Integration)
dotnet test HQStudio.Desktop.Tests --filter "Category!=Integration"
```

### Tests with Coverage

```bash
# API
dotnet test HQStudio.API.Tests --collect:"XPlat Code Coverage"

# Web
cd HQStudio.Web && npm test -- --coverage
```

### Codecov

Coverage is automatically uploaded to [Codecov](https://codecov.io/gh/randomu3/hqstudio) on every push.

---

## Pull Request

### Process

1. Create a branch from `main`:
   ```bash
   git checkout -b feat/my-feature
   ```

2. Make changes and commits

3. Ensure tests pass locally

4. Create a PR with a description of changes

### PR Checklist

- [ ] Code follows project style (EditorConfig)
- [ ] Tests added/updated
- [ ] Documentation updated (if needed)
- [ ] All tests pass locally
- [ ] Commits follow Conventional Commits
- [ ] PR description filled out per template

### Automatic Checks

When creating a PR, the following automatically run:
- CI tests (API, Web, Desktop)
- CodeQL security analysis
- Lint and type check

---

## CI/CD Pipeline

### Workflows

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| **CI** | Push/PR to main, develop | Tests + Codecov |
| **Release** | Push to main | Semantic Release + Docker + Desktop |
| **Pages** | Push to main | Deploy Web to GitHub Pages |
| **CodeQL** | Push/PR + Weekly | Security analysis |
| **Dependabot Auto-merge** | Dependabot PR | Auto-merge patch/minor |

### Dependabot

Automatically creates PRs for dependency updates:
- **npm** (Web) — weekly
- **NuGet** (API, Desktop) — weekly
- **GitHub Actions** — monthly

Patch and minor updates are automatically merged after CI passes.

---

## Checking CI Status

### After pushing to main, always check status:

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
```

### Expected Result

All workflows should be `success`:
- CI
- Release
- Deploy to GitHub Pages
- CodeQL Security Analysis

### If CI Fails

1. Check logs: `gh run view <run-id> --log-failed`
2. Fix tests locally
3. Make a fix commit and push

---

## Additional Documentation

- [Git Integration & CI/CD](docs/GIT-INTEGRATION.md) — full technical documentation
- [Architecture](docs/ARCHITECTURE.md) — system overview
- [API Documentation](docs/API.md) — REST endpoints
