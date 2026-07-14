namespace AlMuhasib.Core.Licensing;

public sealed class DesktopActivationPayload
{
    public Guid InstallationId { get; set; }
    public string Mode { get; set; } = "Lifetime";
    public DateTime IssuedAtUtc { get; set; }
}
