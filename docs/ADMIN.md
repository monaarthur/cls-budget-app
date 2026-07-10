# Admin API

Platform-admin endpoints and UI for associating users with existing tenants (e.g. migrated households that have budget data but no login yet).

Protected by a shared secret in `Admin:ApiKey` (env: `Admin__ApiKey`), sent as the `X-Admin-Api-Key` request header.

## Admin UI

Open **`/admin`** in the frontend (e.g. `http://localhost:3000/admin`).

CloudFront must rewrite `/admin/` to `admin/index.html` on S3 (not root `index.html`). Run once after creating the distribution:

```powershell
.\scripts\update-cloudfront-spa-index.ps1
```

1. Sign in with your `Admin:ApiKey`.
2. Review the tenant list.
3. Select a tenant, enter email + display name, and send an invite.

The invited user receives an email with a **Create your account** link (`/reset-password?token=...&invite=1`), sets a password, then signs in at `/login`.

## Production (AWS)

CloudFront already proxies **`/api/*`** to EC2 (includes `/api/v1/admin/*`). The admin **UI** is at **`/admin/`** on the static site.

After deploy, ensure:

1. EC2 env includes `Admin__ApiKey` and `Cors__AllowedOrigins__0` (CloudFront URL).
2. Run `.\scripts\update-cloudfront-api.ps1` if admin API calls fail from the browser.
3. Redeploy API after backend admin/CORS fixes.

```powershell
.\scripts\deploy-api-ec2.ps1 `
  -Ec2PublicIp 100.59.50.231 `
  -Password $env:RDS_MASTER_PASSWORD `
  -CorsOrigin "https://d12onqucnncuke.cloudfront.net"
```

Open **https://d12onqucnncuke.cloudfront.net/admin/** and sign in with the Admin key from deploy output.

## Configure

Add to `appsettings.Development.json`:

```json
"Admin": {
  "ApiKey": "choose-a-long-random-secret"
}
```

Or set `CLS_BUDGET_ADMIN_API_KEY` when using the helper script.

## List tenants

```powershell
.\scripts\invite-tenant-user.ps1 -ListTenants -AdminApiKey "your-key"
```

Returns each tenant id, name, and existing user emails.

## Invite a user to a tenant

Creates an `AppUser` linked to the tenant and emails a set-password link (same flow as forgot-password).

```powershell
.\scripts\invite-tenant-user.ps1 `
  -TenantId "00000000-0000-0000-0000-000000000001" `
  -Email "you@gmail.com" `
  -DisplayName "Mona" `
  -Role Owner `
  -AdminApiKey "your-key"
```

The invited user opens the email link, sets a password at `/reset-password`, then signs in at `/login`.

## HTTP API

| Method | Path | Body |
|--------|------|------|
| GET | `/api/v1/admin/tenants` | — |
| POST | `/api/v1/admin/tenant-users/invite` | `{ "tenantId", "email", "displayName", "role": "Owner" \| "Member" }` |

Header on every request: `X-Admin-Api-Key: <Admin:ApiKey>`

## Production

Set a strong `Admin__ApiKey` on EC2 (deploy env file) and keep it out of git. Use the same invite script against your API URL, or call the endpoints from your own tooling.
