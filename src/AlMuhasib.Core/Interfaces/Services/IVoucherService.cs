using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IVoucherService
{
    Task<Voucher> CreateVoucherAsync(Voucher voucher);
    Task<Voucher?> GetByIdAsync(int id);
    Task<(IEnumerable<Voucher> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, VoucherType? type = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<string> GenerateVoucherNumberAsync();

    /// <summary>
    /// For BankReceipt vouchers: records BankFees as a loss.
    /// </summary>
    Task<Voucher> CreateBankReceiptAsync(Voucher voucher, decimal bankFees);

    Task DeleteVoucherAsync(int id);
}
