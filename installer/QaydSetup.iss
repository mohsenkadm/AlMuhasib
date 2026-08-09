; Qayd (قيد) — single-file Windows installer
; Build with: scripts\build-installer.ps1
; Pass /DSourcePath=... and optionally /DAppVersion=x.y.z
;
; App is published self-contained (no separate .NET install).
; LocalDB + VC++ redist are extracted via dontcopy before install starts.

#ifndef AppVersion
  #define AppVersion "1.14.7"
#endif

#ifndef DotNetRuntimeFile
  #define DotNetRuntimeFile "windowsdesktop-runtime-10.0.0-win-x64.exe"
#endif

#ifndef LocalDbMsiFile
  #define LocalDbMsiFile "SqlLocalDB.msi"
#endif

#ifndef VcRedistFile
  #define VcRedistFile "vc_redist.x64.exe"
#endif

[Setup]
AppId={{A7E4C9F2-8B3D-4E1A-9C5F-2D6E8A1B4C7F}
AppName=قيد
AppVerName=قيد {#AppVersion}
AppVersion={#AppVersion}
AppPublisher=Qayd
AppPublisherURL=https://github.com/mohsenkadm/AlMuhasib
DefaultDirName={autopf}\Qayd
DefaultGroupName=قيد
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=Qayd-Setup-{#AppVersion}
SetupIconFile=assets\qayd-icon.ico
UninstallDisplayIcon={app}\qayd-icon.ico
WizardImageFile=assets\wizard-large.bmp
WizardSmallImageFile=assets\wizard-small.bmp
Compression=lzma2/ultra64
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
WizardStyle=modern
ShowLanguageDialog=no

[Languages]
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"

[Tasks]
Name: "desktopicon"; Description: "إنشاء اختصار على سطح المكتب"; GroupDescription: "اختصارات إضافية:"; Flags: checkedonce
Name: "launchapp"; Description: "تشغيل قيد بعد اكتمال التثبيت"; GroupDescription: "بعد التثبيت:"; Flags: checkedonce

[Files]
; Application (self-contained — includes .NET runtime)
Source: "{#SourcePath}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "appsettings.json"
Source: "assets\qayd-icon.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "appsettings.template.json"; DestDir: "{tmp}"; DestName: "appsettings.template.json"; Flags: dontcopy
; Prerequisites must use dontcopy so PrepareToInstall can ExtractTemporaryFile before [Files] copy
Source: "prerequisites\{#VcRedistFile}"; DestDir: "{tmp}"; Flags: dontcopy
Source: "prerequisites\{#LocalDbMsiFile}"; DestDir: "{tmp}"; Flags: dontcopy
#ifexist "prerequisites\windowsdesktop-runtime-10.0.0-win-x64.exe"
Source: "prerequisites\{#DotNetRuntimeFile}"; DestDir: "{tmp}"; Flags: dontcopy
#define HasDotNetFallback
#endif

[Icons]
Name: "{group}\قيد"; Filename: "{app}\AlMuhasib.exe"; IconFilename: "{app}\qayd-icon.ico"
Name: "{autodesktop}\قيد"; Filename: "{app}\AlMuhasib.exe"; IconFilename: "{app}\qayd-icon.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\AlMuhasib.exe"; Description: "تشغيل قيد"; Flags: nowait postinstall skipifsilent; Tasks: launchapp

[Code]
var
  DataDirPage: TInputDirWizardPage;
  SelectedDataDir: string;

function JsonEscapePath(const S: string): string;
begin
  Result := S;
  StringChange(Result, '\', '\\');
end;

function IsRebootExitCode(const Code: Integer): Boolean;
begin
  Result := (Code = 3010) or (Code = 1641);
end;

function IsSuccessExitCode(const Code: Integer): Boolean;
begin
  Result := (Code = 0) or IsRebootExitCode(Code);
end;

function EnsureTempPrereq(const FileName: string): Boolean;
begin
  if FileExists(ExpandConstant('{tmp}\' + FileName)) then
  begin
    Result := True;
    Exit;
  end;
  ExtractTemporaryFile(FileName);
  Result := FileExists(ExpandConstant('{tmp}\' + FileName));
end;

function DotNetDesktop10Installed: Boolean;
var
  ResultCode: Integer;
begin
  if RegKeyExists(HKLM64, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App') then
  begin
    Result := True;
    Exit;
  end;
  if RegKeyExists(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App') then
  begin
    Result := True;
    Exit;
  end;

  if Exec(ExpandConstant('{cmd}'), '/c dotnet --list-runtimes 2>nul | findstr /C:"Microsoft.WindowsDesktop.App 10." >nul', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    Result := (ResultCode = 0)
  else
    Result := False;
end;

function LocalDbInstalled: Boolean;
begin
  if RegKeyExists(HKLM64, 'SOFTWARE\Microsoft\Microsoft SQL Server Local DB\Installed Versions\16.0') or
     RegKeyExists(HKLM64, 'SOFTWARE\Microsoft\Microsoft SQL Server Local DB\Installed Versions\15.0') or
     RegKeyExists(HKLM64, 'SOFTWARE\Microsoft\Microsoft SQL Server Local DB\Installed Versions\13.0') then
  begin
    Result := True;
    Exit;
  end;
  if RegKeyExists(HKLM, 'SOFTWARE\Microsoft\Microsoft SQL Server Local DB\Installed Versions\16.0') or
     RegKeyExists(HKLM, 'SOFTWARE\Microsoft\Microsoft SQL Server Local DB\Installed Versions\15.0') or
     RegKeyExists(HKLM, 'SOFTWARE\Microsoft\Microsoft SQL Server Local DB\Installed Versions\13.0') then
  begin
    Result := True;
    Exit;
  end;
  Result := FileExists(ExpandConstant('{pf}\Microsoft SQL Server\160\Tools\Binn\SqlLocalDB.exe')) or
            FileExists(ExpandConstant('{pf}\Microsoft SQL Server\150\Tools\Binn\SqlLocalDB.exe')) or
            FileExists(ExpandConstant('{pf}\Microsoft SQL Server\130\Tools\Binn\SqlLocalDB.exe'));
end;

function FindSqlLocalDbExe: string;
begin
  if FileExists(ExpandConstant('{pf}\Microsoft SQL Server\160\Tools\Binn\SqlLocalDB.exe')) then
    Result := ExpandConstant('{pf}\Microsoft SQL Server\160\Tools\Binn\SqlLocalDB.exe')
  else if FileExists(ExpandConstant('{pf}\Microsoft SQL Server\150\Tools\Binn\SqlLocalDB.exe')) then
    Result := ExpandConstant('{pf}\Microsoft SQL Server\150\Tools\Binn\SqlLocalDB.exe')
  else if FileExists(ExpandConstant('{pf}\Microsoft SQL Server\130\Tools\Binn\SqlLocalDB.exe')) then
    Result := ExpandConstant('{pf}\Microsoft SQL Server\130\Tools\Binn\SqlLocalDB.exe')
  else
    Result := 'sqllocaldb';
end;

function StartLocalDbInstance: Boolean;
var
  ExePath: string;
  ResultCode: Integer;
begin
  ExePath := FindSqlLocalDbExe;
  Exec(ExePath, 'create MSSQLLocalDB', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := Exec(ExePath, 'start MSSQLLocalDB', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

function VcRedistInstalled: Boolean;
var
  Version: string;
begin
  Result := False;
  if RegQueryStringValue(HKLM64, 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\X64', 'Version', Version) then
  begin
    Result := True;
    Exit;
  end;
  if RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\X64', 'Version', Version) then
    Result := True;
end;

function InstallVcRedist: Boolean;
var
  ResultCode: Integer;
  InstallerPath: string;
begin
  if VcRedistInstalled then
  begin
    Result := True;
    Exit;
  end;

  if not EnsureTempPrereq('{#VcRedistFile}') then
  begin
    Log('VC++ redistributable payload missing; continuing (may already be present).');
    Result := True;
    Exit;
  end;

  InstallerPath := ExpandConstant('{tmp}\{#VcRedistFile}');
  WizardForm.StatusLabel.Caption := 'جاري تثبيت Microsoft Visual C++ Redistributable...';
  if not Exec(InstallerPath, '/install /quiet /norestart', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    MsgBox('فشل تشغيل مثبت Visual C++ Redistributable.', mbError, MB_OK);
    Result := False;
    Exit;
  end;

  if not IsSuccessExitCode(ResultCode) then
  begin
    MsgBox('فشل تثبيت Visual C++ Redistributable (رمز الخطأ: ' + IntToStr(ResultCode) + ').', mbError, MB_OK);
    Result := False;
    Exit;
  end;

  Result := True;
end;

function InstallDotNetDesktopFallback: Boolean;
var
  InstallerPath: string;
  ResultCode: Integer;
begin
  { Self-contained builds do not need this. Optional payload only. }
  Result := True;
#ifndef HasDotNetFallback
  Exit;
#endif
  if DotNetDesktop10Installed then
    Exit;

  EnsureTempPrereq('{#DotNetRuntimeFile}');
  InstallerPath := ExpandConstant('{tmp}\{#DotNetRuntimeFile}');
  if not FileExists(InstallerPath) then
    Exit;

  WizardForm.StatusLabel.Caption := 'جاري تثبيت .NET Desktop Runtime (احتياطي)...';
  if Exec(InstallerPath, '/install /quiet /norestart', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    Result := IsSuccessExitCode(ResultCode) or DotNetDesktop10Installed;
end;

function InstallLocalDb: Boolean;
var
  InstallerPath: string;
  LogPath: string;
  ResultCode: Integer;
begin
  if LocalDbInstalled then
  begin
    StartLocalDbInstance;
    Result := True;
    Exit;
  end;

  if not InstallVcRedist then
  begin
    Result := False;
    Exit;
  end;

  if not EnsureTempPrereq('{#LocalDbMsiFile}') then
  begin
    MsgBox('لم يتم تضمين مثبت SQL Server LocalDB داخل ملف التنصيب.' + #13#10 +
           'أعد بناء المثبت عبر scripts\build-installer.ps1 بعد توفر SqlLocalDB.msi.',
           mbError, MB_OK);
    Result := False;
    Exit;
  end;

  InstallerPath := ExpandConstant('{tmp}\{#LocalDbMsiFile}');
  LogPath := ExpandConstant('{tmp}\Qayd-SqlLocalDB-Install.log');
  WizardForm.StatusLabel.Caption := 'جاري تثبيت SQL Server LocalDB...';

  if not Exec('msiexec.exe',
      '/i "' + InstallerPath + '" /qn /norestart IACCEPTSQLLOCALDBLICENSETERMS=YES /L*v "' + LogPath + '"',
      '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    MsgBox('فشل تشغيل مثبت SQL LocalDB.', mbError, MB_OK);
    Result := False;
    Exit;
  end;

  if not IsSuccessExitCode(ResultCode) then
  begin
    MsgBox('فشل تثبيت SQL Server LocalDB (رمز الخطأ: ' + IntToStr(ResultCode) + ').' + #13#10 +
           'راجع سجل التثبيت:' + #13#10 + LogPath,
           mbError, MB_OK);
    Result := False;
    Exit;
  end;

  StartLocalDbInstance;
  Result := LocalDbInstalled;
  if not Result then
    MsgBox('اكتمل مثبت LocalDB لكن تعذر التحقق من المثيل.' + #13#10 +
           'جرّب إعادة تشغيل الجهاز ثم افتح التطبيق.' + #13#10 +
           'السجل: ' + LogPath, mbInformation, MB_OK);
end;

procedure WriteAppSettings;
var
  TemplatePath, TargetPath: string;
  ContentA: AnsiString;
  Content: string;
  DataDirJson: string;
begin
  TargetPath := ExpandConstant('{app}\appsettings.json');
  if FileExists(TargetPath) then
    Exit;

  TemplatePath := ExpandConstant('{tmp}\appsettings.template.json');
  DataDirJson := JsonEscapePath(SelectedDataDir);

  if not FileExists(TemplatePath) then
    ExtractTemporaryFile('appsettings.template.json');

  if LoadStringFromFile(TemplatePath, ContentA) then
  begin
    Content := string(ContentA);
    StringChange(Content, '{DATA_DIRECTORY}', DataDirJson);
    SaveStringToFile(TargetPath, Content, False);
  end
  else
  begin
    SaveStringToFile(TargetPath,
      '{' + #13#10 +
      '  "Installation": {' + #13#10 +
      '    "DataDirectory": "' + DataDirJson + '"' + #13#10 +
      '  },' + #13#10 +
      '  "ConnectionStrings": {' + #13#10 +
      '    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=AlMuhasibDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"' + #13#10 +
      '  }' + #13#10 +
      '}', False);
  end;
end;

procedure InitializeWizard;
begin
  DataDirPage := CreateInputDirPage(wpSelectDir,
    'مجلد البيانات',
    'اختر مكان تخزين قاعدة البيانات',
    'سيتم حفظ ملفات قاعدة البيانات (SQL LocalDB) في المجلد الذي تختاره. يُنصح باختيار قرص بمساحة كافية.',
    False, '');
  DataDirPage.Add('');
  DataDirPage.Values[0] := ExpandConstant('{userdocs}\قيد');
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = DataDirPage.ID then
    SelectedDataDir := DataDirPage.Values[0];
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  NeedsRestart := False;

  WizardForm.StatusLabel.Caption := 'جاري تجهيز المتطلبات...';

  { App is self-contained; .NET install is optional fallback only. }
  InstallDotNetDesktopFallback;

  if not InstallLocalDb then
  begin
    Result := 'تعذر تثبيت SQL Server LocalDB. ثبّت Visual C++ Redistributable ثم أعد المحاولة، أو راجع السجل في مجلد Temp.';
    Exit;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    if SelectedDataDir = '' then
      SelectedDataDir := ExpandConstant('{userdocs}\قيد');
    ForceDirectories(SelectedDataDir);
    WriteAppSettings;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
    MsgBox('تم إزالة تطبيق قيد.' + #13#10 +
           'لم يتم حذف ملفات قاعدة البيانات تلقائياً من مجلد البيانات الذي اخترته.',
           mbInformation, MB_OK);
end;
