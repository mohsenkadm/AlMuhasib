using AlMuhasib.Core.Models.Ux;

namespace AlMuhasib.Core.Interfaces.Services;

public interface ISmartAlertService
{
    Task<SmartAlertSummary> GetSummaryAsync(CancellationToken cancellationToken = default);
}
