using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Models.Ux;

public class NetworkConnectionProfile
{
    public string MainServerHost { get; set; } = string.Empty;
    public int SqlPort { get; set; } = 1433;
    public string? SqlInstance { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    public ApplicationSystemType SystemType { get; set; }
    public string SqlUsername { get; set; } = string.Empty;
    public string SqlPasswordEncrypted { get; set; } = string.Empty;
    public string PairingCode { get; set; } = string.Empty;
    public bool UseDiscovery { get; set; }
    public DateTime? LastSuccessfulConnection { get; set; }
    public int ConnectionTimeoutSeconds { get; set; } = 15;
    public string? ServerLabel { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(MainServerHost)
        && !string.IsNullOrWhiteSpace(DatabaseName)
        && !string.IsNullOrWhiteSpace(SqlUsername);
}
