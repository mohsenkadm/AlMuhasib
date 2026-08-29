using System.Collections.ObjectModel;

namespace AlMuhasib.UI.Models;

public sealed class GoldInvoiceDetailDisplayModel
{
    public string Title { get; init; } = string.Empty;
    public string InvoiceNumber { get; init; } = string.Empty;
    public string DateText { get; init; } = string.Empty;
    public string InvoiceTypeText { get; init; } = string.Empty;
    public string PartyLabel { get; init; } = string.Empty;
    public string PartyName { get; init; } = string.Empty;
    public string WarehouseName { get; init; } = string.Empty;
    public string PaymentMethod { get; init; } = string.Empty;
    public string StatusText { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public string GoldValueText { get; init; } = string.Empty;
    public string MakingChargeText { get; init; } = string.Empty;
    public string DiscountText { get; init; } = string.Empty;
    public string WeightText { get; init; } = string.Empty;
    public string GrandTotalText { get; init; } = string.Empty;
    public string? PaidAmountText { get; init; }
    public string? RemainingAmountText { get; init; }
    public bool HasCreditInfo { get; init; }
    public bool HasNotes { get; init; }
    public bool HasPayments { get; init; }
    public ObservableCollection<GoldInvoiceDetailLineRow> Lines { get; init; } = [];
    public ObservableCollection<GoldInvoiceDetailPaymentRow> Payments { get; init; } = [];
}

public sealed class GoldInvoiceDetailLineRow
{
    public int Number { get; init; }
    public string Description { get; init; } = string.Empty;
    public string KaratText { get; init; } = string.Empty;
    public string WeightText { get; init; } = string.Empty;
    public string GoldValueText { get; init; } = string.Empty;
    public string MakingChargeText { get; init; } = string.Empty;
    public string LineTotalText { get; init; } = string.Empty;
}

public sealed class GoldInvoiceDetailPaymentRow
{
    public int Number { get; init; }
    public string DateText { get; init; } = string.Empty;
    public string AmountText { get; init; } = string.Empty;
    public string CurrencyText { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
}
