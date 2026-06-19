using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels;

public partial class BackupRestoreViewModel : ViewModelBase
{
    private readonly IBackupService _backupService;
    private readonly IUserPreferencesService _preferences;
    private readonly ICurrentUserService _currentUserService;

    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isOperationInProgress;
    [ObservableProperty] private bool _isSuccess;
    [ObservableProperty] private bool _isError;
    [ObservableProperty] private string _lastBackupPath = string.Empty;
    [ObservableProperty] private double _progressValue;
    [ObservableProperty] private string _selectedRestoreFile = string.Empty;
    [ObservableProperty] private string _backupFolder = string.Empty;
    [ObservableProperty] private bool _autoBackupEnabled;
    [ObservableProperty] private BackupSchedule _backupSchedule = BackupSchedule.Daily;
    [ObservableProperty] private int _retainCount = 7;

    public ObservableCollection<string> RecentBackups { get; } = [];

    public BackupRestoreViewModel(
        IBackupService backupService,
        IUserPreferencesService preferences,
        ICurrentUserService currentUserService)
    {
        _backupService = backupService;
        _preferences = preferences;
        _currentUserService = currentUserService;
        PageTitle = "النسخ الاحتياطي والاستعادة";
    }

    public override Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Backup");
        BackupFolder = _preferences.Current.Backup.BackupFolderPath
            ?? _backupService.GetDefaultBackupDirectory();
        AutoBackupEnabled = _preferences.Current.Backup.AutoBackupEnabled;
        BackupSchedule = _preferences.Current.Backup.Schedule;
        RetainCount = _preferences.Current.Backup.RetainCount <= 0 ? 7 : _preferences.Current.Backup.RetainCount;
        RefreshBackupList();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task BackupToFolderAsync()
    {
        var dialog = new SaveFileDialog
        {
            Title = "حفظ النسخة الاحتياطية",
            Filter = "Backup Files (*.bak)|*.bak",
            FileName = $"AlMuhasib_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak",
            InitialDirectory = BackupFolder
        };

        if (dialog.ShowDialog() != true)
            return;

        await PerformBackupAsync(dialog.FileName);
    }

    [RelayCommand]
    private async Task BackupToDefaultFolderAsync()
    {
        var defaultDir = _backupService.GetDefaultBackupDirectory();
        BackupFolder = defaultDir;
        var fileName = $"AlMuhasib_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
        await PerformBackupAsync(Path.Combine(defaultDir, fileName));
    }

