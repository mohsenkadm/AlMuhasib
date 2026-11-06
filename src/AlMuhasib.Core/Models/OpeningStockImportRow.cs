namespace AlMuhasib.Core.Models;

public class OpeningStockImportRow
{
    public int RowNumber { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal Quantity { get; set; }
    /// <summary>اختياري للتوافق مع قوالب قديمة — غير مطلوب للكمية فقط.</summary>
    public decimal UnitCost { get; set; }
    public decimal? ProductSalePrice { get; set; }
    public List<string> Errors { get; set; } = [];
    public bool IsValid => Errors.Count == 0;
    public string ErrorsText => Errors.Count == 0 ? "—" : string.Join(" | ", Errors);
}
