using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IMaintenanceService
{
    Task<MaintenanceRecord?> GetByIdAsync(int id);
    Task<IReadOnlyList<MaintenanceRecord>> GetRecentAsync(int take = 50);
    Task<MaintenanceRecord> SaveAsync(MaintenanceRecord record);
    Task DeleteAsync(int id);
    Task<string> GetNextRecordNumberAsync();
}
