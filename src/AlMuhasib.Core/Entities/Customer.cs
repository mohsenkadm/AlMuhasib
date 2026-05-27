namespace AlMuhasib.Core.Entities;

/// <summary>العملاء</summary>
public class Customer : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? FileNumber { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public ICollection<Invoice> Invoices { get; set; } = [];
    public ICollection<InstallmentPlan> InstallmentPlans { get; set; } = [];
    public ICollection<Voucher> Vouchers { get; set; } = [];
    public ICollection<CustomerAttachment> Attachments { get; set; } = [];
}
