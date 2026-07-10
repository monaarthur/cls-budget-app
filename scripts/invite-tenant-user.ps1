param(
    [string]$ApiUrl = "http://localhost:5123",
    [string]$AdminApiKey,
    [switch]$ListTenants,
    [Guid]$TenantId,
    [string]$Email,
    [string]$DisplayName,
    [ValidateSet("Owner", "Member")]
    [string]$Role = "Owner"
)

$ErrorActionPreference = "Stop"

$ApiUrl = $ApiUrl.TrimEnd("/")

if (-not $AdminApiKey) {
    $AdminApiKey = $env:CLS_BUDGET_ADMIN_API_KEY
}
if (-not $AdminApiKey) {
    throw "Admin API key required. Pass -AdminApiKey or set CLS_BUDGET_ADMIN_API_KEY."
}

$headers = @{ "X-Admin-Api-Key" = $AdminApiKey }

function Invoke-AdminApi {
    param(
        [string]$Method,
        [string]$Path,
        [object]$Body
    )

    $params = @{
        Uri = "$ApiUrl$Path"
        Method = $Method
        Headers = $headers
        ContentType = "application/json"
        UseBasicParsing = $true
        TimeoutSec = 30
    }
    if ($Body) {
        $params.Body = ($Body | ConvertTo-Json -Compress)
    }

    try {
        $response = Invoke-WebRequest @params
        return $response.Content | ConvertFrom-Json
    }
    catch {
        if ($_.Exception.Response) {
            $reader = [IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
            $errorBody = $reader.ReadToEnd()
            if ($errorBody) {
                throw ($errorBody | ConvertFrom-Json | ConvertTo-Json -Depth 5)
            }
        }
        throw
    }
}

if ($ListTenants -or (-not $TenantId -and -not $Email)) {
    Write-Host ""
    Write-Host "=== Tenants ===" -ForegroundColor Cyan
    $result = Invoke-AdminApi -Method GET -Path "/api/v1/admin/tenants"
    foreach ($tenant in $result.data) {
        $emails = if ($tenant.userEmails.Count -gt 0) { ($tenant.userEmails -join ", ") } else { "(no users)" }
        Write-Host ("{0}  {1}  users={2}  [{3}]" -f $tenant.tenantId, $tenant.name, $tenant.userCount, $emails)
    }
    Write-Host ""
    Write-Host "Invite a user:" -ForegroundColor DarkGray
    Write-Host "  .\scripts\invite-tenant-user.ps1 -TenantId <guid> -Email you@gmail.com -DisplayName `"Your Name`" -AdminApiKey <key>"
    Write-Host ""
    Write-Host "Production (via CloudFront):" -ForegroundColor DarkGray
    Write-Host "  .\scripts\invite-tenant-user.ps1 -ApiUrl https://d12onqucnncuke.cloudfront.net -ListTenants"
    return
}

if (-not $TenantId) { throw "-TenantId is required when inviting a user." }
if (-not $Email) { throw "-Email is required when inviting a user." }
if (-not $DisplayName) { $DisplayName = ($Email -split "@")[0] }

Write-Host ""
Write-Host "=== Invite tenant user ===" -ForegroundColor Cyan
Write-Host "Tenant: $TenantId"
Write-Host "Email:  $Email"
Write-Host ""

$body = @{
    tenantId = $TenantId
    email = $Email
    displayName = $DisplayName
    role = $Role
}

$result = Invoke-AdminApi -Method POST -Path "/api/v1/admin/tenant-users/invite" -Body $body
Write-Host "User created:" -ForegroundColor Green
$result.data | Format-List

$setupLink = $result.data.setupLink
if ($setupLink) {
    Write-Host ""
    Write-Host "=== Setup link (open in browser to set password) ===" -ForegroundColor Cyan
    Write-Host $setupLink
    Write-Host ""
}

if ($result.data.inviteSent) {
    Write-Host "A set-password email was also sent if SMTP is configured." -ForegroundColor DarkGray
} else {
    Write-Host "No email sent (SMTP not configured). Use the setup link above." -ForegroundColor DarkGray
}
