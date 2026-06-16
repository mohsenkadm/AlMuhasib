namespace AlMuhasib.Core.Entities.Hotel;

public class HotelCashBox : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsBank { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public bool IsActive { get; set; } = true;
    public string Notes { get; set; } = string.Empty;
}
