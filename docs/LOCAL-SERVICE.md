# Always-on local API (Windows Service)

Run the CLS Budget API as a **Windows Service** so it starts automatically when Windows boots and keeps running without an open terminal.

| Item | Value |
|------|-------|
| Service name | `ClsBudgetApi` |
| Default URL | http://localhost:5123 |
| Install path | `C:\Services\ClsBudgetApi` |

The Next.js frontend is **not** included in the Windows Service — start it manually when you want the UI (see [Frontend: `npm run dev`](#frontend-npm-run-dev) below).

---

## Quick start (daily use)

| Step | Where | Command |
|------|--------|---------|
| API | Runs automatically (Windows Service) | http://localhost:5123 |
| Frontend | From repo `scripts` folder | `.\start-frontend-dev.ps1` |

Open a **normal** PowerShell or terminal (not Administrator):

```powershell
cd c:\Repos\cls-budget-app\scripts
.\start-frontend-dev.ps1
```

**Double-click (easiest):** run **`Start CLS Budget.bat`** in the repo root (`c:\Repos\cls-budget-app`), or double-click `scripts\start-frontend-dev.bat`. A terminal opens, starts the frontend, and opens your browser.

That script starts `npm run dev` and opens **Google Chrome** to:

- **http://localhost:5123/swagger** (API / backend)
- **http://localhost:3000** (frontend, when Next.js is ready)

Leave the terminal open while you use the app.

**One-liner** (from any folder):

```powershell
& "c:\Repos\cls-budget-app\scripts\start-frontend-dev.ps1"
```

First time only (install dependencies):

```powershell
cd c:\Repos\cls-budget-app\frontend\cls-budget-web
npm install
```

Then use `.\start-frontend-dev.ps1` as above.

## Prerequisites

- Windows 10/11
- [.NET 9 SDK/runtime](https://dotnet.microsoft.com/download)
- PostgreSQL running locally with `ClsBudget` database and migrations applied
- **Administrator** PowerShell for install/uninstall

---

## 1. Configure local settings

Copy the example config and set your Postgres password:

```powershell
cd c:\Repos\cls-budget-app\scripts
copy local-service.appsettings.example.json local-service.appsettings.json
```

Edit `local-service.appsettings.json` — update `Password` in the connection string. This file is gitignored.

---

## 2. Install the service

Open **PowerShell as Administrator**:

```powershell
cd c:\Repos\cls-budget-app\scripts
.\install-local-api-service.ps1
```

This will:

1. Publish the API to `C:\Services\ClsBudgetApi`
2. Copy your `local-service.appsettings.json` as `appsettings.Development.json` in the install folder
3. Register `ClsBudgetApi` Windows Service (automatic start)
4. Start the service

Verify: open http://localhost:5123/swagger

---

## 3. Manage the service

```powershell
# Status
Get-Service ClsBudgetApi

# Stop
Stop-Service ClsBudgetApi

# Start
Start-Service ClsBudgetApi

# Restart after code changes (re-run install script)
.\install-local-api-service.ps1
```

Or use **services.msc** → find **CLS Budget API**.

---

## 4. Uninstall

Administrator PowerShell:

```powershell
cd c:\Repos\cls-budget-app\scripts
.\uninstall-local-api-service.ps1
```

---

## Frontend: `npm run dev`

The API Windows Service does **not** start the website. Use the helper script (recommended) or run `npm run dev` manually.

### Recommended: helper script

From PowerShell (any folder):

```powershell
cd c:\Repos\cls-budget-app\scripts
.\start-frontend-dev.ps1
```

Or as a one-liner:

```powershell
& "c:\Repos\cls-budget-app\scripts\start-frontend-dev.ps1"
```

The script runs `npm run dev` in `c:\Repos\cls-budget-app\frontend\cls-budget-web` and opens:

| Tab | URL |
|-----|-----|
| API (Swagger) | http://localhost:5123/swagger |
| Frontend | http://localhost:3000 (or **3001** if 3000 is busy) |

Ensure the **ClsBudgetApi** Windows Service is running (or `dotnet run` on port 5123) before starting the script.

If PowerShell blocks the script:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
& "c:\Repos\cls-budget-app\scripts\start-frontend-dev.ps1"
```

### Manual alternative

**Directory:** `c:\Repos\cls-budget-app\frontend\cls-budget-web`

```powershell
cd c:\Repos\cls-budget-app\frontend\cls-budget-web
npm run dev
```

### After starting

You should see:

```
▲ Next.js ...
- Local: http://localhost:3000
✓ Ready
```

Open **http://localhost:3000** in your browser (or let `start-frontend-dev.ps1` open it for you).

Stop the frontend with **Ctrl+C** in that terminal. The API keeps running in the background.

### Environment (`.env.local`)

In the same folder (`frontend\cls-budget-web`), ensure `.env.local` contains:

```
NEXT_PUBLIC_API_BASE_URL=http://localhost:5123
NEXT_PUBLIC_AUTH_ENABLED=false
```

Copy from `.env.example` if the file is missing:

```powershell
cd c:\Repos\cls-budget-app\frontend\cls-budget-web
copy .env.example .env.local
```

---

## Troubleshooting

| Issue | Fix |
|-------|-----|
| Budget API returns 404 | **Wrong app on port 5123** — another project (e.g. PropertyRevenue) may be using it. Stop that process; start CLS Budget API with `.\start-api-local.ps1` |
| Budget API returns 500 / DB errors | **User env var** `ConnectionStrings__BudgetDatabase` may point at Supabase (from migration testing). Remove it in Windows **Environment Variables**, or use `.\start-api-local.ps1` which clears it for that session |
| `password authentication failed` | Fix Postgres password in `appsettings.Development.json` or `scripts\local-api.appsettings.json` |
| Service won't start | Check **Event Viewer → Windows Logs → Application** for .NET errors |
| Port 5123 in use | Stop other `dotnet run` instances or change `Urls` in `local-service.appsettings.json` |
| DB connection failed | Fix password in `local-service.appsettings.json`, re-run install script |
| Auth errors | Set `"Auth": { "Enabled": false }` for local service, or set `Jwt:SigningKey` |

---

## Optional: always-on frontend

Running Next.js 24/7 locally requires a production build as a second Windows Service (or Task Scheduler). For most people, **always-on API + `npm run dev` when needed** is enough. Use Vercel for an always-on website.
