# Builds a release folder + zip + version.json for online updates (GitHub Releases).
param(
    [Parameter(Mandatory = $true)]
    [string] $Version,

    [string] $OutputDir = ".\publish\release",
    [string] $GitHubRepo = "mohsenkadm/AlMuhasib",
    [string] $GitBranch = "master",
    [string] $ReleaseNotes = "تحديث جديد",
    [switch] $Mandatory,
    [switch] $CopyManifestToRepoRoot
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$uiProject = Join-Path $root "src\AlMuhasib.UI\AlMuhasib.UI.csproj"
$outputRoot = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDir)
if (-not (Test-Path $outputRoot)) {
    New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
}
$publishDir = Join-Path $outputRoot "app"
$zipName = "AlMuhasib-$Version.zip"
$zipPath = Join-Path $outputRoot $zipName
$tag = "v$Version"
$downloadUrl = "https://github.com/$GitHubRepo/releases/download/$tag/$zipName"
$manifestUrl = "https://raw.githubusercontent.com/$GitHubRepo/$GitBranch/version.json"

Write-Host "Publishing AlMuhasib $Version ..." -ForegroundColor Cyan

if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
New-Item -ItemType Directory -Path $publishDir | Out-Null

dotnet publish $uiProject -c Release -o $publishDir `
    /p:Version=$Version /p:AssemblyVersion=$Version.0 /p:FileVersion=$Version.0

if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

$requiredUpdaterFiles = @(
    "AlMuhasib.Updater.exe",
    "AlMuhasib.Updater.dll",
    "AlMuhasib.Updater.runtimeconfig.json"
)
foreach ($file in $requiredUpdaterFiles) {
    $path = Join-Path $publishDir $file
    if (-not (Test-Path $path)) {
        throw "Publish output is missing required updater file: $file"
    }
}

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -CompressionLevel Optimal

$hash = (Get-FileHash -Path $zipPath -Algorithm SHA256).Hash
$size = (Get-Item $zipPath).Length

$manifest = [ordered]@{
    version             = $Version
    releaseDate         = (Get-Date -Format "yyyy-MM-dd")
    downloadUrl         = $downloadUrl
    sha256              = $hash
    sizeBytes           = $size
    releaseNotes        = $ReleaseNotes
    isMandatory         = [bool]$Mandatory
    minSupportedVersion = "1.0.0"
}

$manifestPath = Join-Path $outputRoot "version.json"
$manifest | ConvertTo-Json | Set-Content -Path $manifestPath -Encoding UTF8

$repoRootManifest = Join-Path $root "version.json"
$manifest | ConvertTo-Json | Set-Content -Path $repoRootManifest -Encoding UTF8

Write-Host ""
Write-Host "Done." -ForegroundColor Green
Write-Host "  ZIP:      $zipPath"
Write-Host "  Manifest: $manifestPath"
Write-Host "  SHA256:   $hash"
Write-Host ""
Write-Host "Client manifest URL (appsettings Updates:ManifestUrl):" -ForegroundColor Cyan
Write-Host "  $manifestUrl"
Write-Host ""
Write-Host "Publish to GitHub ($GitHubRepo):" -ForegroundColor Yellow
Write-Host "  1. Create release tag $tag and upload: $zipName"
Write-Host "  2. Commit and push version.json on branch $GitBranch"
Write-Host ""
Write-Host "  gh release create $tag `"AlMuhasib $Version`" `"$zipPath`" --repo $GitHubRepo"
Write-Host "  git add version.json && git commit -m `"release: $Version`" && git push"
