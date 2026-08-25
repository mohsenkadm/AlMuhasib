using AlMuhasib.Core.Models.Gold;

namespace AlMuhasib.Core.Interfaces.Services.Gold;

public interface IGoldItemsExcelService
{
    byte[] GenerateTemplate();
    IReadOnlyList<GoldItemsImportRow> ParseImportFile(string filePath);
}
