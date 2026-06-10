namespace AlMuhasib.Cloud.Infrastructure.Options;

public sealed class OneSignalOptions
{
    public const string SectionName = "OneSignal";
    public string AppId { get; set; } = string.Empty;
    public string RestApiKey { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}
