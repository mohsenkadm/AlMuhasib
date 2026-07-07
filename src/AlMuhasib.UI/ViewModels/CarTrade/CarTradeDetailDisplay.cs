using AlMuhasib.Core.Entities.CarTrade;
using AlMuhasib.Core.Enums;
using System.Collections.ObjectModel;

namespace AlMuhasib.UI.ViewModels.CarTrade;

public sealed class CarTradeDetailDisplay
{
    public int Id { get; init; }
    public string TransactionNumber { get; init; } = string.Empty;
    public string TransactionDate { get; init; } = string.Empty;
    public string TradeType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string CarName { get; init; } = string.Empty;
    public string CarColor { get; init; } = string.Empty;
    public string PlateNumber { get; init; } = string.Empty;
    public string ChassisNumber { get; init; } = string.Empty;
    public string CarType { get; init; } = string.Empty;
    public string SellerName { get; init; } = string.Empty;
    public string SellerPhone { get; init; } = string.Empty;
    public string BuyerName { get; init; } = string.Empty;
    public string BuyerPhone { get; init; } = string.Empty;
    public string PurchasePrice { get; init; } = string.Empty;
    public string SalePrice { get; init; } = string.Empty;
    public string TotalAmount { get; init; } = string.Empty;
    public string PaymentMode { get; init; } = string.Empty;
    public string AmountPaid { get; init; } = string.Empty;
    public string RemainingAmount { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
    public ObservableCollection<CarTradePaymentDisplay> Payments { get; init; } = [];

    public static CarTradeDetailDisplay FromEntity(CarTradeTransaction t) => new()
    {
        Id = t.Id,
        TransactionNumber = t.TransactionNumber,
        TransactionDate = t.TransactionDate.ToString("yyyy/MM/dd"),
        TradeType = GetTradeTypeLabel(t.TradeType),
        Status = GetStatusLabel(t.Status),
        CarName = Display(t.CarName),
        CarColor = Display(t.CarColor),
        PlateNumber = Display(t.PlateNumber),
        ChassisNumber = Display(t.ChassisNumber),
        CarType = Display(t.CarType),
        SellerName = Display(t.SellerName),
        SellerPhone = Display(t.SellerPhone),
        BuyerName = Display(t.BuyerName),
        BuyerPhone = Display(t.BuyerPhone),
        PurchasePrice = t.PurchasePrice.ToString("N0"),
        SalePrice = t.SalePrice.ToString("N0"),
        TotalAmount = t.TotalAmount.ToString("N0"),
        PaymentMode = GetPaymentModeLabel(t.PaymentMode),
        AmountPaid = t.AmountPaid.ToString("N0"),
        RemainingAmount = t.RemainingAmount.ToString("N0"),
        Notes = Display(t.Notes),
        Payments = new ObservableCollection<CarTradePaymentDisplay>(
            t.Payments.OrderByDescending(p => p.PaymentDate).Select(CarTradePaymentDisplay.FromEntity))
    };

    private static string Display(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();

    private static string GetTradeTypeLabel(CarTradeType type) => type switch
    {
        CarTradeType.Sell => "بيع",
        _ => "شراء"
    };

    private static string GetPaymentModeLabel(CarTradePaymentMode mode) => mode switch
    {
        CarTradePaymentMode.FullCash => "نقد كامل",
        _ => "دفع جزئي"
    };

    private static string GetStatusLabel(CarTradeStatus status) => status switch
    {
        CarTradeStatus.Completed => "مكتمل",
        CarTradeStatus.Cancelled => "ملغى",
        _ => "نشط"
    };
}

public sealed class CarTradePaymentDisplay
{
    public string PaymentDate { get; init; } = string.Empty;
    public string Amount { get; init; } = string.Empty;
    public string RemainingBefore { get; init; } = string.Empty;
    public string RemainingAfter { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;

    public static CarTradePaymentDisplay FromEntity(CarTradePayment payment) => new()
    {
        PaymentDate = payment.PaymentDate.ToString("yyyy/MM/dd"),
        Amount = payment.Amount.ToString("N0"),
        RemainingBefore = payment.RemainingBefore.ToString("N0"),
        RemainingAfter = payment.RemainingAfter.ToString("N0"),
        Notes = string.IsNullOrWhiteSpace(payment.Notes) ? "—" : payment.Notes
    };
}
