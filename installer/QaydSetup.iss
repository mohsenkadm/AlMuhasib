; Qayd (قيد) — single-file Windows installer
; Build with: scripts\build-installer.ps1

#define AppVersion ExtractFileVersion(AddBackslash(SourcePath) + "AlMuhasib.exe")

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
RightToLeft=yes
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
Source: "prerequisites\*"; DestDir: "{tmp}\qayd-prereqs"; Flags: deleteafterinstall; Check: PrereqPayloadExists

[Icons]
Name: "{group}\قيد"; Filename: "{app}\AlMuhasib.exe"; IconFilename: "{app}\qayd-icon.ico"
Name: "{autodesktop}\قيد"; Filename: "{app}\AlMuhasib.exe"; IconFilename: "{app}\qayd-icon.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\AlMuhasib.exe"; Description: "تشغيل قيد"; Flags: nowait postinstall skipifsilent; Tasks: launchapp

[Code]
var
  DataDirPage: TInputDirWizardPage;
  SelectedDataDir: string;

function PrereqPayloadExists: Boolean;
begin
  Result := DirExists(ExpandConstant('{src}\prerequisites'));
end;

function JsonEscapePath(const S: string): string;
begin
  Result := S;
  StringChange(Result, '\', '\\');
end;

function DotNetDesktop10Installed: Boolean;
var
  ResultCode: Integer;
begin
  if RegKeyExists(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App\10.0') then
  begin
    Result := True;
    Exit;
  end;

  if Exec(ExpandConstant('{cmd}'), '/c dotnet --list-runtimes | findstr /C:"Microsoft.WindowsDesktop.App 10." >nul', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    Result := (ResultCode = 0)
  else
    Result := False;
end;

function LocalDbInstalled: Boolean;
var
  ResultCode: Integer;
begin
  if Exec(ExpandConstant('{cmd}'), '/c sqllocaldb info MSSQLLocalDB >nul 2>&1', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    Result := (ResultCode = 0)
  else
    Result := False;
end;

function FindPrereqFile(const Pattern: string): string;
var
  FindRec: TFindRec;
  SearchPath: string;
begin
  Result := '';
  SearchPath := ExpandConstant('{tmp}\qayd-prereqs\' + Pattern);
  if FindFirst(SearchPath, FindRec) then
  begin
  try
    repeat
      if (FindRec.Name <> '.') and (FindRec.Name <> '..') then
      begin
        Result := ExpandConstant('{tmp}\qayd-prereqs\' + FindRec.Name);
        Exit;
      end;
    until not FindNext(FindRec);
  finally
    FindClose(FindRec);
  end;
  end;
end;

function InstallDotNetDesktop: Boolean;
var
  InstallerPath: string;
  ResultCode: Integer;
begin
  if DotNetDesktop10Installed then
  begin
    Result := True;
    Exit;
  end;

  InstallerPath := FindPrereqFile('windowsdesktop-runtime*.exe');
  if InstallerPath = '' then
  begin
    MsgBox('لم يتم العثور على مثبت .NET 10 Desktop Runtime.' + #13#10 +
           'يرجى تثبيته يدوياً من https://dotnet.microsoft.com/download ثم إعادة تشغيل المثبت.',
           mbError, MB_OK);
    Result := False;
    Exit;
  end;

  WizardForm.StatusLabel.Caption := 'جاري تثبيت .NET 10 Desktop Runtime...';
  if not Exec(InstallerPath, '/install /quiet /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
  begin
    MsgBox('فشل تشغيل مثبت .NET.', mbError, MB_OK);
    Result := False;
    Exit;
  end;

  Result := DotNetDesktop10Installed;
  if not Result then
    MsgBox('لم يتم التحقق من تثبيت .NET 10 بنجاح. قد تحتاج لإعادة تشغيل الجهاز ثم إعادة المحاولة.', mbInformation, MB_OK);
end;

function InstallLocalDb: Boolean;
var
  InstallerPath: string;
  ResultCode: Integer;
begin
  if LocalDbInstalled then
  begin
  Exec(ExpandConstant('{cmd}'), '/c sqllocaldb start MSSQLLocalDB', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Result := True;
    Exit;
  end;

  InstallerPath := FindPrereqFile('SqlLocalDB*.msi');
  if InstallerPath = '' then
    InstallerPath := FindPrereqFile('*.msi');

  if InstallerPath = '' then
  begin
    MsgBox('لم يتم العثور على مثبت SQL Server LocalDB.' + #13#10 +
           'يرجى تثبيت SQL Server Express LocalDB ثم إعادة المحاولة.',
           mbError, MB_OK);
    Result := False;
    Exit;
  end;

  WizardForm.StatusLabel.Caption := 'جاري تثبيت SQL Server LocalDB...';
  if not Exec('msiexec.exe', '/i "' + InstallerPath + '" /qn /norestart IACCEPTSQLLOCALDBLICENSETERMS=YES', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
  begin
    MsgBox('فشل تشغيل مثبت SQL LocalDB.', mbError, MB_OK);
    Result := False;
    Exit;
  end;

  Exec(ExpandConstant('{cmd}'), '/c sqllocaldb start MSSQLLocalDB', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := LocalDbInstalled;
  if not Result then
    MsgBox('لم يتم التحقق من تثبيت SQL LocalDB بنجاح.', mbInformation, MB_OK);
end;

procedure WriteAppSettings;
var
  TemplatePath, TargetPath, Content: AnsiString;
  DataDirJson: string;
begin
  TargetPath := ExpandConstant('{app}\appsettings.json');
  if FileExists(TargetPath) then
    Exit;

  TemplatePath := ExpandConstant('{tmp}\appsettings.template.json');
  DataDirJson := JsonEscapePath(SelectedDataDir);

  if not FileExists(TemplatePath) then
    ExtractTemporaryFile('appsettings.template.json');

  if LoadStringFromFile(TemplatePath, Content) then
  begin
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

  WizardForm.StatusLabel.Caption := 'جاري التحقق من المتطلبات...';
  if not InstallDotNetDesktop then
  begin
    Result := 'تعذر تثبيت .NET 10 Desktop Runtime.';
    Exit;
  end;

  if not InstallLocalDb then
  begin
    Result := 'تعذر تثبيت SQL Server LocalDB.';
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
