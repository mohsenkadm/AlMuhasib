using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Interfaces.Services;

public interface ICashBoxService
{
    Task<CashBox> CreateAsync(CashBox cashBox);
    Task<CashBox?> GetByIdAsync(int id);
    Task<IEnumerable<CashBox>> GetAllAsync();
    Task UpdateAsync(CashBox cashBox);
    Task DeleteAsync(int id);
    Task AddToBalanceAsync(int cashBoxId, decimal amount);
    Task DeductFromBalanceAsync(int cashBoxId, decimal amount);
    Task<decimal> GetBalanceAsync(int cashBoxId);
}
