# Deployment

CLS Budget runs across three hosted services:

| Layer | Platform | Repo path |
|-------|----------|-----------|
| Frontend | [Vercel](https://vercel.com) | `frontend/cls-budget-web` |
| API | [Azure App Service](https://azure.microsoft.com/products/app-service) | `backend/src/CLS.Budget.Api` |
| Database | [Supabase](https://supabase.com) (PostgreSQL) | EF migrations in `backend/src/CLS.Budget.Migration` |
| Database (AWS) | [Amazon RDS PostgreSQL](https://aws.amazon.com/rds/) | See [AWS-RDS-SETUP.md](AWS-RDS-SETUP.md) |

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

Npgsql requires **key=value** format with SSL. Do **not** paste the `postgresql://` URI from Supabase — EF/Npgsql will reject it with `Format of the initialization string does not conform to specification`.

**Use this format (direct connection, port 5432):**

```
Host=db.<project-ref>.supabase.co;Port=5432;Database=postgres;Username=postgres.<project-ref>;Password=<your-password>;SSL Mode=Require;Trust Server Certificate=true
```

| Supabase shows | Use in GitHub secret? |
|----------------|----------------------|
| `postgresql://postgres.xxx:password@db.xxx.supabase.co:5432/postgres` | **No** — convert to `Host=...` format below |
| Direct connection → .NET / Npgsql | **Yes** — if it starts with `Host=` |

**Example** (replace password and project ref):

```
Host=db.vbycplizeylhcraildxq.supabase.co;Port=5432;Database=postgres;Username=postgres.vbycplizeylhcraildxq;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true
```

When saving the GitHub secret, paste the value **without** surrounding quotes.

### Run migrations locally

From the repo root, use the helper script (recommended):

```powershell
.\scripts\migrate-supabase.ps1 -ConnectionString "<supabase-connection-string>"
```

Or run EF directly from the `backend` directory:

```powershell
cd c:\Repos\cls-budget-app\backend
dotnet ef database update `
  --project src/CLS.Budget.Migration `
  --startup-project src/CLS.Budget.Migration `
  --connection "<supabase-connection-string>"
```

Use `--startup-project src/CLS.Budget.Migration` (not the Api project) and pass the Supabase connection string via `--connection`.

Store the same connection string in Azure App Service (next section). Do **not** commit it to GitHub.

### GitHub Actions migrations

Migrations run automatically via [`.github/workflows/migrate-database.yml`](../.github/workflows/migrate-database.yml) when changes under `backend/src/CLS.Budget.Migration/**` are pushed to `main`. You can also trigger the workflow manually from the **Actions** tab (**Migrate Database** → **Run workflow**).

**Repository secrets (one per target):**

| Secret | When to use |
|--------|-------------|
| `RDS_CONNECTION_STRING` | AWS RDS production (default on push to `main`) |
| `SUPABASE_CONNECTION_STRING` | Supabase (legacy; choose **supabase** when running manually) |

**RDS connection string example** (no quotes around the secret value):

```
Host=cls-budget-db.cm7e8coy6c7r.us-east-1.rds.amazonaws.com;Port=5432;Database=postgres;Username=postgres;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true
```

**Network:** GitHub-hosted runners connect from the public internet. RDS must allow inbound **5432** from the runner (temporary **My IP** rule, a self-hosted runner in your VPC, or public access with a tight SG). After migration, remove any broad `0.0.0.0/0` rule.

**Manual fallback:** `.\scripts\migrate-supabase.ps1 -ConnectionString "<same string>"` (works for RDS or Supabase).

**Deploy order:** Run database migrations **before** deploying the API. If a push changes both migrations and API code, verify the migration workflow succeeded in **Actions** before relying on new schema in production.

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

## 3b. AWS EC2 (API alternative)

Host the ASP.NET Core API on **EC2** with **RDS PostgreSQL** (same database you migrated with `import-rds-sql.ps1`).

| Component | Value |
|-----------|--------|
| RDS endpoint | `cls-budget-db.cm7e8coy6c7r.us-east-1.rds.amazonaws.com` |
| EC2 instance name | `cls-budget-api` |
| API port | `80` → container `8080` |

### Step 1 — Create EC2 host

From the repo root (requires AWS CLI + `budget-admin` credentials):

```powershell
.\scripts\create-ec2-api.ps1
```

This script:

- Creates an SSH key pair at `scripts/keys/cls-budget-api.pem` (gitignored)
- Creates security group `cls-budget-api-sg` (HTTP 80 public, SSH from your IP)
- Adds RDS inbound rule: port **5432** from the API security group
- Launches **Amazon Linux 2023** with Docker pre-installed

Save the **public IP** from the output. Wait ~2 minutes for bootstrap before deploying.

### Step 2 — Print env checklist (optional)

```powershell
.\scripts\setup-ec2-api.ps1 -Ec2PublicIp <PUBLIC_IP>
```

### Step 3 — Deploy API

```powershell
.\scripts\deploy-api-ec2.ps1 `
  -Ec2PublicIp <PUBLIC_IP> `
  -Password 'YOUR_RDS_PASSWORD' `
  -CorsOrigin 'https://your-cloudfront-url.cloudfront.net'
```

The deploy script:

1. `dotnet publish` the API locally
2. Copies files to EC2 via SCP
3. Runs the app in Docker (`mcr.microsoft.com/dotnet/aspnet:9.0`)

**Application settings** (written to EC2 as `cls-budget-api.env`):

| Name | Value |
|------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__BudgetDatabase` | RDS Npgsql string with SSL |
| `Jwt__SigningKey` | Auto-generated (save from deploy output) |
| `Auth__Enabled` | `true` |
| `Cors__AllowedOrigins__0` | CloudFront URL (when frontend is on S3) |

### Step 4 — Verify API

```powershell
curl http://<PUBLIC_IP>/api/v1/budget-payment-statuses
```

A `401 Unauthorized` response means the API is up (auth is required). Check container logs:

```powershell
ssh -i scripts/keys/cls-budget-api.pem ec2-user@<PUBLIC_IP> "docker logs cls-budget-api"
```

### Step 5 — Point frontend at EC2

Set GitHub secret for [`.github/workflows/AWS-DeployReactAppS3.yml`](../.github/workflows/AWS-DeployReactAppS3.yml):

| Secret | Value |
|--------|--------|
| `NEXT_PUBLIC_API_BASE_URL` | `http://<PUBLIC_IP>` (or HTTPS URL after ALB/domain) |

Redeploy the S3 workflow. Ensure `Cors__AllowedOrigins__0` on EC2 matches your CloudFront URL.

### Security notes

- RDS is reachable from EC2 via **security group** (no public IP required on RDS once EC2 is linked).
- You can disable RDS **public access** after EC2 deploy if you no longer need pgAdmin from home.
- For production HTTPS, add an **Application Load Balancer** + ACM certificate, or nginx + Let's Encrypt on EC2.
- SSH key `scripts/keys/cls-budget-api.pem` must stay private.

### Redeploy after code changes

```powershell
.\scripts\deploy-api-ec2.ps1 -Ec2PublicIp <PUBLIC_IP> -Password 'YOUR_RDS_PASSWORD'
```

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

## 4b. AWS S3 + CloudFront (frontend alternative)

Deploy the static Next.js export via GitHub Actions:

- Workflow: [`.github/workflows/AWS-DeployReactAppS3.yml`](../.github/workflows/AWS-DeployReactAppS3.yml)
- Bucket: `cls-budget-app-prod` (us-east-1)
- Trigger: push to `main` (frontend paths) or **Actions → AWS Deploy React to S3 → Run workflow**

**GitHub secrets:**

| Secret | Value |
|--------|--------|
| `NEXT_PUBLIC_API_BASE_URL` | CloudFront URL (proxies `/api/*` to EC2) e.g. `https://d123.cloudfront.net` |
| `CLOUDFRONT_DISTRIBUTION_ID` | Optional — invalidates cache after deploy |

**CloudFront:** set default root object `index.html` and custom error responses (403/404 → `/index.html` with 200) for client-side routes.

**Cursor AWS MCP (local troubleshooting):** see [AWS-MCP.md](AWS-MCP.md) for `uvx` setup and how that differs from this S3 workflow.

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
| `Format of the initialization string does not conform to specification` | GitHub secret is `postgresql://` URI or has extra quotes | Use `Host=...;Port=...` Npgsql format; no quotes around secret value |
| CORS error in browser | Vercel origin not in `Cors__AllowedOrigins` | Add origin in Azure settings |
| API won't start | Missing `Jwt__SigningKey` when auth enabled | Set signing key in App Service |
| DB connection failed | Wrong Supabase string or missing SSL | Use SSL connection string from step 2 |
| Vercel build fails | Wrong root directory | Set root to `frontend/cls-budget-web` |
| Login works locally but not prod | `NEXT_PUBLIC_AUTH_ENABLED` not `true` on Vercel | Set env var and redeploy |
