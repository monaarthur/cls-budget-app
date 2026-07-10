param(
    [string]$Email = "test@example.com",
    [string]$ApiUrl = "http://localhost:5123"
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "=== Test forgot-password locally ===" -ForegroundColor Cyan
Write-Host "API:   $ApiUrl"
Write-Host "Email: $Email"
Write-Host ""

$body = @{ email = $Email } | ConvertTo-Json
try {
    $response = Invoke-WebRequest `
        -Uri "$ApiUrl/api/v1/auth/forgot-password" `
        -Method POST `
        -Body $body `
        -ContentType "application/json" `
        -UseBasicParsing `
        -TimeoutSec 30

    Write-Host "HTTP $($response.StatusCode)" -ForegroundColor Green
    Write-Host $response.Content
}
catch {
    if ($_.Exception.Response) {
        $reader = [IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
        $errorBody = $reader.ReadToEnd()
        Write-Host "HTTP $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
        if ($errorBody) { Write-Host $errorBody } else { Write-Host "(empty response body)" }
    }
    else {
        Write-Host $_.Exception.Message -ForegroundColor Red
        Write-Host ""
        Write-Host "Start the API first:" -ForegroundColor Yellow
        Write-Host "  .\scripts\start-api-local.ps1"
    }
    exit 1
}

Write-Host ""
Write-Host "If that email exists in AppUser, the reset link is in the API console:" -ForegroundColor DarkGray
Write-Host '  Password reset link for ... -> http://localhost:3000/reset-password?token=...'
Write-Host ""
Write-Host "Browser test (enable auth in frontend/.env.local):" -ForegroundColor DarkGray
Write-Host "  NEXT_PUBLIC_AUTH_ENABLED=true"
Write-Host "  npm run dev   -> open /forgot-password/"
