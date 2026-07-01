using System.Collections.ObjectModel;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;

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
    public ObservableCollection<LocalQueryDefinition> AssistantQueries { get; } = [];
    public ObservableCollection<string> AssistantQueryResults { get; } = [];

    [ObservableProperty] private string _assistantQueryTitle = string.Empty;
    [ObservableProperty] private string _assistantQuerySummary = string.Empty;

    private static readonly string[] StaticTips =
    [
        "استخدم Ctrl+K للبحث السريع في العملاء والمنتجات والأقساط.",
        "ثبّت التبويبات الأكثر استخداماً من «تخصيص القائمة».",
        "لوحة التحصيل اليومية — مستحق/متأخر/هذا الأسبوع.",
        "احفظ قوالب الفواتير المتكررة من شاشة المبيعات أو المشتريات.",
        "راجع التنبيهات يومياً من لوحة التحكم أو المساعد الذكي."
    ];

    [RelayCommand]
    private async Task ToggleSmartAssistantAsync()
    {
        IsVoiceAssistantOpen = false;
        IsMenuCustomizerOpen = false;
        IsQuickAssistOpen = false;
        IsTasksPanelOpen = false;
        IsNotesPanelOpen = false;
        IsNotificationPanelOpen = false;
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

            AssistantQueries.Clear();
            using (var scope = _serviceProvider.CreateScope())
            {
                var localQuery = scope.ServiceProvider.GetRequiredService<ILocalQueryService>();
                foreach (var q in localQuery.GetAvailableQueries())
                    AssistantQueries.Add(q);
            }

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
    private async Task RunAssistantQueryAsync(LocalQueryDefinition? query)
    {
        if (query is null) return;
        using var scope = _serviceProvider.CreateScope();
        var localQuery = scope.ServiceProvider.GetRequiredService<ILocalQueryService>();
        var result = await localQuery.ExecuteAsync(query.Key);
        AssistantQueryTitle = result.Title;
        AssistantQuerySummary = result.Summary;
        AssistantQueryResults.Clear();
        foreach (var line in result.Lines)
            AssistantQueryResults.Add(line);
    }

    [RelayCommand]
    private async Task QuickOpenPosAsync()
    {
        IsSmartAssistantOpen = false;
        IsQuickAssistOpen = false;
        await OpenTabAsync(typeof(PosQuickSaleViewModel), "بيع سريع (POS)", PackIconKind.PointOfSale);
    }
}
