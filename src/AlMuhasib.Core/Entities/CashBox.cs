using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;

namespace AlMuhasib.Core.Entities;

/// <summary>القاصة/الصندوق</summary>
public class CashBox : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }

    /// <summary>عملة القاصة الثابتة — افتراضي دينار للحفاظ على التوافق مع البيانات القديمة.</summary>
    public AccountingCurrency Currency { get; set; } = AccountingCurrency.IQD;

    public string DisplayNameWithCurrency =>
        $"{Name} ({AccountingCurrencyHelper.GetLabel(Currency)})";
}
