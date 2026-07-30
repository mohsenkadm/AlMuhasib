namespace AlMuhasib.Core.Entities;

/// <summary>وحدات/تعبئة المنتج (قطعة/كرتون/كيلو) مرتبطة بأنواع التعبئة.</summary>
public class ProductUnit : BaseEntity
{
    public int ProductId { get; set; }
    public int? PackagingTypeId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal ConversionFactor { get; set; } = 1m;
    public bool IsDefault { get; set; }

    public Product Product { get; set; } = null!;
    public PackagingType? PackagingType { get; set; }
}
