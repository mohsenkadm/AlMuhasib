using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities;

/// <summary>المصرف</summary>
public class BankAccount : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? AccountNumber { get; set; }
    public decimal Balance { get; set; }

    /// <summary>عملة الحساب الثابتة — افتراضي دينار.</summary>
    public AccountingCurrency Currency { get; set; } = AccountingCurrency.IQD;
}
