using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IOtherIncomeService
{
    Task<IEnumerable<OtherIncomeType>> GetAllTypesAsync();
    Task EnsureDefaultTypesAsync();
    Task<OtherIncomeType> AddTypeAsync(string name);
    Task UpdateTypeAsync(int id, string name);
    Task SetTypeActiveAsync(int id, bool isActive);
    Task DeleteTypeAsync(int id);

    Task<OtherIncome> AddAsync(
        int typeId,
        decimal amount,
        DateTime date,
        int cashBoxId,
        string? notes,
        AccountingCurrency currency,
        decimal fxRate);

    Task UpdateAsync(
        int id,
        int typeId,
        decimal amount,
        DateTime date,
        int cashBoxId,
        string? notes,
        AccountingCurrency currency,
        decimal fxRate);

    Task DeleteAsync(int id);

    Task<(IEnumerable<OtherIncome> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        int? typeId = null,
        int? cashBoxId = null,
        AccountingCurrency? currency = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? searchTerm = null);
}
