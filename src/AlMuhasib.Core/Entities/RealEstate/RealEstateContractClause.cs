namespace AlMuhasib.Core.Entities.RealEstate;

public class RealEstateContractClause : BaseEntity
{
    public int ContractId { get; set; }
    public RealEstateContract Contract { get; set; } = null!;

    public int SortOrder { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
