namespace AlMuhasib.Core.Interfaces.Services;

public interface ICustomerStatementQuickService
{
    Task<CustomerQuickStatementResult> GetStatementAsync(int customerId, CancellationToken cancellationToken = default);
    Task<string> ExportToPdfAsync(int customerId, string filePath, CancellationToken cancellationToken = default);
    void Print(int customerId);
}

public class CustomerQuickStatementResult
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public decimal Balance { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public int OverdueInstallmentCount { get; set; }
    public decimal OverdueInstallmentAmount { get; set; }
    public List<CustomerQuickStatementLine> Lines { get; set; } = [];
}

public class CustomerQuickStatementLine
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal RunningBalance { get; set; }
}
