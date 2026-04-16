namespace AlMuhasib.Core.Entities;

/// <summary>الموردون</summary>
public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public ICollection<Invoice> Invoices { get; set; } = [];
}
