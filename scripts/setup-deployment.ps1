param(
    [string]$SupabaseConnectionString,
    [string]$AzureApiUrl,
    [string]$VercelUrl,
    [switch]$SkipMigrations,
    [switch]$DeployVercel
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

function New-JwtSigningKey {
    return ([guid]::NewGuid().ToString("N") + [guid]::NewGuid().ToString("N"))
}

Write-Host ""
Write-Host "=== CLS Budget deployment setup ===" -ForegroundColor Cyan
Write-Host ""

if (-not $SupabaseConnectionString) {
    $SupabaseConnectionString = Read-Host "Paste Supabase connection string (or press Enter to skip)"
}

if ($SupabaseConnectionString -and -not $SkipMigrations) {
    Write-Host ""
    Write-Host "Running EF migrations against Supabase..." -ForegroundColor Yellow
    & (Join-Path $PSScriptRoot "migrate-supabase.ps1") -ConnectionString $SupabaseConnectionString
}

$jwtKey = New-JwtSigningKey

if (-not $AzureApiUrl) {
    $AzureApiUrl = Read-Host "Azure API URL (e.g. https://cls-budget-api.azurewebsites.net, or Enter to skip)"
}

if (-not $VercelUrl) {
    $VercelUrl = Read-Host "Vercel frontend URL (e.g. https://cls-budget-app.vercel.app, or Enter to skip)"
}

Write-Host ""
Write-Host "=== Azure App Service settings (copy into Configuration) ===" -ForegroundColor Green
Write-Host ""
Write-Host "ASPNETCORE_ENVIRONMENT=Production"
Write-Host "Auth__Enabled=true"
Write-Host "Jwt__SigningKey=$jwtKey"
if ($SupabaseConnectionString) {
    Write-Host "ConnectionStrings__BudgetDatabase=$SupabaseConnectionString"
}
if ($VercelUrl) {
    Write-Host "Cors__AllowedOrigins__0=$VercelUrl"
}

Write-Host ""
Write-Host "=== Vercel environment variables ===" -ForegroundColor Green
Write-Host ""
if ($AzureApiUrl) {
    Write-Host "NEXT_PUBLIC_API_BASE_URL=$AzureApiUrl"
}
Write-Host "NEXT_PUBLIC_AUTH_ENABLED=true"

if ($DeployVercel) {
    Write-Host ""
    Write-Host "Deploying frontend to Vercel..." -ForegroundColor Yellow
    Push-Location (Join-Path $repoRoot "frontend/cls-budget-web")
    try {
        if ($AzureApiUrl) {
            $env:NEXT_PUBLIC_API_BASE_URL = $AzureApiUrl
        }
        $env:NEXT_PUBLIC_AUTH_ENABLED = "true"
        npx vercel deploy --prod --yes
    }
    finally {
        Pop-Location
    }
}

Write-Host ""
Write-Host "=== Next steps ===" -ForegroundColor Cyan
Write-Host "1. Paste Azure settings above into App Service -> Configuration"
Write-Host "2. Publish API: VS Code Azure extension, or set GitHub secrets for deploy-api.yml"
Write-Host "3. Import repo in Vercel dashboard (root: frontend/cls-budget-web) if not using CLI"
Write-Host "4. Test register/login on your Vercel URL"
Write-Host ""
Write-Host "Full guide: docs/DEPLOYMENT.md"
Write-Host ""
