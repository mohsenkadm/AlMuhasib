using System.Collections.ObjectModel;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class MainWindowViewModel
{
    private readonly IVoiceRecognitionService _voiceRecognition;
    private readonly VoiceCommandCatalog _voiceCommandCatalog;
    private readonly VoiceCommandMatcher _voiceCommandMatcher;
    private readonly VoiceCommandExecutor _voiceCommandExecutor;
    private IReadOnlyList<VoiceCommandDefinition> _voiceCommands = [];
    private CancellationTokenSource? _voiceCts;

    [ObservableProperty] private bool _isVoiceAssistantOpen;
    [ObservableProperty] private VoiceAssistantState _voiceAssistantState = VoiceAssistantState.Idle;
    [ObservableProperty] private string _voiceTranscript = string.Empty;
    [ObservableProperty] private string _voiceStatusMessage = "قل أمراً أو اختر اقتراحاً";
    [ObservableProperty] private bool _isVoiceSpeechUnavailable;
    [ObservableProperty] private bool _isVoicePackInstalling;
    [ObservableProperty] private double _voiceInstallProgress;
    [ObservableProperty] private string _voiceInstallStep = string.Empty;

    public ObservableCollection<VoiceCommandDefinition> VoiceSuggestions { get; } = [];

    private void WireVoiceRecognition()
    {
        _voiceRecognition.TranscriptChanged += OnVoiceTranscriptChanged;
        _voiceRecognition.PhraseRecognized += OnVoicePhraseRecognized;
        _voiceRecognition.StateChanged += OnVoiceStateChanged;
        _voiceRecognition.ErrorOccurred += OnVoiceError;
    }

    [RelayCommand]
    private async Task ToggleVoiceAssistantAsync()
    {
        if (IsVoiceAssistantOpen)
        {
            CloseVoiceAssistant();
            return;
        }

        CloseOtherPanelsForVoice();
        IsVoiceAssistantOpen = true;
        RefreshVoiceSuggestions();
        _voiceCts?.Dispose();
        _voiceCts = new CancellationTokenSource();

        try
        {
            if (!await EnsureVoiceRecognitionReadyAsync(_voiceCts.Token))
                return;

            if (_voiceCts.IsCancellationRequested || !IsVoiceAssistantOpen)
                return;

            await StartVoiceListeningAsync();
        }
        catch (OperationCanceledException)
        {
            ResetVoiceAssistantAfterCancel();
        }
    }

    [RelayCommand]
    private void CloseVoiceAssistant()
    {
        _voiceCts?.Cancel();

        try
        {
            if (_voiceRecognition.IsAvailable)
                _voiceRecognition.StopListening();
        }
        catch
        {
            // ignore speech shutdown races
        }

        IsVoiceAssistantOpen = false;
        VoiceTranscript = string.Empty;
        VoiceAssistantState = VoiceAssistantState.Idle;
        VoiceStatusMessage = "قل أمراً أو اختر اقتراحاً";
        IsVoiceSpeechUnavailable = false;
        IsVoicePackInstalling = false;
        VoiceInstallProgress = 0;
        VoiceInstallStep = string.Empty;
    }

    private void ResetVoiceAssistantAfterCancel()
    {
        IsVoicePackInstalling = false;
        if (!IsVoiceAssistantOpen)
            return;

        VoiceAssistantState = VoiceAssistantState.Idle;
        VoiceStatusMessage = "تم إلغاء التنزيل.";
        VoiceInstallProgress = 0;
        VoiceInstallStep = string.Empty;
    }

    [RelayCommand]
    private async Task RetryVoiceModelSetupAsync()
    {
        _voiceRecognition.ResetSpeechPackInstallCache();
        if (!IsVoiceAssistantOpen)
            return;

        _voiceCts?.Cancel();
        _voiceCts?.Dispose();
        _voiceCts = new CancellationTokenSource();

        try
        {
            if (await EnsureVoiceRecognitionReadyAsync(_voiceCts.Token))
                await StartVoiceListeningAsync();
        }
        catch (OperationCanceledException)
        {
            ResetVoiceAssistantAfterCancel();
        }
    }

    [RelayCommand]
    private async Task ExecuteVoiceSuggestionAsync(VoiceCommandDefinition? command)
    {
        if (command is null)
            return;

        VoiceTranscript = command.DisplayLabel;
        await ProcessVoiceCommandAsync(command);
    }

    private async Task<bool> EnsureVoiceRecognitionReadyAsync(CancellationToken cancellationToken)
    {
        if (_voiceRecognition.IsAvailable)
        {
            IsVoiceSpeechUnavailable = false;
            return true;
        }

        VoiceAssistantState = VoiceAssistantState.Installing;
        IsVoicePackInstalling = true;
        VoiceInstallProgress = 0;
        VoiceInstallStep = "الخطوة 1 من 3";
        VoiceStatusMessage = "جاري تنزيل نموذج التعرف الصوتي العربي (أول مرة فقط)...";
        IsVoiceSpeechUnavailable = false;

        try
        {
            var progress = new Progress<SpeechPackInstallProgress>(ReportVoiceInstallProgress);
            var installed = await _voiceRecognition.EnsureAvailabilityAsync(progress, cancellationToken);

            if (cancellationToken.IsCancellationRequested || !IsVoiceAssistantOpen)
                return false;

            IsVoicePackInstalling = false;

            if (installed)
            {
                IsVoiceSpeechUnavailable = false;
                VoiceStatusMessage = "جاري الاستماع...";
                return true;
            }

            VoiceAssistantState = VoiceAssistantState.Error;
            IsVoiceSpeechUnavailable = true;
            VoiceInstallProgress = 100;
            VoiceInstallStep = "تعذر التثبيت";
            VoiceStatusMessage = _voiceRecognition.UnavailableReason
                ?? "تعذر تجهيز التعرف الصوتي — استخدم الاقتراحات بالنقر أو أعد محاولة التنزيل";
            return false;
        }
        catch (OperationCanceledException)
        {
            IsVoicePackInstalling = false;
            throw;
        }
    }

    private void ReportVoiceInstallProgress(SpeechPackInstallProgress update)
    {
        if (_voiceCts?.IsCancellationRequested == true)
            return;

        var dispatcher = System.Windows.Application.Current.Dispatcher;
        if (dispatcher.CheckAccess())
        {
            ApplyVoiceInstallProgress(update);
            return;
        }

        dispatcher.BeginInvoke(() => ApplyVoiceInstallProgress(update));
    }

    private void ApplyVoiceInstallProgress(SpeechPackInstallProgress update)
    {
        if (_voiceCts?.IsCancellationRequested == true || !IsVoiceAssistantOpen)
            return;

        VoiceStatusMessage = update.Message;
        VoiceInstallProgress = update.Percent;
        if (!string.IsNullOrWhiteSpace(update.StepLabel))
            VoiceInstallStep = update.StepLabel;
    }

    private async Task StartVoiceListeningAsync()
    {
        if (!_voiceRecognition.IsAvailable)
            return;

        VoiceStatusMessage = "جاري الاستماع...";
        var phrases = _voiceCommands.SelectMany(c => c.Phrases).ToList();
        _voiceRecognition.UpdatePhrases(phrases);
        _voiceCts ??= new CancellationTokenSource();
        await _voiceRecognition.StartListeningAsync(_voiceCts.Token);
    }

    private void CloseOtherPanelsForVoice()
    {
        IsMenuCustomizerOpen = false;
        IsQuickAssistOpen = false;
        IsSmartAssistantOpen = false;
        IsTasksPanelOpen = false;
        IsNotesPanelOpen = false;
        IsNotificationPanelOpen = false;
        IsGlobalSearchOpen = false;
        IsRecentActivityOpen = false;
        IsQuickStatementOpen = false;
        CloseReportFlyout();
    }

    private void RefreshVoiceSuggestions()
    {
        _voiceCommands = _voiceCommandCatalog.Build(
            _moduleRegistry,
            vm => TryAuthorizeScreen(vm, out _),
            MenuItems);

        VoiceSuggestions.Clear();
        foreach (var cmd in _voiceCommands
                     .Where(c => c.ActionType is not VoiceCommandActionType.CloseAssistant and not VoiceCommandActionType.ShowHelp)
                     .Take(8))
        {
            VoiceSuggestions.Add(cmd);
        }
    }

    private void OnVoiceTranscriptChanged(object? sender, string transcript)
    {
        System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
        {
            VoiceTranscript = transcript;
            if (VoiceAssistantState == VoiceAssistantState.Listening)
                VoiceStatusMessage = "سمعتك...";
        });
    }

    private async void OnVoicePhraseRecognized(object? sender, string transcript)
    {
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            VoiceTranscript = transcript;
            var match = _voiceCommandMatcher.Match(transcript, _voiceCommands);
            if (match is null)
            {
                VoiceAssistantState = VoiceAssistantState.Error;
                VoiceStatusMessage = string.IsNullOrWhiteSpace(transcript)
                    ? "لم أفهم — جرّب: فاتورة مبيعات، المنتجات، بحث"
                    : $"سمعت «{transcript}» — جرّب أمراً من الاقتراحات";
                _sound.Play(SoundEffect.Error);
                await RestartVoiceListeningAsync();
                return;
            }

            await ProcessVoiceCommandAsync(match.Command);
        });
    }

    private async Task ProcessVoiceCommandAsync(VoiceCommandDefinition command)
    {
        VoiceAssistantState = VoiceAssistantState.Processing;
        _voiceRecognition.StopListening();

        var result = await _voiceCommandExecutor.ExecuteAsync(command, this);

        if (result.Succeeded)
        {
            VoiceAssistantState = VoiceAssistantState.Success;
            if (!string.IsNullOrWhiteSpace(result.Message))
            {
                VoiceStatusMessage = result.Message;
                _toast.ShowSuccess(result.Message);
            }

            _sound.Play(SoundEffect.Success);

            if (result.CloseAssistant)
            {
                await Task.Delay(450);
                CloseVoiceAssistant();
            }
            else
            {
                RefreshVoiceSuggestions();
                VoiceStatusMessage = "اختر أمراً من الاقتراحات أو تحدث مجدداً";
            }
        }
        else
        {
            VoiceAssistantState = VoiceAssistantState.Error;
            VoiceStatusMessage = result.Message;
            _toast.ShowWarning(result.Message);
            _sound.Play(SoundEffect.Error);
            await RestartVoiceListeningAsync();
        }
    }

    private async Task RestartVoiceListeningAsync()
    {
        if (!IsVoiceAssistantOpen || !_voiceRecognition.IsAvailable)
            return;

        await Task.Delay(600);
        VoiceAssistantState = VoiceAssistantState.Listening;
        VoiceStatusMessage = "جاري الاستماع...";
        VoiceTranscript = string.Empty;
        _voiceCts = new CancellationTokenSource();
        await _voiceRecognition.StartListeningAsync(_voiceCts.Token);
    }

    private void OnVoiceStateChanged(object? sender, VoiceAssistantState state)
    {
        System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
        {
            VoiceAssistantState = state;
        });
    }

    private void OnVoiceError(object? sender, string message)
    {
        System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
        {
            VoiceAssistantState = VoiceAssistantState.Error;
            VoiceStatusMessage = message;
        });
    }
}
