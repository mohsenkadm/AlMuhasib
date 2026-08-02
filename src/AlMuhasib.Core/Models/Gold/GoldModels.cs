using AlMuhasib.Core.Enums.Gold;

namespace AlMuhasib.Core.Models.Gold;

public class GoldDashboardData
{
    public decimal TodaySalesIqd { get; set; }
    public decimal TodaySalesUsd { get; set; }
    public decimal TodayPurchasesIqd { get; set; }
    public decimal TodayPurchasesUsd { get; set; }
    public decimal CashBalanceIqd { get; set; }
    public decimal CashBalanceUsd { get; set; }
    public decimal TotalStockGrams { get; set; }
    public decimal TotalStockValueIqd { get; set; }
    public int OpenCreditCount { get; set; }
    public decimal OpenCreditIqd { get; set; }
    public decimal OpenCreditUsd { get; set; }
    public int OverdueCreditCount { get; set; }
    public int LowStockKaratCount { get; set; }
    public bool PricesUpdatedToday { get; set; }
    public decimal? LatestUsdToIqd { get; set; }
    public List<GoldStockRow> StockByKarat { get; set; } = [];
    public List<GoldInvoiceListItem> RecentInvoices { get; set; } = [];
    public List<GoldAlertItem> Alerts { get; set; } = [];
    public List<GoldMithqalPriceRow> LatestPrices { get; set; } = [];
}

public class GoldStockRow
{
    public int KaratValue { get; set; }
    public string KaratName { get; set; } = string.Empty;
    public decimal GramsOnHand { get; set; }
    public decimal AverageCostPerGram { get; set; }
    public decimal StockValue { get; set; }
    public int PieceCount { get; set; }
    public bool IsLowStock { get; set; }
}

public class GoldInvoiceListItem
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public GoldInvoiceType InvoiceType { get; set; }
    public GoldPaymentMethod PaymentMethod { get; set; }
    public GoldInvoiceStatus Status { get; set; }
    public string? CustomerName { get; set; }
    public GoldCurrency PricingCurrency { get; set; }
    public GoldCurrency PaymentCurrency { get; set; }
    public decimal TotalWeightGrams { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalAmountIqd { get; set; }
    public decimal TotalAmountUsd { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class GoldCustomerListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal CreditBalanceIqd { get; set; }
    public decimal CreditBalanceUsd { get; set; }
    public bool IsActive { get; set; }
    public int OpenInvoiceCount { get; set; }
    public DateTime? LastTransactionDate { get; set; }
}

public class GoldMithqalPriceRow
{
    public int Id { get; set; }
    public DateTime PriceDate { get; set; }
    public int KaratValue { get; set; }
    public string KaratName { get; set; } = string.Empty;
    public decimal PricePerMithqal { get; set; }
    public GoldCurrency Currency { get; set; }
    public decimal? FxRateUsed { get; set; }
    public decimal? PricePerGram { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class GoldAlertItem
{
    public int? NotificationId { get; set; }
    public GoldNotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? RelatedEntity { get; set; }
    public int? RelatedId { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
}

public class GoldSaleRequest
{
    public DateTime InvoiceDate { get; set; } = DateTime.Today;
    public GoldPaymentMethod PaymentMethod { get; set; } = GoldPaymentMethod.Cash;
    public int? CustomerId { get; set; }
    public GoldCurrency PricingCurrency { get; set; } = GoldCurrency.USD;
    public GoldCurrency PaymentCurrency { get; set; } = GoldCurrency.IQD;
    public decimal FxRate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public int? CashBoxId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool WeightFromScale { get; set; }
    public List<GoldSaleLineRequest> Lines { get; set; } = [];
}

public class GoldSaleLineRequest
{
    public int? ItemId { get; set; }
    public int KaratValue { get; set; }
    public decimal WeightGrams { get; set; }
    public decimal MithqalPrice { get; set; }
    public decimal MakingCharge { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool WeightFromScale { get; set; }
}

public class GoldPurchaseRequest
{
    public DateTime InvoiceDate { get; set; } = DateTime.Today;
    public GoldPaymentMethod PaymentMethod { get; set; } = GoldPaymentMethod.Cash;
    public int? CustomerId { get; set; }
    public GoldCurrency PricingCurrency { get; set; } = GoldCurrency.USD;
    public GoldCurrency PaymentCurrency { get; set; } = GoldCurrency.IQD;
    public decimal FxRate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public int? CashBoxId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool WeightFromScale { get; set; }
    public List<GoldSaleLineRequest> Lines { get; set; } = [];
}

public class GoldPaymentRequest
{
    public int InvoiceId { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public GoldCurrency Currency { get; set; } = GoldCurrency.IQD;
    public decimal FxRate { get; set; }
    public int? CashBoxId { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class GoldPricingQuote
{
    public int KaratValue { get; set; }
    public decimal WeightGrams { get; set; }
    public decimal MithqalGrams { get; set; }
    public decimal MithqalPrice { get; set; }
    public GoldCurrency PricingCurrency { get; set; }
    public decimal PricePerGram { get; set; }
    public decimal GoldValue { get; set; }
    public decimal MakingCharge { get; set; }
    public decimal LineTotal { get; set; }
    public decimal? FxRate { get; set; }
    public decimal? LineTotalIqd { get; set; }
    public decimal? LineTotalUsd { get; set; }
}

public class GoldReportSummary
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int SaleCount { get; set; }
    public int PurchaseCount { get; set; }
    public decimal TotalSalesIqd { get; set; }
    public decimal TotalSalesUsd { get; set; }
    public decimal TotalPurchasesIqd { get; set; }
    public decimal TotalPurchasesUsd { get; set; }
    public decimal TotalMakingChargesIqd { get; set; }
    public decimal TotalMakingChargesUsd { get; set; }
    public decimal TotalWeightSoldGrams { get; set; }
    public decimal TotalWeightPurchasedGrams { get; set; }
    public decimal CashInIqd { get; set; }
    public decimal CashInUsd { get; set; }
    public decimal CashOutIqd { get; set; }
    public decimal CashOutUsd { get; set; }
    public decimal CreditOutstandingIqd { get; set; }
    public decimal CreditOutstandingUsd { get; set; }
    public List<GoldStockRow> ClosingStock { get; set; } = [];
    public List<GoldInvoiceListItem> Invoices { get; set; } = [];
}
