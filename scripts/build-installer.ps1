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
    if ([string]::IsNullOrWhiteSpace($Version)) { $Version = "1.14.14" }
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

function Test-IsWindowsInstallerPackage {
    param([Parameter(Mandatory = $true)][string] $Path)
    if (-not (Test-Path $Path)) { return $false }
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -lt 8) { return $false }
    # Real MSI = OLE compound document (D0 CF 11 E0). MZ (4D 5A) means an EXE was saved as .msi.
    if ($bytes[0] -ne 0xD0 -or $bytes[1] -ne 0xCF -or $bytes[2] -ne 0x11 -or $bytes[3] -ne 0xE0) {
        return $false
    }
    try {
        $wi = New-Object -ComObject WindowsInstaller.Installer
        $db = $wi.OpenDatabase((Resolve-Path $Path).Path, 0)
        $null = $db
        return $true
    }
    catch {
        return $false
    }
}

function Get-MsiProductVersion {
    param([Parameter(Mandatory = $true)][string] $Path)
    if (-not (Test-Path $Path)) { return $null }
    try {
        $wi = New-Object -ComObject WindowsInstaller.Installer
        $db = $wi.OpenDatabase((Resolve-Path $Path).Path, 0)
        $view = $db.OpenView("SELECT Value FROM Property WHERE Property='ProductVersion'")
        $view.Execute()
        $record = $view.Fetch()
        if ($null -eq $record) { return $null }
        return [string]$record.StringData(1)
    }
    catch {
        return $null
    }
}

function Test-IsSqlLocalDb2017Package {
    param([Parameter(Mandatory = $true)][string] $Path)
    if (-not (Test-IsWindowsInstallerPackage -Path $Path)) { return $false }
    $productVersion = Get-MsiProductVersion -Path $Path
    if ([string]::IsNullOrWhiteSpace($productVersion)) { return $false }
    # SQL Server 2017 LocalDB ProductVersion is 14.x
    return $productVersion.StartsWith("14.")
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
    # Prefer SQL Server 2017 LocalDB (14.x) — more reliable on many customer Windows 10 PCs than 2022.
    # Do NOT use fwlink/?linkid=2215160 (that is an Express bootstrapper EXE, msiexec 1620).
    $localDbUrl = "https://download.microsoft.com/download/E/F/2/EF23C21D-7860-4F05-88CE-39AA114B014B/SqlLocalDB.msi"
    if (-not (Test-IsSqlLocalDb2017Package -Path $localDbDest)) {
        if (Test-Path $localDbDest) {
            $existingVer = Get-MsiProductVersion -Path $localDbDest
            Write-Warning "Replacing SqlLocalDB.msi (found '$existingVer'; need SQL Server 2017 / 14.x)..."
            Remove-Item $localDbDest -Force
        }
        if (-not (Get-RemoteFile -Url $localDbUrl -Destination $localDbDest -Label "SQL Server 2017 LocalDB MSI")) {
            throw "Failed to download SQL Server 2017 SqlLocalDB.msi from $localDbUrl"
        }
        if (-not (Test-IsSqlLocalDb2017Package -Path $localDbDest)) {
            $got = Get-MsiProductVersion -Path $localDbDest
            throw "Downloaded SqlLocalDB.msi is not SQL Server 2017 (got '$got'). Refusing to build installer."
        }
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
    if (-not (Test-IsSqlLocalDb2017Package -Path $localDbDest)) {
        throw "SqlLocalDB.msi failed SQL Server 2017 validation."
    }

    Write-Host "Prerequisites OK:" -ForegroundColor Green
    Get-ChildItem $prereqDir -File | Where-Object { $_.Name -match '^(vc_redist|SqlLocalDB)' } | ForEach-Object {
        $extra = ""
        if ($_.Name -eq "SqlLocalDB.msi") {
            $extra = " [ProductVersion=$(Get-MsiProductVersion -Path $_.FullName)]"
        }
        Write-Host ("  {0} ({1:N1} MB){2}" -f $_.Name, ($_.Length / 1MB), $extra)
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
