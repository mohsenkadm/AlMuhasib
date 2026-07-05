using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.Models;

public partial class RecentExcelExportItem : ObservableObject
{
    public string Id { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string SheetName { get; init; } = string.Empty;
    public DateTime ExportedAt { get; init; }
    public string FolderPath { get; init; } = string.Empty;
    public string ExportedAtDisplay { get; init; } = string.Empty;
    public string TimeAgoDisplay { get; init; } = string.Empty;

    [ObservableProperty]
    private bool _fileExists = true;
}
