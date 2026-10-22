using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Enums.Customs;

namespace AlMuhasib.Core.Entities.Customs;

public class CustomsReceipt : BaseEntity
{
    public int CustomerId { get; set; }
    public CustomsCustomer Customer { get; set; } = null!;

    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public AccountingCurrency Currency { get; set; } = AccountingCurrency.IQD;
    public decimal FxRate { get; set; } = 1m;
    public CustomsPaymentMethod PaymentMethod { get; set; } = CustomsPaymentMethod.Cash;
    public string ReceiverName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public ICollection<CustomsReceiptAllocation> Allocations { get; set; } = [];
}
