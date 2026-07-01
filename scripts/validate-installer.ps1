# Validates installer publish output and appsettings template (no GUI install).
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$staging = Join-Path $root "publish\installer-staging"
$template = Join-Path $root "installer\appsettings.template.json"

if (-not (Test-Path (Join-Path $staging "AlMuhasib.exe"))) {
    Write-Host "Running publish first..." -ForegroundColor Yellow
    & (Join-Path $root "scripts\build-installer.ps1") -SkipLogo -SkipPrerequisites -SkipCompile
}

$required = @(
    "AlMuhasib.exe",
    "AlMuhasib.Updater.exe",
    "appsettings.json",
    "Assets\Brand\qayd-icon.ico"
)
foreach ($file in $required) {
    $path = Join-Path $staging $file
    if (-not (Test-Path $path)) { throw "Missing publish artifact: $file" }
}

$testDataDir = Join-Path $env:TEMP "QaydInstallTest\Data"
New-Item -ItemType Directory -Path $testDataDir -Force | Out-Null
$escaped = $testDataDir.Replace('\', '\\')
$content = Get-Content $template -Raw
$content = $content.Replace('{DATA_DIRECTORY}', $escaped)
$testSettings = Join-Path $env:TEMP "QaydInstallTest\appsettings.json"
$content | Set-Content $testSettings -Encoding UTF8

$parsed = Get-Content $testSettings -Raw | ConvertFrom-Json
if ($parsed.Installation.DataDirectory -ne $testDataDir) {
    throw "DataDirectory not written correctly in generated appsettings"
}
if ($parsed.ConnectionStrings.DefaultConnection -notmatch "localdb") {
    throw "DefaultConnection should target LocalDB"
}

Write-Host "Installer validation passed:" -ForegroundColor Green
Write-Host "  Publish staging: $staging"
Write-Host "  Sample appsettings: $testSettings"
Write-Host "  DataDirectory: $($parsed.Installation.DataDirectory)"
