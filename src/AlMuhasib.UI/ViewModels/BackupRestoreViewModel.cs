using System.Diagnostics;
using System.IO;
using System.Windows;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.ViewModels;

public partial class BackupRestoreViewModel : ViewModelBase
{
    private readonly IBackupService _backupService;
    private readonly ICurrentUserService _currentUserService;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isOperationInProgress;

    [ObservableProperty]
    private bool _isSuccess;

    [ObservableProperty]
    private bool _isError;

    [ObservableProperty]
    private string _lastBackupPath = string.Empty;

    [ObservableProperty]
    private double _progressValue;

    [ObservableProperty]
    private string _selectedRestoreFile = string.Empty;

    public BackupRestoreViewModel(IBackupService backupService, ICurrentUserService currentUserService)
    {
        _backupService = backupService;
        _currentUserService = currentUserService;
        PageTitle = "النسخ الاحتياطي والاستعادة";
        LoadPermissions(currentUserService, "Backup");
    }

    [RelayCommand]
    private async Task BackupToFolder()
    {
        var dialog = new SaveFileDialog
        {
            Title = "حفظ النسخة الاحتياطية",
            Filter = "Backup Files (*.bak)|*.bak",
            FileName = $"AlMuhasib_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak",
            InitialDirectory = _backupService.GetDefaultBackupDirectory()
        };

        if (dialog.ShowDialog() != true) return;

        await PerformBackup(dialog.FileName);
    }

    [RelayCommand]
    private async Task BackupToDefaultFolder()
    {
        var defaultDir = _backupService.GetDefaultBackupDirectory();
        var fileName = $"AlMuhasib_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
        var fullPath = Path.Combine(defaultDir, fileName);

        await PerformBackup(fullPath);
    }

    [RelayCommand]
    private async Task BackupAndShare()
    {
        // First backup to temp location
        var tempDir = Path.Combine(Path.GetTempPath(), "AlMuhasib_Backup");
        if (!Directory.Exists(tempDir))
            Directory.CreateDirectory(tempDir);

        var fileName = $"AlMuhasib_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
        var fullPath = Path.Combine(tempDir, fileName);

        var success = await PerformBackup(fullPath);
        if (!success) return;

        // Open Windows share dialog
        try
        {
            var explorerPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
            Process.Start(new ProcessStartInfo
            {
                FileName = explorerPath,
                Arguments = $"/select,\"{fullPath}\"",
                UseShellExecute = false
            });

            StatusMessage = "تم فتح مجلد النسخة الاحتياطية. يمكنك مشاركة الملف من هنا.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"تم إنشاء النسخة بنجاح في: {fullPath}\n(لم يتم فتح المجلد: {ex.Message})";
        }
    }

    [RelayCommand]
    private async Task BackupToOneDrive()
    {
        // Find OneDrive folder
        var oneDrivePath = Environment.GetEnvironmentVariable("OneDrive")
            ?? Environment.GetEnvironmentVariable("OneDriveConsumer")
            ?? Environment.GetEnvironmentVariable("OneDriveCommercial");

        if (string.IsNullOrEmpty(oneDrivePath) || !Directory.Exists(oneDrivePath))
        {
            SetError("لم يتم العثور على مجلد OneDrive. تأكد من تثبيت OneDrive وتسجيل الدخول.");
            return;
        }

        var backupDir = Path.Combine(oneDrivePath, "AlMuhasib_Backups");
        if (!Directory.Exists(backupDir))
            Directory.CreateDirectory(backupDir);

        var fileName = $"AlMuhasib_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
        var fullPath = Path.Combine(backupDir, fileName);

        await PerformBackup(fullPath);
    }

    [RelayCommand]
    private void BrowseRestoreFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "اختر ملف النسخة الاحتياطية",
            Filter = "Backup Files (*.bak)|*.bak|All Files (*.*)|*.*",
            InitialDirectory = _backupService.GetDefaultBackupDirectory()
        };

        if (dialog.ShowDialog() == true)
        {
            SelectedRestoreFile = dialog.FileName;
        }
    }

    [RelayCommand]
    private async Task RestoreDatabase()
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

var confirmed = BeautifulMessageDialog.ShowConfirm(
                "هل أنت متأكد من استعادة قاعدة البيانات؟\n\nسيتم استبدال جميع البيانات الحالية بالبيانات من النسخة الاحتياطية.\nسيتم إعادة تشغيل البرنامج بعد الاستعادة.");

            if (!confirmed) return;

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

            // Restart the application
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

    private async Task<bool> PerformBackup(string path)
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
