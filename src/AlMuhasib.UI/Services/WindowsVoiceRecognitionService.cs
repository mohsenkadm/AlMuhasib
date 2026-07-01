using System.Globalization;
using System.Speech.Recognition;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;

namespace AlMuhasib.UI.Services;

public sealed class WindowsVoiceRecognitionService : IVoiceRecognitionService
{
    private readonly object _sync = new();
    private SpeechRecognitionEngine? _engine;
    private bool _isListening;
    private VoiceAssistantState _state = VoiceAssistantState.Idle;

    public bool IsAvailable { get; private set; }
    public string? UnavailableReason { get; private set; }
    public VoiceAssistantState State => _state;

    public event EventHandler<string>? TranscriptChanged;
    public event EventHandler<string>? PhraseRecognized;
    public event EventHandler<VoiceAssistantState>? StateChanged;
    public event EventHandler<string>? ErrorOccurred;

    public WindowsVoiceRecognitionService() => TryInitializeEngine();

    public Task<bool> EnsureAvailabilityAsync(IProgress<SpeechPackInstallProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        if (IsAvailable)
            return Task.FromResult(true);

        if (!ArabicSpeechCapability.IsWindowsArabicRecognizerInstalled())
        {
            UnavailableReason = "Windows لا يوفّر محرك تعرف صوتي عربي على هذا الجهاز.";
            return Task.FromResult(false);
        }

        TryInitializeEngine();
        return Task.FromResult(IsAvailable);
    }

    public void ResetSpeechPackInstallCache() { }

    public void OpenSpeechLanguageSettings() =>
        ArabicSpeechCapability.OpenLanguageSettings();

    public void UpdatePhrases(IEnumerable<string> phrases)
    {
        if (!IsAvailable || _engine is null)
            return;

        lock (_sync)
        {
            _engine.UnloadAllGrammars();
            var choices = new Choices();
            var count = 0;
            foreach (var phrase in phrases.Select(VoiceCommandMatcher.NormalizeArabic).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct())
            {
                choices.Add(phrase);
                count++;
            }

            if (count == 0)
                return;

            var builder = new GrammarBuilder(choices) { Culture = _engine.RecognizerInfo.Culture };
            _engine.LoadGrammar(new Grammar(builder) { Name = "QaydVoiceCommands" });
        }
    }

    public Task StartListeningAsync(CancellationToken cancellationToken = default)
    {
        if (!IsAvailable || _engine is null)
        {
            SetState(VoiceAssistantState.Error);
            RaiseError(UnavailableReason ?? "التعرف الصوتي غير متاح");
            return Task.CompletedTask;
        }

        lock (_sync)
        {
            if (_isListening)
                return Task.CompletedTask;

            try
            {
                _engine.RecognizeAsync(RecognizeMode.Multiple);
                _isListening = true;
                SetState(VoiceAssistantState.Listening);
            }
            catch (Exception ex)
            {
                SetState(VoiceAssistantState.Error);
                RaiseError(ex.Message);
            }
        }

        return Task.CompletedTask;
    }

    public void StopListening()
    {
        lock (_sync)
        {
            if (_engine is null)
            {
                SetState(VoiceAssistantState.Idle);
                return;
            }

            if (!_isListening)
            {
                SetState(VoiceAssistantState.Idle);
                return;
            }

            try
            {
                _engine.RecognizeAsyncStop();
            }
            catch (InvalidOperationException)
            {
                // engine not running
            }
            catch
            {
                // ignore stop races
            }

            _isListening = false;
            SetState(VoiceAssistantState.Idle);
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_engine is null)
                return;

            try
            {
                _engine.SpeechRecognized -= OnSpeechRecognized;
                _engine.SpeechHypothesized -= OnSpeechHypothesized;
                _engine.RecognizeCompleted -= OnRecognizeCompleted;
                _engine.RecognizeAsyncCancel();
                _engine.Dispose();
            }
            catch
            {
                // ignore dispose errors
            }
            finally
            {
                _engine = null;
                _isListening = false;
            }
        }
    }

    private void TryInitializeEngine()
    {
        DisposeEngineOnly();

        try
        {
            var culture = ResolveInstalledCulture();
            if (culture is null)
            {
                IsAvailable = false;
                UnavailableReason = "محرك التعرف الصوتي العربي في Windows غير متوفر.";
                return;
            }

            _engine = new SpeechRecognitionEngine(culture);
            _engine.SetInputToDefaultAudioDevice();
            _engine.SpeechRecognized += OnSpeechRecognized;
            _engine.SpeechHypothesized += OnSpeechHypothesized;
            _engine.RecognizeCompleted += OnRecognizeCompleted;
            IsAvailable = true;
            UnavailableReason = null;
        }
        catch (Exception ex)
        {
            IsAvailable = false;
            UnavailableReason = $"تعذر تهيئة الميكروفون: {ex.Message}";
        }
    }

    private static CultureInfo? ResolveInstalledCulture()
    {
        foreach (var name in new[] { "ar-SA", "ar-IQ", "ar-EG", "ar-AE" })
        {
            try
            {
                var culture = CultureInfo.GetCultureInfo(name);
                foreach (var info in SpeechRecognitionEngine.InstalledRecognizers())
                {
                    if (info.Culture.Name.Equals(culture.Name, StringComparison.OrdinalIgnoreCase)
                        || info.Culture.TwoLetterISOLanguageName.Equals(culture.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase))
                        return info.Culture;
                }
            }
            catch
            {
                // try next culture
            }
        }

        return SpeechRecognitionEngine.InstalledRecognizers()
            .Select(r => r.Culture)
            .FirstOrDefault(c => c.TwoLetterISOLanguageName.Equals("ar", StringComparison.OrdinalIgnoreCase));
    }

    private void DisposeEngineOnly()
    {
        if (_engine is null)
            return;

        try
        {
            _engine.SpeechRecognized -= OnSpeechRecognized;
            _engine.SpeechHypothesized -= OnSpeechHypothesized;
            _engine.RecognizeCompleted -= OnRecognizeCompleted;
            _engine.RecognizeAsyncCancel();
            _engine.Dispose();
        }
        catch
        {
            // ignore
        }
        finally
        {
            _engine = null;
            _isListening = false;
            IsAvailable = false;
        }
    }

    private void OnSpeechHypothesized(object? sender, SpeechHypothesizedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.Result.Text))
            TranscriptChanged?.Invoke(this, e.Result.Text);
    }

    private void OnSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.Result.Text))
            return;

        SetState(VoiceAssistantState.Processing);
        TranscriptChanged?.Invoke(this, e.Result.Text);
        PhraseRecognized?.Invoke(this, e.Result.Text);
    }

    private void OnRecognizeCompleted(object? sender, RecognizeCompletedEventArgs e)
    {
        if (e.Error is not null)
        {
            SetState(VoiceAssistantState.Error);
            RaiseError(e.Error.Message);
            return;
        }

        if (_isListening)
            SetState(VoiceAssistantState.Listening);
    }

    private void SetState(VoiceAssistantState state)
    {
        _state = state;
        StateChanged?.Invoke(this, state);
    }

    private void RaiseError(string message) =>
        ErrorOccurred?.Invoke(this, message);
}
