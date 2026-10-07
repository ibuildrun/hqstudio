# Local installation (single PC)

HQ Studio can be installed on one Windows PC by a non-technical user: the desktop app plus the
site, API and database running in Docker, optionally published to the internet through
[Tuna](https://tuna.am) (a Russian ngrok-like tunnel service).

End-user guide (Russian, plain language): [INSTALL.ru.html](INSTALL.ru.html).

## Architecture

```
Browser / Desktop app
        |
   http://localhost:8080  (127.0.0.1 only)
        |
      proxy (nginx)  --- tuna (optional tunnel, https://<name>.ru.tuna.am)
      /          \
   web:3000     api:5000 --- db (Postgres 16, volume hqstudio-db)
```

- `deploy/docker-compose.yml` is the production stack for a single PC. Images are
  `ghcr.io/ibuildrun/hqstudio/{api,web}:${HQSTUDIO_VERSION}` built by the Release workflow.
- The web image is built with `NEXT_PUBLIC_API_URL=/api`, so the browser always talks to the
  same origin. nginx routes `/api/` to the API and everything else to the site.
- Everything is configured through `deploy/.env` (created by the installer; see `.env.example`).
  Secrets (database password, JWT key) are generated locally and never change on re-install.
- The first admin account is created from `ADMIN_PASSWORD` / `ADMIN_NAME` (`Seed__AdminPassword`,
  `Seed__AdminName` in the API) on the first start only; the installer blanks the password in
  `.env` afterwards.
- The Tuna container only starts when `TUNA_TOKEN` is set (compose profile `tunnel`). The public
  URL is read from the container log (`Forwarding https://... ->`).

## Installed layout (per user, no admin rights)

| Path | Content |
| --- | --- |
| `%LOCALAPPDATA%\Programs\HQ Studio` | `HQStudio.exe`, user guide |
| `%LOCALAPPDATA%\HQStudio\server` | compose file, nginx config, `.env`, `public-url.txt` |
| `%LOCALAPPDATA%\HQStudio\install.json` | `serverDir`, `webUrl`, `apiUrl`, `repo` (read by the app) |
| `%APPDATA%\HQStudio\settings.json` | app settings (`ApiUrl` points to the local site) |

## Components

| Project | Purpose |
| --- | --- |
| `HQStudio.Setup` | GUI installer (single-file exe, payload embedded): Docker check/auto-install, main account, optional keys, install with live stages, uninstall entry |
| `HQStudio.Desktop` page "Сайт" | status of every service, start/stop/restart, keys, logs, uninstall |
| `HQStudio.Desktop` updates | one-click "Обновить приложение / сайт / всё" from the latest GitHub release |
| `HQStudio.Desktop` bug report | creates a GitHub issue (device flow sign-in, or prefilled browser fallback) |
| `.github/workflows/issue-triage.yml` | labels issues created by the app (`from-app`, `needs-triage`) |

## Releases

Conventional commits on `main` trigger `release.yml` (semantic-release). For every release it:

1. pushes `ghcr.io/ibuildrun/hqstudio/api` and `.../web` images tagged with the version and `latest`;
2. builds the desktop app and the installer with `installer/build.ps1`;
3. uploads to the GitHub Release: `HQStudio-Setup-<ver>.exe`, `HQStudio-Desktop-v<ver>.zip` (used by
   the in-app updater), `HQStudio-Server-v<ver>.zip` (compose/nginx files used by the in-app updater).

Build the installer locally: `pwsh installer/build.ps1 -Version 1.2.3` (needs the .NET 8 SDK).
The installer embeds the version, so it pulls images with the same tag: build it only for a
version whose images exist.

## One-time owner setup

1. **GHCR visibility.** New container packages are private. After the first release open
   `https://github.com/users/ibuildrun/packages/container/hqstudio%2Fapi/settings` and
   `.../hqstudio%2Fweb/settings`, section "Danger Zone", set visibility to **Public**. Without this the
   installer and the in-app updater cannot pull images.
2. **Automatic bug reports (optional).** Create an OAuth App at
   <https://github.com/settings/developers> ("New OAuth App", any homepage/callback URL), tick
   **Enable Device Flow**, and put the Client ID into `BugReportConfig.DefaultClientId`
   (`HQStudio.Desktop/Services/BugReport/BugReportConfig.cs`) or the `HQSTUDIO_GITHUB_CLIENT_ID`
   environment variable. Without it the app opens a prefilled "new issue" page in the browser, where
   GitHub asks the user to sign in or register.

## Testing without Docker or a real PC

- `HQStudio-Setup.exe --simulate` runs the whole installer UI against a fake backend;
  `--simulate-fail=<stage>` forces a failure to check the retry flow.
- Unit tests: `dotnet test HQStudio.Setup.Tests`, `dotnet test HQStudio.Desktop.Tests`.

## Public address options (Tuna)

| Option | `.env` | Plan |
| --- | --- | --- |
| Temporary address (changes on restart) | `TUNA_TOKEN` only | free |
| The permanent free subdomain Tuna assigns to the account (random name like `brave-otter-4821`, shown at <https://my.tuna.am/domains>) | `TUNA_TOKEN` + `TUNA_SUBDOMAIN` | free |
| Any chosen subdomain name | `TUNA_TOKEN` + `TUNA_SUBDOMAIN` | paid |
| Own domain (CNAME/A record at the registrar, verified in the Tuna cabinet) | `TUNA_TOKEN` + `TUNA_DOMAIN` (`TUNA_SUBDOMAIN` is left empty) | paid |

The end-user guide ([INSTALL.ru.html](INSTALL.ru.html)) walks through buying a domain at reg.ru and the
DNS record to create there and in Tuna.
