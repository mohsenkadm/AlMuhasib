using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Enums.Customs;

namespace AlMuhasib.Core.Models.Customs;

public sealed class CustomsInvoiceListItem
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public CustomsServiceType ServiceType { get; set; }
    public string ServiceTypeLabel { get; set; } = string.Empty;
    public AccountingCurrency Currency { get; set; }
    public string CurrencyLabel { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public CustomsInvoiceStatus Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public int VehicleCount { get; set; }
    public bool CanApprove => Status == CustomsInvoiceStatus.Draft;
    public bool CanCancel => Status != CustomsInvoiceStatus.Cancelled && PaidAmount <= 0;
    public bool CanEdit => Status != CustomsInvoiceStatus.Cancelled;
}

public sealed class CustomsInvoiceVehicleDraft
{
    public int? Id { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string IraqiPlate { get; set; } = string.Empty;
    public string IraqiDriver { get; set; } = string.Empty;
    public string SyrianPlate { get; set; } = string.Empty;
    public string SyrianDriver { get; set; } = string.Empty;
    public string CargoType { get; set; } = string.Empty;
    public DateTime? LoadingDate { get; set; }
    public DateTime? ExitDate { get; set; }
    public DateTime? WarehouseArrivalDate { get; set; }
    public DateTime? UnloadingDate { get; set; }
    public decimal VehiclePrice { get; set; }
    public CustomsVehicleStatus Status { get; set; } = CustomsVehicleStatus.InProgress;
    public string Notes { get; set; } = string.Empty;
}

public sealed class CustomsInvoiceExpenseDraft
{
    public int? Id { get; set; }
    public int ExpenseTypeId { get; set; }
    public string ExpenseTypeName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public AccountingCurrency Currency { get; set; } = AccountingCurrency.IQD;
    public decimal FxRate { get; set; } = 1m;
    public DateTime ExpenseDate { get; set; } = DateTime.Today;
    public string Recipient { get; set; } = string.Empty;
}

public sealed class CustomsInvoiceSaveRequest
{
    public int CustomerId { get; set; }
    public DateTime InvoiceDate { get; set; } = DateTime.Today;
    public CustomsServiceType ServiceType { get; set; } = CustomsServiceType.Clearance;
    public AccountingCurrency Currency { get; set; } = AccountingCurrency.IQD;
    public decimal? FxRate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
    public string InternalNotes { get; set; } = string.Empty;
    public string CustomerNotes { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public List<CustomsInvoiceVehicleDraft> Vehicles { get; set; } = [];
    public List<CustomsInvoiceExpenseDraft> Expenses { get; set; } = [];
}

public sealed class CustomsInvoiceDetail
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public CustomsServiceType ServiceType { get; set; }
    public string ServiceTypeLabel { get; set; } = string.Empty;
    public AccountingCurrency Currency { get; set; }
    public string CurrencyLabel { get; set; } = string.Empty;
    public decimal FxRate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
    public decimal VehiclesSum { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public CustomsInvoiceStatus Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public string InternalNotes { get; set; } = string.Empty;
    public string CustomerNotes { get; set; } = string.Empty;
    public List<CustomsInvoiceVehicleDraft> Vehicles { get; set; } = [];
    public List<CustomsInvoiceExpenseDraft> Expenses { get; set; } = [];
    public List<CustomsAttachmentItem> Attachments { get; set; } = [];
    public bool CanApprove => Status == CustomsInvoiceStatus.Draft;
    public bool CanCancel => Status != CustomsInvoiceStatus.Cancelled && PaidAmount <= 0;
    public bool CanEdit => Status != CustomsInvoiceStatus.Cancelled;
}

public sealed class CustomsExpenseTypeItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
