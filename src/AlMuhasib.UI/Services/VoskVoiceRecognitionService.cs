using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Vosk;

namespace AlMuhasib.UI.Services;

public sealed class VoskVoiceRecognitionService : IVoiceRecognitionService
{
    private const float TargetSampleRate = 16000f;
    private const int SilenceFinalizeMs = 900;
    private const int MicCheckMs = 2500;

    private readonly object _sync = new();
    private Model? _model;
    private VoskRecognizer? _recognizer;
    private IWaveIn? _capture;
    private WaveFormat? _captureFormat;
    private bool _isListening;
    private VoiceAssistantState _state = VoiceAssistantState.Idle;

    private DispatcherTimer? _silenceTimer;
    private string _lastPartial = string.Empty;
    private DateTime _lastSpeechUtc = DateTime.MinValue;
    private DateTime _listenStartedUtc = DateTime.MinValue;
    private bool _micWarningRaised;

    public bool IsAvailable { get; private set; }
    public string? UnavailableReason { get; private set; }
    public VoiceAssistantState State => _state;

    public event EventHandler<string>? TranscriptChanged;
    public event EventHandler<string>? PhraseRecognized;
    public event EventHandler<VoiceAssistantState>? StateChanged;
    public event EventHandler<string>? ErrorOccurred;

    public async Task<bool> EnsureAvailabilityAsync(IProgress<SpeechPackInstallProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        if (IsAvailable)
            return true;

        if (!await VoskModelInstaller.EnsureModelAsync(progress, cancellationToken))
        {
            UnavailableReason = "تعذر تنزيل نموذج التعرف الصوتي العربي. تحقق من الإنترنت أو استخدم الاقتراحات بالنقر.";
            return false;
        }

        return TryInitializeEngine();
    }

    public void ResetSpeechPackInstallCache() => VoskModelInstaller.Reset();

    public void OpenSpeechLanguageSettings() => ResetSpeechPackInstallCache();

    public void UpdatePhrases(IEnumerable<string> phrases)
    {
        // Phrases are matched in VoiceCommandMatcher after open recognition.
        _ = phrases;
    }

