namespace AlMuhasib.Core.Entities.RealEstate;

public class RealEstateParty : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public DateTime? IdDate { get; set; }
    public string Notes { get; set; } = string.Empty;
}
