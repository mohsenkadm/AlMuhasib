# Builds Qayd single-file installer (Inno Setup).
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
    if ([string]::IsNullOrWhiteSpace($Version)) { $Version = "1.14.4" }
}

$publishPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($PublishDir)
if (Test-Path $publishPath) { Remove-Item $publishPath -Recurse -Force }
New-Item -ItemType Directory -Path $publishPath -Force | Out-Null

Write-Host "Publishing Qayd $Version (framework-dependent) ..." -ForegroundColor Cyan
dotnet publish $uiProject -c Release -o $publishPath `
    /p:Version=$Version /p:AssemblyVersion=$Version.0 /p:FileVersion=$Version.0

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

if (-not $SkipPrerequisites) {
    New-Item -ItemType Directory -Path $prereqDir -Force | Out-Null

    $dotnetInstaller = Get-ChildItem -Path $prereqDir -Filter "windowsdesktop-runtime*.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $dotnetInstaller) {
        Write-Host "Downloading .NET 10 Desktop Runtime ..." -ForegroundColor Yellow
        $dotnetUrl = "https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/10.0.0/windowsdesktop-runtime-10.0.0-win-x64.exe"
        $dotnetDest = Join-Path $prereqDir "windowsdesktop-runtime-10.0.0-win-x64.exe"
        try {
            Invoke-WebRequest -Uri $dotnetUrl -OutFile $dotnetDest -UseBasicParsing
        }
        catch {
            Write-Warning "Could not download .NET runtime from $dotnetUrl. Place windowsdesktop-runtime-*.exe in installer\prerequisites manually."
        }
    }

    $localDbInstaller = Get-ChildItem -Path $prereqDir -Filter "SqlLocalDB*.msi" -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $localDbInstaller) {
        $localDbInstaller = Get-ChildItem -Path $prereqDir -Filter "*.msi" -ErrorAction SilentlyContinue | Select-Object -First 1
    }
    if (-not $localDbInstaller) {
        Write-Host "Downloading SQL Server 2022 LocalDB ..." -ForegroundColor Yellow
        $localDbUrl = "https://go.microsoft.com/fwlink/?linkid=2215160"
        $localDbDest = Join-Path $prereqDir "SqlLocalDB.msi"
        try {
            Invoke-WebRequest -Uri $localDbUrl -OutFile $localDbDest -UseBasicParsing
        }
        catch {
            Write-Warning "Could not download SqlLocalDB.msi. Place SqlLocalDB.msi in installer\prerequisites manually."
        }
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

Write-Host "Installer build completed." -ForegroundColor Green
Get-ChildItem -Path $distPath -Filter "Qayd-Setup-*.exe" | ForEach-Object { Write-Host "  $($_.FullName)" -ForegroundColor Green }
