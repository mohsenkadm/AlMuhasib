using AlMuhasib.Core.Models;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IPlatformDeductionExcelService
{
    IReadOnlyList<PlatformDeductionImportRow> ParseImportFile(string filePath);
}
