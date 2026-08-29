using AlMuhasib.Core.Enums.Gold;
using System.ComponentModel.DataAnnotations.Schema;

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
    public GoldCustomer? Customer { get; set; }
    public int? SupplierId { get; set; }
    public GoldSupplier? Supplier { get; set; }
    public bool IsOpeningBalance { get; set; }
    public bool AffectsCashBox { get; set; } = true;
    public string Notes { get; set; } = string.Empty;

    [NotMapped]
    public string PartyDisplayName =>
        Customer?.Name ?? Supplier?.Name ?? string.Empty;
}
