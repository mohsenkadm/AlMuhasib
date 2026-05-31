using System.Collections.ObjectModel;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.ViewModels;

public partial class MainWindowViewModel
{
    private readonly ISmartAlertService _smartAlertService;

    [ObservableProperty] private bool _isSmartAssistantOpen;
    [ObservableProperty] private bool _isSmartAssistantLoading;
    [ObservableProperty] private int _smartAlertCount;

    public ObservableCollection<SmartAlert> AssistantAlerts { get; } = [];
    public ObservableCollection<DailyTaskItem> AssistantTasks { get; } = [];
    public ObservableCollection<string> AssistantTips { get; } = [];

    private static readonly string[] StaticTips =
    [
        "استخدم Ctrl+K للبحث السريع في العملاء والمنتجات والشاشات.",
        "ثبّت التبويبات الأكثر استخداماً من «تخصيص القائمة».",
        "شاشة البيع السريع (POS) مناسبة للكاشير — نقدي فقط.",
        "احفظ قوالب الفواتير المتكررة من شاشة المبيعات أو المشتريات.",
        "راجع التنبيهات يومياً من لوحة التحكم أو المساعد الذكي."
    ];

    [RelayCommand]
    private async Task ToggleSmartAssistantAsync()
    {
        IsMenuCustomizerOpen = false;
        IsQuickAssistOpen = false;
        IsSmartAssistantOpen = !IsSmartAssistantOpen;
        if (IsSmartAssistantOpen)
            await RefreshSmartAssistantAsync();
    }

    [RelayCommand]
    private void CloseSmartAssistant() => IsSmartAssistantOpen = false;

    [RelayCommand]
    private async Task RefreshSmartAssistantAsync()
    {
        IsSmartAssistantLoading = true;
        try
        {
            var summary = await _smartAlertService.GetSummaryAsync();

            AssistantAlerts.Clear();
            foreach (var a in summary.Alerts)
                AssistantAlerts.Add(a);

            AssistantTasks.Clear();
            foreach (var t in summary.DailyTasks)
                AssistantTasks.Add(t);

            AssistantTips.Clear();
            foreach (var tip in StaticTips)
                AssistantTips.Add(tip);

            SmartAlertCount = summary.Alerts.Count;
        }
        finally
        {
            IsSmartAssistantLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExecuteAssistantTaskAsync(DailyTaskItem? task)
    {
        if (task is null) return;
        IsSmartAssistantOpen = false;
        await ExecuteDailyTaskAsync(task.Action);
    }

    [RelayCommand]
    private async Task ExecuteAssistantAlertAsync(SmartAlert? alert)
    {
        if (alert is null || alert.Action == SmartAlertAction.None) return;
        IsSmartAssistantOpen = false;
        await ExecuteDailyTaskAsync(alert.Action);
    }

    [RelayCommand]
    private async Task QuickOpenPosAsync()
    {
        IsSmartAssistantOpen = false;
        IsQuickAssistOpen = false;
        await OpenTabAsync(typeof(PosQuickSaleViewModel), "بيع سريع (POS)", PackIconKind.PointOfSale);
    }
}
