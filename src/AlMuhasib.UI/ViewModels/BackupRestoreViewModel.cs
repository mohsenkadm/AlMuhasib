using System.Collections.ObjectModel;
using System.IO;
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

    [ObservableProperty] private string _backupFolder = string.Empty;
    [ObservableProperty] private string? _selectedBackupFile;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _autoBackupEnabled;
    [ObservableProperty] private BackupSchedule _backupSchedule = BackupSchedule.Daily;

    public ObservableCollection<string> RecentBackups { get; } = [];

    public BackupRestoreViewModel(
        IBackupService backupService,
        IUserPreferencesService preferences,
        ICurrentUserService currentUserService)
    {
        _backupService = backupService;
        _preferences = preferences;
        _currentUserService = currentUserService;
        PageTitle = "النسخ الاحتياطي";
    }

    public override Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Backup");
        BackupFolder = _preferences.Current.Backup.BackupFolderPath
            ?? _backupService.GetDefaultBackupDirectory();
        AutoBackupEnabled = _preferences.Current.Backup.AutoBackupEnabled;
        BackupSchedule = _preferences.Current.Backup.Schedule;
        RefreshBackupList();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void BrowseFolder()
    {
        var dlg = new OpenFolderDialog
        {
            Title = "مجلد النسخ الاحتياطي",
            InitialDirectory = BackupFolder
        };
        if (dlg.ShowDialog() == true)
        {
            BackupFolder = dlg.FolderName;
            RefreshBackupList();
        }
    }

    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "جاري إنشاء النسخة الاحتياطية...";
            var path = await _backupService.BackupDatabaseAsync(BackupFolder);
            StatusMessage = $"تم الحفظ: {path}";
            RefreshBackupList();
            BeautifulMessageDialog.ShowSuccess("تم إنشاء النسخة الاحتياطية بنجاح");
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RestoreBackupAsync()
    {
        if (string.IsNullOrEmpty(SelectedBackupFile))
        {
            BeautifulMessageDialog.ShowWarning("يرجى اختيار ملف نسخة احتياطية");
            return;
        }

        if (!BeautifulMessageDialog.ShowConfirm(
                "سيتم استبدال قاعدة البيانات الحالية وإعادة تشغيل التطبيق.\nهل تريد المتابعة؟",
                "تأكيد الاستعادة"))
            return;

        try
        {
            IsBusy = true;
            StatusMessage = "جاري الاستعادة...";
            await _backupService.RestoreDatabaseAsync(SelectedBackupFile);
            BeautifulMessageDialog.ShowSuccess("تمت الاستعادة — سيتم إعادة تشغيل التطبيق");
            System.Windows.Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SaveScheduleSettings()
    {
        _preferences.Update(p =>
        {
            p.Backup.AutoBackupEnabled = AutoBackupEnabled;
            p.Backup.Schedule = BackupSchedule;
            p.Backup.BackupFolderPath = BackupFolder;
        });
        StatusMessage = "تم حفظ إعدادات الجدولة";
        BeautifulMessageDialog.ShowSuccess("تم حفظ إعدادات النسخ التلقائي");
    }

    private void RefreshBackupList()
    {
        RecentBackups.Clear();
        if (!Directory.Exists(BackupFolder)) return;
        foreach (var file in Directory.GetFiles(BackupFolder, "*.bak").OrderByDescending(f => f))
            RecentBackups.Add(file);
        if (RecentBackups.Count > 0 && SelectedBackupFile is null)
            SelectedBackupFile = RecentBackups[0];
    }
}
