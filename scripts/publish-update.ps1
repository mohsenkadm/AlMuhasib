# Builds a release folder + zip + version.json for online updates.
param(
    [Parameter(Mandatory = $true)]
    [string] $Version,

    [string] $OutputDir = ".\publish\release",
    [string] $DownloadUrlBase = "https://YOUR-SERVER.com/almahasib/releases",
    [string] $ReleaseNotes = "تحديث جديد",
    [switch] $Mandatory
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$uiProject = Join-Path $root "src\AlMuhasib.UI\AlMuhasib.UI.csproj"
$publishDir = Join-Path (Resolve-Path $OutputDir) "app"
$zipName = "AlMuhasib-$Version.zip"
$zipPath = Join-Path (Resolve-Path $OutputDir) $zipName

Write-Host "Publishing AlMuhasib $Version ..." -ForegroundColor Cyan

if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
New-Item -ItemType Directory -Path $publishDir | Out-Null

dotnet publish $uiProject -c Release -o $publishDir `
    /p:Version=$Version /p:AssemblyVersion=$Version.0 /p:FileVersion=$Version.0

if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -CompressionLevel Optimal

$hash = (Get-FileHash -Path $zipPath -Algorithm SHA256).Hash
$size = (Get-Item $zipPath).Length

$manifest = [ordered]@{
    version             = $Version
    releaseDate         = (Get-Date -Format "yyyy-MM-dd")
    downloadUrl         = "$DownloadUrlBase/$zipName"
    sha256              = $hash
    sizeBytes           = $size
    releaseNotes        = $ReleaseNotes
    isMandatory         = [bool]$Mandatory
    minSupportedVersion = "1.0.0"
}

$manifestPath = Join-Path (Resolve-Path $OutputDir) "version.json"
$manifest | ConvertTo-Json | Set-Content -Path $manifestPath -Encoding UTF8

Write-Host ""
Write-Host "Done." -ForegroundColor Green
Write-Host "  ZIP:      $zipPath"
Write-Host "  Manifest: $manifestPath"
Write-Host "  SHA256:   $hash"
Write-Host ""
Write-Host "Upload both files to your server and set Updates:ManifestUrl in appsettings.json"