    [RelayCommand]
    private async Task BackupAndShareAsync()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "AlMuhasib_Backup");
        Directory.CreateDirectory(tempDir);

        var fullPath = Path.Combine(tempDir, $"AlMuhasib_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak");
        if (!await PerformBackupAsync(fullPath))
            return;

        try
        {
            var explorerPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
            Process.Start(new ProcessStartInfo
            {
                FileName = explorerPath,
                Arguments = $"/select,\"{fullPath}\"",
                UseShellExecute = false
            });
            SetSuccess("تم فتح مجلد النسخة الاحتياطية. يمكنك مشاركة الملف من هنا.");
        }
        catch (Exception ex)
        {
            SetSuccess($"تم إنشاء النسخة بنجاح في:\n{fullPath}\n(لم يتم فتح المجلد: {ex.Message})");
        }
    }

    [RelayCommand]
    private async Task BackupToOneDriveAsync()
    {
        var oneDrivePath = Environment.GetEnvironmentVariable("OneDrive")
            ?? Environment.GetEnvironmentVariable("OneDriveConsumer")
            ?? Environment.GetEnvironmentVariable("OneDriveCommercial");

        if (string.IsNullOrEmpty(oneDrivePath) || !Directory.Exists(oneDrivePath))
        {
            SetError("لم يتم العثور على مجلد OneDrive. تأكد من تثبيت OneDrive وتسجيل الدخول.");
            return;
        }

        var backupDir = Path.Combine(oneDrivePath, "AlMuhasib_Backups");
        Directory.CreateDirectory(backupDir);
        var fullPath = Path.Combine(backupDir, $"AlMuhasib_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak");
        await PerformBackupAsync(fullPath);
    }

    [RelayCommand]
    private void BrowseRestoreFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "اختر ملف النسخة الاحتياطية",
            Filter = "Backup Files (*.bak)|*.bak|All Files (*.*)|*.*",
            InitialDirectory = Directory.Exists(BackupFolder) ? BackupFolder : _backupService.GetDefaultBackupDirectory()
        };

        if (dialog.ShowDialog() == true)
            SelectedRestoreFile = dialog.FileName;
    }

    [RelayCommand]
    private void BrowseBackupFolder()
    {
        var dlg = new OpenFolderDialog
        {
            Title = "مجلد النسخ الاحتياطي الافتراضي",
            InitialDirectory = BackupFolder
        };

        if (dlg.ShowDialog() == true)
        {
            BackupFolder = dlg.FolderName;
            RefreshBackupList();
        }
    }

    [RelayCommand]
    private async Task RestoreDatabaseAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedRestoreFile))
        {
            SetError("الرجاء اختيار ملف النسخة الاحتياطية أولاً.");
            return;
        }

        if (!File.Exists(SelectedRestoreFile))
        {
            SetError("ملف النسخة الاحتياطية غير موجود.");
            return;
        }

        if (!BeautifulMessageDialog.ShowConfirm(
                "هل أنت متأكد من استعادة قاعدة البيانات؟\n\nسيتم استبدال جميع البيانات الحالية بالبيانات من النسخة الاحتياطية.\nسيتم إعادة تشغيل البرنامج بعد الاستعادة.",
                "تأكيد الاستعادة"))
            return;

        ResetStatus();
        IsOperationInProgress = true;
        ProgressValue = 0;
        StatusMessage = "جاري استعادة قاعدة البيانات...";

        try
        {
            ProgressValue = 30;
            await _backupService.RestoreDatabaseAsync(SelectedRestoreFile);
            ProgressValue = 100;
            SetSuccess("تمت الاستعادة بنجاح! سيتم إعادة تشغيل البرنامج الآن...");
            await Task.Delay(1500);
            RestartApplication();
        }
        catch (Exception ex)
        {
            SetError($"فشل في استعادة قاعدة البيانات:\n{ex.Message}");
        }
        finally
        {
            IsOperationInProgress = false;
        }
    }

    [RelayCommand]
    private void SelectRecentBackup(string? file)
    {
        if (!string.IsNullOrWhiteSpace(file))
            SelectedRestoreFile = file;
    }

    [RelayCommand]
    private void SaveScheduleSettings()
    {
        _preferences.Update(p =>
        {
            p.Backup.AutoBackupEnabled = AutoBackupEnabled;
            p.Backup.Schedule = BackupSchedule;
            p.Backup.BackupFolderPath = BackupFolder;
            p.Backup.RetainCount = RetainCount;
        });
        SetSuccess("تم حفظ إعدادات النسخ التلقائي");
    }

    private async Task<bool> PerformBackupAsync(string path)
    {
        ResetStatus();
        IsOperationInProgress = true;
        ProgressValue = 0;
        StatusMessage = "جاري إنشاء النسخة الاحتياطية...";

        try
        {
            ProgressValue = 20;
            var resultPath = await _backupService.BackupDatabaseAsync(path);
            ProgressValue = 100;
            LastBackupPath = resultPath;
            BackupFolder = Path.GetDirectoryName(resultPath) ?? BackupFolder;
            _preferences.Update(p => p.Backup.BackupFolderPath = BackupFolder);
            RefreshBackupList();
            SetSuccess($"تم إنشاء النسخة الاحتياطية بنجاح!\n{resultPath}");
            return true;
        }
        catch (Exception ex)
        {
            SetError($"فشل في إنشاء النسخة الاحتياطية:\n{ex.Message}");
            return false;
        }
        finally
        {
            IsOperationInProgress = false;
        }
    }

    private void RefreshBackupList()
    {
        RecentBackups.Clear();
        var folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (Directory.Exists(BackupFolder))
            folders.Add(BackupFolder);
        folders.Add(_backupService.GetDefaultBackupDirectory());

        foreach (var folder in folders)
        {
            if (!Directory.Exists(folder))
                continue;

            foreach (var file in Directory.GetFiles(folder, "*.bak").OrderByDescending(f => f))
            {
                if (!RecentBackups.Contains(file))
                    RecentBackups.Add(file);
            }
        }

        if (RecentBackups.Count > 0 && string.IsNullOrWhiteSpace(SelectedRestoreFile))
            SelectedRestoreFile = RecentBackups[0];
    }

    private void SetSuccess(string message)
    {
        IsSuccess = true;
        IsError = false;
        StatusMessage = message;
    }

    private void SetError(string message)
    {
        IsSuccess = false;
        IsError = true;
        StatusMessage = message;
    }

    private void ResetStatus()
    {
        IsSuccess = false;
        IsError = false;
        StatusMessage = string.Empty;
        ProgressValue = 0;
    }

    private static void RestartApplication()
    {
        var exePath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(exePath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true
            });
        }

        Application.Current.Shutdown();
    }
}
