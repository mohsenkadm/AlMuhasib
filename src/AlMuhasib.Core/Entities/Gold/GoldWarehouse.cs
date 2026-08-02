namespace AlMuhasib.Core.Entities.Gold;

public class GoldWarehouse : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public string Notes { get; set; } = string.Empty;
}
