namespace AlMuhasib.Core.Entities;

/// <summary>وحدات قياس المنتج (قطعة/كرتون/كيلو).</summary>
public class ProductUnit : BaseEntity
{
    public int ProductId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal ConversionFactor { get; set; } = 1m;
    public bool IsDefault { get; set; }

    public Product Product { get; set; } = null!;
}
