using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities.Hotel;

public class HousekeepingTask : BaseEntity
{
    public int RoomId { get; set; }
    public HousekeepingStatus Status { get; set; } = HousekeepingStatus.Pending;
    public string AssignedTo { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Notes { get; set; } = string.Empty;

    public Room Room { get; set; } = null!;
}
