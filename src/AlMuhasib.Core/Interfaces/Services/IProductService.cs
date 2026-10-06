using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IProductService
{
    Task<Product> CreateAsync(Product product);
    Task<Product?> GetByIdAsync(int id);
    Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        int? categoryId = null,
        string? searchTerm = null,
        string? sizeName = null,
        string? colorName = null,
        bool? hasBatches = null);
    Task UpdateAsync(Product product);
    Task DeleteAsync(int id);
    Task ApplyDiscountToProductsAsync(
        IEnumerable<int> productIds,
        DiscountType discountType,
        decimal discountValue,
        DateTime? discountExpiresAt);
    Task<Product?> GetByBarcodeAsync(string barcode);
    Task<IEnumerable<Product>> SearchByNameAsync(string name);

    /// <summary>معرّفات الفروع التي يظهر فيها المنتج.</summary>
    Task<IReadOnlyList<int>> GetBranchIdsForProductAsync(int productId, CancellationToken ct = default);

    /// <summary>أسماء الفروع المرتبطة بعدة منتجات (للعرض في الجدول).</summary>
    Task<IReadOnlyDictionary<int, string>> GetBranchNamesByProductIdsAsync(
        IEnumerable<int> productIds,
        CancellationToken ct = default);

    /// <summary>استبدال فروع ظهور المنتج بالكامل.</summary>
    Task SetProductBranchesAsync(int productId, IEnumerable<int> branchIds, CancellationToken ct = default);

    /// <summary>منتجات ظاهرة في فرع معيّن (للفواتير والبحث).</summary>
    Task<IReadOnlyList<Product>> GetVisibleInBranchAsync(int branchId, CancellationToken ct = default);

    /// <summary>منتجات ظاهرة في الفرع الحالي للجلسة.</summary>
    Task<IReadOnlyList<Product>> GetVisibleInCurrentBranchAsync(CancellationToken ct = default);

    /// <summary>هل المنتج ظاهر في الفرع؟ منتجات بلا أي ربط تُعامل كظاهرة (توافق قديم).</summary>
    Task<bool> IsVisibleInBranchAsync(int productId, int branchId, CancellationToken ct = default);

    /// <summary>حفظ كميات المخازن للمنتج عبر الفروع المختارة (يتجاوز عزل الفرع بأمان للأسطر المسموحة).</summary>
    Task UpsertWarehouseStocksAsync(
        int productId,
        IEnumerable<(int WarehouseId, decimal Quantity, decimal MinQuantity)> rows,
        CancellationToken ct = default);

    /// <summary>صفوف كميات المخازن لفروع محددة.</summary>
    Task<IReadOnlyList<(int WarehouseId, string WarehouseName, int BranchId, string BranchName, decimal Quantity, decimal MinQuantity, int? StockId)>>
        GetWarehouseStockRowsForBranchesAsync(int? productId, IEnumerable<int> branchIds, CancellationToken ct = default);
}
