namespace AlMuhasib.Core.Interfaces.Services;

public interface IBarcodeLabelService
{
    void PrintLabels(IEnumerable<BarcodeLabelItem> items);

    /// <summary>Creates a scannable Code128 barcode as PNG bytes for preview or printing.</summary>
    byte[]? CreateBarcodePng(string barcode, int width = 280, int height = 90);
}

public class BarcodeLabelItem
{
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public int? KaratValue { get; set; }
    public decimal? WeightGrams { get; set; }
}
