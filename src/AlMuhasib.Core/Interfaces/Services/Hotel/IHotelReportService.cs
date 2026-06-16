using AlMuhasib.Core.Models.Hotel;

namespace AlMuhasib.Core.Interfaces.Services.Hotel;

public interface IHotelReportService
{
    Task<OccupancyReportData> GetOccupancyReportAsync(
        HotelReportFilter filter,
        CancellationToken cancellationToken = default);

    Task<RevenueReportData> GetRevenueReportAsync(
        HotelReportFilter filter,
        CancellationToken cancellationToken = default);

    Task<NightAuditReportData> GetNightAuditReportAsync(
        DateTime auditDate,
        CancellationToken cancellationToken = default);
}
