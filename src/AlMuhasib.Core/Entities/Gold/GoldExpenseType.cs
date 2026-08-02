namespace AlMuhasib.Core.Entities.Gold;

public class GoldExpenseType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
