using AlMuhasib.Core.Enums.Gold;

namespace AlMuhasib.Core.Entities.Gold;

public class GoldNotification : BaseEntity
{
    public GoldNotificationType Type { get; set; } = GoldNotificationType.Info;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? RelatedEntity { get; set; }
    public int? RelatedId { get; set; }
}
