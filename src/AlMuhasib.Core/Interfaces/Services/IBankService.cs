using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IBankService
{
    Task<BankAccount> CreateAsync(BankAccount bankAccount);
    Task<BankAccount?> GetByIdAsync(int id);
    Task<IEnumerable<BankAccount>> GetAllAsync();
    Task UpdateAsync(BankAccount bankAccount);
    Task DeleteAsync(int id);
    Task AddToBalanceAsync(int bankAccountId, decimal amount);
    Task DeductFromBalanceAsync(int bankAccountId, decimal amount);
    Task<decimal> GetBalanceAsync(int bankAccountId);
    Task TransferAsync(int fromId, int toId, decimal amount);
}
