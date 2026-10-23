using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Enums.Customs;

namespace AlMuhasib.Core.Models.Customs;

public sealed class CustomsReceiptListItem
{
    public int Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public AccountingCurrency Currency { get; set; }
    public string CurrencyLabel { get; set; } = string.Empty;
    public CustomsPaymentMethod PaymentMethod { get; set; }
    public string PaymentMethodLabel { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public int AllocationCount { get; set; }
    public bool CanEdit => true;
    public bool CanCancel => true;
}

public sealed class CustomsReceiptAllocationDraft
{
    public int? Id { get; set; }
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public sealed class CustomsReceiptSaveRequest
{
    public int CustomerId { get; set; }
    public DateTime ReceiptDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public AccountingCurrency Currency { get; set; } = AccountingCurrency.IQD;
    public decimal? FxRate { get; set; }
    public CustomsPaymentMethod PaymentMethod { get; set; } = CustomsPaymentMethod.Cash;
    public string ReceiverName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public List<CustomsReceiptAllocationDraft> Allocations { get; set; } = [];
}

public sealed class CustomsReceiptDetail
{
    public int Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public decimal Amount { get; set; }
    public AccountingCurrency Currency { get; set; }
    public string CurrencyLabel { get; set; } = string.Empty;
    public decimal FxRate { get; set; }
    public CustomsPaymentMethod PaymentMethod { get; set; }
    public string PaymentMethodLabel { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public List<CustomsReceiptAllocationDraft> Allocations { get; set; } = [];
    public List<CustomsAttachmentItem> Attachments { get; set; } = [];
    public bool CanEdit => true;
    public bool CanCancel => true;
}

public sealed class CustomsOpenInvoiceItem
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public AccountingCurrency Currency { get; set; }
    public string CurrencyLabel { get; set; } = string.Empty;
}
