using AlMuhasib.Core.Entities.RealEstate;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data.RealEstate;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public sealed class RealEstateClauseTemplateService : IRealEstateClauseTemplateService
{
    private readonly IDbContextFactory<RealEstateDbContext> _contextFactory;

    public RealEstateClauseTemplateService(IDbContextFactory<RealEstateDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<RealEstateClauseTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.RealEstateClauseTemplates
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RealEstateClauseTemplate>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.RealEstateClauseTemplates
            .Where(t => t.IsActive)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<RealEstateClauseTemplate> SaveAsync(RealEstateClauseTemplate template, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        if (template.Id == 0)
        {
            await context.RealEstateClauseTemplates.AddAsync(template, cancellationToken);
        }
        else
        {
            var existing = await context.RealEstateClauseTemplates.FirstOrDefaultAsync(t => t.Id == template.Id, cancellationToken)
                ?? throw new InvalidOperationException("البند غير موجود");
            existing.Title = template.Title;
            existing.Body = template.Body;
            existing.SortOrder = template.SortOrder;
            existing.IsActive = template.IsActive;
            template = existing;
        }

        await context.SaveChangesAsync(cancellationToken);
        return template;
    }

    public async Task DeleteAsync(int id, string deletedBy, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var template = await context.RealEstateClauseTemplates.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("البند غير موجود");
        template.MarkSoftDeleted(deletedBy);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task EnsureDefaultsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        if (await context.RealEstateClauseTemplates.AnyAsync(cancellationToken))
            return;

        var defaults = new[]
        {
            new RealEstateClauseTemplate
            {
                SortOrder = 1,
                Title = "التسليم",
                Body = "يلتزم الطرف الأول بتسليم العقار/قطعة الأرض للطرف الثاني خالية من الشواغل وفق الموعد المتفق عليه.",
                IsActive = true
            },
            new RealEstateClauseTemplate
            {
                SortOrder = 2,
                Title = "نقل الملكية",
                Body = "يلتزم الطرفان بإتمام إجراءات نقل الملكية (الطابو) بعد تسديد كامل المبلغ المتفق عليه.",
                IsActive = true
            },
            new RealEstateClauseTemplate
            {
                SortOrder = 3,
                Title = "الشرط الجزائي",
                Body = "في حال نكول أحد الطرفين عن إتمام العقد يتحمل الطرف الناكل الشرط الجزائي المتفق عليه.",
                IsActive = true
            },
            new RealEstateClauseTemplate
            {
                SortOrder = 4,
                Title = "الديون والمستحقات",
                Body = "يتحمل الطرف الأول أي ديون أو رسوم أو أجور ماء وكهرباء مترتبة قبل تاريخ العقد ما لم يُتفق خلاف ذلك.",
                IsActive = true
            }
        };

        await context.RealEstateClauseTemplates.AddRangeAsync(defaults, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
