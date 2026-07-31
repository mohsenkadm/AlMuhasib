namespace AlMuhasib.Core.Entities;

/// <summary>السواقين — لتوصيل فواتير البيع عند تفعيل ميزة نسخة المخزن والسائق</summary>
public class Driver : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }

    public ICollection<Invoice> Invoices { get; set; } = [];
}
