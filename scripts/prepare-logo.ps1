# Generates qayd-icon.ico, qayd-icon.png, qayd-mark.png and installer wizard images.
param(
    [string] $SourceImage = "",
    [string] $OutputDir = ""
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

if ([string]::IsNullOrWhiteSpace($SourceImage)) {
    $assetsDir = Join-Path $root "assets"
    $candidates = @(
        Get-ChildItem -Path $assetsDir -Filter "*ChatGPT_Image*.png" -ErrorAction SilentlyContinue
        Get-ChildItem -Path $root -Recurse -Filter "qayd-source.png" -ErrorAction SilentlyContinue
    ) | Select-Object -First 1
    if (-not $candidates) {
        throw "Logo source not found. Pass -SourceImage or place PNG under assets/."
    }
    $SourceImage = $candidates.FullName
}

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $root "src\AlMuhasib.UI\Assets\Brand"
}

$toolProject = Join-Path $root "tools\PrepareLogo\PrepareLogo.csproj"
dotnet run --project $toolProject -c Release -- $SourceImage $OutputDir
if ($LASTEXITCODE -ne 0) { throw "PrepareLogo failed" }

Write-Host "Logo prepared in $OutputDir" -ForegroundColor Green

$installerAssets = Join-Path $root "installer\assets"
New-Item -ItemType Directory -Path $installerAssets -Force | Out-Null
Copy-Item (Join-Path $OutputDir "qayd-icon.ico") (Join-Path $installerAssets "qayd-icon.ico") -Force
Copy-Item (Join-Path $OutputDir "qayd-icon.png") (Join-Path $installerAssets "qayd-icon.png") -Force
