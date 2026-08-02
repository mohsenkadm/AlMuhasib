using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Models.Gold;

namespace AlMuhasib.Core.Interfaces.Services.Gold;

public interface IGoldExpenseService
{
    Task<IReadOnlyList<GoldExpenseType>> GetExpenseTypesAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    Task<GoldExpenseType> CreateExpenseTypeAsync(GoldExpenseType type, CancellationToken cancellationToken = default);
    Task<GoldExpenseType> UpdateExpenseTypeAsync(GoldExpenseType type, CancellationToken cancellationToken = default);
    Task DeleteExpenseTypeAsync(int id, string deletedBy, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<GoldExpenseListItem> Items, int TotalCount)> GetExpensesPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        int? expenseTypeId = null,
        int? cashBoxId = null,
        int? warehouseId = null,
        GoldCurrency? currency = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        CancellationToken cancellationToken = default);

    Task<GoldExpense?> GetExpenseByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<GoldExpense> CreateExpenseAsync(GoldExpense expense, CancellationToken cancellationToken = default);
    Task<GoldExpense> UpdateExpenseAsync(GoldExpense expense, CancellationToken cancellationToken = default);
    Task DeleteExpenseAsync(int id, string deletedBy, CancellationToken cancellationToken = default);
}
