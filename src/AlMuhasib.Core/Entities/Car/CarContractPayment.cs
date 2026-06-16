namespace AlMuhasib.Core.Entities.Car;

public class CarContractPayment : BaseEntity
{
    public int ContractId { get; set; }
    public CarSaleContract Contract { get; set; } = null!;

    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public decimal RemainingBefore { get; set; }
    public decimal RemainingAfter { get; set; }
}
