param(
    [Parameter(Mandatory = $true)]
    [string]$ServerUrl,

    [string]$CompanionExe,

    [string]$DownloadUrl = "https://github.com/gilbreis/Anki-Importer/releases/latest/download/AnkiImporter.Companion.exe"
)

$ErrorActionPreference = "Stop"

$InstallDir = Join-Path $env:LOCALAPPDATA "AnkiImporter"
$InstalledExe = Join-Path $InstallDir "AnkiImporter.Companion.exe"
$StartupDir = [Environment]::GetFolderPath("Startup")
$StartupCmd = Join-Path $StartupDir "AnkiImporter.Companion.cmd"

Write-Host "Anki Importer - Desktop Companion installer"
Write-Host ""

if (-not (Test-Path $InstallDir)) {
    New-Item -ItemType Directory -Path $InstallDir | Out-Null
}

if ($CompanionExe) {
    $source = (Resolve-Path $CompanionExe).Path
    Copy-Item -LiteralPath $source -Destination $InstalledExe -Force
}
elseif ($DownloadUrl) {
    Write-Host "Downloading Companion..."
    Invoke-WebRequest -Uri $DownloadUrl -OutFile $InstalledExe -UseBasicParsing
}
else {
    throw "Provide either -CompanionExe or -DownloadUrl."
}

Write-Host "Installed: $InstalledExe"
Write-Host ""
Write-Host "Testing AnkiConnect..."

& $InstalledExe health
if ($LASTEXITCODE -ne 0) {
    Write-Warning "AnkiConnect test failed. Make sure Anki Desktop is open and AnkiConnect is installed."
}

Write-Host ""
Write-Host "Starting pairing..."
Write-Host "A short pairing code will appear below."
Write-Host "Send that code to the Anki Importer plugin in ChatGPT."
Write-Host ""

& $InstalledExe pair $ServerUrl
if ($LASTEXITCODE -ne 0) {
    throw "Pairing failed."
}

$cmd = @"
@echo off
start "" /min "$InstalledExe" run
"@
Set-Content -LiteralPath $StartupCmd -Value $cmd -Encoding ASCII

Write-Host ""
Write-Host "Startup entry created: $StartupCmd"
Write-Host "Starting Companion..."
Start-Process -FilePath $InstalledExe -ArgumentList "run" -WindowStyle Hidden

Write-Host ""
Write-Host "Installation complete."
Write-Host "The Companion will start automatically when you sign in to Windows."
