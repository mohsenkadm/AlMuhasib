namespace AlMuhasib.Core.Models.Gold;

public class GoldItemsImportRow
{
    public int RowNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public int KaratValue { get; set; }
    public decimal WeightGrams { get; set; }
    public decimal MakingCharge { get; set; }
    public decimal CostPerGram { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class GoldItemsImportResult
{
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> Errors { get; set; } = [];
    public int RowCount { get; set; }
}
