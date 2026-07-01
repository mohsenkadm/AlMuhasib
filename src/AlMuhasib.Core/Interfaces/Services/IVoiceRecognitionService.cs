using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Models.Ux;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IVoiceRecognitionService : IDisposable
{
    bool IsAvailable { get; }
    string? UnavailableReason { get; }
    VoiceAssistantState State { get; }

    event EventHandler<string>? TranscriptChanged;
    event EventHandler<string>? PhraseRecognized;
    event EventHandler<VoiceAssistantState>? StateChanged;
    event EventHandler<string>? ErrorOccurred;

    void UpdatePhrases(IEnumerable<string> phrases);
    Task<bool> EnsureAvailabilityAsync(IProgress<SpeechPackInstallProgress>? progress = null, CancellationToken cancellationToken = default);
    void ResetSpeechPackInstallCache();
    void OpenSpeechLanguageSettings();
    Task StartListeningAsync(CancellationToken cancellationToken = default);
    void StopListening();
}
