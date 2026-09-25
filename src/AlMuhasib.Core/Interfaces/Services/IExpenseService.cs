using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IExpenseService
{
    // ── ExpenseType CRUD ──
    Task<IEnumerable<ExpenseType>> GetAllExpenseTypesAsync();
    Task<ExpenseType> AddExpenseTypeAsync(string name);
    Task UpdateExpenseTypeAsync(int id, string name);
    Task DeleteExpenseTypeAsync(int id);

    // ── Expense operations ──
    Task<Expense> AddExpenseAsync(
        int expenseTypeId,
        decimal amount,
        DateTime date,
        int cashBoxId,
        string? notes,
        AccountingCurrency? currency = null,
        decimal fxRate = 1m);
    Task DeleteExpenseAsync(int id);

    Task<(IEnumerable<Expense> Items, int TotalCount)> GetPagedExpensesAsync(
        int page, int pageSize,
        int? expenseTypeId = null,
        int? cashBoxId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? searchTerm = null);

    Task<decimal> GetTotalExpensesAsync(DateTime? fromDate = null, DateTime? toDate = null);
}
