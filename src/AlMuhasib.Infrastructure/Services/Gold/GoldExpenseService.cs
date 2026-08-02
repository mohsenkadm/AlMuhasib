using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.Infrastructure.Data.Gold;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Gold;

public sealed class GoldExpenseService : IGoldExpenseService
{
    private readonly IDbContextFactory<GoldDbContext> _contextFactory;

    public GoldExpenseService(IDbContextFactory<GoldDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<GoldExpenseType>> GetExpenseTypesAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.GoldExpenseTypes.AsNoTracking().AsQueryable();
        if (activeOnly)
            query = query.Where(t => t.IsActive);

        return await query.OrderBy(t => t.Name).ToListAsync(cancellationToken);
    }

    public async Task<GoldExpenseType> CreateExpenseTypeAsync(
        GoldExpenseType type,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(type.Name))
            throw new InvalidOperationException("اسم نوع المصروف مطلوب");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        type.IsActive = true;
        await context.GoldExpenseTypes.AddAsync(type, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return type;
    }

    public async Task<GoldExpenseType> UpdateExpenseTypeAsync(
        GoldExpenseType type,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await context.GoldExpenseTypes.FirstOrDefaultAsync(t => t.Id == type.Id, cancellationToken)
            ?? throw new InvalidOperationException("نوع المصروف غير موجود");

        existing.Name = type.Name;
        existing.IsActive = type.IsActive;
        await context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task DeleteExpenseTypeAsync(int id, string deletedBy, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var type = await context.GoldExpenseTypes.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("نوع المصروف غير موجود");

        var inUse = await context.GoldExpenses.AnyAsync(e => e.ExpenseTypeId == id, cancellationToken);
        if (inUse)
            throw new InvalidOperationException("لا يمكن حذف نوع مصروف مستخدم");

        type.MarkSoftDeleted(deletedBy);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<GoldExpenseListItem> Items, int TotalCount)> GetExpensesPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        int? expenseTypeId = null,
        int? cashBoxId = null,
        int? warehouseId = null,
        GoldCurrency? currency = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.GoldExpenses.AsNoTracking()
            .Include(e => e.ExpenseType)
            .Include(e => e.CashBox)
            .Include(e => e.Warehouse)
            .AsQueryable();

        if (expenseTypeId.HasValue)
            query = query.Where(e => e.ExpenseTypeId == expenseTypeId.Value);
        if (cashBoxId.HasValue)
            query = query.Where(e => e.CashBoxId == cashBoxId.Value);
        if (warehouseId.HasValue)
            query = query.Where(e => e.WarehouseId == warehouseId.Value);
        if (currency.HasValue)
            query = query.Where(e => e.Currency == currency.Value);
        if (dateFrom.HasValue)
            query = query.Where(e => e.ExpenseDate.Date >= dateFrom.Value.Date);
        if (dateTo.HasValue)
            query = query.Where(e => e.ExpenseDate.Date <= dateTo.Value.Date);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e =>
                e.Notes.Contains(term) ||
                (e.ExpenseType != null && e.ExpenseType.Name.Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var expenses = await query
            .OrderByDescending(e => e.ExpenseDate)
            .ThenByDescending(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = expenses.Select(e => new GoldExpenseListItem
        {
            Id = e.Id,
            ExpenseDate = e.ExpenseDate,
            ExpenseTypeId = e.ExpenseTypeId,
            ExpenseTypeName = e.ExpenseType?.Name ?? string.Empty,
            Amount = e.Amount,
            Currency = e.Currency,
            CashBoxId = e.CashBoxId,
            CashBoxName = e.CashBox?.Name,
            WarehouseId = e.WarehouseId,
            WarehouseName = e.Warehouse?.Name,
            Notes = e.Notes
        }).ToList();

        return (items, totalCount);
    }

    public async Task<GoldExpense?> GetExpenseByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.GoldExpenses.AsNoTracking()
            .Include(e => e.ExpenseType)
            .Include(e => e.CashBox)
            .Include(e => e.Warehouse)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<GoldExpense> CreateExpenseAsync(GoldExpense expense, CancellationToken cancellationToken = default)
    {
        if (expense.Amount <= 0)
            throw new InvalidOperationException("مبلغ المصروف يجب أن يكون أكبر من صفر");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await using var tx = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            _ = await context.GoldExpenseTypes.FirstOrDefaultAsync(t => t.Id == expense.ExpenseTypeId, cancellationToken)
                ?? throw new InvalidOperationException("نوع المصروف غير موجود");

            await GoldCashService.EnsureDefaultCashBoxesAsync(context, cancellationToken);
            var cashBox = await GoldCashService.ResolveCashBoxAsync(
                context,
                expense.CashBoxId > 0 ? expense.CashBoxId : null,
                expense.Currency,
                cancellationToken);
            expense.CashBoxId = cashBox.Id;
            expense.Currency = cashBox.Currency;
            expense.Notes ??= string.Empty;

            if (expense.WarehouseId.HasValue)
                await GoldWarehouseService.ResolveWarehouseIdAsync(context, expense.WarehouseId, cancellationToken);

            GoldCashService.AdjustCashBoxBalance(cashBox, -expense.Amount);

            await context.GoldExpenses.AddAsync(expense, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return (await GetExpenseByIdAsync(expense.Id, cancellationToken))!;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<GoldExpense> UpdateExpenseAsync(GoldExpense expense, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await using var tx = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var existing = await context.GoldExpenses.FirstOrDefaultAsync(e => e.Id == expense.Id, cancellationToken)
                ?? throw new InvalidOperationException("المصروف غير موجود");

            // Reverse old cash impact, apply new.
            var oldBox = await context.GoldCashBoxes.FirstOrDefaultAsync(c => c.Id == existing.CashBoxId, cancellationToken);
            if (oldBox is not null)
                GoldCashService.AdjustCashBoxBalance(oldBox, existing.Amount);

            _ = await context.GoldExpenseTypes.FirstOrDefaultAsync(t => t.Id == expense.ExpenseTypeId, cancellationToken)
                ?? throw new InvalidOperationException("نوع المصروف غير موجود");

            await GoldCashService.EnsureDefaultCashBoxesAsync(context, cancellationToken);
            var cashBox = await GoldCashService.ResolveCashBoxAsync(
                context,
                expense.CashBoxId > 0 ? expense.CashBoxId : null,
                expense.Currency,
                cancellationToken);

            if (expense.Amount <= 0)
                throw new InvalidOperationException("مبلغ المصروف يجب أن يكون أكبر من صفر");

            GoldCashService.AdjustCashBoxBalance(cashBox, -expense.Amount);

            existing.ExpenseDate = expense.ExpenseDate.Date;
            existing.ExpenseTypeId = expense.ExpenseTypeId;
            existing.Amount = expense.Amount;
            existing.Currency = cashBox.Currency;
            existing.CashBoxId = cashBox.Id;
            existing.Notes = expense.Notes ?? string.Empty;
            existing.WarehouseId = expense.WarehouseId;

            await context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return (await GetExpenseByIdAsync(existing.Id, cancellationToken))!;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteExpenseAsync(int id, string deletedBy, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await using var tx = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var expense = await context.GoldExpenses.FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
                ?? throw new InvalidOperationException("المصروف غير موجود");

            var cashBox = await context.GoldCashBoxes.FirstOrDefaultAsync(c => c.Id == expense.CashBoxId, cancellationToken);
            if (cashBox is not null)
                GoldCashService.AdjustCashBoxBalance(cashBox, expense.Amount);

            expense.MarkSoftDeleted(deletedBy);
            await context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
