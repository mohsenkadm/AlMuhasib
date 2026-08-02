using AlMuhasib.Core.Enums.Gold;

namespace AlMuhasib.Core.Entities.Gold;

public class GoldVoucher : BaseEntity
{
    public string VoucherNumber { get; set; } = string.Empty;
    public DateTime VoucherDate { get; set; } = DateTime.Today;
    public GoldVoucherType VoucherType { get; set; } = GoldVoucherType.Receipt;
    public GoldCurrency Currency { get; set; } = GoldCurrency.IQD;
    public decimal Amount { get; set; }
    public int? CashBoxId { get; set; }
    public int? CustomerId { get; set; }
    public string Notes { get; set; } = string.Empty;
}
