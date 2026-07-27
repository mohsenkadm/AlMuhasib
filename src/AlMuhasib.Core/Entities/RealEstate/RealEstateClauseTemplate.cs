namespace AlMuhasib.Core.Entities.RealEstate;

public class RealEstateClauseTemplate : BaseEntity
{
    public int SortOrder { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
