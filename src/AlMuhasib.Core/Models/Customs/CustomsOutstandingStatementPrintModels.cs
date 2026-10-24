using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Models.Customs;

/// <summary>
/// Outstanding-balance claim form (مطالبة بدين متبقي) for Customs customer statement print.
/// Branding header from print settings is applied by the document/PDF builders.
/// </summary>
public sealed class CustomsOutstandingStatementPrintData
{
    public string Title { get; set; } = "مطالبة بدين متبقي (Outstanding Balance Statement)";
    public string DocumentNumber { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; } = DateTime.Today;
    public string CustomerName { get; set; } = string.Empty;
    public string InvoiceReferenceText { get; set; } = string.Empty;
    public string AccountStatusText { get; set; } = "دين متبقي مستحق الدفع";
    public string PaidColumnHeader { get; set; } = "المبلغ المسدد (الواصل)";
    public string CurrencyLabel { get; set; } = "د.ع";
    public bool IsUsd { get; set; }
    public List<CustomsOutstandingStatementPrintRow> Rows { get; set; } = [];
    public string BaseInvoiceTotalLabel { get; set; } = string.Empty;
    public string BaseInvoiceTotalDisplay { get; set; } = string.Empty;
    public string ReceiptsTotalLabel { get; set; } = string.Empty;
    public string ReceiptsTotalDisplay { get; set; } = string.Empty;
    public string RemainingLabel { get; set; } = "الرصيد المتبقي (دين مستحق الدفع)";
    public string RemainingDisplay { get; set; } = string.Empty;
    public string AmountInWords { get; set; } = string.Empty;
    public string BalanceDueDisplay { get; set; } = string.Empty;
    public decimal RemainingTotal { get; set; }
    public string OrganizerSignatureLabel { get; set; } = "منظّم المطالبة";
    public string PayerSignatureLabel { get; set; } = "إقرار وتعهد بالدفع";
}

public sealed class CustomsOutstandingStatementPrintRow
{
    public int Number { get; set; }
    public string Description { get; set; } = string.Empty;
    public string InvoiceTotalDisplay { get; set; } = string.Empty;
    public string PaidAmountDisplay { get; set; } = string.Empty;
    public string RemainingAmountDisplay { get; set; } = string.Empty;
}

/// <summary>Domain payload used to build the outstanding-balance print form.</summary>
public sealed class CustomsOutstandingClaimDetail
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public DateTime DocumentDate { get; set; } = DateTime.Today;
    public string DocumentNumber { get; set; } = string.Empty;
    public AccountingCurrency Currency { get; set; } = AccountingCurrency.IQD;
    public List<CustomsOutstandingClaimInvoice> Invoices { get; set; } = [];
}

public sealed class CustomsOutstandingClaimInvoice
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ServiceLabel { get; set; } = string.Empty;
    public int VehicleCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public AccountingCurrency Currency { get; set; } = AccountingCurrency.IQD;
    public List<string> ReceiptNumbers { get; set; } = [];
}
