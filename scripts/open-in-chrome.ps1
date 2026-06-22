function Open-InChrome {
    param([Parameter(Mandatory = $true)][string]$Url)

    $chromePaths = @(
        "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
        ${env:ProgramFiles(x86)} + "\Google\Chrome\Application\chrome.exe",
        "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe"
    )

    $chrome = $chromePaths | Where-Object { Test-Path $_ } | Select-Object -First 1

    if ($chrome) {
        Start-Process -FilePath $chrome -ArgumentList $Url
    }
    else {
        Write-Warning "Google Chrome not found. Opening with default browser."
        Start-Process $Url
    }
}
