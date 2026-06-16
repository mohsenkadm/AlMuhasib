using AlMuhasib.Core.Entities.Hotel;

namespace AlMuhasib.Core.Interfaces.Services.Hotel;

public interface IHotelExpenseService
{
    Task<IReadOnlyList<HotelExpenseType>> GetExpenseTypesAsync(CancellationToken cancellationToken = default);
    Task<HotelExpenseType?> GetExpenseTypeByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<HotelExpenseType> CreateExpenseTypeAsync(HotelExpenseType expenseType, CancellationToken cancellationToken = default);
    Task<HotelExpenseType> UpdateExpenseTypeAsync(HotelExpenseType expenseType, CancellationToken cancellationToken = default);
    Task DeleteExpenseTypeAsync(int id, string deletedBy, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<HotelExpense> Items, int TotalCount)> GetExpensesPagedAsync(
        int page,
        int pageSize,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int? expenseTypeId = null,
        int? cashBoxId = null,
        CancellationToken cancellationToken = default);

    Task<HotelExpense?> GetExpenseByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<HotelExpense> CreateExpenseAsync(HotelExpense expense, CancellationToken cancellationToken = default);
    Task<HotelExpense> UpdateExpenseAsync(HotelExpense expense, CancellationToken cancellationToken = default);
    Task DeleteExpenseAsync(int id, string deletedBy, CancellationToken cancellationToken = default);
}
