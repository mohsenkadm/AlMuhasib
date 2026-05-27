using AlMuhasib.Core.Entities;

namespace AlMuhasib.UI.Models;

public sealed class ProductPickerResult
{
    public required Product Product { get; init; }
    public decimal Quantity { get; init; }
    public decimal SuggestedUnitPrice { get; init; }
}
