using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Models.Inventory;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IProductBatchService
{
    Task<IReadOnlyList<ProductBatch>> GetByProductAsync(int productId, int? warehouseId = null, bool inStockOnly = false);
    Task<ProductBatch> UpsertAsync(int productId, int warehouseId, string? batchNumber, DateTime? expiryDate, decimal quantityDelta);
    Task DeductAsync(int batchId, decimal quantity);
    Task<ProductBatch?> FindFifoAsync(int productId, int warehouseId, decimal requiredQty);

    /// <summary>
    /// يوزّع الكمية المطلوبة على الدفعات الأقرب لانتهاء الصلاحية (FEFO).
    /// يرمي إذا لم تكفِ الكميات المتاحة.
    /// </summary>
    Task<IReadOnlyList<BatchAllocation>> AllocateFefoAsync(int productId, int warehouseId, decimal requiredQty);

    /// <summary>يخصم عدة تخصيصات دفعات دفعة واحدة.</summary>
    Task DeductAllocationsAsync(IEnumerable<BatchAllocation> allocations);
}
