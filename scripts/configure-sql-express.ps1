#Requires -RunAsAdministrator
param(
    [Parameter(Mandatory = $true)][string]$DatabaseName,
    [Parameter(Mandatory = $true)][string]$BranchUsername,
    [Parameter(Mandatory = $true)][string]$BranchPassword,
    [int]$SqlPort = 1433
)

$ErrorActionPreference = 'Stop'

function Test-SqlExpressInstance {
    $services = Get-Service -ErrorAction SilentlyContinue | Where-Object { $_.Name -like 'MSSQL$*' -or $_.Name -eq 'MSSQLSERVER' }
    return $services.Count -gt 0
}

if (-not (Test-SqlExpressInstance)) {
    Write-Error 'SQL Server Express غير مثبت. يرجى تثبيت SQL Server Express 2022 أولاً.'
}

# فتح منفذ SQL في جدار الحماية
$ruleName = 'Qayd SQL Server TCP'
$existing = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
if (-not $existing) {
    New-NetFirewallRule -DisplayName $ruleName -Direction Inbound -Action Allow -Protocol TCP -LocalPort $SqlPort | Out-Null
}

$instanceName = 'SQLEXPRESS'
$dataSource = "localhost\$instanceName"

$masterConnection = "Server=$dataSource;Database=master;Trusted_Connection=True;TrustServerCertificate=True;"

$createLogin = @"
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'$BranchUsername')
    CREATE LOGIN [$BranchUsername] WITH PASSWORD = N'$($BranchPassword.Replace("'", "''"))', CHECK_POLICY = OFF;
ELSE
    ALTER LOGIN [$BranchUsername] WITH PASSWORD = N'$($BranchPassword.Replace("'", "''"))';
"@

sqlcmd -S $dataSource -Q $createLogin -b
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$createUser = @"
USE [$DatabaseName];
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$BranchUsername')
BEGIN
    CREATE USER [$BranchUsername] FOR LOGIN [$BranchUsername];
    ALTER ROLE db_datareader ADD MEMBER [$BranchUsername];
    ALTER ROLE db_datawriter ADD MEMBER [$BranchUsername];
END
"@

sqlcmd -S $dataSource -Q $createUser -b
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Output "تم تهيئة SQL Express للفروع بنجاح."
