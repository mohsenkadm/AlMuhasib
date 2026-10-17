using System.Collections.ObjectModel;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.ViewModels;

public partial class AiChatViewModel : ObservableObject
{
    private readonly IAssistantService _assistant;
    private CancellationTokenSource? _sendCts;
    private int _sendGate;

    public AiChatViewModel(IAssistantService assistant)
    {
        _assistant = assistant;
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
    private string _statusText = "جاهز للمساعدة";

    [ObservableProperty]
    private AssistantMessage? _lastAddedMessage;

    public bool CanSend => !IsBusy && !string.IsNullOrWhiteSpace(DraftText);

    partial void OnDraftTextChanged(string value) => SendCommand.NotifyCanExecuteChanged();

    partial void OnIsBusyChanged(bool value)
    {
        SendCommand.NotifyCanExecuteChanged();
        StatusText = value ? "يكتب..." : _assistant.StatusDescription;
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync()
    {
        var text = DraftText.Trim();
        if (string.IsNullOrEmpty(text))
            return;

        // Prevent double-submit from rapid clicks / Enter.
        if (Interlocked.Exchange(ref _sendGate, 1) == 1)
            return;

        try
        {
            DraftText = string.Empty;
            ShowWelcome = false;

            var userMsg = new AssistantMessage
            {
                Role = AssistantMessageRole.User,
                Content = text
            };
            Messages.Add(userMsg);
            LastAddedMessage = userMsg;

            IsBusy = true;
            _sendCts?.Cancel();
            _sendCts = new CancellationTokenSource();

            var history = Messages.ToList();
            var reply = await _assistant.GetReplyAsync(text, history, _sendCts.Token);
            Messages.Add(reply);
            LastAddedMessage = reply;
        }
        catch (OperationCanceledException)
        {
            // ignored — new chat / close
        }
        catch (Exception ex)
        {
            var err = new AssistantMessage
            {
                Role = AssistantMessageRole.Assistant,
                Content = $"تعذّر إكمال الطلب:\n{ex.Message}",
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
    private void NewChat()
    {
        _sendCts?.Cancel();
        Messages.Clear();
        DraftText = string.Empty;
        ShowWelcome = true;
        IsBusy = false;
        LastAddedMessage = null;
        StatusText = _assistant.StatusDescription;
        Interlocked.Exchange(ref _sendGate, 0);
        SendCommand.NotifyCanExecuteChanged();
    }

    private static IReadOnlyList<AssistantSuggestion> CreateSuggestions() =>
    [
        new()
        {
            Title = "تحليل المبيعات",
            Prompt = "حلّل أداء المبيعات واعرض أبرز المؤشرات.",
            IconKind = PackIconKind.ChartLine,
            AccentHex = "#1565C0",
            BackgroundHex = "#E3F2FD"
        },
        new()
        {
            Title = "ملخص المصروفات",
            Prompt = "أعطني ملخصاً للمصروفات الحالية حسب التصنيف.",
            IconKind = PackIconKind.CashMinus,
            AccentHex = "#EF6C00",
            BackgroundHex = "#FFF3E0"
        },
        new()
        {
            Title = "الأرباح والخسائر",
            Prompt = "اعرض ملخص الأرباح والخسائر للفترة الحالية.",
            IconKind = PackIconKind.Finance,
            AccentHex = "#2E7D32",
            BackgroundHex = "#E8F5E9"
        },
        new()
        {
            Title = "أرصدة العملاء",
            Prompt = "ما هي أبرز أرصدة العملاء المستحقة؟",
            IconKind = PackIconKind.AccountCash,
            AccentHex = "#6A1B9A",
            BackgroundHex = "#F3E5F5"
        },
        new()
        {
            Title = "حركة الصندوق",
            Prompt = "لخّص حركة الصندوق اليوم.",
            IconKind = PackIconKind.CashRegister,
            AccentHex = "#00838F",
            BackgroundHex = "#E0F7FA"
        },
        new()
        {
            Title = "شرح التقارير",
            Prompt = "اشرح لي أهم التقارير المحاسبية في النظام وكيف أستخدمها.",
            IconKind = PackIconKind.FileDocumentOutline,
            AccentHex = "#3949AB",
            BackgroundHex = "#E8EAF6"
        }
    ];
}
