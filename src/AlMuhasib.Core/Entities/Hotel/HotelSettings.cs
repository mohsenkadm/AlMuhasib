namespace AlMuhasib.Core.Entities.Hotel;

public class HotelSettings : BaseEntity
{
    public string HotelName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public TimeSpan CheckInTime { get; set; } = new(14, 0, 0);
    public TimeSpan CheckOutTime { get; set; } = new(12, 0, 0);
    public string CancellationPolicy { get; set; } = string.Empty;
    public string Currency { get; set; } = "IQD";
    public bool IsConfigured { get; set; }
}
