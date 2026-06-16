using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Core.Models.Hotel;
using AlMuhasib.Infrastructure.Data.Hotel;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Hotel;

public sealed class HotelHousekeepingService : IHousekeepingService
{
    private readonly IDbContextFactory<HotelDbContext> _contextFactory;

    public HotelHousekeepingService(IDbContextFactory<HotelDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<HousekeepingTask>> GetTasksAsync(
        HousekeepingTaskFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.HousekeepingTasks
            .Include(t => t.Room)
                .ThenInclude(r => r.Floor)
            .AsQueryable();

        if (filter is not null)
        {
            if (filter.Status.HasValue)
                query = query.Where(t => t.Status == filter.Status.Value);
            if (filter.RoomId.HasValue)
                query = query.Where(t => t.RoomId == filter.RoomId.Value);
            if (filter.FloorId.HasValue)
                query = query.Where(t => t.Room.FloorId == filter.FloorId.Value);
            if (!string.IsNullOrWhiteSpace(filter.AssignedTo))
            {
                var assigned = filter.AssignedTo.Trim();
                query = query.Where(t => t.AssignedTo == assigned);
            }
        }

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<HousekeepingTask?> GetTaskByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.HousekeepingTasks
            .Include(t => t.Room)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<HousekeepingTask> CreateTaskAsync(
        HousekeepingTask task,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await context.HousekeepingTasks.AddAsync(task, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return task;
    }

    public async Task<HousekeepingTask> UpdateTaskAsync(
        HousekeepingTask task,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await context.HousekeepingTasks
            .Include(t => t.Room)
            .FirstOrDefaultAsync(t => t.Id == task.Id, cancellationToken)
            ?? throw new InvalidOperationException("مهمة التدبير المنزلي غير موجودة");

        var previousStatus = existing.Status;
        existing.Status = task.Status;
        existing.AssignedTo = task.AssignedTo;
        existing.StartedAt = task.StartedAt;
        existing.CompletedAt = task.CompletedAt;
        existing.Notes = task.Notes;

        if (previousStatus != HousekeepingStatus.Done
            && task.Status == HousekeepingStatus.Done
            && existing.Room is not null)
        {
            existing.Room.Status = RoomStatus.Available;
            existing.CompletedAt ??= DateTime.Now;
        }

        await context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task DeleteTaskAsync(int id, string deletedBy, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var task = await context.HousekeepingTasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("مهمة التدبير المنزلي غير موجودة");

        task.MarkSoftDeleted(deletedBy);
        await context.SaveChangesAsync(cancellationToken);
    }
}
