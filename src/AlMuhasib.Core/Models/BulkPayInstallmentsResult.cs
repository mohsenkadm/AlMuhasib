namespace AlMuhasib.Core.Models;

public class BulkPayInstallmentsResult
{
    public int PaidCount { get; init; }
    public decimal TotalPaid { get; init; }
    public List<string> Errors { get; init; } = [];
    public bool AllSucceeded => Errors.Count == 0;
}
