using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities.CarTrade;

public class CarTradeTransaction : BaseEntity
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

    public ICollection<CarTradePayment> Payments { get; set; } = [];
}
