namespace AlMuhasib.Core.Models.Ux;

public sealed class RecentExcelExportEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string SheetName { get; set; } = string.Empty;
    public DateTime ExportedAt { get; set; } = DateTime.Now;
}
