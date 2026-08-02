using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;

namespace AlMuhasib.Core.Interfaces.Services.Gold;

public interface IGoldCashService
{
    Task<IReadOnlyList<GoldCashBox>> GetCashBoxesAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<GoldCashBox?> GetCashBoxByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<GoldCashBox?> GetDefaultCashBoxAsync(GoldCurrency currency, CancellationToken cancellationToken = default);
    Task<GoldCashBox> CreateCashBoxAsync(GoldCashBox cashBox, CancellationToken cancellationToken = default);
    Task<GoldCashBox> UpdateCashBoxAsync(GoldCashBox cashBox, CancellationToken cancellationToken = default);
    Task DeleteCashBoxAsync(int id, string deletedBy, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<GoldVoucher> Items, int TotalCount)> GetVouchersPagedAsync(
        int page,
        int pageSize,
        GoldVoucherType? type = null,
        GoldCurrency? currency = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int? cashBoxId = null,
        CancellationToken cancellationToken = default);

    Task<GoldVoucher?> GetVoucherByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<GoldVoucher> CreateVoucherAsync(GoldVoucher voucher, CancellationToken cancellationToken = default);
    Task<string> GetNextVoucherNumberAsync(GoldVoucherType type, CancellationToken cancellationToken = default);
}
