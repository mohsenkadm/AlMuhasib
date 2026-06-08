using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class UserTaskService : IUserTaskService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ICurrentUserService _currentUserService;

    public UserTaskService(IDbContextFactory<AppDbContext> contextFactory, ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
    }

    private int RequireUserId()
    {
        if (_currentUserService.UserId is not int userId)
            throw new InvalidOperationException("المستخدم غير مسجل الدخول.");
        return userId;
    }

    public async Task<IReadOnlyList<UserTask>> GetAllAsync()
    {
        var userId = RequireUserId();
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.UserTasks
            .Include(t => t.AssignedByUser)
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.Status == UserTaskStatus.Completed)
            .ThenBy(t => t.DueDate ?? DateTime.MaxValue)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<UserTask> CreateAsync(string title, string? details, DateTime? dueDate, UserTaskStatus status, int assignedToUserId)
    {
        var currentUserId = RequireUserId();
        await using var context = await _contextFactory.CreateDbContextAsync();

        var assigneeExists = await context.Users
            .AnyAsync(u => u.Id == assignedToUserId && u.IsActive);
        if (!assigneeExists)
            throw new InvalidOperationException("المستخدم المحدد غير موجود أو غير نشط.");

        var task = new UserTask
        {
            UserId = assignedToUserId,
            AssignedByUserId = currentUserId,
            Title = title.Trim(),
            Details = string.IsNullOrWhiteSpace(details) ? null : details.Trim(),
            DueDate = dueDate?.Date,
            Status = status,
            CreatedBy = _currentUserService.Username,
            CreatedAt = DateTime.UtcNow
        };

        await context.UserTasks.AddAsync(task);
        await context.SaveChangesAsync();

        await context.Entry(task).Reference(t => t.AssignedByUser).LoadAsync();
        return task;
    }

    public async Task UpdateAsync(int id, string title, string? details, DateTime? dueDate, UserTaskStatus status)
    {
        var userId = RequireUserId();
        await using var context = await _contextFactory.CreateDbContextAsync();

        var task = await context.UserTasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId)
            ?? throw new InvalidOperationException("المهمة غير موجودة.");

        task.Title = title.Trim();
        task.Details = string.IsNullOrWhiteSpace(details) ? null : details.Trim();
        task.DueDate = dueDate?.Date;
        task.Status = status;
        task.UpdatedBy = _currentUserService.Username;
        task.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    public async Task UpdateStatusAsync(int id, UserTaskStatus status)
    {
        var userId = RequireUserId();
        await using var context = await _contextFactory.CreateDbContextAsync();

        var task = await context.UserTasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId)
            ?? throw new InvalidOperationException("المهمة غير موجودة.");

        task.Status = status;
        task.UpdatedBy = _currentUserService.Username;
        task.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var userId = RequireUserId();
        await using var context = await _contextFactory.CreateDbContextAsync();

        var task = await context.UserTasks.FirstOrDefaultAsync(t => t.Id == id && (t.UserId == userId || t.AssignedByUserId == userId))
            ?? throw new InvalidOperationException("المهمة غير موجودة.");

        task.IsDeleted = true;
        task.DeletedAt = DateTime.UtcNow;
        task.DeletedBy = _currentUserService.Username;

        await context.SaveChangesAsync();
    }

    public async Task<int> GetPendingCountAsync()
    {
        var userId = RequireUserId();
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.UserTasks
            .CountAsync(t => t.UserId == userId && t.Status != UserTaskStatus.Completed);
    }
}
