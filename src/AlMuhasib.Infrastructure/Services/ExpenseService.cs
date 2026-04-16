using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class ExpenseService : IExpenseService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ICurrentUserService _currentUserService;

    public ExpenseService(IDbContextFactory<AppDbContext> contextFactory, ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
    }

    public async Task<IEnumerable<ExpenseType>> GetAllExpenseTypesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ExpenseTypes.OrderBy(et => et.Name).ToListAsync();
    }

    public async Task<ExpenseType> AddExpenseTypeAsync(string name)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var expenseType = new ExpenseType { Name = name };
        await context.ExpenseTypes.AddAsync(expenseType);
        await context.SaveChangesAsync();
        return expenseType;
    }

    public async Task UpdateExpenseTypeAsync(int id, string name)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var expenseType = await context.ExpenseTypes.FindAsync(id)
            ?? throw new InvalidOperationException("نوع المصروف غير موجود");
        expenseType.Name = name;
        await context.SaveChangesAsync();
    }

    public async Task DeleteExpenseTypeAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var expenseType = await context.ExpenseTypes.FindAsync(id)
            ?? throw new InvalidOperationException("نوع المصروف غير موجود");
        var hasExpenses = await context.Expenses.AnyAsync(e => e.ExpenseTypeId == id);
        if (hasExpenses) throw new InvalidOperationException("لا يمكن حذف نوع مصروف مرتبط بمصاريف");
        expenseType.IsDeleted = true;
        expenseType.DeletedAt = DateTime.UtcNow;
        expenseType.DeletedBy = _currentUserService.Username;
        await context.SaveChangesAsync();
    }

    public async Task<Expense> AddExpenseAsync(int expenseTypeId, decimal amount, DateTime date, int cashBoxId, string? notes)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var cashBox = await context.CashBoxes.FindAsync(cashBoxId)
                ?? throw new InvalidOperationException("القاصة غير موجودة");
            if (cashBox.Balance < amount)
                throw new InvalidOperationException($"رصيد القاصة غير كافٍ. الرصيد الحالي: {cashBox.Balance:N0}");

            cashBox.Balance -= amount;
            var expense = new Expense { ExpenseTypeId = expenseTypeId, Amount = amount, Date = date, CashBoxId = cashBoxId, Notes = notes };
            await context.Expenses.AddAsync(expense);
            await context.SaveChangesAsync();

            if (_currentUserService.UserId.HasValue)
            {
                await context.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = _currentUserService.UserId.Value, Action = AuditAction.Add,
                    EntityName = "Expense", EntityId = expense.Id,
                    NewValues = $"مصروف: {amount:N0} من قاصة: {cashBox.Name}",
                    Timestamp = DateTime.UtcNow, CreatedBy = _currentUserService.Username, CreatedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }
            await transaction.CommitAsync();
            return expense;
        }
        catch { await transaction.RollbackAsync(); throw; }
    }

    public async Task<(IEnumerable<Expense> Items, int TotalCount)> GetPagedExpensesAsync(
        int page, int pageSize, int? expenseTypeId = null, int? cashBoxId = null,
        DateTime? fromDate = null, DateTime? toDate = null, string? searchTerm = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Expenses.Include(e => e.ExpenseType).Include(e => e.CashBox).AsQueryable();
        if (expenseTypeId.HasValue) query = query.Where(e => e.ExpenseTypeId == expenseTypeId.Value);
        if (cashBoxId.HasValue) query = query.Where(e => e.CashBoxId == cashBoxId.Value);
        if (fromDate.HasValue) query = query.Where(e => e.Date >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(e => e.Date <= toDate.Value);
        if (!string.IsNullOrWhiteSpace(searchTerm)) query = query.Where(e => e.Notes != null && e.Notes.Contains(searchTerm));

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(e => e.Date).ThenByDescending(e => e.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, totalCount);
    }

    public async Task<decimal> GetTotalExpensesAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Expenses.AsQueryable();
        if (fromDate.HasValue) query = query.Where(e => e.Date >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(e => e.Date <= toDate.Value);
        return await query.SumAsync(e => e.Amount);
    }
}
