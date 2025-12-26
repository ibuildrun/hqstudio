# Git Integration & CI/CD

Complete documentation on Git integration, automation, and CI/CD pipelines for the HQ Studio project.

## Table of Contents

- [Infrastructure Overview](#infrastructure-overview)
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

## Infrastructure Overview

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

### Key Components

| Component | File | Purpose |
|-----------|------|---------|
| Commitlint | `commitlint.config.js` | Commit message validation |
| Husky | `.husky/commit-msg` | Git hooks for commit checking |
| Semantic Release | `.releaserc.json` | Automatic versioning |
| Dependabot | `.github/dependabot.yml` | Auto-update dependencies |
| Codecov | `codecov.yml` | Code coverage tracking |
| EditorConfig | `.editorconfig` | Unified code style |

---

## Conventional Commits

The project uses [Conventional Commits](https://www.conventionalcommits.org/) to standardize commit messages.

### Format

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
| `style` | Formatting | No release |
| `test` | Tests | No release |
| `build` | Build/dependencies | No release |
| `ci` | CI/CD configuration | No release |
| `chore` | Miscellaneous changes | No release |
| `revert` | Revert changes | Depends on type |

### Scopes

| Scope | Description |
|-------|-------------|
| `api` | HQStudio.API (ASP.NET Core) |
| `web` | HQStudio.Web (Next.js) |
| `desktop` | HQStudio.Desktop (WPF) |
| `tests` | Tests for any component |
| `docker` | Docker configuration |
| `ci` | CI/CD pipelines |
| `deps` | Dependencies |
| `release` | Automatic releases |

### Commit Examples

```bash
# New feature (minor release)
feat(api): add order export endpoint

# Bug fix (patch release)
fix(web): fix contact form validation error

# Documentation (no release)
docs: update API documentation

# Refactoring (patch release)
refactor(desktop): optimize DataService for caching

# Breaking change (major release)
feat(api)!: change API response format

BREAKING CHANGE: `status` field now returns enum instead of string

# Dependencies (no release)
chore(deps): update NuGet dependencies
```

### Interactive Commit

```bash
# Run Commitizen for interactive commit creation
npm run commit
```

---

## Git Hooks (Husky)

### Structure

```
.husky/
├── _/                  # Husky internals
└── commit-msg          # Commit message validation
```

### commit-msg hook

File `.husky/commit-msg`:
```bash
npx --no -- commitlint --edit $1
```

This hook automatically checks each commit for Conventional Commits compliance.

### Commitlint Configuration

File `commitlint.config.js`:
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
    'subject-case': [0],        // Any case allowed
    'body-max-line-length': [0] // No body length limit
  }
};
```

### Installing Hooks

```bash
# Automatically on npm install (via prepare script)
npm install

# Or manually
npx husky install
```

---

## GitHub Actions Workflows

### 1. CI Workflow (`ci.yml`)

**Triggers:** Push/PR to `main`, `develop`

```yaml
jobs:
  api-test:      # Ubuntu, .NET 8.0
  web-test:      # Ubuntu, Node 20
  desktop-build: # Windows, .NET 8.0
  docker-build:  # Ubuntu (after tests)
```

**API Tests Steps:**
1. Checkout code
2. Setup .NET 8.0
3. Restore dependencies
4. Build project
5. Run tests with coverage
6. Upload coverage to Codecov (flag: `api`)
7. Upload test results artifact

**Web Tests Steps:**
1. Checkout code
2. Setup Node.js 20
3. npm ci (with caching)
4. ESLint check
5. TypeScript type check
6. Vitest with coverage
7. Upload coverage to Codecov (flag: `web`)

**Desktop Build Steps:**
1. Checkout code
2. Setup .NET 8.0
3. Restore dependencies
4. Build Release
5. Run unit tests (without Integration)

**Docker Build Steps:**
1. Build API image
2. Build Web image

### 2. Release Workflow (`release.yml`)

**Triggers:** Push to `main`, manual dispatch

```yaml
jobs:
  test:     # Run all tests
  release:  # Semantic Release
  docker:   # Push images to GHCR
  desktop:  # Build and upload ZIP
