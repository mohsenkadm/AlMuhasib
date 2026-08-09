; Qayd (قيد) — Windows installer
; Build: scripts\build-installer.ps1
; App is self-contained (no separate .NET). LocalDB/VC++ are embedded and extracted
; with ExtractTemporaryFile BEFORE file copy (PrepareToInstall).

#ifndef AppVersion
  #define AppVersion "1.14.8"
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
Filename: "{app}\AlMuhasib.exe"; Description: "تشغيل قيد"; Flags: nowait postinstall skipifsilent; Tasks: launchapp

[Code]
var
  DataDirPage: TInputDirWizardPage;
  SelectedDataDir: string;
  LocalDbWarning: string;

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

function LocalDbInstalled: Boolean;
var
  ResultCode: Integer;
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

  if Exec(ExpandConstant('{cmd}'), '/c sqllocaldb info >nul 2>&1', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    Result := (ResultCode = 0)
  else
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
  else
    Result := 'sqllocaldb';
end;

procedure StartLocalDbInstance;
var
  ExePath: string;
  ResultCode: Integer;
begin
  ExePath := FindSqlLocalDbExe;
  Exec(ExePath, 'create MSSQLLocalDB', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Exec(ExePath, 'start MSSQLLocalDB', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
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

function TryInstallLocalDbMsi(const UiSwitch: string; const LogPath: string; var ResultCode: Integer): Boolean;
var
  InstallerPath: string;
  Params: string;
begin
  InstallerPath := ExpandConstant('{tmp}\{#LocalDbMsiFile}');
  { Include both common license property spellings used across LocalDB MSI versions. }
  Params := '/i "' + InstallerPath + '" ' + UiSwitch + ' /norestart ' +
            'IACCEPTSQLLOCALDBLICENSETERMS=YES IAcceptSqlLocalDBLicenseTerms=YES ' +
            '/L*v "' + LogPath + '"';
  Result := Exec(ExpandConstant('{sys}\msiexec.exe'), Params, '', SW_SHOW, ewWaitUntilTerminated, ResultCode);
end;

function InstallLocalDb: Boolean;
var
  LogPath: string;
  SavedLog: string;
  ResultCode: Integer;
begin
  LocalDbWarning := '';

  if LocalDbInstalled then
  begin
    StartLocalDbInstance;
    Result := True;
    Exit;
  end;

  InstallVcRedists;

  if not EnsureTempPrereq('{#LocalDbMsiFile}') then
  begin
    LocalDbWarning := 'ملف SqlLocalDB.msi غير مضمّن في المثبت.';
    Result := False;
    Exit;
  end;

  LogPath := ExpandConstant('{tmp}\Qayd-SqlLocalDB-Install.log');

  { 1) Quiet first }
  WizardForm.StatusLabel.Caption := 'جاري تثبيت SQL Server LocalDB (صامت)...';
  if not TryInstallLocalDbMsi('/qn', LogPath, ResultCode) then
  begin
    LocalDbWarning := 'تعذر تشغيل msiexec لتثبيت LocalDB.';
    Result := False;
    Exit;
  end;
  Log('LocalDB /qn exit code=' + IntToStr(ResultCode));

  { 2) Retry with basic UI — more reliable on some Windows 10 machines }
  if not IsSuccessExitCode(ResultCode) then
  begin
    WizardForm.StatusLabel.Caption := 'إعادة محاولة تثبيت LocalDB...';
    if not TryInstallLocalDbMsi('/qb!', LogPath, ResultCode) then
    begin
      LocalDbWarning := 'تعذر تشغيل msiexec (المحاولة الثانية).';
      Result := False;
      Exit;
    end;
    Log('LocalDB /qb! exit code=' + IntToStr(ResultCode));
  end;

  SavedLog := CopyLogToDocs(LogPath);

  if not IsSuccessExitCode(ResultCode) then
  begin
    LocalDbWarning :=
      'فشل تثبيت SQL Server LocalDB (رمز ' + IntToStr(ResultCode) + ').' + #13#10 +
      'السجل: ' + SavedLog + #13#10 +
      'يمكنك إكمال تثبيت قيد واختيار سيرفر SQL من معالج الإعداد عند أول تشغيل.';
    Result := False;
    Exit;
  end;

  Sleep(1500);
  StartLocalDbInstance;

  { MSI succeeded — do not fail the whole product install if detection lags. }
  if LocalDbInstalled then
    Result := True
  else
  begin
    LocalDbWarning :=
      'مثبت LocalDB اكتمل، لكن التحقق تأخر.' + #13#10 +
      'قد تحتاج إعادة تشغيل الجهاز ثم فتح قيد.' + #13#10 +
      'السجل: ' + SavedLog;
    Result := True;
  end;
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
