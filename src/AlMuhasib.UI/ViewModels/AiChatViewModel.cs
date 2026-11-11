using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.ViewModels;

public partial class AiChatViewModel : ObservableObject
{
    private readonly IAssistantService _assistant;
    private readonly ICurrentUserService _currentUser;
    private readonly AssistantHistoryStore _history;
    private CancellationTokenSource? _sendCts;
    private CancellationTokenSource? _revealCts;
    private int _sendGate;
    private int _historyState;
    private int? _loadedForUser;

    public AiChatViewModel(IAssistantService assistant, ICurrentUserService currentUser, AssistantHistoryStore history)
    {
        _assistant = assistant;
        _currentUser = currentUser;
        _history = history;
        StatusText = _assistant.StatusDescription;
        Suggestions = CreateSuggestions();
    }

    public ObservableCollection<AssistantMessage> Messages { get; } = new();

    public IReadOnlyList<AssistantSuggestion> Suggestions { get; }

    [ObservableProperty]
    private string _draftText = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _showWelcome = true;

    [ObservableProperty]
    private string _statusText = "متصل ببيانات النظام";

    [ObservableProperty]
    private AssistantMessage? _lastAddedMessage;

    public bool CanSend => !IsBusy && !string.IsNullOrWhiteSpace(DraftText);

    partial void OnDraftTextChanged(string value) => SendCommand.NotifyCanExecuteChanged();

    partial void OnIsBusyChanged(bool value)
    {
        SendCommand.NotifyCanExecuteChanged();
        StatusText = value ? "يكتب..." : _assistant.StatusDescription;
    }

    public void OnChatOpened()
    {
        var userId = _currentUser.UserId;
        if (_loadedForUser != userId)
        {
            _revealCts?.Cancel();
            Messages.Clear();
            ShowWelcome = true;
            _historyState = 0;
            _loadedForUser = userId;
        }

        if (Interlocked.CompareExchange(ref _historyState, 1, 0) != 0)
            return;

        _ = LoadHistoryAsync(userId);
    }

