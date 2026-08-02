using AlMuhasib.Core.Enums.Gold;

namespace AlMuhasib.Sync.Dtos;

public sealed class GoldSettingsSyncDto : SyncDtoBase
{
    public decimal MithqalGrams { get; set; } = 5;
    public string ScaleComPort { get; set; } = string.Empty;
    public int ScaleBaudRate { get; set; } = 9600;
    public decimal ScaleStabilityThresholdGrams { get; set; } = 0.01m;
    public bool AllowManualWeightEdit { get; set; } = true;
    public decimal LowStockAlertGrams { get; set; } = 10;
    public int OverdueDaysThreshold { get; set; } = 30;
    public string EnabledKaratsCsv { get; set; } = "24,22,21,18";
    public GoldMakingChargeMode DefaultMakingChargeMode { get; set; } = GoldMakingChargeMode.Fixed;
}

public sealed class GoldFxRateSyncDto : SyncDtoBase
{
    public DateTime RateDate { get; set; } = DateTime.Today;
    public decimal UsdToIqd { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class GoldKaratSyncDto : SyncDtoBase
{
    public int KaratValue { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PurityFactor { get; set; } = 1.0m;
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}

public sealed class GoldMithqalPriceSyncDto : SyncDtoBase
{
    public DateTime PriceDate { get; set; } = DateTime.Today;
    public int KaratValue { get; set; }
    public decimal PricePerMithqal { get; set; }
    public GoldCurrency Currency { get; set; } = GoldCurrency.USD;
    public decimal? FxRateUsed { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class GoldItemSyncDto : SyncDtoBase
{
    public string Name { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public int KaratValue { get; set; }
    public decimal WeightGrams { get; set; }
    public decimal SuggestedMakingCharge { get; set; }
    public GoldCurrency MakingChargeCurrency { get; set; } = GoldCurrency.IQD;
    public decimal CostPerGram { get; set; }
    public GoldItemStatus Status { get; set; } = GoldItemStatus.InStock;
    public bool TrackAsPiece { get; set; } = true;
}

public sealed class GoldStockBalanceSyncDto : SyncDtoBase
{
    public int WarehouseId { get; set; }
    public Guid? WarehouseSyncId { get; set; }
    public int KaratValue { get; set; }
    public decimal GramsOnHand { get; set; }
    public decimal AverageCostPerGram { get; set; }
}

public sealed class GoldCustomerSyncDto : SyncDtoBase
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public decimal CreditBalanceIqd { get; set; }
    public decimal CreditBalanceUsd { get; set; }
    public decimal GoldCreditGrams { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class GoldSupplierSyncDto : SyncDtoBase
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public decimal CreditBalanceIqd { get; set; }
    public decimal CreditBalanceUsd { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class GoldWarehouseSyncDto : SyncDtoBase
{
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public string Notes { get; set; } = string.Empty;
}

public sealed class GoldExpenseTypeSyncDto : SyncDtoBase
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class GoldExpenseSyncDto : SyncDtoBase
{
    public DateTime ExpenseDate { get; set; } = DateTime.Today;
    public Guid ExpenseTypeSyncId { get; set; }
    public decimal Amount { get; set; }
    public GoldCurrency Currency { get; set; } = GoldCurrency.IQD;
    public Guid CashBoxSyncId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public Guid? WarehouseSyncId { get; set; }
}

public sealed class GoldWarehouseTransferSyncDto : SyncDtoBase
{
    public DateTime TransferDate { get; set; } = DateTime.Today;
    public Guid FromWarehouseSyncId { get; set; }
    public Guid ToWarehouseSyncId { get; set; }
    public int KaratValue { get; set; }
    public decimal WeightGrams { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class GoldCashBoxSyncDto : SyncDtoBase
{
    public string Name { get; set; } = string.Empty;
    public GoldCurrency Currency { get; set; } = GoldCurrency.IQD;
    public decimal Balance { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class GoldInvoiceSyncDto : SyncDtoBase
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; } = DateTime.Today;
    public GoldInvoiceType InvoiceType { get; set; } = GoldInvoiceType.Sale;
    public GoldPaymentMethod PaymentMethod { get; set; } = GoldPaymentMethod.Cash;
    public GoldInvoiceStatus Status { get; set; } = GoldInvoiceStatus.Completed;
    public Guid? CustomerSyncId { get; set; }
    public Guid? SupplierSyncId { get; set; }
    public Guid? WarehouseSyncId { get; set; }
    public bool IsExchange { get; set; }
    public decimal ExchangeCashDifference { get; set; }
    public GoldCurrency PricingCurrency { get; set; } = GoldCurrency.USD;
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
    public Guid? CashBoxSyncId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool WeightFromScale { get; set; }
    public Guid? RelatedInvoiceSyncId { get; set; }
}

public sealed class GoldInvoiceLineSyncDto : SyncDtoBase
{
    public Guid InvoiceSyncId { get; set; }
    public Guid? ItemSyncId { get; set; }
    public int KaratValue { get; set; }
    public decimal WeightGrams { get; set; }
    public decimal MithqalPrice { get; set; }
    public decimal PricePerGram { get; set; }
    public decimal GoldValue { get; set; }
    public decimal MakingCharge { get; set; }
    public GoldMakingChargeMode MakingChargeMode { get; set; } = GoldMakingChargeMode.Fixed;
    public decimal MakingChargeRate { get; set; }
    public decimal LineTotal { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool WeightFromScale { get; set; }
    public GoldInvoiceLineDirection LineDirection { get; set; } = GoldInvoiceLineDirection.Out;
}

public sealed class GoldPaymentSyncDto : SyncDtoBase
{
    public Guid InvoiceSyncId { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public GoldCurrency Currency { get; set; } = GoldCurrency.IQD;
    public decimal FxRate { get; set; }
    public Guid? CashBoxSyncId { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class GoldVoucherSyncDto : SyncDtoBase
{
    public string VoucherNumber { get; set; } = string.Empty;
    public DateTime VoucherDate { get; set; } = DateTime.Today;
    public GoldVoucherType VoucherType { get; set; } = GoldVoucherType.Receipt;
    public GoldCurrency Currency { get; set; } = GoldCurrency.IQD;
    public decimal Amount { get; set; }
    public Guid? CashBoxSyncId { get; set; }
    public Guid? CustomerSyncId { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class GoldNotificationSyncDto : SyncDtoBase
{
    public GoldNotificationType Type { get; set; } = GoldNotificationType.Info;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? RelatedEntity { get; set; }
    public int? RelatedId { get; set; }
}
