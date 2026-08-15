; Qayd (قيد) — Windows installer
; Build: scripts\build-installer.ps1
; App is self-contained (no separate .NET). LocalDB/VC++ are embedded and extracted
; with ExtractTemporaryFile BEFORE file copy (PrepareToInstall).
;
; LocalDB policy (1.14.14+):
; - Skip MSI only when an existing LocalDB is HEALTHY (exe + start MSSQLLocalDB).
; - Broken leftovers → repair/reinstall SQL Server 2017 LocalDB MSI.
; - Exit 3010/1641 → NeedsRestart; do not launch the app until reboot.
; - Never delete customer .mdf files during repair.

#ifndef AppVersion
  #define AppVersion "1.14.14"
#endif

#ifndef LocalDbMsiFile
  #define LocalDbMsiFile "SqlLocalDB.msi"
#endif

#ifndef VcRedistX64File
  #define VcRedistX64File "vc_redist.x64.exe"
#endif

#ifndef VcRedistX86File
  #define VcRedistX86File "vc_redist.x86.exe"
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
Source: "{#SourcePath}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "appsettings.json"
Source: "assets\qayd-icon.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "appsettings.template.json"; DestDir: "{tmp}"; DestName: "appsettings.template.json"; Flags: dontcopy
; nocompression: avoids ExtractTemporaryFile corruption with SolidCompression for MSI/EXE payloads
Source: "prerequisites\{#VcRedistX64File}"; DestDir: "{tmp}"; Flags: dontcopy nocompression
Source: "prerequisites\{#VcRedistX86File}"; DestDir: "{tmp}"; Flags: dontcopy nocompression
Source: "prerequisites\{#LocalDbMsiFile}"; DestDir: "{tmp}"; Flags: dontcopy nocompression

[Icons]
Name: "{group}\قيد"; Filename: "{app}\AlMuhasib.exe"; IconFilename: "{app}\qayd-icon.ico"
Name: "{autodesktop}\قيد"; Filename: "{app}\AlMuhasib.exe"; IconFilename: "{app}\qayd-icon.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\AlMuhasib.exe"; Description: "تشغيل قيد"; Flags: nowait postinstall skipifsilent skipifdoesntexist; Tasks: launchapp; Check: CanLaunchAfterInstall

[Code]
var
  DataDirPage: TInputDirWizardPage;
  SelectedDataDir: string;
  LocalDbWarning: string;
  LocalDbNeedsRestart: Boolean;
  LastLocalDbMsiExitCode: Integer;

