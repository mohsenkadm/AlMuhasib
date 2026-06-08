using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IUserTaskService
{
    Task<IReadOnlyList<UserTask>> GetAllAsync();
    Task<UserTask> CreateAsync(string title, string? details, DateTime? dueDate, UserTaskStatus status, int assignedToUserId);
    Task UpdateAsync(int id, string title, string? details, DateTime? dueDate, UserTaskStatus status);
    Task UpdateStatusAsync(int id, UserTaskStatus status);
    Task DeleteAsync(int id);
    Task<int> GetPendingCountAsync();
}
