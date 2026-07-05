using AlMuhasib.Core.Models.Ux;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IRecentExcelExportService
{
    event Action? ExportsChanged;
    void RecordExport(string filePath, string sheetName);
    IReadOnlyList<RecentExcelExportEntry> GetRecent(int count = 50);
    void Remove(string id);
    void Clear();
}
