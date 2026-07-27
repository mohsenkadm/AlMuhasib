using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Models.Ux;

public class DiscoveredMainServer
{
    public string Host { get; set; } = string.Empty;
    public int SqlPort { get; set; } = 1433;
    public string? SqlInstance { get; set; }
    public ApplicationSystemType SystemType { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    public string ServerLabel { get; set; } = string.Empty;
    public bool RequiresPairing { get; set; } = true;
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
}
