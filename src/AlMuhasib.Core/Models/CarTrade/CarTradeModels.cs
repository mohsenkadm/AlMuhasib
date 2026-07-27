using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Car;

namespace AlMuhasib.Core.Models.CarTrade;

public class CarTradeFilter
{
    public string? SearchText { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public CarTradeType? TradeType { get; set; }
    public CarTradeStatusFilter StatusFilter { get; set; } = CarTradeStatusFilter.All;
    public CarTradePaymentMode? PaymentMode { get; set; }
    public bool UnpaidOnly { get; set; }
    public CarTradeSoldFilter SoldFilter { get; set; } = CarTradeSoldFilter.All;
}

public enum CarTradeStatusFilter
{
    All,
    Active,
    Completed,
    Cancelled
}

public enum CarTradeSoldFilter
{
    All,
    Available,
    Sold
}

public class CarTradeListItem
{
    public int Id { get; set; }
    public string TransactionNumber { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string TradeType { get; set; } = string.Empty;
    public CarTradeType TradeTypeValue { get; set; }
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
    public string PaymentMode { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
    public decimal RemainingAmount { get; set; }
    public bool IsSold { get; set; }
    public string SoldStatus { get; set; } = string.Empty;
    public DateTime? SaleDate { get; set; }
    public string SalePaymentMode { get; set; } = string.Empty;
    public decimal SaleAmountPaid { get; set; }
    public decimal SaleRemainingAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public bool CanSell => !IsSold;
    public bool CanPaySeller => RemainingAmount > 0;
    public bool CanPayBuyer => IsSold && SaleRemainingAmount > 0;
}

public class CarTradeSellRequest
{
    public string BuyerName { get; set; } = string.Empty;
    public string BuyerPhone { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public CarTradePaymentMode SalePaymentMode { get; set; } = CarTradePaymentMode.FullCash;
    public decimal SaleAmountPaid { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.Today;
    public string? Notes { get; set; }
}

public class CarTradeDashboardStats
{
    public int TodayTransactions { get; set; }
    public int MonthTransactions { get; set; }
    public int TotalTransactions { get; set; }
    public int UnpaidTransactions { get; set; }
    public int BuyCount { get; set; }
    public int SellCount { get; set; }
    public int AvailableCount { get; set; }
    public int SoldCount { get; set; }
    public decimal TotalBuyValue { get; set; }
    public decimal TotalSellValue { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalRemaining { get; set; }
    public decimal TotalSaleRemaining { get; set; }
    public List<NameCountPoint> MonthlyBuy { get; set; } = [];
    public List<NameCountPoint> MonthlySell { get; set; } = [];
    public List<NameAmountPoint> PaymentStatusChart { get; set; } = [];
    public List<NameCountPoint> TopCarTypes { get; set; } = [];
    public List<CarTradeListItem> RecentTransactions { get; set; } = [];
}

public class CarTradeReportData
{
    public List<CarTradeListItem> Rows { get; set; } = [];
    public int BuyCount { get; set; }
    public int SellCount { get; set; }
    public int AvailableCount { get; set; }
    public int SoldCount { get; set; }
    public decimal TotalBuyValue { get; set; }
    public decimal TotalSellValue { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalRemaining { get; set; }
    public decimal TotalSaleRemaining { get; set; }
    public List<NameCountPoint> MonthlyBuy { get; set; } = [];
    public List<NameCountPoint> MonthlySell { get; set; } = [];
    public List<NameAmountPoint> CollectedVsRemaining { get; set; } = [];
    public List<NameCountPoint> ByCarType { get; set; } = [];
}

public class CarTradePartyStatementFilter
{
    public string PartyName { get; set; } = string.Empty;
    public string? PartyPhone { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

public class CarTradePartyStatementRow
{
    public int TransactionId { get; set; }
    public DateTime TransactionDate { get; set; }
    public string TransactionNumber { get; set; } = string.Empty;
    public string TradeType { get; set; } = string.Empty;
    public string CarName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RemainingAmount { get; set; }
    public string PartyRole { get; set; } = string.Empty;
    public string DebtKind { get; set; } = string.Empty;
    public string PartyPhone { get; set; } = string.Empty;

    public bool IsSellerDebt => PartyRole == "بائع";
    public bool IsBuyerDebt => PartyRole == "مشتري";
    public bool CanSettle => RemainingAmount > 0;
}

public class CarTradePartyStatementData
{
    public string PartyName { get; set; } = string.Empty;
    public string PartyPhone { get; set; } = string.Empty;
    public List<CarTradePartyStatementRow> Rows { get; set; } = [];
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal Balance { get; set; }
}

public class CarTradeDebtSummaryRow
{
    public string PartyName { get; set; } = string.Empty;
    public string PartyPhone { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RemainingAmount { get; set; }
}
