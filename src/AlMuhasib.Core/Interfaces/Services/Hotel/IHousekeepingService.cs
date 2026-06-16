using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Core.Models.Hotel;

namespace AlMuhasib.Core.Interfaces.Services.Hotel;

public interface IHousekeepingService
{
    Task<IReadOnlyList<HousekeepingTask>> GetTasksAsync(
        HousekeepingTaskFilter? filter = null,
        CancellationToken cancellationToken = default);

    Task<HousekeepingTask?> GetTaskByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<HousekeepingTask> CreateTaskAsync(HousekeepingTask task, CancellationToken cancellationToken = default);
    Task<HousekeepingTask> UpdateTaskAsync(HousekeepingTask task, CancellationToken cancellationToken = default);
    Task DeleteTaskAsync(int id, string deletedBy, CancellationToken cancellationToken = default);
}
