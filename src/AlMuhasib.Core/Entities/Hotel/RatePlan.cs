namespace AlMuhasib.Core.Entities.Hotel;

public class RatePlan : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int RoomTypeId { get; set; }
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; } = true;
    public string Notes { get; set; } = string.Empty;

    public RoomType RoomType { get; set; } = null!;
    public ICollection<RatePlanSeason> Seasons { get; set; } = [];
}
