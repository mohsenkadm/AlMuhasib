using AlMuhasib.Core.Entities.Customs;
using AlMuhasib.Core.Interfaces.Services.Customs;
using AlMuhasib.Core.Models.Customs;
using AlMuhasib.Infrastructure.Data.Customs;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Customs;

public sealed class CustomsExpenseTypeService : ICustomsExpenseTypeService
{
    private readonly IDbContextFactory<CustomsDbContext> _contextFactory;

    public CustomsExpenseTypeService(IDbContextFactory<CustomsDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<CustomsExpenseTypeItem>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.CustomsExpenseTypes.AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new CustomsExpenseTypeItem
            {
                Id = t.Id,
                Name = t.Name,
                IsActive = t.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomsExpenseTypeItem> CreateAsync(
        string name,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("اسم نوع المصروف مطلوب");

        var trimmed = name.Trim();
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var existing = await db.CustomsExpenseTypes
            .FirstOrDefaultAsync(t => t.Name == trimmed, cancellationToken);
        if (existing is not null)
        {
            if (!existing.IsActive)
            {
                existing.IsActive = true;
                existing.UpdatedBy = createdBy;
                existing.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }

            return new CustomsExpenseTypeItem
            {
                Id = existing.Id,
                Name = existing.Name,
                IsActive = existing.IsActive
            };
        }

        var entity = new CustomsExpenseType
        {
            Name = trimmed,
            IsActive = true,
            CreatedBy = createdBy
        };
        await db.CustomsExpenseTypes.AddAsync(entity, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return new CustomsExpenseTypeItem
        {
            Id = entity.Id,
            Name = entity.Name,
            IsActive = entity.IsActive
        };
    }
}
