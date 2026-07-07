namespace AlMuhasib.Core.Entities.CarTrade;

public class CarTradePayment : BaseEntity
{
    public int TransactionId { get; set; }
    public CarTradeTransaction Transaction { get; set; } = null!;

    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public decimal RemainingBefore { get; set; }
    public decimal RemainingAfter { get; set; }
}
