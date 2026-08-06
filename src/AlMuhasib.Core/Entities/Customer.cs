namespace AlMuhasib.Core.Entities;

/// <summary>العملاء</summary>
public class Customer : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? FileNumber { get; set; }
    public string? Notes { get; set; }

    /// <summary>حد أقصى للدين الآجل.</summary>
    public decimal? MaxCreditLimit { get; set; }

    /// <summary>حد أقصى لدين الأقساط.</summary>
    public decimal? MaxInstallmentDebt { get; set; }

    /// <summary>درجة موثوقية (0-100).</summary>
    public int ReliabilityScore { get; set; } = 50;

    /// <summary>اسم الضامن/الكفيل.</summary>
    public string? GuarantorName { get; set; }

    /// <summary>هاتف الضامن.</summary>
    public string? GuarantorPhone { get; set; }

    /// <summary>قيم الحقول المخصصة JSON — مفاتيح cf1..cf8.</summary>
    public string? CustomFieldsJson { get; set; }

    // Navigation
    public ICollection<Invoice> Invoices { get; set; } = [];
    public ICollection<InstallmentPlan> InstallmentPlans { get; set; } = [];
    public ICollection<Voucher> Vouchers { get; set; } = [];
    public ICollection<CustomerAttachment> Attachments { get; set; } = [];
}
