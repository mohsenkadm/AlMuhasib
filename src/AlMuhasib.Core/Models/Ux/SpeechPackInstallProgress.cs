namespace AlMuhasib.Core.Models.Ux;

public sealed class SpeechPackInstallProgress
{
    public string Message { get; init; } = string.Empty;
    public double Percent { get; init; }
    public string? StepLabel { get; init; }
}
