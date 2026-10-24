using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Enums.Customs;

namespace AlMuhasib.Core.Entities.Customs;

public class CustomsInvoice : BaseEntity
{
    public int CustomerId { get; set; }
    public CustomsCustomer Customer { get; set; } = null!;

    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; } = DateTime.Today;
    public CustomsServiceType ServiceType { get; set; } = CustomsServiceType.Clearance;
    /// <summary>Internal/External when <see cref="ServiceType"/> is Transport; ignored otherwise.</summary>
    public CustomsTransportKind? TransportKind { get; set; }
    /// <summary>Internal transport route, e.g. من منفذ ربيعة ← إلى بغداد.</summary>
    public string TransportRoute { get; set; } = string.Empty;
    public AccountingCurrency Currency { get; set; } = AccountingCurrency.IQD;
    public decimal FxRate { get; set; } = 1m;
    public string Description { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public CustomsInvoiceStatus Status { get; set; } = CustomsInvoiceStatus.Draft;
    public string InternalNotes { get; set; } = string.Empty;
    public string CustomerNotes { get; set; } = string.Empty;

    public ICollection<CustomsVehicle> Vehicles { get; set; } = [];
    public ICollection<CustomsExpense> Expenses { get; set; } = [];
    public ICollection<CustomsReceiptAllocation> ReceiptAllocations { get; set; } = [];
}
