namespace AlMuhasib.Core.Models;

public class OpeningInstallmentImportRow
{
    public int RowNumber { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? FileNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public int NumberOfInstallments { get; set; }
    public int PaidInstallmentsCount { get; set; }
    public DateTime StartDate { get; set; }
    public string? Notes { get; set; }
    public List<string> Errors { get; set; } = [];
    public bool IsValid => Errors.Count == 0;
    public string ErrorsText => Errors.Count == 0 ? "—" : string.Join(" | ", Errors);
}
