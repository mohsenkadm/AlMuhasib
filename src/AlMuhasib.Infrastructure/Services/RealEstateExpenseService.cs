using AlMuhasib.Core.Entities.RealEstate;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.RealEstate;
using AlMuhasib.Infrastructure.Data.RealEstate;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public sealed class RealEstateExpenseService : IRealEstateExpenseService
{
    private readonly IDbContextFactory<RealEstateDbContext> _contextFactory;

    public RealEstateExpenseService(IDbContextFactory<RealEstateDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task EnsureDefaultTypesAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        if (await context.RealEstateExpenseTypes.AnyAsync(cancellationToken))
            return;

        var defaults = new[]
        {
            "عمولة المكتب",
            "رسوم تسجيل / طابو",
            "دعاية وإعلان",
            "إيجار المكتب",
            "رواتب وأجور",
            "كهرباء وماء واتصالات",
            "صيانة ومستلزمات",
            "مواصلات ونقل",
            "استشارات قانونية",
            "مصاريف أخرى"
        };

        var i = 1;
        foreach (var name in defaults)
        {
            await context.RealEstateExpenseTypes.AddAsync(new RealEstateExpenseType
            {
                Name = name,
                Notes = string.Empty,
                IsActive = true
            }, cancellationToken);
            i++;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RealEstateExpenseType>> GetTypesAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.RealEstateExpenseTypes
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<RealEstateExpenseType> SaveTypeAsync(RealEstateExpenseType type, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(type.Name))
            throw new InvalidOperationException("اسم نوع المصروف مطلوب");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var name = type.Name.Trim();
        var duplicate = await context.RealEstateExpenseTypes
            .AnyAsync(t => t.Name == name && t.Id != type.Id, cancellationToken);
        if (duplicate)
            throw new InvalidOperationException("يوجد نوع مصروف بنفس الاسم");

        if (type.Id == 0)
        {
            type.Name = name;
            await context.RealEstateExpenseTypes.AddAsync(type, cancellationToken);
        }
        else
        {
            var existing = await context.RealEstateExpenseTypes.FirstOrDefaultAsync(t => t.Id == type.Id, cancellationToken)
                ?? throw new InvalidOperationException("نوع المصروف غير موجود");
            existing.Name = name;
            existing.Notes = type.Notes ?? string.Empty;
            existing.IsActive = type.IsActive;
            type = existing;
        }

        await context.SaveChangesAsync(cancellationToken);
        return type;
    }

    public async Task DeleteTypeAsync(int id, string deletedBy, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var type = await context.RealEstateExpenseTypes.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("نوع المصروف غير موجود");

        var hasExpenses = await context.RealEstateExpenses.AnyAsync(e => e.ExpenseTypeId == id, cancellationToken);
        if (hasExpenses)
            throw new InvalidOperationException("لا يمكن حذف نوع مرتبط بمصاريف. عطّله بدلاً من الحذف.");

        type.MarkSoftDeleted(deletedBy);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<RealEstateExpenseListItem> Items, int TotalCount, decimal TotalAmount)> GetPagedAsync(
        int page,
        int pageSize,
        RealEstateExpenseFilter filter,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = BuildQuery(context, filter);

        var totalCount = await query.CountAsync(cancellationToken);
        var totalAmount = await query.SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

        var items = await query
            .OrderByDescending(e => e.ExpenseDate)
            .ThenByDescending(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new RealEstateExpenseListItem
            {
                Id = e.Id,
                ExpenseDate = e.ExpenseDate,
                ExpenseTypeId = e.ExpenseTypeId,
                ExpenseTypeName = e.ExpenseType.Name,
                Amount = e.Amount,
                Description = e.Description,
                Notes = e.Notes,
                RelatedContractId = e.RelatedContractId,
                RelatedContractNumber = e.RelatedContract != null ? e.RelatedContract.ContractNumber : string.Empty
            })
            .ToListAsync(cancellationToken);

        return (items, totalCount, totalAmount);
    }

    public async Task<RealEstateExpense?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.RealEstateExpenses
            .Include(e => e.ExpenseType)
            .Include(e => e.RelatedContract)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<RealEstateExpense> SaveAsync(RealEstateExpense expense, CancellationToken cancellationToken = default)
    {
        if (expense.Amount <= 0)
            throw new InvalidOperationException("مبلغ المصروف يجب أن يكون أكبر من صفر");
        if (expense.ExpenseTypeId <= 0)
            throw new InvalidOperationException("اختر نوع المصروف");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var typeExists = await context.RealEstateExpenseTypes.AnyAsync(t => t.Id == expense.ExpenseTypeId, cancellationToken);
        if (!typeExists)
            throw new InvalidOperationException("نوع المصروف غير موجود");

        if (expense.RelatedContractId.HasValue)
        {
            var contractExists = await context.RealEstateContracts.AnyAsync(c => c.Id == expense.RelatedContractId.Value, cancellationToken);
            if (!contractExists)
                throw new InvalidOperationException("العقد المرتبط غير موجود");
        }

        if (expense.Id == 0)
        {
            await context.RealEstateExpenses.AddAsync(expense, cancellationToken);
        }
        else
        {
            var existing = await context.RealEstateExpenses.FirstOrDefaultAsync(e => e.Id == expense.Id, cancellationToken)
                ?? throw new InvalidOperationException("المصروف غير موجود");
            existing.ExpenseTypeId = expense.ExpenseTypeId;
            existing.ExpenseDate = expense.ExpenseDate;
            existing.Amount = expense.Amount;
            existing.Description = expense.Description ?? string.Empty;
            existing.Notes = expense.Notes ?? string.Empty;
            existing.RelatedContractId = expense.RelatedContractId;
            expense = existing;
        }

        await context.SaveChangesAsync(cancellationToken);
        return expense;
    }

    public async Task DeleteAsync(int id, string deletedBy, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var expense = await context.RealEstateExpenses.FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("المصروف غير موجود");
        expense.MarkSoftDeleted(deletedBy);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.RealEstateExpenses.AsQueryable();
        if (from.HasValue) query = query.Where(e => e.ExpenseDate >= from.Value.Date);
        if (to.HasValue) query = query.Where(e => e.ExpenseDate <= to.Value.Date);
        return await query.SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;
    }

    private static IQueryable<RealEstateExpense> BuildQuery(RealEstateDbContext context, RealEstateExpenseFilter filter)
    {
        var query = context.RealEstateExpenses
            .Include(e => e.ExpenseType)
            .Include(e => e.RelatedContract)
            .AsQueryable();

        if (filter.DateFrom.HasValue)
            query = query.Where(e => e.ExpenseDate >= filter.DateFrom.Value.Date);
        if (filter.DateTo.HasValue)
            query = query.Where(e => e.ExpenseDate <= filter.DateTo.Value.Date);
        if (filter.ExpenseTypeId.HasValue)
            query = query.Where(e => e.ExpenseTypeId == filter.ExpenseTypeId.Value);
        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var term = filter.SearchText.Trim();
            query = query.Where(e =>
                e.Description.Contains(term) ||
                e.Notes.Contains(term) ||
                e.ExpenseType.Name.Contains(term) ||
                (e.RelatedContract != null && e.RelatedContract.ContractNumber.Contains(term)));
        }

        return query;
    }
}
