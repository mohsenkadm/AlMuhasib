namespace AlMuhasib.Core.Interfaces.Services;

public interface IBarcodeLabelService
{
    void PrintLabels(IEnumerable<BarcodeLabelItem> items);
}

public class BarcodeLabelItem
{
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public decimal? Price { get; set; }
}
