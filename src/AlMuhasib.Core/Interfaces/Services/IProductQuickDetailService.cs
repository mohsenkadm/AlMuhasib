namespace AlMuhasib.Core.Interfaces.Services;

public interface IProductQuickDetailService
{
    Task<ProductQuickDetailResult?> GetDetailAsync(int productId, CancellationToken cancellationToken = default);
}

public class ProductQuickDetailResult
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string? CategoryName { get; set; }
    public string? Description { get; set; }

    public decimal TotalStockQuantity { get; set; }
    public List<ProductQuickStockRow> StocksByWarehouse { get; set; } = [];

    public decimal? LastPurchasePrice { get; set; }
    public DateTime? LastPurchaseDate { get; set; }
    public decimal? LastSalePrice { get; set; }
    public DateTime? LastSaleDate { get; set; }

    public decimal? CurrentSalePrice { get; set; }
    public decimal? CurrentPurchasePrice { get; set; }

    public int SaleDealCount { get; set; }
    public decimal TotalSoldQuantity { get; set; }
    public int PurchaseDealCount { get; set; }
    public decimal TotalPurchasedQuantity { get; set; }
}

public class ProductQuickStockRow
{
    public string WarehouseName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}
