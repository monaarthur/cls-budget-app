param(
    [int[]]$Ports = @(3000, 3001),
    [int]$TimeoutSeconds = 120
)

. (Join-Path $PSScriptRoot "open-in-chrome.ps1")

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
while ((Get-Date) -lt $deadline) {
    foreach ($port in $Ports) {
        $url = "http://localhost:$port"
        try {
            $null = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
            Open-InChrome $url
            exit 0
        }
        catch {
            # try next port
        }
    }
    Start-Sleep -Milliseconds 750
}

exit 1
