using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Models;

/// <summary>تقرير واحد داخل لوحة التقارير الجانبية.</summary>
public class ReportMenuEntry
{
    public required string Title { get; init; }
    public PackIconKind Icon { get; init; }
    public required Type ViewModelType { get; init; }
    public required string ScreenName { get; init; }
    public string AccentColor { get; init; } = "#1565C0";
    public string AccentLightColor { get; init; } = "#E3F2FD";
}
