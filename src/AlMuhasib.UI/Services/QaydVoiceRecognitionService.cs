using System.Globalization;
using System.Speech.Recognition;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;

namespace AlMuhasib.UI.Services;

/// <summary>
/// Uses Windows desktop speech when Arabic is available; otherwise falls back to offline Vosk Arabic model.
/// </summary>
public sealed class QaydVoiceRecognitionService : IVoiceRecognitionService
{
    private readonly WindowsVoiceRecognitionService _windows = new();
    private readonly VoskVoiceRecognitionService _vosk = new();
    private IVoiceRecognitionService _active;
    private bool _useVosk;

    public QaydVoiceRecognitionService()
    {
        _active = ArabicSpeechCapability.IsWindowsArabicRecognizerInstalled() ? _windows : _vosk;
        _useVosk = ReferenceEquals(_active, _vosk);
        WireEvents(_active);
    }

    public bool IsAvailable => _active.IsAvailable;
    public string? UnavailableReason => _active.UnavailableReason;
    public VoiceAssistantState State => _active.State;

    public event EventHandler<string>? TranscriptChanged;
    public event EventHandler<string>? PhraseRecognized;
    public event EventHandler<VoiceAssistantState>? StateChanged;
    public event EventHandler<string>? ErrorOccurred;

    public async Task<bool> EnsureAvailabilityAsync(IProgress<SpeechPackInstallProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        if (_active.IsAvailable)
            return true;

        if (!_useVosk && ArabicSpeechCapability.IsWindowsArabicRecognizerInstalled())
        {
            SwitchActive(_windows);
            if (await _windows.EnsureAvailabilityAsync(progress, cancellationToken))
                return true;
        }

        SwitchActive(_vosk);
        _useVosk = true;
        return await _vosk.EnsureAvailabilityAsync(progress, cancellationToken);
    }

    public void ResetSpeechPackInstallCache()
    {
        _windows.ResetSpeechPackInstallCache();
        _vosk.ResetSpeechPackInstallCache();
    }

    public void OpenSpeechLanguageSettings()
    {
        if (_useVosk)
            _vosk.ResetSpeechPackInstallCache();
        else
            ArabicSpeechCapability.OpenLanguageSettings();
    }

    public void UpdatePhrases(IEnumerable<string> phrases) =>
        _active.UpdatePhrases(phrases);

    public Task StartListeningAsync(CancellationToken cancellationToken = default) =>
        _active.StartListeningAsync(cancellationToken);

    public void StopListening() =>
        _active.StopListening();

    public void Dispose()
    {
        _windows.Dispose();
        _vosk.Dispose();
    }

    private void SwitchActive(IVoiceRecognitionService service)
    {
        if (ReferenceEquals(_active, service))
            return;

        UnwireEvents(_active);
        _active = service;
        WireEvents(_active);
    }

    private void WireEvents(IVoiceRecognitionService service)
    {
        service.TranscriptChanged += ForwardTranscript;
        service.PhraseRecognized += ForwardPhrase;
        service.StateChanged += ForwardState;
        service.ErrorOccurred += ForwardError;
    }

    private void UnwireEvents(IVoiceRecognitionService service)
    {
        service.TranscriptChanged -= ForwardTranscript;
        service.PhraseRecognized -= ForwardPhrase;
        service.StateChanged -= ForwardState;
        service.ErrorOccurred -= ForwardError;
    }

    private void ForwardTranscript(object? sender, string text) =>
        TranscriptChanged?.Invoke(this, text);

    private void ForwardPhrase(object? sender, string text) =>
        PhraseRecognized?.Invoke(this, text);

    private void ForwardState(object? sender, VoiceAssistantState state) =>
        StateChanged?.Invoke(this, state);

    private void ForwardError(object? sender, string message) =>
        ErrorOccurred?.Invoke(this, message);
}

internal static class ArabicSpeechCapability
{
    private static readonly string[] PreferredCultures = ["ar-SA", "ar-IQ", "ar-EG", "ar-AE"];

    public static bool IsWindowsArabicRecognizerInstalled()
    {
        foreach (var name in PreferredCultures)
        {
            try
            {
                var culture = CultureInfo.GetCultureInfo(name);
                foreach (var info in SpeechRecognitionEngine.InstalledRecognizers())
                {
                    if (info.Culture.Name.Equals(culture.Name, StringComparison.OrdinalIgnoreCase)
                        || info.Culture.TwoLetterISOLanguageName.Equals(culture.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            catch
            {
                // try next
            }
        }

        return SpeechRecognitionEngine.InstalledRecognizers()
            .Any(r => r.Culture.TwoLetterISOLanguageName.Equals("ar", StringComparison.OrdinalIgnoreCase));
    }

    public static void OpenLanguageSettings()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:regionlanguage") { UseShellExecute = true });
        }
        catch
        {
            // ignore
        }
    }
}
