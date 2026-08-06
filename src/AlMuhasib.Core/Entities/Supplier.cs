namespace AlMuhasib.Core.Entities;

/// <summary>الموردون</summary>
public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }

    /// <summary>قيم الحقول المخصصة JSON — مفاتيح cf1..cf8.</summary>
    public string? CustomFieldsJson { get; set; }

    // Navigation
    public ICollection<Invoice> Invoices { get; set; } = [];
}
