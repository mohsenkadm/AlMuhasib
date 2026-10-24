namespace AlMuhasib.Core.Models.Customs;

/// <summary>
/// Portrait Customs customer receipt (وصل قبض مالي) matching the company paper form.
/// Branding header from print settings is applied by the document/PDF builders.
/// </summary>
public sealed class CustomsReceiptPrintData
{
    public string Title { get; set; } = "وصل قبض مالي (Receipt Voucher)";
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string AmountDisplay { get; set; } = string.Empty;
    public string AmountInWords { get; set; } = string.Empty;
    public string PaymentMethodLabel { get; set; } = string.Empty;
    public string PurposeText { get; set; } = string.Empty;
    public string CurrencyLabel { get; set; } = "د.ع";
    public bool IsUsd { get; set; }
    public decimal Amount { get; set; }
    public List<CustomsReceiptPrintAllocationRow> Allocations { get; set; } = [];
    public string TotalReceivedDisplay { get; set; } = string.Empty;
    public string NoteText { get; set; } = string.Empty;
    public string ReceiverSignatureLabel { get; set; } = "توقيع واستلام";
    public string PayerSignatureLabel { get; set; } = "توقيع المسلّم";
}

public sealed class CustomsReceiptPrintAllocationRow
{
    public string AccountDetails { get; set; } = string.Empty;
    public string InvoiceTotalDisplay { get; set; } = string.Empty;
    public string PaidAmountDisplay { get; set; } = string.Empty;
    public string RemainingAmountDisplay { get; set; } = string.Empty;
}
