namespace AlMuhasib.Core.Entities;

/// <summary>القاصة/الصندوق</summary>
public class CashBox : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}
