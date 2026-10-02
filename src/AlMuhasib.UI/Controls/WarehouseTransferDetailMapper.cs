using System.Collections.ObjectModel;
using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.UI.Controls;

public static class WarehouseTransferDetailMapper
{
    public static WarehouseTransferDetailDisplayModel FromDetail(WarehouseTransferDetailResult detail) => new()
    {
        Title = "فاتورة نقل مخازن",
        TransferNumber = detail.TransferNumber,
        DateText = detail.Date.ToString("yyyy/MM/dd HH:mm"),
        FromWarehouseName = detail.FromWarehouseName,
        ToWarehouseName = detail.ToWarehouseName,
        Notes = string.IsNullOrWhiteSpace(detail.Notes) ? "—" : detail.Notes!,
        CreatedBy = detail.CreatedBy,
        LineCountText = detail.LineCount.ToString("N0"),
        TotalQuantityText = detail.TotalQuantity.ToString("N2"),
        Lines = new ObservableCollection<WarehouseTransferDetailLineRow>(
            detail.Lines.Select((line, index) => new WarehouseTransferDetailLineRow
            {
                Index = index + 1,
                ProductName = line.ProductName,
                Barcode = line.Barcode ?? "—",
                QuantityText = line.Quantity.ToString("N2")
            }))
    };
}

public sealed class WarehouseTransferDetailDisplayModel
{
    public string Title { get; init; } = string.Empty;
    public string TransferNumber { get; init; } = string.Empty;
    public string DateText { get; init; } = string.Empty;
    public string FromWarehouseName { get; init; } = string.Empty;
    public string ToWarehouseName { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
    public string CreatedBy { get; init; } = string.Empty;
    public string LineCountText { get; init; } = "0";
    public string TotalQuantityText { get; init; } = "0";
    public ObservableCollection<WarehouseTransferDetailLineRow> Lines { get; init; } = [];
}

public sealed class WarehouseTransferDetailLineRow
{
    public int Index { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string Barcode { get; init; } = string.Empty;
    public string QuantityText { get; init; } = string.Empty;
}
