param(
    [string]$ApiSwaggerUrl = "http://localhost:5123/swagger",
    [int]$StartupTimeoutSeconds = 120
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "open-in-chrome.ps1")

$frontendRoot = Resolve-Path (Join-Path $PSScriptRoot "..\frontend\cls-budget-web")
$waiterScript = Join-Path $PSScriptRoot "open-frontend-when-ready.ps1"

function Test-UrlReady([string]$Url) {
    try {
        $null = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
        return $true
    }
    catch {
        return $false
    }
}

Write-Host "Opening API (Swagger): $ApiSwaggerUrl"
if (Test-UrlReady $ApiSwaggerUrl) {
    Open-InChrome $ApiSwaggerUrl
}
else {
    Write-Warning "API not responding yet at $ApiSwaggerUrl - opening tab anyway (refresh after the service starts)."
    Open-InChrome $ApiSwaggerUrl
}

# Separate process so the browser opens in your desktop session (Start-Job cannot reliably do this)
$waiter = Start-Process powershell.exe -PassThru -WindowStyle Hidden -ArgumentList @(
    "-NoProfile",
    "-ExecutionPolicy", "Bypass",
    "-File", $waiterScript,
    "-TimeoutSeconds", $StartupTimeoutSeconds
)

Push-Location $frontendRoot
try {
    Write-Host "Starting Next.js dev server in $frontendRoot"
    Write-Host "Browser will open in Chrome when ready (http://localhost:3000 or 3001). Press Ctrl+C to stop."
    Write-Host ""
    npm run dev
}
finally {
    Pop-Location
    if ($waiter -and -not $waiter.HasExited) {
        Stop-Process -Id $waiter.Id -Force -ErrorAction SilentlyContinue
    }
}
