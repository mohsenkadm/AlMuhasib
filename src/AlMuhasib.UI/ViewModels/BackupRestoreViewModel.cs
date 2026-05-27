using System.Diagnostics;
using System.IO;
using System.Windows;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
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

    public bool CanOpenLastBackup => !string.IsNullOrWhiteSpace(LastBackupPath) && File.Exists(LastBackupPath);

    public BackupRestoreViewModel(IBackupService backupService, ICurrentUserService currentUserService)
    {
        _backupService = backupService;
        _currentUserService = currentUserService;
        PageTitle = "النسخ الاحتياطي والاستعادة";
        LoadPermissions(currentUserService, "Backup");
    }

    partial void OnLastBackupPathChanged(string value) => OnPropertyChanged(nameof(CanOpenLastBackup));

    /// <summary>اختيار مكان الحفظ يدوياً (موصى به).</summary>
    [RelayCommand]
    private async Task BackupToFolder()
    {
        var dialog = CreateBackupSaveDialog();
        if (dialog.ShowDialog() != true) return;
        await PerformBackup(dialog.FileName);
    }

    /// <summary>حفظ تلقائي على سطح المكتب (بدون ضغط — متوافق مع SQL Express).</summary>
    [RelayCommand]
    private async Task BackupToDesktop()
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (!Directory.Exists(desktop))
        {
            SetError("تعذر الوصول إلى سطح المكتب. استخدم «حفظ في مجلد» واختر مساراً آخر.");
            return;
        }

        var fullPath = Path.Combine(desktop, $"AlMuhasib_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak");
        await PerformBackup(fullPath);
    }

    [RelayCommand]
    private async Task BackupAndShare()
    {
        var dialog = CreateBackupSaveDialog();
        if (dialog.ShowDialog() != true) return;

        var success = await PerformBackup(dialog.FileName);
        if (!success) return;

        OpenBackupInExplorer(dialog.FileName);
        StatusMessage = "تم إنشاء النسخة. يمكنك نسخ الملف أو مشاركته من المجلد الذي فُتح.";
    }

    [RelayCommand]
    private async Task BackupToOneDrive()
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
        await PerformBackup(fullPath);
    }

    [RelayCommand]
    private void OpenLastBackupFolder()
    {
        if (CanOpenLastBackup)
            OpenBackupInExplorer(LastBackupPath);
    }

    [RelayCommand]
    private void BrowseRestoreFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "اختر ملف النسخة الاحتياطية",
            Filter = "ملف النسخ الاحتياطي (*.bak)|*.bak|All Files (*.*)|*.*",
            InitialDirectory = GetBackupDialogInitialDirectory()
        };

        if (dialog.ShowDialog() == true)
            SelectedRestoreFile = dialog.FileName;
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
            OpenBackupInExplorer(resultPath);
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

    private static SaveFileDialog CreateBackupSaveDialog() => new()
    {
        Title = "حفظ النسخة الاحتياطية",
        Filter = "ملف النسخ الاحتياطي (*.bak)|*.bak",
        FileName = $"AlMuhasib_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak",
        InitialDirectory = GetBackupDialogInitialDirectory(),
        AddExtension = true,
        DefaultExt = "bak",
        OverwritePrompt = true
    };

    private static string GetBackupDialogInitialDirectory()
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (Directory.Exists(desktop))
            return desktop;

        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Directory.Exists(documents) ? documents : @"D:\";
    }

    private static void OpenBackupInExplorer(string fullPath)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{fullPath}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Backup] Could not open explorer: {ex}");
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