    public Task StartListeningAsync(CancellationToken cancellationToken = default)
    {
        if (!IsAvailable || _recognizer is null)
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
                StopCaptureUnsafe();
                _recognizer.Reset();
                _lastPartial = string.Empty;
                _micWarningRaised = false;
                _listenStartedUtc = DateTime.UtcNow;
                _lastSpeechUtc = DateTime.UtcNow;

                _capture = CreateCapture();
                _captureFormat = _capture.WaveFormat;
                _capture.DataAvailable += OnDataAvailable;
                _capture.RecordingStopped += OnRecordingStopped;
                _capture.StartRecording();

                _isListening = true;
                StartSilenceTimer();
                SetState(VoiceAssistantState.Listening);
            }
            catch (Exception ex)
            {
                SetState(VoiceAssistantState.Error);
                RaiseError($"تعذر تشغيل الميكروفون: {ex.Message}");
            }
        }

        return Task.CompletedTask;
    }

    public void StopListening()
    {
        lock (_sync)
        {
            StopSilenceTimer();
            StopCaptureUnsafe();
            _isListening = false;
            SetState(VoiceAssistantState.Idle);
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            StopSilenceTimer();
            StopCaptureUnsafe();
            _recognizer?.Dispose();
            _recognizer = null;
            _model?.Dispose();
            _model = null;
            IsAvailable = false;
        }
    }

    private bool TryInitializeEngine()
    {
        lock (_sync)
        {
            DisposeEngineOnly();

            try
            {
                if (!VoskModelInstaller.IsModelReady())
                {
                    IsAvailable = false;
                    UnavailableReason = "نموذج التعرف الصوتي غير جاهز.";
                    return false;
                }

                Vosk.Vosk.SetLogLevel(-1);
                _model = new Model(VoskModelInstaller.ModelPath);
                _recognizer = new VoskRecognizer(_model, TargetSampleRate);
                _recognizer.SetMaxAlternatives(0);
                _recognizer.SetWords(true);
                IsAvailable = true;
                UnavailableReason = null;
                return true;
            }
            catch (Exception ex)
            {
                IsAvailable = false;
                UnavailableReason = $"تعذر تهيئة التعرف الصوتي: {ex.Message}";
                return false;
            }
        }
    }

    private static IWaveIn CreateCapture()
    {
        try
        {
            var enumerator = new MMDeviceEnumerator();
            var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications)
                         ?? enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
            return new WasapiCapture(device);
        }
        catch
        {
            var waveIn = new WaveInEvent { BufferMilliseconds = 50 };
            waveIn.WaveFormat = new WaveFormat(16000, 16, 1);
            return waveIn;
        }
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (e.BytesRecorded <= 0 || _captureFormat is null)
            return;

        VoskRecognizer? recognizer;
        lock (_sync)
            recognizer = _recognizer;

        if (recognizer is null)
            return;

        try
        {
            var pcm16k = ConvertToPcm16Mono16k(e.Buffer, e.BytesRecorded, _captureFormat);
            if (pcm16k.Length == 0)
                return;

            if (HasAudioEnergy(pcm16k))
            {
                _lastSpeechUtc = DateTime.UtcNow;
                _micWarningRaised = false;
            }
            else if (!_micWarningRaised
                     && _isListening
                     && DateTime.UtcNow - _listenStartedUtc > TimeSpan.FromMilliseconds(MicCheckMs)
                     && DateTime.UtcNow - _lastSpeechUtc > TimeSpan.FromMilliseconds(MicCheckMs))
            {
                _micWarningRaised = true;
                DispatchError("لم يُلتقط صوت من الميكروفون — تحقق من صلاحيات Windows للميكروفون.");
            }

            if (recognizer.AcceptWaveform(pcm16k, pcm16k.Length))
                HandleFinalText(ExtractText(recognizer.Result()));
            else
                HandlePartialText(ExtractPartial(recognizer.PartialResult()));
        }
        catch
        {
            // ignore frame errors
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception is not null)
            DispatchError(e.Exception.Message);
    }

    private void HandlePartialText(string? partial)
    {
        if (string.IsNullOrWhiteSpace(partial))
            return;

        _lastPartial = partial;
        _lastSpeechUtc = DateTime.UtcNow;
        DispatchTranscript(partial);
    }

    private void HandleFinalText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        _lastPartial = string.Empty;
        DispatchPhrase(text, final: true);
    }

    private void FinalizeOnSilence()
    {
        if (!_isListening)
            return;

        if (DateTime.UtcNow - _lastSpeechUtc < TimeSpan.FromMilliseconds(SilenceFinalizeMs))
            return;

        if (string.IsNullOrWhiteSpace(_lastPartial))
            return;

        VoskRecognizer? recognizer;
        lock (_sync)
            recognizer = _recognizer;

        if (recognizer is null)
            return;

        try
        {
            var text = ExtractText(recognizer.FinalResult());
            if (string.IsNullOrWhiteSpace(text))
                text = _lastPartial;

            _lastPartial = string.Empty;
            if (!string.IsNullOrWhiteSpace(text))
                DispatchPhrase(text, final: true);
        }
        catch
        {
            // ignore
        }
    }

    private void StartSilenceTimer()
    {
        StopSilenceTimer();
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
            return;

        _silenceTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(250), DispatcherPriority.Background, (_, _) =>
        {
            FinalizeOnSilence();
        }, dispatcher);
        _silenceTimer.Start();
    }

    private void StopSilenceTimer()
    {
        _silenceTimer?.Stop();
        _silenceTimer = null;
    }

    private static byte[] ConvertToPcm16Mono16k(byte[] buffer, int length, WaveFormat sourceFormat)
    {
        using var input = new MemoryStream(buffer, 0, length, writable: false);
        using var raw = new RawSourceWaveStream(input, sourceFormat);
        ISampleProvider samples = raw.ToSampleProvider();
        if (sourceFormat.Channels > 1)
            samples = samples.ToMono();

        var resampled = new WdlResamplingSampleProvider(samples, (int)TargetSampleRate);
        var pcm16 = new SampleToWaveProvider16(resampled);

        using var pcmStream = new MemoryStream();
        var convertBuffer = new byte[4096];
        int read;
        while ((read = pcm16.Read(convertBuffer, 0, convertBuffer.Length)) > 0)
            pcmStream.Write(convertBuffer, 0, read);

        return pcmStream.ToArray();
    }

    private static bool HasAudioEnergy(byte[] pcm16)
    {
        if (pcm16.Length < 2)
            return false;

        long sum = 0;
        var count = pcm16.Length / 2;
        for (var i = 0; i < pcm16.Length - 1; i += 2)
        {
            var sample = BitConverter.ToInt16(pcm16, i);
            sum += Math.Abs(sample);
        }

        return sum / (double)count > 180;
    }

    private static string? ExtractText(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("text", out var text)
            ? text.GetString()?.Trim()
            : null;
    }

    private static string? ExtractPartial(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("partial", out var partial)
            ? partial.GetString()?.Trim()
            : null;
    }

    private void DispatchTranscript(string text) =>
        Application.Current?.Dispatcher.BeginInvoke(() => TranscriptChanged?.Invoke(this, text));

    private void DispatchPhrase(string text, bool final)
    {
        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            TranscriptChanged?.Invoke(this, text);
            if (final)
            {
                SetState(VoiceAssistantState.Processing);
                PhraseRecognized?.Invoke(this, text);
            }
        });
    }

    private void DispatchError(string message) =>
        Application.Current?.Dispatcher.BeginInvoke(() => ErrorOccurred?.Invoke(this, message));

    private void StopCaptureUnsafe()
    {
        if (_capture is null)
            return;

        try
        {
            _capture.DataAvailable -= OnDataAvailable;
            _capture.RecordingStopped -= OnRecordingStopped;
            _capture.StopRecording();
            _capture.Dispose();
        }
        catch
        {
            // ignore
        }
        finally
        {
            _capture = null;
            _captureFormat = null;
        }
    }

    private void DisposeEngineOnly()
    {
        _recognizer?.Dispose();
        _recognizer = null;
        _model?.Dispose();
        _model = null;
        IsAvailable = false;
    }

    private void SetState(VoiceAssistantState state)
    {
        _state = state;
        StateChanged?.Invoke(this, state);
    }

    private void RaiseError(string message) =>
        ErrorOccurred?.Invoke(this, message);
}
