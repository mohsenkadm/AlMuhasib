using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Models.Hotel;

namespace AlMuhasib.Core.Interfaces.Services.Hotel;

public interface IHotelCashService
{
    Task<IReadOnlyList<HotelCashBox>> GetCashBoxesAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<HotelCashBox?> GetCashBoxByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<HotelCashBox> CreateCashBoxAsync(HotelCashBox cashBox, CancellationToken cancellationToken = default);
    Task<HotelCashBox> UpdateCashBoxAsync(HotelCashBox cashBox, CancellationToken cancellationToken = default);
    Task DeleteCashBoxAsync(int id, string deletedBy, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<HotelVoucher> Items, int TotalCount)> GetVouchersPagedAsync(
        int page,
        int pageSize,
        HotelVoucherFilter? filter = null,
        CancellationToken cancellationToken = default);

    Task<HotelVoucher?> GetVoucherByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<HotelVoucher> CreateVoucherAsync(HotelVoucher voucher, CancellationToken cancellationToken = default);
    Task<string> GetNextVoucherNumberAsync(HotelVoucherType type, CancellationToken cancellationToken = default);
}
