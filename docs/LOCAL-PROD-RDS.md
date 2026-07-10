# Local app against production RDS

Use this when production `/admin/` is blocked but you need to create a login or push local data to RDS.

## Prerequisites

- AWS CLI configured
- RDS master password
- Prod **Admin API key** (from last `deploy-api-ec2.ps1` output), or any secret in `local-api-rds.appsettings.json` for local invites only

## 1. Allow your laptop to reach RDS

```powershell
.\scripts\allow-rds-dev-ip.ps1
.\scripts\test-rds-connection.ps1 `
  -RdsConnectionString "Host=cls-budget-db.cm7e8coy6c7r.us-east-1.rds.amazonaws.com;Port=5432;Database=postgres;Username=postgres;SSL Mode=Require;Trust Server Certificate=true" `
  -Password $env:RDS_MASTER_PASSWORD
```

Remove the rule from the RDS security group when finished.

## 2. Configure local API for RDS

```powershell
copy scripts\local-api-rds.appsettings.example.json scripts\local-api-rds.appsettings.json
```

Edit `scripts\local-api-rds.appsettings.json`:

- `ConnectionStrings:BudgetDatabase` — RDS password
- `PasswordReset:FrontendBaseUrl` — `https://d12onqucnncuke.cloudfront.net` (setup links for prod login)
- `Admin:ApiKey` — prod admin key or your own secret for local invites

## 3. Run locally against prod data

Terminal 1:

```powershell
.\scripts\start-api-local-rds.ps1
```

Terminal 2:

```powershell
.\scripts\start-frontend-local.ps1
```

Open http://localhost:3000 — auth is off; you see **MonaArthur** tenant data from RDS.

## 4. Create your prod login (no CloudFront /admin needed)

With `start-api-local-rds.ps1` running:

```powershell
.\scripts\bootstrap-prod-user.ps1 `
  -Email "you@gmail.com" `
  -DisplayName "Mona" `
  -RdsPassword $env:RDS_MASTER_PASSWORD `
  -AllowDevIp
```

Copy the **setup link**, set your password, then sign in at:

https://d12onqucnncuke.cloudfront.net/login/

## 5. Push local DB changes to RDS (optional)

If you changed data locally and need RDS to match:

```powershell
.\scripts\sync-local-db-to-rds.ps1 -RdsPassword $env:RDS_MASTER_PASSWORD -AllowDevIp
```

This exports `local-api.appsettings.json` database and imports into RDS. **Overwrites RDS data.**

## Apply schema migrations only

```powershell
$conn = "Host=cls-budget-db.cm7e8coy6c7r.us-east-1.rds.amazonaws.com;Port=5432;Database=postgres;Username=postgres;Password=$env:RDS_MASTER_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
.\scripts\migrate-supabase.ps1 -ConnectionString $conn
```
