# Prerequisite installers (downloaded by scripts\build-installer.ps1)

Required (embedded into Qayd-Setup via dontcopy + ExtractTemporaryFile):
- vc_redist.x64.exe   Visual C++ 2015-2022 x64 (LocalDB dependency)
- vc_redist.x86.exe   Visual C++ 2015-2022 x86 (LocalDB / SqlWriter helpers)
- SqlLocalDB.msi      SQL Server 2022 LocalDB

Do NOT ship a separate prerequisites folder next to the setup EXE.
The build script verifies these names are embedded in the compiled installer.
