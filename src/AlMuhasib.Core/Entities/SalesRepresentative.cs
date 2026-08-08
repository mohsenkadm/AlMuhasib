namespace AlMuhasib.Core.Entities;

/// <summary>مندوب المبيعات — يُفعَّل عبر ميزة المندوبين</summary>
public class SalesRepresentative : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }

    /// <summary>المنطقة / المحافظة</summary>
    public string? Region { get; set; }

    /// <summary>تاريخ المباشرة</summary>
    public DateTime StartDate { get; set; } = DateTime.Today;

    public bool IsActive { get; set; } = true;

    /// <summary>راتب شهري ثابت (اختياري)</summary>
    public decimal? MonthlySalary { get; set; }

    /// <summary>ملاحظات عامة عن الراتب/العمولة</summary>
    public string? CompensationNotes { get; set; }

    public string? Notes { get; set; }

    public ICollection<Customer> Customers { get; set; } = [];
    public ICollection<Invoice> Invoices { get; set; } = [];
    public ICollection<SalesRepCommissionRule> CommissionRules { get; set; } = [];
    public ICollection<SalesRepCommissionEntry> CommissionEntries { get; set; } = [];
    public ICollection<SalesRepTarget> Targets { get; set; } = [];
    public ICollection<SalesRepCollection> Collections { get; set; } = [];
}
