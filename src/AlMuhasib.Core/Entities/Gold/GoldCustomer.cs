namespace AlMuhasib.Core.Entities.Gold;

public class GoldCustomer : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public decimal CreditBalanceIqd { get; set; }
    public decimal CreditBalanceUsd { get; set; }
    /// <summary>Grams of gold sold on credit that the customer still owes (not reduced by cash collection).</summary>
    public decimal GoldCreditGrams { get; set; }
    public bool IsActive { get; set; } = true;
}
