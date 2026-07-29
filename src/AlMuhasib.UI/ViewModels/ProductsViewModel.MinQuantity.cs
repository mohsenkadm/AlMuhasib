using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.ViewModels;

public partial class ProductsViewModel
{
    public ObservableCollection<ProductMinQuantityEditRow> EditMinQuantities { get; } = [];

    private async Task LoadMinQuantitiesForProductAsync(int? productId)
    {
        EditMinQuantities.Clear();

        var warehouses = (await _unitOfWork.Warehouses.GetAllAsync())
            .OrderBy(w => w.Name)
            .ToList();

        Dictionary<int, WarehouseStock> stocksByWarehouse = new();
        if (productId is int pid)
        {
            var stocks = await _unitOfWork.WarehouseStocks.FindAsync(s => s.ProductId == pid);
            stocksByWarehouse = stocks.ToDictionary(s => s.WarehouseId);
        }

        foreach (var warehouse in warehouses)
        {
            stocksByWarehouse.TryGetValue(warehouse.Id, out var stock);
            EditMinQuantities.Add(new ProductMinQuantityEditRow
            {
                WarehouseId = warehouse.Id,
                WarehouseName = warehouse.Name,
                CurrentQuantity = stock?.Quantity ?? 0,
                MinQuantity = stock?.MinQuantity ?? 0,
                WarehouseStockId = stock?.Id
            });
        }
    }

    private async Task SaveMinQuantitiesAsync(int productId)
    {
        if (EditMinQuantities.Count == 0)
            return;

        var existing = (await _unitOfWork.WarehouseStocks.FindAsync(s => s.ProductId == productId))
            .ToDictionary(s => s.WarehouseId);

        var username = _currentUserService.Username ?? "system";
        var now = DateTime.UtcNow;
        var changed = false;

        foreach (var row in EditMinQuantities)
        {
            var minQty = row.MinQuantity < 0 ? 0 : row.MinQuantity;

            if (existing.TryGetValue(row.WarehouseId, out var stock))
            {
                if (stock.MinQuantity == minQty)
                    continue;

                stock.MinQuantity = minQty;
                stock.UpdatedAt = now;
                stock.UpdatedBy = username;
                _unitOfWork.WarehouseStocks.Update(stock);
                changed = true;
            }
            else if (minQty > 0)
            {
                await _unitOfWork.WarehouseStocks.AddAsync(new WarehouseStock
                {
                    WarehouseId = row.WarehouseId,
                    ProductId = productId,
                    Quantity = 0,
                    OpeningQuantity = 0,
                    UnitCost = 0,
                    MinQuantity = minQty,
                    CreatedAt = now,
                    CreatedBy = username
                });
                changed = true;
            }
        }

        if (changed)
            await _unitOfWork.SaveChangesAsync();
    }
}
