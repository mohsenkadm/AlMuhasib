namespace AlMuhasib.Core.Entities;

/// <summary>المخازن</summary>
public class Warehouse : BranchScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }

    // Navigation
    public ICollection<WarehouseStock> WarehouseStocks { get; set; } = [];
}
