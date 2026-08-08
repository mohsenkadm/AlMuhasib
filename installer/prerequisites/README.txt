# Prerequisite installers (downloaded automatically by scripts\build-installer.ps1)

Required for customer-friendly setup:
- vc_redist.x64.exe          (Visual C++ 2015-2022 x64 — required by SQL LocalDB)
- SqlLocalDB.msi            (SQL Server 2022 LocalDB)

Optional fallback (app is published self-contained, so usually unused):
- windowsdesktop-runtime-10.x.x-win-x64.exe

Do NOT rely on a prerequisites folder next to Qayd-Setup.exe at the customer PC.
These files are embedded into the setup and extracted with ExtractTemporaryFile.
