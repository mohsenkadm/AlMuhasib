# Prerequisite installers (downloaded by scripts\build-installer.ps1)

Required (embedded into Qayd-Setup via dontcopy + nocompression + ExtractTemporaryFile):
- vc_redist.x64.exe   Visual C++ 2015-2022 x64
- vc_redist.x86.exe   Visual C++ 2015-2022 x86
- SqlLocalDB.msi      REAL Windows Installer package — SQL Server 2017 LocalDB (ProductVersion 14.x)

IMPORTANT:
- Qayd ships SQL Server 2017 LocalDB (not 2022). Connection string stays (localdb)\MSSQLLocalDB.
- The installer SKIPS LocalDB MSI when any LocalDB is already present, so recent customers
  who already have LocalDB 2022/Express keep their working setup untouched.
- Do NOT use https://go.microsoft.com/fwlink/?linkid=2215160 as SqlLocalDB.msi
  That link downloads SQL2022-SSEI-Expr.exe (bootstrapper). msiexec then fails with error 1620.
- Correct SQL Server 2017 LocalDB MSI CDN:
  https://download.microsoft.com/download/E/F/2/EF23C21D-7860-4F05-88CE-39AA114B014B/SqlLocalDB.msi
