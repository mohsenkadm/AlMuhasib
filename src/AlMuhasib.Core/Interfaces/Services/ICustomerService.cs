using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Interfaces.Services;

public interface ICustomerService
{
    Task<Customer> CreateAsync(Customer customer);
    Task<Customer?> GetByIdAsync(int id);
    Task<(IEnumerable<Customer> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? searchTerm = null);
    Task UpdateAsync(Customer customer);
    Task DeleteAsync(int id);
    Task<IEnumerable<Customer>> SearchByNameAsync(string name);

    /// <summary>
    /// Recycles FileNumber from soft-deleted customers if available,
    /// otherwise generates the next sequential number.
    /// </summary>
    Task<string> GenerateFileNumberAsync();
}
