using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Models.Ux;

public class SystemProfile
{
    public ApplicationSystemType? SelectedSystem { get; set; }
    public DateTime? SelectedAt { get; set; }
    public bool IsConfigured => SelectedSystem.HasValue;
}
