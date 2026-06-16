namespace AlMuhasib.Core.Entities.Hotel;

public class Guest : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public ICollection<Reservation> Reservations { get; set; } = [];
}
