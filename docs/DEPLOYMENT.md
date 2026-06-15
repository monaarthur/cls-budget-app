# Deployment

CLS Budget runs across three hosted services:

| Layer | Platform | Repo path |
|-------|----------|-----------|
| Frontend | [Vercel](https://vercel.com) | `frontend/cls-budget-web` |
| API | [Azure App Service](https://azure.microsoft.com/products/app-service) | `backend/src/CLS.Budget.Api` |
| Database | [Supabase](https://supabase.com) (PostgreSQL) | EF migrations in `backend/src/CLS.Budget.Migration` |

Secrets live in each platform's dashboard — never commit passwords, JWT keys, or `.env.local` files.

---

## 1. GitHub

Repository: `https://github.com/monaarthur/cls-budget-app`

Default branch should be `main` with your full project history. If GitHub `main` only contains an auto-generated README, overwrite it:

```powershell
cd c:\Repos\cls-budget-app
git push -u origin main --force
```

---

## 2. Supabase (database)

### Create project

1. Sign up at [supabase.com](https://supabase.com) and create a project (free tier).
2. Go to **Project Settings → Database**.
3. Copy the **direct connection** string (not the pooler for initial migrations).

### Connection string format

Npgsql requires SSL for Supabase:

```
Host=db.<project-ref>.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=<your-password>;SSL Mode=Require;Trust Server Certificate=true
```

### Run migrations

From PowerShell:

```powershell
cd c:\Repos\cls-budget-app\backend
$env:ConnectionStrings__BudgetDatabase = "<supabase-connection-string>"
dotnet ef database update --project src/CLS.Budget.Migration --startup-project src/CLS.Budget.Api
```

Or use the helper script:

```powershell
.\scripts\migrate-supabase.ps1 -ConnectionString "<supabase-connection-string>"
```

Store the same connection string in Azure App Service (next section). Do **not** commit it to GitHub.

---

## 3. Azure App Service (API)

### Create the app

1. Azure Portal → **Create a resource → Web App**.
2. Runtime: **Linux**, stack **.NET 9**.
3. Choose a region and pricing tier (Free F1 works for testing).

### Application settings

In **Configuration → Application settings**, add:

| Name | Value | Notes |
|------|-------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Required |
| `ConnectionStrings__BudgetDatabase` | Supabase connection string | From step 2 |
| `Jwt__SigningKey` | 32+ character random string | Generate with `[guid]::NewGuid().ToString("N") + [guid]::NewGuid().ToString("N")` |
| `Auth__Enabled` | `true` | Required in production |
| `Cors__AllowedOrigins__0` | `https://your-app.vercel.app` | Your Vercel URL (add more `__1`, `__2` as needed) |

### Deploy

**Option A — Visual Studio / VS Code:** Use the Azure extension to publish `CLS.Budget.Api` to the App Service.

**Option B — GitHub Actions:** Configure repository secrets (see `.github/workflows/deploy-api.yml`):

| Secret | Description |
|--------|-------------|
| `AZURE_WEBAPP_NAME` | App Service name |
| `AZURE_WEBAPP_PUBLISH_PROFILE` | Download from App Service → **Get publish profile** |

Push to `main` to trigger the workflow.

### Verify API

Open `https://<your-app>.azurewebsites.net/swagger` (Swagger is disabled in Production by default). Test with:

```powershell
curl https://<your-app>.azurewebsites.net/api/v1/budget-payment-statuses
```

(Requires a valid JWT when `Auth__Enabled` is `true`.)

---

## 4. Vercel (frontend)

### Import project

1. [vercel.com](https://vercel.com) → **Add New Project**.
2. Import `monaarthur/cls-budget-app` from GitHub.
3. Set **Root Directory** to `frontend/cls-budget-web`.
4. Framework preset: **Next.js** (auto-detected).

### Environment variables

In **Project Settings → Environment Variables**:

| Variable | Production value |
|----------|------------------|
| `NEXT_PUBLIC_API_BASE_URL` | `https://<your-app>.azurewebsites.net` |
| `NEXT_PUBLIC_AUTH_ENABLED` | `true` |

Redeploy after setting variables.

### Update API CORS

Add your Vercel URL to Azure `Cors__AllowedOrigins__0` (see step 3). Restart the App Service if needed.

---

## 5. End-to-end verification

1. Open your Vercel URL.
2. Register a new account (or log in).
3. Confirm budgets, accounts, and payments load without CORS errors in the browser console.
4. Confirm API requests return tenant-scoped data.

---

## Environment variable reference

### Frontend (Vercel / `.env.local`)

| Variable | Local dev | Production |
|----------|-----------|------------|
| `NEXT_PUBLIC_API_BASE_URL` | `http://localhost:5123` | Azure App Service URL |
| `NEXT_PUBLIC_AUTH_ENABLED` | `false` | `true` |

### Backend (Azure App Service / user secrets)

| Variable | Local dev | Production |
|----------|-----------|------------|
| `ConnectionStrings__BudgetDatabase` | Local Postgres | Supabase connection string |
| `Jwt__SigningKey` | Any dev string | Strong random secret |
| `Auth__Enabled` | `false` (typical) | `true` |
| `Cors__AllowedOrigins__0` | (not needed — dev CORS uses localhost) | Vercel URL |

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---------|--------------|-----|
| CORS error in browser | Vercel origin not in `Cors__AllowedOrigins` | Add origin in Azure settings |
| API won't start | Missing `Jwt__SigningKey` when auth enabled | Set signing key in App Service |
| DB connection failed | Wrong Supabase string or missing SSL | Use SSL connection string from step 2 |
| Vercel build fails | Wrong root directory | Set root to `frontend/cls-budget-web` |
| Login works locally but not prod | `NEXT_PUBLIC_AUTH_ENABLED` not `true` on Vercel | Set env var and redeploy |
