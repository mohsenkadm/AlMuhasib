namespace AlMuhasib.Core.Entities.Gold;

/// <summary>تصنيف أصناف الذهب (خاتم، سلسلة، سبيكة…).</summary>
public class GoldCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
