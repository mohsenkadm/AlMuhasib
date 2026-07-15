using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.Infrastructure.Services;

internal static class SyncProgressReporter
{
    public const int TotalSteps = 6;

    public static void Report(IProgress<SyncProgressUpdate>? progress, int step, string message) =>
        progress?.Report(new SyncProgressUpdate
        {
            CurrentStep = step,
            TotalSteps = TotalSteps,
            Message = message
        });
}
