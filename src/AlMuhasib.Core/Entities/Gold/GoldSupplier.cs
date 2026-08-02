namespace AlMuhasib.Core.Entities.Gold;

public class GoldSupplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public decimal CreditBalanceIqd { get; set; }
    public decimal CreditBalanceUsd { get; set; }
    public bool IsActive { get; set; } = true;
}
