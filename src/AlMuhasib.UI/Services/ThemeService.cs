using System.Windows;
using System.Windows.Media;
using AlMuhasib.Core.Interfaces.Services;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Services;

public sealed class ThemeService
{
    /// <summary>Fired after palette, brushes, and chart theme are updated.</summary>
    public static event EventHandler? ThemeChanged;

    private readonly IUserPreferencesService _preferences;

    public ThemeService(IUserPreferencesService preferences)
    {
        _preferences = preferences;
    }

    public void ApplyFromPreferences()
    {
        ApplyTheme(_preferences.Current.IsDarkTheme, _preferences.Current.FontScale);
    }

    public void ToggleTheme()
    {
        _preferences.Update(p => p.IsDarkTheme = !p.IsDarkTheme);
        ApplyFromPreferences();
    }

    public void SetFontScale(double scale)
    {
        var clamped = Math.Clamp(scale, 0.9, 1.35);
        _preferences.Update(p => p.FontScale = clamped);
        ApplyFromPreferences();
    }

    public void ApplyTheme(bool isDark, double fontScale)
    {
        var palette = new PaletteHelper();
        var theme = palette.GetTheme();
        theme.SetBaseTheme(isDark ? BaseTheme.Dark : BaseTheme.Light);
        palette.SetTheme(theme);

        if (Application.Current is null) return;

        var res = Application.Current.Resources;

        SetBrush(res, "BackgroundBrush", isDark ? "#12151C" : "#F5F7FA");
        SetBrush(res, "CardBrush", isDark ? "#1E2430" : "#FFFFFF");
        SetBrush(res, "ContentBackground", isDark ? "#12151C" : "#F5F7FA");
        SetBrush(res, "TopBarBackground", isDark ? "#1A2030" : "#FFFFFF");
        SetBrush(res, "AppTitleBarBackground", isDark ? "#1A2030" : "#FAFCFF");
        SetBrush(res, "SubtleBorderBrush", isDark ? "#2D3544" : "#E8EDF2");
        SetBrush(res, "TextSecondaryBrush", isDark ? "#B0BEC5" : "#757575");
        SetBrush(res, "TextPrimaryBrush", isDark ? "#ECEFF1" : "#212121");
        SetBrush(res, "HintForegroundBrush", isDark ? "#90A4AE" : "#9E9E9E");
        SetBrush(res, "CardBorderBrush", isDark ? "#2D3544" : "#E8EEF5");
        SetBrush(res, "MutedIconBackgroundBrush", isDark ? "#2A3344" : "#ECEFF1");
        SetBrush(res, "ChartEmptyIconBrush", isDark ? "#546E7A" : "#BDBDBD");
        SetBrush(res, "PrimaryHueLightBrush", isDark ? "#1A2744" : "#E3F2FD");
        SetBrush(res, "PrimaryHueLightForegroundBrush", isDark ? "#ECEFF1" : "#212121");
        SetBrush(res, "SearchPanelBackgroundBrush", isDark ? "#1E2430" : "#FAFCFE");
        SetBrush(res, "SearchPanelBorderBrush", isDark ? "#3D4A5C" : "#D5E3F0");
        SetBrush(res, "SearchResultHoverBrush", isDark ? "#263045" : "#E8F4FD");
        SetBrush(res, "SearchResultTitleBrush", isDark ? "#ECEFF1" : "#263238");
        SetBrush(res, "SearchResultSubtitleBrush", isDark ? "#90A4AE" : "#757575");

        res["ChromeTabStripBackground"] = CreateChromeTabStripBrush(isDark);

        // Chrome tabs
        SetBrush(res, "ChromeTabInactiveBrush", isDark ? "#2A3344" : "#D5E0EC");
        SetBrush(res, "ChromeTabInactiveBorderBrush", isDark ? "#3D4A5C" : "#B8C9DB");
        SetBrush(res, "ChromeTabHoverBrush", isDark ? "#323D52" : "#EBF4FC");
        SetBrush(res, "ChromeTabSelectedBrush", isDark ? "#1E2430" : "#FFFFFF");
        SetBrush(res, "ChromeTabSelectedBorderBrush", isDark ? "#42A5F5" : "#90CAF9");
        SetBrush(res, "ChromeTabForegroundBrush", isDark ? "#CFD8DC" : "#37474F");
        SetBrush(res, "ChromeTabSelectedForegroundBrush", isDark ? "#E3F2FD" : "#1565C0");

        // DataGrids
        SetBrush(res, "DataGridBackgroundBrush", isDark ? "#1E2430" : "#FFFFFF");
        SetBrush(res, "DataGridRowBrush", isDark ? "#1E2430" : "#FFFFFF");
        SetBrush(res, "DataGridRowAltBrush", isDark ? "#252D3D" : "#F7F9FC");
        SetBrush(res, "DataGridRowHoverBrush", isDark ? "#2A3A52" : "#E8F4FD");
        SetBrush(res, "DataGridRowSelectedBrush", isDark ? "#1A3A5C" : "#D6EBFC");
        SetBrush(res, "DataGridHeaderBackgroundBrush", isDark ? "#252D3D" : "#EEF3F8");
        SetBrush(res, "DataGridHeaderHoverBrush", isDark ? "#2A3A52" : "#E3F2FD");
        SetBrush(res, "DataGridHeaderForegroundBrush", isDark ? "#B0BEC5" : "#37474F");
        SetBrush(res, "DataGridCellForegroundBrush", isDark ? "#ECEFF1" : "#263238");
        SetBrush(res, "DataGridGridLineBrush", isDark ? "#2D3544" : "#ECEFF1");

        res["NormalFontSize"] = 14.0 * fontScale;
        res["SmallFontSize"] = 12.0 * fontScale;
        res["LargeFontSize"] = 16.0 * fontScale;

        Charts.ChartThemeConfig.ApplyTheme(isDark);
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    private static void SetBrush(ResourceDictionary res, string key, string colorHex)
    {
        res[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)!);
    }

    private static LinearGradientBrush CreateChromeTabStripBrush(bool isDark)
    {
        var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
        if (isDark)
        {
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#1E2430")!, 0));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#171C26")!, 1));
        }
        else
        {
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#EEF3F9")!, 0));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#E3EBF4")!, 1));
        }
        return brush;
    }
}
