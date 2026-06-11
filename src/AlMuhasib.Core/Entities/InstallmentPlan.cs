using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities;

/// <summary>خطة الأقساط</summary>
public class InstallmentPlan : BaseEntity
{
    public int InvoiceId { get; set; }
    public int CustomerId { get; set; }
    public string? FileNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public int NumberOfInstallments { get; set; }
    public decimal InstallmentAmount { get; set; }
    public DateTime StartDate { get; set; }
    public InstallmentType InstallmentType { get; set; } = InstallmentType.Manual;
    /// <summary>نسبة الشركة (مثلاً 0.08 = 8%)</summary>
    public decimal CompanyFeePercentage { get; set; }
    /// <summary>مبلغ نسبة الشركة</summary>
    public decimal CompanyFeeAmount { get; set; }

    /// <summary>اسم الضامن/الكفيل.</summary>
    public string? GuarantorName { get; set; }

    /// <summary>هاتف الضامن.</summary>
    public string? GuarantorPhone { get; set; }

    /// <summary>معرّف البائع/المستخدم المسؤول.</summary>
    public int? SalespersonUserId { get; set; }

    /// <summary>نسبة عمولة التحصيل (0-1).</summary>
    public decimal CollectionCommissionRate { get; set; }

    // Navigation
    public Invoice Invoice { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public ICollection<Installment> Installments { get; set; } = [];
}
