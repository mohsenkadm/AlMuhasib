using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities;

/// <summary>موظف — سلفه تُدار عبر سندات الدفع/القبض</summary>
public class Employee : BranchScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? JobTitle { get; set; }
    public DateTime HireDate { get; set; } = DateTime.Today;
    public bool IsActive { get; set; } = true;

    /// <summary>رصيد افتتاحي لسلف الموظف (موجب = عليه سلفة).</summary>
    public decimal OpeningBalance { get; set; }

    /// <summary>عملة الرصيد الافتتاحي — افتراضي دينار للتوافق مع البيانات القديمة.</summary>
    public AccountingCurrency OpeningBalanceCurrency { get; set; } = AccountingCurrency.IQD;

    public string? Notes { get; set; }

    public ICollection<Voucher> Vouchers { get; set; } = [];
}
