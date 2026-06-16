namespace AlMuhasib.Core.Entities.Hotel;

public class RatePlanSeason : BaseEntity
{
    public int RatePlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal PricePerNight { get; set; }

    public RatePlan RatePlan { get; set; } = null!;
}
