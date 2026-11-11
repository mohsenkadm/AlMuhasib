using AlMuhasib.Core.Intelligence;
using AlMuhasib.UI.Charts;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Models;

public enum AssistantMessageRole
{
    User,
    Assistant,
    System
}

public partial class AssistantMessage : ObservableObject
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public AssistantMessageRole Role { get; init; }
    public string Content { get; set; } = string.Empty;
    public string PlainText { get; set; } = string.Empty;
    public string? RelatedQuestion { get; set; }
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public bool IsError { get; init; }
    public string? SourceLine { get; set; }
    public string Header1 { get; set; } = string.Empty;
    public string Header2 { get; set; } = string.Empty;
    public string Header3 { get; set; } = string.Empty;
    public string Header4 { get; set; } = string.Empty;
    public bool ShowHeader2 { get; set; }
    public bool ShowHeader3 { get; set; }
    public bool ShowHeader4 { get; set; }
    public List<AssistantMetricCard> Metrics { get; set; } = [];
    public List<AssistantTableRow> Rows { get; set; } = [];
    public List<AssistantChartPoint> ChartPoints { get; set; } = [];

    [ObservableProperty]
    private string _displayContent = string.Empty;

    public bool IsFromUser => Role == AssistantMessageRole.User;
    public bool IsFromAssistant => Role == AssistantMessageRole.Assistant;
    public bool HasMetrics => Metrics.Count > 0;
    public bool HasTable => Rows.Count > 0;
    public bool HasSource => !string.IsNullOrWhiteSpace(SourceLine);
    public bool HasRetry => IsFromAssistant && !string.IsNullOrWhiteSpace(RelatedQuestion);
    public bool HasChart { get; private set; }
    public IEnumerable<ISeries> ChartSeries { get; private set; } = [];
    public IEnumerable<Axis> ChartXAxes { get; private set; } = [];
    public IEnumerable<Axis> ChartYAxes { get; private set; } = [];

    public void AttachChart()
    {
        if (ChartPoints.Count == 0)
        {
            HasChart = false;
            return;
        }

        ChartSeries = [ChartThemeConfig.Column(ChartPoints.Select(point => point.Value).ToArray(), "القيمة")];
        ChartXAxes = [ChartThemeConfig.CreateXAxis(ChartPoints.Select(point => point.Label).ToArray(), ChartPoints.Count > 6 ? 35 : 0)];
        ChartYAxes = [ChartThemeConfig.CreateYAxis("د.ع")];
        HasChart = true;
    }
}

public sealed class AssistantTableRow
{
    public string Col1 { get; init; } = string.Empty;
    public string Col2 { get; init; } = string.Empty;
    public string Col3 { get; init; } = string.Empty;
    public string Col4 { get; init; } = string.Empty;
    public bool Show2 { get; init; }
    public bool Show3 { get; init; }
    public bool Show4 { get; init; }
}

public sealed class AssistantSuggestion
{
    public required string Title { get; init; }
    public required string Prompt { get; init; }
    public required PackIconKind IconKind { get; init; }
    public required string AccentHex { get; init; }
    public required string BackgroundHex { get; init; }
}