function JsonEscapePath(const S: string): string;
begin
  Result := S;
  StringChange(Result, '\', '\\');
end;

function IsSuccessExitCode(const Code: Integer): Boolean;
begin
  { 0=ok, 3010/1641=reboot required, 1638=newer/same product already installed }
  Result := (Code = 0) or (Code = 3010) or (Code = 1641) or (Code = 1638);
end;

function IsRebootExitCode(const Code: Integer): Boolean;
begin
  Result := (Code = 3010) or (Code = 1641);
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

function RegLocalDbVersionExists(const Ver: string): Boolean;
begin
  Result :=
    RegKeyExists(HKLM64, 'SOFTWARE\Microsoft\Microsoft SQL Server Local DB\Installed Versions\' + Ver) or
    RegKeyExists(HKLM, 'SOFTWARE\Microsoft\Microsoft SQL Server Local DB\Installed Versions\' + Ver);
end;

function LocalDbPresentInRegistryOrPath: Boolean;
begin
  { Cover common LocalDB major versions including SQL 2025 (17.0) and older. }
  if RegLocalDbVersionExists('17.0') or RegLocalDbVersionExists('16.0') or
     RegLocalDbVersionExists('15.0') or RegLocalDbVersionExists('14.0') or
     RegLocalDbVersionExists('13.0') or RegLocalDbVersionExists('12.0') or
     RegLocalDbVersionExists('11.0') then
  begin
    Result := True;
    Exit;
  end;

  if FileExists(ExpandConstant('{pf}\Microsoft SQL Server\170\Tools\Binn\SqlLocalDB.exe')) or
     FileExists(ExpandConstant('{pf}\Microsoft SQL Server\160\Tools\Binn\SqlLocalDB.exe')) or
     FileExists(ExpandConstant('{pf}\Microsoft SQL Server\150\Tools\Binn\SqlLocalDB.exe')) or
     FileExists(ExpandConstant('{pf}\Microsoft SQL Server\140\Tools\Binn\SqlLocalDB.exe')) or
     FileExists(ExpandConstant('{pf}\Microsoft SQL Server\130\Tools\Binn\SqlLocalDB.exe')) or
     FileExists(ExpandConstant('{pf}\Microsoft SQL Server\120\Tools\Binn\SqlLocalDB.exe')) or
     FileExists(ExpandConstant('{pf}\Microsoft SQL Server\110\Tools\Binn\SqlLocalDB.exe')) then
  begin
    Result := True;
    Exit;
  end;

  Result := False;
end;

function FindSqlLocalDbExe: string;
begin
  if FileExists(ExpandConstant('{pf}\Microsoft SQL Server\170\Tools\Binn\SqlLocalDB.exe')) then
    Result := ExpandConstant('{pf}\Microsoft SQL Server\170\Tools\Binn\SqlLocalDB.exe')
  else if FileExists(ExpandConstant('{pf}\Microsoft SQL Server\160\Tools\Binn\SqlLocalDB.exe')) then
    Result := ExpandConstant('{pf}\Microsoft SQL Server\160\Tools\Binn\SqlLocalDB.exe')
  else if FileExists(ExpandConstant('{pf}\Microsoft SQL Server\150\Tools\Binn\SqlLocalDB.exe')) then
    Result := ExpandConstant('{pf}\Microsoft SQL Server\150\Tools\Binn\SqlLocalDB.exe')
  else if FileExists(ExpandConstant('{pf}\Microsoft SQL Server\140\Tools\Binn\SqlLocalDB.exe')) then
    Result := ExpandConstant('{pf}\Microsoft SQL Server\140\Tools\Binn\SqlLocalDB.exe')
  else if FileExists(ExpandConstant('{pf}\Microsoft SQL Server\130\Tools\Binn\SqlLocalDB.exe')) then
    Result := ExpandConstant('{pf}\Microsoft SQL Server\130\Tools\Binn\SqlLocalDB.exe')
  else if FileExists(ExpandConstant('{pf}\Microsoft SQL Server\120\Tools\Binn\SqlLocalDB.exe')) then
    Result := ExpandConstant('{pf}\Microsoft SQL Server\120\Tools\Binn\SqlLocalDB.exe')
  else if FileExists(ExpandConstant('{pf}\Microsoft SQL Server\110\Tools\Binn\SqlLocalDB.exe')) then
    Result := ExpandConstant('{pf}\Microsoft SQL Server\110\Tools\Binn\SqlLocalDB.exe')
  else
    Result := '';
end;

function FindSqlLocalDbExe2017: string;
begin
  if FileExists(ExpandConstant('{pf}\Microsoft SQL Server\140\Tools\Binn\SqlLocalDB.exe')) then
    Result := ExpandConstant('{pf}\Microsoft SQL Server\140\Tools\Binn\SqlLocalDB.exe')
  else
    Result := '';
end;

function LocalDbStartSucceeded(const ExitCode: Integer): Boolean;
begin
  { sqllocaldb start returns 0 on success; some builds also succeed with already-running. }
  Result := (ExitCode = 0);
end;

function TryStartLocalDbInstance(const ExePath: string; const Prefer2017Create: Boolean): Boolean;
var
  ResultCode: Integer;
begin
  Result := False;
  if ExePath = '' then
    Exit;

  if Prefer2017Create then
    Exec(ExePath, 'create MSSQLLocalDB 14.0', '', SW_HIDE, ewWaitUntilTerminated, ResultCode)
  else
    Exec(ExePath, 'create MSSQLLocalDB', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

  if not Exec(ExePath, 'start MSSQLLocalDB', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    Log('Failed to launch SqlLocalDB start via: ' + ExePath);
    Exit;
  end;

  Log('sqllocaldb start exit code=' + IntToStr(ResultCode) + ' via ' + ExePath);
  Result := LocalDbStartSucceeded(ResultCode);
end;

function LocalDbIsHealthy: Boolean;
var
  Exe2017: string;
  ExePath: string;
begin
  Result := False;

  Exe2017 := FindSqlLocalDbExe2017;
  if Exe2017 <> '' then
  begin
    { Prefer SQL 2017 tools when present (what Qayd ships). Do not force version on newer-only boxes. }
    if TryStartLocalDbInstance(Exe2017, True) then
    begin
      Result := True;
      Exit;
    end;
  end;

  ExePath := FindSqlLocalDbExe;
  if ExePath = '' then
  begin
    Log('LocalDB health check: SqlLocalDB.exe not found.');
    Exit;
  end;

  { Newer LocalDB only — never force create ... 14.0 }
  if TryStartLocalDbInstance(ExePath, False) then
  begin
    Result := True;
    Exit;
  end;

  Log('LocalDB health check failed for: ' + ExePath);
end;

procedure StartLocalDbInstance;
begin
  { Best-effort start after MSI; health is verified separately. }
  if FindSqlLocalDbExe2017 <> '' then
    TryStartLocalDbInstance(FindSqlLocalDbExe2017, True)
  else if FindSqlLocalDbExe <> '' then
    TryStartLocalDbInstance(FindSqlLocalDbExe, False);
  Sleep(1500);
end;

function RunVcRedist(const FileName: string; const LabelText: string): Boolean;
var
  ResultCode: Integer;
  InstallerPath: string;
begin
  Result := True;
  if not EnsureTempPrereq(FileName) then
  begin
    Log('Missing VC++ payload: ' + FileName);
    Exit;
  end;

  InstallerPath := ExpandConstant('{tmp}\' + FileName);
  WizardForm.StatusLabel.Caption := LabelText;
  { Always run — upgrades/repairs existing installs. }
  if not Exec(InstallerPath, '/install /quiet /norestart', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    Log('Failed to launch VC++ installer: ' + FileName);
    Result := False;
    Exit;
  end;

  Log('VC++ ' + FileName + ' exit code=' + IntToStr(ResultCode));
  if not IsSuccessExitCode(ResultCode) then
  begin
    { Non-fatal for already-broken machines; LocalDB step may still succeed. }
    Log('VC++ returned non-success code ' + IntToStr(ResultCode) + ' for ' + FileName);
    Result := False;
  end;
end;

function InstallVcRedists: Boolean;
begin
  WizardForm.StatusLabel.Caption := 'جاري تثبيت Visual C++ Redistributable (مطلوب لـ LocalDB)...';
  RunVcRedist('{#VcRedistX64File}', 'جاري تثبيت Visual C++ x64...');
  RunVcRedist('{#VcRedistX86File}', 'جاري تثبيت Visual C++ x86...');
  { Brief pause so WinSxS registration settles before MSI. }
  Sleep(2000);
  Result := True;
end;

function CopyLogToDocs(const SourceLog: string): string;
var
  DestDir, DestFile: string;
begin
  Result := SourceLog;
  DestDir := ExpandConstant('{userdocs}\قيد');
  ForceDirectories(DestDir);
  DestFile := DestDir + '\Qayd-SqlLocalDB-Install.log';
  if FileExists(SourceLog) then
  begin
    if CopyFile(SourceLog, DestFile, False) then
      Result := DestFile;
  end;
end;

function TryInstallLocalDbMsi(const UiSwitch: string; const LogPath: string; const RepairMode: Boolean; var ResultCode: Integer): Boolean;
var
  InstallerPath: string;
  Params: string;
begin
  InstallerPath := ExpandConstant('{tmp}\{#LocalDbMsiFile}');
  { Include both common license property spellings used across LocalDB MSI versions. }
  if RepairMode then
    Params := '/faveums "' + InstallerPath + '" ' + UiSwitch + ' /norestart ' +
              'IACCEPTSQLLOCALDBLICENSETERMS=YES IAcceptSqlLocalDBLicenseTerms=YES ' +
              '/L*v "' + LogPath + '"'
  else
    Params := '/i "' + InstallerPath + '" ' + UiSwitch + ' /norestart ' +
              'IACCEPTSQLLOCALDBLICENSETERMS=YES IAcceptSqlLocalDBLicenseTerms=YES ' +
              '/L*v "' + LogPath + '"';
  Result := Exec(ExpandConstant('{sys}\msiexec.exe'), Params, '', SW_SHOW, ewWaitUntilTerminated, ResultCode);
end;

function RunLocalDbMsiInstall(const RepairMode: Boolean): Boolean;
var
  LogPath: string;
  SavedLog: string;
  ResultCode: Integer;
  StatusPrefix: string;
begin
  Result := False;
  LastLocalDbMsiExitCode := -1;

  if not EnsureTempPrereq('{#LocalDbMsiFile}') then
  begin
    LocalDbWarning := 'ملف SqlLocalDB.msi غير مضمّن في المثبت.';
    Exit;
  end;

  LogPath := ExpandConstant('{tmp}\Qayd-SqlLocalDB-Install.log');
  if RepairMode then
    StatusPrefix := 'إصلاح'
  else
    StatusPrefix := 'تثبيت';

  { 1) Quiet first — embedded package is SQL Server 2017 LocalDB (14.x) }
  WizardForm.StatusLabel.Caption := 'جاري ' + StatusPrefix + ' SQL Server 2017 LocalDB (صامت)...';
  if not TryInstallLocalDbMsi('/qn', LogPath, RepairMode, ResultCode) then
  begin
    LocalDbWarning := 'تعذر تشغيل msiexec لـ LocalDB.';
    Exit;
  end;
  LastLocalDbMsiExitCode := ResultCode;
  Log('LocalDB /qn exit code=' + IntToStr(ResultCode) + ' repair=' + IntToStr(Ord(RepairMode)));

  { 2) Retry with basic UI — more reliable on some Windows 10 machines }
  if not IsSuccessExitCode(ResultCode) then
  begin
    WizardForm.StatusLabel.Caption := 'إعادة محاولة ' + StatusPrefix + ' SQL Server 2017 LocalDB...';
    if not TryInstallLocalDbMsi('/qb!', LogPath, RepairMode, ResultCode) then
    begin
      LocalDbWarning := 'تعذر تشغيل msiexec (المحاولة الثانية).';
      Exit;
    end;
    LastLocalDbMsiExitCode := ResultCode;
    Log('LocalDB /qb! exit code=' + IntToStr(ResultCode) + ' repair=' + IntToStr(Ord(RepairMode)));
  end;

  SavedLog := CopyLogToDocs(LogPath);

  if not IsSuccessExitCode(ResultCode) then
  begin
    LocalDbWarning :=
      'فشل ' + StatusPrefix + ' SQL Server LocalDB (رمز ' + IntToStr(ResultCode) + ').' + #13#10 +
      'السجل: ' + SavedLog + #13#10 +
      'يمكنك إكمال تثبيت قيد واختيار سيرفر SQL من معالج الإعداد عند أول تشغيل.';
    Exit;
  end;

  if IsRebootExitCode(ResultCode) then
  begin
    LocalDbNeedsRestart := True;
    LocalDbWarning :=
      'تم ' + StatusPrefix + ' SQL Server LocalDB بنجاح، لكن الجهاز يحتاج إعادة تشغيل' + #13#10 +
      'قبل تشغيل قيد لأول مرة.' + #13#10 +
      'السجل: ' + SavedLog;
  end;

  Sleep(1500);
  StartLocalDbInstance;

  if LocalDbIsHealthy then
  begin
    Result := True;
    Exit;
  end;

  if LocalDbNeedsRestart then
  begin
    { Reboot pending — treat install as OK but block launch. }
    Result := True;
    Exit;
  end;

  LocalDbWarning :=
    'اكتمل ' + StatusPrefix + ' LocalDB، لكن التحقق من التشغيل فشل.' + #13#10 +
    'يُفضّل إعادة تشغيل الجهاز ثم فتح قيد.' + #13#10 +
    'السجل: ' + SavedLog;
  Result := True;
end;

function InstallLocalDb: Boolean;
var
  HadBrokenLocalDb: Boolean;
begin
  LocalDbWarning := '';
  LocalDbNeedsRestart := False;
  LastLocalDbMsiExitCode := 0;
  HadBrokenLocalDb := False;

  { Skip MSI only when LocalDB is healthy. Broken leftovers → repair/reinstall. }
  if LocalDbIsHealthy then
  begin
    Log('LocalDB healthy — skipping MSI (keep existing version).');
    Result := True;
    Exit;
  end;

  if LocalDbPresentInRegistryOrPath or (FindSqlLocalDbExe <> '') then
  begin
    HadBrokenLocalDb := True;
    Log('LocalDB present but unhealthy — will repair/reinstall SQL Server 2017 MSI.');
  end
  else
    Log('LocalDB not found — installing SQL Server 2017 LocalDB.');

  InstallVcRedists;

  if HadBrokenLocalDb then
  begin
    { Try repair first (does not wipe customer databases). Fall back to install. }
    if RunLocalDbMsiInstall(True) then
    begin
      Result := True;
      Exit;
    end;
    Log('LocalDB repair failed — attempting fresh MSI install.');
  end;

  Result := RunLocalDbMsiInstall(False);
end;

function CanLaunchAfterInstall: Boolean;
begin
  Result := not LocalDbNeedsRestart;
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
  LocalDbWarning := '';
  LocalDbNeedsRestart := False;
  LastLocalDbMsiExitCode := 0;
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
  WizardForm.StatusLabel.Caption := 'جاري تجهيز المتطلبات (Visual C++ و LocalDB)...';

  if not InstallLocalDb then
  begin
    { Soft-fail: allow Qayd install to continue. First-run wizard can pick SQL Express/other. }
    if LocalDbWarning = '' then
      LocalDbWarning := 'تعذر تثبيت SQL Server LocalDB تلقائياً.';
    MsgBox(
      LocalDbWarning + #13#10 + #13#10 +
      'سيتم متابعة تثبيت برنامج قيد.' + #13#10 +
      'عند أول تشغيل يمكنك اختيار سيرفر SQL من معالج الإعداد، أو تثبيت LocalDB يدوياً ثم إعادة المحاولة.',
      mbInformation, MB_OK);
  end
  else if LocalDbWarning <> '' then
    MsgBox(LocalDbWarning, mbInformation, MB_OK);

  if LocalDbNeedsRestart then
    NeedsRestart := True;
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
