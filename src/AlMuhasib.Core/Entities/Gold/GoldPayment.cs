using AlMuhasib.Core.Enums.Gold;

namespace AlMuhasib.Core.Entities.Gold;

public class GoldPayment : BaseEntity
{
    public int InvoiceId { get; set; }
    public GoldInvoice? Invoice { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public GoldCurrency Currency { get; set; } = GoldCurrency.IQD;
    public decimal FxRate { get; set; }
    public int? CashBoxId { get; set; }
    public string Notes { get; set; } = string.Empty;
}
