using System.Collections.ObjectModel;

namespace AlMuhasib.UI.Models;

public sealed class InvoiceDetailDisplayModel
{
    public string Title { get; init; } = string.Empty;
    public string InvoiceNumber { get; init; } = string.Empty;
    public string DateText { get; init; } = string.Empty;
    public string? CreditDueDateText { get; init; }
    public string PartyLabel { get; init; } = string.Empty;
    public string PartyName { get; init; } = string.Empty;
    public string WarehouseName { get; init; } = string.Empty;
    public string PaymentMethod { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public string SubtotalText { get; init; } = string.Empty;
    public string DiscountText { get; init; } = string.Empty;
    public string RoundingText { get; init; } = string.Empty;
    public string GrandTotalText { get; init; } = string.Empty;
    public string? CompanyFeeText { get; init; }
    public string? PaidAmountText { get; init; }
    public string? RemainingAmountText { get; init; }
    public string? CreditStatusText { get; init; }
    public string? InstallmentSummaryText { get; init; }
    public bool HasCreditInfo { get; init; }
    public bool HasInstallments { get; init; }
    public bool HasNotes { get; init; }
    public ObservableCollection<InvoiceDetailItemRow> Items { get; init; } = [];
    public ObservableCollection<InvoiceDetailInstallmentRow> Installments { get; init; } = [];
}

public sealed class InvoiceDetailItemRow
{
    public int Number { get; init; }
    public string ItemName { get; init; } = string.Empty;
    public string QuantityText { get; init; } = string.Empty;
    public string UnitPriceText { get; init; } = string.Empty;
    public string TotalPriceText { get; init; } = string.Empty;
}

public sealed class InvoiceDetailInstallmentRow
{
    public int Number { get; init; }
    public string DueDateText { get; init; } = string.Empty;
    public string AmountText { get; init; } = string.Empty;
    public string StatusText { get; init; } = string.Empty;
}
