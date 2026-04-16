namespace AlMuhasib.Core.Interfaces.Services;

public interface IAccountingValidationService
{
    Task<ValidationSummary> ValidateAllBalancesAsync();
    Task<ValidationResult> ValidateCashBoxBalanceAsync(int cashBoxId);
    Task<ValidationResult> ValidateCustomerBalanceAsync(int customerId);
    Task<ValidationResult> ValidateSupplierBalanceAsync(int supplierId);
    Task<ValidationResult> ValidateInventoryAsync(int warehouseId);
    Task<ValidationResult> ValidateBalanceSheetAsync(DateTime date);
}

public class ValidationSummary
{
    public bool IsValid => Results.All(r => r.IsValid);
    public List<ValidationResult> Results { get; set; } = [];
    public int TotalChecks => Results.Count;
    public int PassedChecks => Results.Count(r => r.IsValid);
    public int FailedChecks => Results.Count(r => !r.IsValid);
}

public class ValidationResult
{
    public string Category { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public decimal ExpectedValue { get; set; }
    public decimal ActualValue { get; set; }
    public decimal Difference { get; set; }
    public string Message { get; set; } = string.Empty;
}
