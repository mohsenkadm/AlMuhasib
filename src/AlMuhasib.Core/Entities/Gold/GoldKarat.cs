namespace AlMuhasib.Core.Entities.Gold;

public class GoldKarat : BaseEntity
{
    public int KaratValue { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PurityFactor { get; set; } = 1.0m;
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}
