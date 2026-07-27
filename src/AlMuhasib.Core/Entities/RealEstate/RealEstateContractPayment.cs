namespace AlMuhasib.Core.Entities.RealEstate;

public class RealEstateContractPayment : BaseEntity
{
    public int ContractId { get; set; }
    public RealEstateContract Contract { get; set; } = null!;

    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public decimal RemainingBefore { get; set; }
    public decimal RemainingAfter { get; set; }
}
