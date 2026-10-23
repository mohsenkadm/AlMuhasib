using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Enums.Customs;

namespace AlMuhasib.Core.Models.Customs;

public sealed class CustomsCustomerListItem
{
    public int Id { get; set; }
    public CustomsCustomerType CustomerType { get; set; }
    public string CustomerTypeLabel { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string StatusLabel => IsActive ? "نشط" : "موقوف";
    public decimal BalanceIqd { get; set; }
    public decimal BalanceUsd { get; set; }
    public int OpenInvoiceCount { get; set; }
    public DateTime? LastTransactionDate { get; set; }
}

public sealed class CustomsCustomerInvoiceRow
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string ServiceTypeLabel { get; set; } = string.Empty;
    public AccountingCurrency Currency { get; set; }
    public string CurrencyLabel { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public CustomsInvoiceStatus Status { get; set; }
}

public sealed class CustomsCustomerVehicleRow
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string IraqiPlate { get; set; } = string.Empty;
    public string SyrianPlate { get; set; } = string.Empty;
    public string IraqiDriver { get; set; } = string.Empty;
    public string SyrianDriver { get; set; } = string.Empty;
    public string CargoType { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public decimal VehiclePrice { get; set; }
    public string CurrencyLabel { get; set; } = string.Empty;
}

public sealed class CustomsCustomerReceiptRow
{
    public int Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; }
    public decimal Amount { get; set; }
    public AccountingCurrency Currency { get; set; }
    public string CurrencyLabel { get; set; } = string.Empty;
    public string PaymentMethodLabel { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public sealed class CustomsAttachmentItem
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class CustomsCustomerFile
{
    public int Id { get; set; }
    public CustomsCustomerType CustomerType { get; set; }
    public string CustomerTypeLabel { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public decimal BalanceIqd { get; set; }
    public decimal BalanceUsd { get; set; }
    public decimal DebitIqd { get; set; }
    public decimal DebitUsd { get; set; }
    public decimal CreditIqd { get; set; }
    public decimal CreditUsd { get; set; }

    public int InvoiceCount { get; set; }
    public int UnpaidInvoiceCount { get; set; }
    public int VehicleCount { get; set; }
    public int ReceiptCount { get; set; }

    /// <summary>Full list totals for pagination (may include cancelled invoices).</summary>
    public int InvoicesTotalCount { get; set; }
    public int VehiclesTotalCount { get; set; }
    public int ReceiptsTotalCount { get; set; }
    public int AttachmentsTotalCount { get; set; }

    public int InvoicePage { get; set; } = 1;
    public int VehiclePage { get; set; } = 1;
    public int ReceiptPage { get; set; } = 1;
    public int AttachmentPage { get; set; } = 1;
    public int PageSize { get; set; } = 50;

    public List<CustomsCustomerInvoiceRow> Invoices { get; set; } = [];
    public List<CustomsCustomerVehicleRow> Vehicles { get; set; } = [];
    public List<CustomsCustomerReceiptRow> Receipts { get; set; } = [];
    public List<CustomsAttachmentItem> Attachments { get; set; } = [];
}
