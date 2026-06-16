using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Infrastructure.Data.Hotel;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Hotel;

public sealed class HotelExpenseService : IHotelExpenseService
{
    private readonly IDbContextFactory<HotelDbContext> _contextFactory;

    public HotelExpenseService(IDbContextFactory<HotelDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<HotelExpenseType>> GetExpenseTypesAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.HotelExpenseTypes.OrderBy(t => t.Name).ToListAsync(cancellationToken);
    }

    public async Task<HotelExpenseType?> GetExpenseTypeByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.HotelExpenseTypes.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<HotelExpenseType> CreateExpenseTypeAsync(
        HotelExpenseType expenseType,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await context.HotelExpenseTypes.AddAsync(expenseType, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return expenseType;
    }

    public async Task<HotelExpenseType> UpdateExpenseTypeAsync(
        HotelExpenseType expenseType,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await context.HotelExpenseTypes.FirstOrDefaultAsync(t => t.Id == expenseType.Id, cancellationToken)
            ?? throw new InvalidOperationException("نوع المصروف غير موجود");

        existing.Name = expenseType.Name;
        existing.Notes = expenseType.Notes;
        await context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task DeleteExpenseTypeAsync(int id, string deletedBy, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var expenseType = await context.HotelExpenseTypes.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("نوع المصروف غير موجود");

        expenseType.MarkSoftDeleted(deletedBy);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<HotelExpense> Items, int TotalCount)> GetExpensesPagedAsync(
        int page,
        int pageSize,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int? expenseTypeId = null,
        int? cashBoxId = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.HotelExpenses
            .Include(e => e.ExpenseType)
            .Include(e => e.HotelCashBox)
            .AsQueryable();

        if (dateFrom.HasValue)
            query = query.Where(e => e.ExpenseDate >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(e => e.ExpenseDate <= dateTo.Value);
        if (expenseTypeId.HasValue)
            query = query.Where(e => e.HotelExpenseTypeId == expenseTypeId.Value);
        if (cashBoxId.HasValue)
            query = query.Where(e => e.HotelCashBoxId == cashBoxId.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(e => e.ExpenseDate)
            .ThenByDescending(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<HotelExpense?> GetExpenseByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.HotelExpenses
            .Include(e => e.ExpenseType)
            .Include(e => e.HotelCashBox)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<HotelExpense> CreateExpenseAsync(HotelExpense expense, CancellationToken cancellationToken = default)
    {
        if (expense.Amount <= 0)
            throw new InvalidOperationException("مبلغ المصروف يجب أن يكون أكبر من صفر");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        if (expense.HotelCashBoxId.HasValue)
        {
            var cashBox = await context.HotelCashBoxes.FirstOrDefaultAsync(c => c.Id == expense.HotelCashBoxId.Value, cancellationToken)
                ?? throw new InvalidOperationException("الصندوق غير موجود");

            if (cashBox.CurrentBalance < expense.Amount)
                throw new InvalidOperationException("رصيد الصندوق غير كافٍ");
        }

        await context.HotelExpenses.AddAsync(expense, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        if (expense.HotelCashBoxId.HasValue)
        {
            var cashBox = await context.HotelCashBoxes.FirstAsync(c => c.Id == expense.HotelCashBoxId.Value, cancellationToken);
            cashBox.CurrentBalance -= expense.Amount;

            var voucherNumber = await GetNextVoucherNumberAsync(context, cancellationToken);
            await context.HotelVouchers.AddAsync(new HotelVoucher
            {
                VoucherNumber = voucherNumber,
                VoucherDate = expense.ExpenseDate,
                Type = HotelVoucherType.Payment,
                Amount = expense.Amount,
                HotelCashBoxId = expense.HotelCashBoxId.Value,
                HotelExpenseId = expense.Id,
                Description = expense.Description,
                Notes = expense.Notes
            }, cancellationToken);

            await context.SaveChangesAsync(cancellationToken);
        }

        return expense;
    }

    public async Task<HotelExpense> UpdateExpenseAsync(HotelExpense expense, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await context.HotelExpenses.FirstOrDefaultAsync(e => e.Id == expense.Id, cancellationToken)
            ?? throw new InvalidOperationException("المصروف غير موجود");

        existing.HotelExpenseTypeId = expense.HotelExpenseTypeId;
        existing.ExpenseDate = expense.ExpenseDate;
        existing.Amount = expense.Amount;
        existing.Description = expense.Description;
        existing.Notes = expense.Notes;
        await context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task DeleteExpenseAsync(int id, string deletedBy, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var expense = await context.HotelExpenses.FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("المصروف غير موجود");

        expense.MarkSoftDeleted(deletedBy);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<string> GetNextVoucherNumberAsync(
        HotelDbContext context,
        CancellationToken cancellationToken)
    {
        const string prefix = "HPY";
        var lastVoucher = await context.HotelVouchers
            .IgnoreQueryFilters()
            .Where(v => v.Type == HotelVoucherType.Payment && v.VoucherNumber.StartsWith(prefix + "-"))
            .OrderByDescending(v => v.Id)
            .Select(v => v.VoucherNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var nextNum = 1;
        if (lastVoucher is not null)
        {
            var parts = lastVoucher.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[1], out var lastNum))
                nextNum = lastNum + 1;
        }

        return $"{prefix}-{nextNum:D4}";
    }
}