```

**Semantic Release Steps:**
1. Analyze commits since last release
2. Determine new version (semver)
3. Generate CHANGELOG.md
4. Create Git tag
5. Create GitHub Release
6. Push changes to repository

**Docker Steps (if new release):**
1. Login to GitHub Container Registry
2. Build and push API image with tags:
   - `ghcr.io/randomu3/hqstudio/api:X.Y.Z`
   - `ghcr.io/randomu3/hqstudio/api:latest`
3. Build and push Web image similarly

**Desktop Steps (if new release):**
1. Update version in .csproj
2. Publish self-contained single-file exe
3. Create ZIP archive
4. Upload to GitHub Release

### 3. Pages Workflow (`pages.yml`)

**Triggers:** Push to `main`, manual dispatch

```yaml
jobs:
  build:   # Next.js static export
  deploy:  # GitHub Pages deployment
```

**Steps:**
1. Checkout code
2. Setup Node.js 20
3. Configure Pages for Next.js
4. npm ci
5. `npm run build` (static export to `out/`)
6. Upload artifact
7. Deploy to GitHub Pages

**URL:** https://randomu3.github.io/hqstudio/

### 4. CodeQL Workflow (`codeql.yml`)

**Triggers:** Push/PR to `main`, Weekly (Monday 6:00 UTC)

```yaml
strategy:
  matrix:
    language: ['csharp', 'javascript-typescript']
