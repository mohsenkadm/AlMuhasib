using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;

namespace AlMuhasib.Infrastructure.Services;

/// <summary>
/// Stubs for accounting-only UX services still referenced by the shared shell in car contracts mode.
/// </summary>
public sealed class NoOpSmartAlertService : ISmartAlertService
{
    public Task<SmartAlertSummary> GetSummaryAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new SmartAlertSummary());
}

public sealed class NoOpUserTaskService : IUserTaskService
{
    public Task<IReadOnlyList<UserTask>> GetAllAsync() =>
        Task.FromResult<IReadOnlyList<UserTask>>(Array.Empty<UserTask>());

    public Task<UserTask> CreateAsync(string title, string? details, DateTime? dueDate, UserTaskStatus status, int assignedToUserId) =>
        Task.FromResult(new UserTask { Title = title, Details = details, DueDate = dueDate, Status = status, UserId = assignedToUserId });

    public Task UpdateAsync(int id, string title, string? details, DateTime? dueDate, UserTaskStatus status) =>
        Task.CompletedTask;

    public Task UpdateStatusAsync(int id, UserTaskStatus status) => Task.CompletedTask;

    public Task DeleteAsync(int id) => Task.CompletedTask;

    public Task<int> GetPendingCountAsync() => Task.FromResult(0);
}

public sealed class NoOpUserNoteService : IUserNoteService
{
    public Task<IReadOnlyList<UserNote>> GetAllAsync() =>
        Task.FromResult<IReadOnlyList<UserNote>>(Array.Empty<UserNote>());

    public Task<UserNote> CreateAsync(string? title = null) =>
        Task.FromResult(new UserNote { Title = title ?? string.Empty });

    public Task UpdateAsync(int id, string title, string content) => Task.CompletedTask;

    public Task DeleteAsync(int id) => Task.CompletedTask;
}

public sealed class NoOpCustomerStatementQuickService : ICustomerStatementQuickService
{
    public Task<CustomerQuickStatementResult> GetStatementAsync(int customerId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new CustomerQuickStatementResult { CustomerId = customerId });

    public Task<string> ExportToPdfAsync(int customerId, string filePath, CancellationToken cancellationToken = default) =>
        Task.FromResult(filePath);

    public void Print(int customerId) { }
}
