using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;
using AlMuhasib.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class MainWindowViewModel
{
    private readonly IRecentExcelExportService _recentExcelExport = null!;

    [ObservableProperty] private bool _isOpenRecentExcelPanelOpen;
    [ObservableProperty] private bool _hasRecentExcelExports;
    [ObservableProperty] private int _recentExcelExportCount;
    [ObservableProperty] private string _recentExcelExportBadgeText = "0";

    public ObservableCollection<RecentExcelExportItem> RecentExcelExports { get; } = [];

    partial void OnRecentExcelExportCountChanged(int value) =>
        RecentExcelExportBadgeText = value > 99 ? "99+" : value.ToString();

    private void OnRecentExcelExportsChanged()
    {
        if (Application.Current?.Dispatcher.CheckAccess() == true)
            RefreshRecentExcelExports();
        else
            Application.Current?.Dispatcher.Invoke(RefreshRecentExcelExports);
    }

    partial void OnIsOpenRecentExcelPanelOpenChanged(bool value)
    {
        if (value)
            RefreshRecentExcelExports();
    }

    [RelayCommand]
    private void ToggleOpenRecentExcelPanel()
    {
        if (IsOpenRecentExcelPanelOpen)
        {
            IsOpenRecentExcelPanelOpen = false;
            return;
        }

        CloseOtherPanelsForOpenRecent();
        IsOpenRecentExcelPanelOpen = true;
    }

    [RelayCommand]
    private void CloseOpenRecentExcelPanel() => IsOpenRecentExcelPanelOpen = false;

    [RelayCommand]
    private void RefreshRecentExcelExports()
    {
        RecentExcelExports.Clear();

        foreach (var entry in _recentExcelExport.GetRecent(50))
            RecentExcelExports.Add(MapRecentExcelExportItem(entry));

        HasRecentExcelExports = RecentExcelExports.Count > 0;
        RecentExcelExportCount = RecentExcelExports.Count;
    }

    [RelayCommand]
    private void OpenRecentExcelFile(RecentExcelExportItem? item)
    {
        if (item is null)
            return;

        if (!File.Exists(item.FilePath))
        {
            item.FileExists = false;
            _toast.ShowWarning("الملف غير موجود في المسار المحفوظ");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = item.FilePath,
                UseShellExecute = true
            });
            _toast.ShowSuccess($"تم فتح {item.FileName}");
        }
        catch (Exception ex)
        {
            _toast.ShowError($"تعذّر فتح الملف: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenRecentExcelFolder(RecentExcelExportItem? item)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.FolderPath))
            return;

        if (!Directory.Exists(item.FolderPath))
        {
            item.FileExists = false;
            _toast.ShowWarning("المجلد غير موجود");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{item.FilePath}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _toast.ShowError($"تعذّر فتح المجلد: {ex.Message}");
        }
    }

    [RelayCommand]
    private void RemoveRecentExcelExport(RecentExcelExportItem? item)
    {
        if (item is null)
            return;

        _recentExcelExport.Remove(item.Id);
        RecentExcelExports.Remove(item);
        HasRecentExcelExports = RecentExcelExports.Count > 0;
        RecentExcelExportCount = RecentExcelExports.Count;
        _toast.ShowSuccess("تمت إزالة الملف من القائمة");
    }

    [RelayCommand]
    private void ClearRecentExcelExports()
    {
        _recentExcelExport.Clear();
        RecentExcelExports.Clear();
        HasRecentExcelExports = false;
        RecentExcelExportCount = 0;
        _toast.ShowSuccess("تم مسح سجل ملفات Excel");
    }

    private void CloseOtherPanelsForOpenRecent()
    {
        IsNotificationPanelOpen = false;
        IsTasksPanelOpen = false;
        IsNotesPanelOpen = false;
        IsQuickAssistOpen = false;
        IsSmartAssistantOpen = false;
        IsGlobalSearchOpen = false;
        IsMenuCustomizerOpen = false;
        IsRecentActivityOpen = false;
        IsVoiceAssistantOpen = false;
    }

    private static RecentExcelExportItem MapRecentExcelExportItem(RecentExcelExportEntry entry)
    {
        var exists = File.Exists(entry.FilePath);
        return new RecentExcelExportItem
        {
            Id = entry.Id,
            FilePath = entry.FilePath,
            FileName = entry.FileName,
            SheetName = entry.SheetName,
            ExportedAt = entry.ExportedAt,
            FolderPath = Path.GetDirectoryName(entry.FilePath) ?? string.Empty,
            ExportedAtDisplay = entry.ExportedAt.ToString("yyyy/MM/dd HH:mm"),
            TimeAgoDisplay = FormatTimeAgo(entry.ExportedAt),
            FileExists = exists
        };
    }

    private static string FormatTimeAgo(DateTime exportedAt)
    {
        var span = DateTime.Now - exportedAt;
        if (span.TotalMinutes < 1)
            return "الآن";
        if (span.TotalMinutes < 60)
            return $"منذ {(int)span.TotalMinutes} د";
        if (span.TotalHours < 24)
            return $"منذ {(int)span.TotalHours} س";
        if (span.TotalDays < 7)
            return $"منذ {(int)span.TotalDays} ي";
        if (span.TotalDays < 30)
            return $"منذ {(int)(span.TotalDays / 7)} أ";
        return exportedAt.ToString("yyyy/MM/dd");
    }
}
