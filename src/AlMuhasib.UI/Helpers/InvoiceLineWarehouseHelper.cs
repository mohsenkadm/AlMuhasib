using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Helpers;

/// <summary>عمليات مخزن السطر المشتركة بين شاشات الفواتير.</summary>
public static class InvoiceLineWarehouseHelper
{
    public static void ApplyHeaderWarehouseToAllItems(
        IEnumerable<InvoiceItemRow> items,
        Warehouse? headerWarehouse)
    {
        foreach (var row in items)
            row.ApplyHeaderWarehouse(headerWarehouse);
    }

    public static int ResolveLineWarehouseId(InvoiceItemRow row, int? headerWarehouseId) =>
        row.ResolveWarehouseId(headerWarehouseId) ?? headerWarehouseId ?? 0;

    public static Warehouse? ResolveLineWarehouse(
        InvoiceItemRow row,
        int? headerWarehouseId,
        IReadOnlyList<Warehouse> warehouses)
    {
        var id = row.ResolveWarehouseId(headerWarehouseId);
        return id is > 0 ? warehouses.FirstOrDefault(w => w.Id == id) : null;
    }

    public static async Task RefreshRowAvailableStockAsync(
        IUnitOfWork unitOfWork,
        InvoiceItemRow row,
        int? headerWarehouseId,
        IReadOnlyList<Warehouse> warehouses)
    {
        if (row.ProductId is not int productId || productId <= 0)
        {
            row.AvailableStock = 0;
            return;
        }

        var stocks = await unitOfWork.WarehouseStocks.FindAsync(s => s.ProductId == productId);
        var warehouseDict = warehouses.ToDictionary(w => w.Id, w => w.Name);

        var lines = stocks
            .Where(s => s.Quantity != 0 && warehouseDict.ContainsKey(s.WarehouseId))
            .Select(s => $"{warehouseDict[s.WarehouseId]}: {s.Quantity:N0}")
            .ToList();

        row.StockInfo = lines.Count > 0 ? string.Join(" | ", lines) : "لا يوجد رصيد";

        var lineWarehouseId = row.ResolveWarehouseId(headerWarehouseId);
        row.AvailableStock = lineWarehouseId is > 0
            ? stocks.FirstOrDefault(s => s.WarehouseId == lineWarehouseId)?.Quantity ?? 0
            : stocks.Where(s => warehouseDict.ContainsKey(s.WarehouseId)).Sum(s => s.Quantity);
    }

    public static void BindRowWarehouse(
        InvoiceItemRow row,
        Warehouse? headerWarehouse,
        IReadOnlyList<Warehouse> warehouses,
        int? storedWarehouseId)
    {
        if (storedWarehouseId is > 0)
            row.SelectedWarehouse = warehouses.FirstOrDefault(w => w.Id == storedWarehouseId);
        else if (row.SelectedWarehouse is null)
            row.ApplyHeaderWarehouse(headerWarehouse);
    }
}
