namespace AlMuhasib.Core.Models;

public class PlatformDeductionImportRow
{
    public int RowNumber { get; set; }
    public string? PlatformInvoiceId { get; set; }
    public string? DeductionId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? MotherName { get; set; }
    public string? GovernmentNumber { get; set; }
    public decimal RequestedAmount { get; set; }
    public decimal DeductedAmount { get; set; }
    public DateTime? DeductionDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? DeductionStatus { get; set; }
    public string? CustomerCategory { get; set; }
    public List<string> Errors { get; } = [];
    public bool HasErrors => Errors.Count > 0;
}

public enum PlatformDeductionMatchStatus
{
    Matched = 0,
    Suggested = 1,
    NotFound = 2,
    Invalid = 3
}

public class PlatformDeductionPayResult
{
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public decimal TotalPaid { get; set; }
    public List<string> Errors { get; } = [];
    public List<string> Messages { get; } = [];
}

public class CustomerAmountPayResult
{
    public decimal AmountApplied { get; set; }
    public decimal AmountRemaining { get; set; }
    public int InstallmentsTouched { get; set; }
    public string? Message { get; set; }
}
