using AlMuhasib.Core.Enums;

namespace AlMuhasib.Sync.Dtos;

public sealed class CarTradeTransactionSyncDto : SyncDtoBase
{
    public string TransactionNumber { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; } = DateTime.Today;
    public CarTradeType TradeType { get; set; } = CarTradeType.Buy;

    public string CarName { get; set; } = string.Empty;
    public string CarColor { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;
    public string ChassisNumber { get; set; } = string.Empty;
    public string CarType { get; set; } = string.Empty;

    public string SellerName { get; set; } = string.Empty;
    public string SellerPhone { get; set; } = string.Empty;
    public string BuyerName { get; set; } = string.Empty;
    public string BuyerPhone { get; set; } = string.Empty;

    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal TotalAmount { get; set; }

    public CarTradePaymentMode PaymentMode { get; set; } = CarTradePaymentMode.FullCash;
    public decimal AmountPaid { get; set; }
    public decimal RemainingAmount { get; set; }

    public CarTradeStatus Status { get; set; } = CarTradeStatus.Active;
    public string Notes { get; set; } = string.Empty;
}

public sealed class CarTradePaymentSyncDto : SyncDtoBase
{
    public Guid TransactionSyncId { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public decimal RemainingBefore { get; set; }
    public decimal RemainingAfter { get; set; }
}
