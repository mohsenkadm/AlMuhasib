using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities;

/// <summary>سجل صيانة جهاز — يظهر عند تفعيل قالب محلات الموبايلات.</summary>
public class MaintenanceRecord : BranchScopedEntity
{
    public string RecordNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Today;
    public int CustomerId { get; set; }
    public int? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>سيريال الجهاز قبل آخر استبدال (إن وُجد).</summary>
    public string? PreviousSerialNumber { get; set; }

    public decimal CostAmount { get; set; }
    public string? Notes { get; set; }
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.UnderRepair;

    /// <summary>يُدخل بعد التسليم.</summary>
    public DateTime? WarrantyEndDate { get; set; }

    public int? CashBoxId { get; set; }
    public AccountingCurrency Currency { get; set; } = AccountingCurrency.IQD;
    public decimal FxRate { get; set; } = 1m;
    public int? VoucherId { get; set; }

    public Customer Customer { get; set; } = null!;
    public Product? Product { get; set; }
    public CashBox? CashBox { get; set; }
    public Voucher? Voucher { get; set; }
}
