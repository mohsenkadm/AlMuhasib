namespace AlMuhasib.Core.Interfaces.Services;

public interface ICustomerCreditService
{
    Task<CreditCheckResult> CheckCreditAsync(int customerId, decimal additionalAmount, bool isInstallment);
    Task UpdateReliabilityScoreAsync(int customerId);
}

public class CreditCheckResult
{
    public bool IsAllowed { get; set; }
    public string? Message { get; set; }
    public decimal CurrentDebt { get; set; }
    public decimal? Limit { get; set; }
}
