using System.ComponentModel.DataAnnotations.Schema;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities;

/// <summary>إيراد آخر نقدي بلا ذمة على زبون أو موظف. يزيد القاصة ويُضاف بعد مجمل الربح.</summary>
public class OtherIncome : BranchScopedEntity
{
    public string IncomeNumber { get; set; } = string.Empty;

    public int OtherIncomeTypeId { get; set; }

    public AccountingCurrency Currency { get; set; } = AccountingCurrency.IQD;

    /// <summary>سعر الصرف وقت الحفظ (دولار→دينار).</summary>
    public decimal FxRate { get; set; } = 1m;

    public decimal Amount { get; set; }

    public DateTime Date { get; set; }

    public int CashBoxId { get; set; }

    public string? Notes { get; set; }

    public OtherIncomeType OtherIncomeType { get; set; } = null!;

    public CashBox CashBox { get; set; } = null!;

    [NotMapped]
    public string CurrencyLabel => Currency == AccountingCurrency.USD ? "$" : "د.ع";
}
