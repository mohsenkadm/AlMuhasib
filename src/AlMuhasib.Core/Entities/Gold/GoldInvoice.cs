using AlMuhasib.Core.Enums.Gold;

namespace AlMuhasib.Core.Entities.Gold;

public class GoldInvoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; } = DateTime.Today;
    public GoldInvoiceType InvoiceType { get; set; } = GoldInvoiceType.Sale;
    public GoldPaymentMethod PaymentMethod { get; set; } = GoldPaymentMethod.Cash;
    public GoldInvoiceStatus Status { get; set; } = GoldInvoiceStatus.Completed;
    public int? CustomerId { get; set; }
    public GoldCustomer? Customer { get; set; }
    public int? SupplierId { get; set; }
    public GoldSupplier? Supplier { get; set; }
    public int? WarehouseId { get; set; }
    public GoldWarehouse? Warehouse { get; set; }
    public bool IsExchange { get; set; }
    public decimal ExchangeCashDifference { get; set; }
    public GoldCurrency PricingCurrency { get; set; } = GoldCurrency.IQD;
    public GoldCurrency PaymentCurrency { get; set; } = GoldCurrency.IQD;
    public decimal FxRate { get; set; }
    public decimal TotalGoldValue { get; set; }
    public decimal TotalMakingCharge { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalAmountIqd { get; set; }
    public decimal TotalAmountUsd { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal TotalWeightGrams { get; set; }
    public int? CashBoxId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool WeightFromScale { get; set; }
    /// <summary>Optional link to the original sale for sale-return invoices.</summary>
    public int? RelatedInvoiceId { get; set; }

    public ICollection<GoldInvoiceLine> Lines { get; set; } = [];
    public ICollection<GoldPayment> Payments { get; set; } = [];
}