    public void ResetForLogout()
    {
        _assistant.ResetSession();
        _revealCts?.Cancel();
        _sendCts?.Cancel();
        Messages.Clear();
        DraftText = string.Empty;
        ShowWelcome = true;
        IsBusy = false;
        LastAddedMessage = null;
        _loadedForUser = null;
        _historyState = 0;
        Interlocked.Exchange(ref _sendGate, 0);
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync()
    {
        var text = DraftText.Trim();
        if (string.IsNullOrEmpty(text))
            return;

        if (Interlocked.Exchange(ref _sendGate, 1) == 1)
            return;

        try
        {
            DraftText = string.Empty;
            ShowWelcome = false;

            var userMsg = new AssistantMessage
            {
                Role = AssistantMessageRole.User,
                Content = text,
                DisplayContent = text,
                PlainText = text
            };
            Messages.Add(userMsg);
            LastAddedMessage = userMsg;

            IsBusy = true;
            _sendCts?.Cancel();
            _sendCts = new CancellationTokenSource();

            var history = Messages.ToList();
            var reply = await _assistant.GetReplyAsync(text, history, _sendCts.Token);
            reply.DisplayContent = reply.Content.Length <= 48 ? reply.Content : string.Empty;
            Messages.Add(reply);
            LastAddedMessage = reply;
            _ = RevealAsync(reply);
            _ = _history.SaveAsync(_currentUser.UserId, Messages.ToList());
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            var err = new AssistantMessage
            {
                Role = AssistantMessageRole.Assistant,
                Content = "تعذّر إكمال الطلب. أعد المحاولة بعد قليل.",
                DisplayContent = "تعذّر إكمال الطلب. أعد المحاولة بعد قليل.",
                PlainText = "تعذّر إكمال الطلب. أعد المحاولة بعد قليل.",
                IsError = true
            };
            Messages.Add(err);
            LastAddedMessage = err;
        }
        finally
        {
            IsBusy = false;
            Interlocked.Exchange(ref _sendGate, 0);
            SendCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand]
    private async Task UseSuggestionAsync(AssistantSuggestion? suggestion)
    {
        if (suggestion is null || IsBusy)
            return;

        DraftText = suggestion.Prompt;
        await SendAsync();
    }

    [RelayCommand]
    private async Task RetryMessageAsync(AssistantMessage? message)
    {
        var text = message?.RelatedQuestion;
        if (string.IsNullOrWhiteSpace(text) || IsBusy)
            return;

        DraftText = text;
        await SendAsync();
    }

    [RelayCommand]
    private void CopyMessage(AssistantMessage? message)
    {
        var text = string.IsNullOrWhiteSpace(message?.PlainText) ? message?.Content : message.PlainText;
        if (string.IsNullOrWhiteSpace(text))
            return;

        try
        {
            Clipboard.SetText(text);
            StatusText = "تم نسخ الإجابة";
        }
        catch
        {
            StatusText = "تعذّر النسخ";
        }
    }

    [RelayCommand]
    private void NewChat()
    {
        _sendCts?.Cancel();
        _revealCts?.Cancel();
        _assistant.ResetSession();
        _history.Delete(_currentUser.UserId);
        Messages.Clear();
        DraftText = string.Empty;
        ShowWelcome = true;
        IsBusy = false;
        LastAddedMessage = null;
        StatusText = _assistant.StatusDescription;
        _historyState = 2;
        Interlocked.Exchange(ref _sendGate, 0);
        SendCommand.NotifyCanExecuteChanged();
    }

    private async Task LoadHistoryAsync(int? userId)
    {
        try
        {
            var items = await _history.LoadAsync(userId);
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher is null)
                return;

            await dispatcher.InvokeAsync(() =>
            {
                if (_currentUser.UserId != userId || Messages.Count > 0 || items.Count == 0)
                    return;

                foreach (var item in items)
                    Messages.Add(item);
                ShowWelcome = false;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally
        {
            _historyState = 2;
        }
    }

    private async Task RevealAsync(AssistantMessage message)
    {
        var full = message.Content ?? string.Empty;
        if (full.Length <= 48)
        {
            message.DisplayContent = full;
            return;
        }

        _revealCts?.Cancel();
        _revealCts = new CancellationTokenSource();
        var token = _revealCts.Token;
        try
        {
            for (var i = 0; i < full.Length; i += 8)
            {
                token.ThrowIfCancellationRequested();
                message.DisplayContent = full[..Math.Min(full.Length, i + 8)];
                await Task.Delay(12, token);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static IReadOnlyList<AssistantSuggestion> CreateSuggestions() =>
    [
        new()
        {
            Title = "كم إجمالي المبيعات اليوم؟",
            Prompt = "كم إجمالي المبيعات اليوم؟",
            IconKind = PackIconKind.ChartLine,
            AccentHex = "#1565C0",
            BackgroundHex = "#E3F2FD"
        },
        new()
        {
            Title = "كم المبالغ المستحقة على العملاء؟",
            Prompt = "كم المبالغ المستحقة على العملاء؟",
            IconKind = PackIconKind.AccountCash,
            AccentHex = "#6A1B9A",
            BackgroundHex = "#F3E5F5"
        },
        new()
        {
            Title = "ما إجمالي المصروفات هذا الشهر؟",
            Prompt = "ما إجمالي المصروفات هذا الشهر؟",
            IconKind = PackIconKind.CashMinus,
            AccentHex = "#EF6C00",
            BackgroundHex = "#FFF3E0"
        },
        new()
        {
            Title = "من هم أكثر العملاء شراءً؟",
            Prompt = "من هم أكثر العملاء شراءً؟",
            IconKind = PackIconKind.AccountGroup,
            AccentHex = "#00695C",
            BackgroundHex = "#E0F2F1"
        },
        new()
        {
            Title = "ما المنتجات الأكثر مبيعاً؟",
            Prompt = "ما المنتجات الأكثر مبيعاً؟",
            IconKind = PackIconKind.PackageVariantClosed,
            AccentHex = "#C62828",
            BackgroundHex = "#FFEBEE"
        },
        new()
        {
            Title = "كم رصيد الصندوق الحالي؟",
            Prompt = "كم رصيد الصندوق الحالي؟",
            IconKind = PackIconKind.CashRegister,
            AccentHex = "#00838F",
            BackgroundHex = "#E0F7FA"
        },
        new()
        {
            Title = "ملخص مالي لهذا الشهر",
            Prompt = "اعرض لي ملخصاً مالياً لهذا الشهر.",
            IconKind = PackIconKind.Finance,
            AccentHex = "#2E7D32",
            BackgroundHex = "#E8F5E9"
        }
    ];
}
