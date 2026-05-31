namespace AlMuhasib.Core.Models;

public class OpeningInstallmentBatchResult
{
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> Errors { get; set; } = [];
}
