# Prerequisite installers (downloaded by scripts\build-installer.ps1)

Required (embedded into Qayd-Setup via dontcopy + nocompression + ExtractTemporaryFile):
- vc_redist.x64.exe   Visual C++ 2015-2022 x64
- vc_redist.x86.exe   Visual C++ 2015-2022 x86
- SqlLocalDB.msi      REAL Windows Installer package (OLE header D0 CF 11 E0)

IMPORTANT:
- Do NOT use https://go.microsoft.com/fwlink/?linkid=2215160 as SqlLocalDB.msi
  That link downloads SQL2022-SSEI-Expr.exe (bootstrapper). msiexec then fails with error 1620.
- Correct MSI CDN:
  https://download.microsoft.com/download/3/8/d/38de7036-2433-4207-8eae-06e247e17b25/SqlLocalDB.msi
- Or extract via:
  SQL2022-SSEI-Expr.exe /ACTION=Download /MEDIATYPE=LocalDB /MEDIAPATH=C:\Temp /QUIET