```

**Steps:**
1. Checkout code
2. Initialize CodeQL with `security-extended` queries
3. Build .NET projects (for C#)
4. Perform CodeQL Analysis
5. Upload results to Security tab

### 5. Dependabot Auto-merge (`dependabot-automerge.yml`)

**Triggers:** PR from dependabot[bot]

**Logic:**
- Patch/Minor updates → Auto-merge (squash)
- GitHub Actions updates → Auto-merge (squash)
- Major updates → Require manual review

---

## Semantic Release

### Configuration (`.releaserc.json`)

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

| Commit Type | Release |
|-------------|---------|
| `feat` | minor |
| `fix` | patch |
| `perf` | patch |
| `refactor` | patch |
| `docs`, `style`, `chore`, `test`, `build`, `ci` | No release |

### CHANGELOG Sections

| Type | CHANGELOG Section |
|------|-------------------|
| `feat` | Features |
| `fix` | Bug Fixes |
| `perf` | Performance |
| `refactor` | Refactoring |

### Local Dry-run

```bash
npm run release:dry
```

---

## Dependabot

### Configuration (`.github/dependabot.yml`)

| Ecosystem | Directory | Schedule | Limit |
|-----------|-----------|----------|-------|
| npm | `/HQStudio.Web` | Weekly (Mon) | 5 PRs |
| npm | `/` (root) | Monthly | 3 PRs |
| nuget | `/HQStudio.API` | Weekly (Mon) | 5 PRs |
| nuget | `/HQStudio.Desktop` | Weekly (Mon) | 5 PRs |
| github-actions | `/` | Monthly | - |

### Ignored Major Updates

- `next` (Next.js)
- `eslint`, `eslint-config-next`
- `vitest`, `@vitest/*`

### Labels

| Label | Description |
|-------|-------------|
| `dependencies` | All Dependabot PRs |
| `web` | Web NPM dependencies |
| `api` | API NuGet dependencies |
| `desktop` | Desktop NuGet dependencies |
| `ci` | GitHub Actions and root npm |

### Commit Prefix

All Dependabot commits use prefix `chore(deps)`:
```
chore(deps): bump framer-motion from 11.0.0 to 11.1.0
```

---

## Codecov

### Configuration (`codecov.yml`)

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

| Flag | Coverage | Source |
|------|----------|--------|
| `api` | HQStudio.API | xUnit + coverlet |
| `web` | HQStudio.Web/lib | Vitest + v8 |

### Badge

```markdown
[![codecov](https://codecov.io/gh/randomu3/hqstudio/graph/badge.svg)](https://codecov.io/gh/randomu3/hqstudio)
```

### Local Run with Coverage

```bash
# API
dotnet test HQStudio.API.Tests --collect:"XPlat Code Coverage"

# Web
cd HQStudio.Web && npm test -- --coverage
```

---

## Issue & PR Templates

### Bug Report (`.github/ISSUE_TEMPLATE/bug_report.md`)

Fields:
- Bug description
- Steps to reproduce
- Expected behavior
- Screenshots
- Environment (component, version, OS, browser)

### Feature Request (`.github/ISSUE_TEMPLATE/feature_request.md`)

Fields:
- Problem
- Proposed solution
- Alternatives
- Component (Web/Desktop/API/Infrastructure)

### Pull Request (`.github/pull_request_template.md`)

Checklist:
- [ ] Change type (fix/feat/docs/refactor/test/chore)
- [ ] Related Issues
- [ ] Code follows style
- [ ] Tests added/updated
- [ ] Documentation updated
- [ ] All tests pass locally
- [ ] Commits follow Conventional Commits

---

## EditorConfig

### Configuration (`.editorconfig`)

| Files | Indent | Notes |
|-------|--------|-------|
| `*.cs` | 4 spaces | .NET naming conventions |
| `*.{ts,tsx,js,jsx}` | 2 spaces | - |
| `*.json` | 2 spaces | - |
| `*.{yml,yaml}` | 2 spaces | - |
| `*.{xml,xaml,csproj}` | 2 spaces | - |
| `*.md` | 2 spaces | Preserve trailing whitespace |
| `Makefile` | tabs | - |
| `*.sh` | 2 spaces | LF line endings |
| `*.{cmd,bat}` | 2 spaces | CRLF line endings |

### General Settings

- Charset: UTF-8
- Line endings: LF (except Windows batch)
- Final newline: Yes
- Trim trailing whitespace: Yes (except Markdown)

---

## Kiro AI Integration

### Steering Files Structure

```
.kiro/
└── steering/
    ├── conventions.md   # Coding conventions and Git rules
    ├── product.md       # Product description
    ├── structure.md     # Project structure
    └── tech.md          # Technology stack and CI/CD
```

### Automatic Inclusion

All steering files are automatically included in Kiro's context when working with the project.

### Key Rules for Kiro

1. **Conventional Commits** — strict format compliance
2. **Check CI after push** — mandatory workflow status check
3. **Local tests** — run tests before push

### CI Status Check Command

```powershell
Invoke-RestMethod -Uri "https://api.github.com/repos/randomu3/hqstudio/actions/runs?per_page=5" `
  -Headers @{Accept="application/vnd.github.v3+json"} | `
  Select-Object -ExpandProperty workflow_runs | `
  ForEach-Object { "$($_.name) | $($_.status) | $($_.conclusion)" }
```

---

## Quick Commands

### Local Development

```bash
# Run tests before commit
dotnet test HQStudio.API.Tests
npm test --prefix HQStudio.Web
dotnet test HQStudio.Desktop.Tests --filter "Category!=Integration"

# Interactive commit
npm run commit

# Release dry-run
npm run release:dry
```

### Status Check

```bash
# Status of recent workflow runs
gh run list --limit 5

# Details of specific run
gh run view <run-id>

# Failed job logs
gh run view <run-id> --log-failed
```

### Docker

```bash
# Local build
docker build -t hqstudio-api:local ./HQStudio.API
docker build -t hqstudio-web:local ./HQStudio.Web

# Pull from GHCR
docker pull ghcr.io/randomu3/hqstudio/api:latest
docker pull ghcr.io/randomu3/hqstudio/web:latest
```
