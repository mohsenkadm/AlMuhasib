# Builds Qayd single-file installer (Inno Setup).
# Publishes a self-contained win-x64 app so customers do not need a separate .NET install.
param(
    [string] $Version = "",
    [string] $PublishDir = ".\publish\installer-staging",
    [string] $OutputDir = ".\dist",
    [switch] $SkipLogo,
    [switch] $SkipPrerequisites,
    [switch] $SkipCompile
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$uiProject = Join-Path $root "src\AlMuhasib.UI\AlMuhasib.UI.csproj"
$installerScript = Join-Path $root "installer\QaydSetup.iss"
$prereqDir = Join-Path $root "installer\prerequisites"

if (-not $SkipLogo) {
    & (Join-Path $root "scripts\prepare-logo.ps1")
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = ([xml](Get-Content $uiProject)).Project.PropertyGroup.Version | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($Version)) { $Version = "1.14.6" }
}

$publishPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($PublishDir)
if (Test-Path $publishPath) { Remove-Item $publishPath -Recurse -Force }
New-Item -ItemType Directory -Path $publishPath -Force | Out-Null

Write-Host "Publishing Qayd $Version (self-contained win-x64) ..." -ForegroundColor Cyan
dotnet publish $uiProject -c Release -r win-x64 --self-contained true -o $publishPath `
    /p:Version=$Version /p:AssemblyVersion=$Version.0 /p:FileVersion=$Version.0 `
    /p:PublishReadyToRun=true

if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

$requiredUpdaterFiles = @(
    "AlMuhasib.Updater.exe",
    "AlMuhasib.Updater.dll",
    "AlMuhasib.Updater.runtimeconfig.json"
)
foreach ($file in $requiredUpdaterFiles) {
    if (-not (Test-Path (Join-Path $publishPath $file))) {
        throw "Publish output is missing required updater file: $file"
    }
}

function Get-RemoteFile {
    param(
        [Parameter(Mandatory = $true)][string] $Url,
        [Parameter(Mandatory = $true)][string] $Destination,
        [Parameter(Mandatory = $true)][string] $Label
    )
    Write-Host "Downloading $Label ..." -ForegroundColor Yellow
    try {
        Invoke-WebRequest -Uri $Url -OutFile $Destination -UseBasicParsing
        if (-not (Test-Path $Destination) -or ((Get-Item $Destination).Length -lt 1MB)) {
            throw "Downloaded file is missing or too small: $Destination"
        }
    }
    catch {
        Write-Warning "Could not download $Label from $Url. $($_.Exception.Message)"
        if (Test-Path $Destination) { Remove-Item $Destination -Force -ErrorAction SilentlyContinue }
        return $false
    }
    return $true
}

if (-not $SkipPrerequisites) {
    New-Item -ItemType Directory -Path $prereqDir -Force | Out-Null

    $vcRedistX64 = Join-Path $prereqDir "vc_redist.x64.exe"
    if (-not (Test-Path $vcRedistX64)) {
        [void](Get-RemoteFile -Url "https://aka.ms/vs/17/release/vc_redist.x64.exe" -Destination $vcRedistX64 -Label "VC++ Redistributable x64")
    }

    $vcRedistX86 = Join-Path $prereqDir "vc_redist.x86.exe"
    if (-not (Test-Path $vcRedistX86)) {
        [void](Get-RemoteFile -Url "https://aka.ms/vs/17/release/vc_redist.x86.exe" -Destination $vcRedistX86 -Label "VC++ Redistributable x86")
    }

    $localDbDest = Join-Path $prereqDir "SqlLocalDB.msi"
    if (-not (Test-Path $localDbDest)) {
        [void](Get-RemoteFile -Url "https://go.microsoft.com/fwlink/?linkid=2215160" -Destination $localDbDest -Label "SQL Server 2022 LocalDB")
    }

    # Optional .NET Desktop Runtime — unused for self-contained app; kept for legacy fallback packaging
    $dotnetInstaller = Get-ChildItem -Path $prereqDir -Filter "windowsdesktop-runtime*.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $dotnetInstaller) {
        $dotnetDest = Join-Path $prereqDir "windowsdesktop-runtime-10.0.0-win-x64.exe"
        [void](Get-RemoteFile -Url "https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/10.0.0/windowsdesktop-runtime-10.0.0-win-x64.exe" -Destination $dotnetDest -Label ".NET 10 Desktop Runtime (optional)")
    }

    foreach ($required in @($localDbDest, $vcRedistX64, $vcRedistX86)) {
        if (-not (Test-Path $required)) {
            throw "Required prerequisite missing: $required"
        }
        if ((Get-Item $required).Length -lt 1MB) {
            throw "Prerequisite looks corrupt (too small): $required"
        }
    }

    Write-Host "Prerequisites OK:" -ForegroundColor Green
    Get-ChildItem $prereqDir -Include "vc_redist*.exe","SqlLocalDB.msi" -File | ForEach-Object {
        Write-Host ("  {0} ({1:N1} MB)" -f $_.Name, ($_.Length / 1MB))
    }
}

if ($SkipCompile) {
    Write-Host "Publish completed: $publishPath" -ForegroundColor Green
    return
}

$isccCandidates = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
)
$iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) {
    $found = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($found) { $iscc = $found.Source }
}
if (-not $iscc) {
    throw "Inno Setup 6 (ISCC.exe) not found. Install from https://jrsoftware.org/isinfo.php"
}

$distPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDir)
New-Item -ItemType Directory -Path $distPath -Force | Out-Null

Write-Host "Compiling installer ..." -ForegroundColor Cyan
& $iscc $installerScript "/DSourcePath=$publishPath" "/DAppVersion=$Version"
if ($LASTEXITCODE -ne 0) { throw "Inno Setup compilation failed" }

$setupPath = Join-Path $distPath "Qayd-Setup-$Version.exe"
if (-not (Test-Path $setupPath)) {
    throw "Expected installer not found: $setupPath"
}

Write-Host "Installer build completed." -ForegroundColor Green
Get-ChildItem -Path $distPath -Filter "Qayd-Setup-*.exe" | ForEach-Object {
    Write-Host ("  {0} ({1:N1} MB)" -f $_.FullName, ($_.Length / 1MB)) -ForegroundColor Green
}
