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

        ApplyTableActionBrushes(res, isDark);
        ApplyPanelChromeBrushes(res, isDark);
        ApplyDashboardBrushes(res, isDark);

        SetBrush(res, "PrimaryHueDarkBrush", isDark ? "#E3F2FD" : "#0D47A1");
        SetBrush(res, "HighlightLightBrush", isDark ? "#2A2540" : "#EDE7F6");
        SetBrush(res, "HighlightBorderBrush", isDark ? "#5E35B1" : "#D1C4E9");
        SetBrush(res, "HighlightBrush", isDark ? "#B39DDB" : "#7E57C2");

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

    private static void ApplyTableActionBrushes(ResourceDictionary res, bool isDark)
    {
        if (isDark)
        {
            SetBrush(res, "TableEditIconBackgroundBrush", "#1E3A5F");
            SetBrush(res, "TableEditIconBorderBrush", "#42A5F6");
            SetBrush(res, "TableEditIconForegroundBrush", "#90CAF9");
            SetBrush(res, "TableEditIconHoverBackgroundBrush", "#254775");

            SetBrush(res, "TableDeleteIconBackgroundBrush", "#3D2226");
            SetBrush(res, "TableDeleteIconBorderBrush", "#EF5350");
            SetBrush(res, "TableDeleteIconForegroundBrush", "#FFAB91");
            SetBrush(res, "TableDeleteIconHoverBackgroundBrush", "#4D2A30");

            SetBrush(res, "TableViewIconBackgroundBrush", "#1A3D38");
            SetBrush(res, "TableViewIconBorderBrush", "#4DB6AC");
            SetBrush(res, "TableViewIconForegroundBrush", "#80CBC4");
            SetBrush(res, "TableViewIconHoverBackgroundBrush", "#234A45");

            SetBrush(res, "TablePrintIconBackgroundBrush", "#3D2E1A");
            SetBrush(res, "TablePrintIconBorderBrush", "#FFB74D");
            SetBrush(res, "TablePrintIconForegroundBrush", "#FFCC80");
            SetBrush(res, "TablePrintIconHoverBackgroundBrush", "#4A3820");

            SetBrush(res, "TableSecondaryIconBackgroundBrush", "#2A3344");
            SetBrush(res, "TableSecondaryIconBorderBrush", "#78909C");
            SetBrush(res, "TableSecondaryIconForegroundBrush", "#B0BEC5");
            SetBrush(res, "TableSecondaryIconHoverBackgroundBrush", "#323D52");

            SetBrush(res, "TableAttachIconBackgroundBrush", "#2E1A3D");
            SetBrush(res, "TableAttachIconBorderBrush", "#BA68C8");
            SetBrush(res, "TableAttachIconForegroundBrush", "#CE93D8");
            SetBrush(res, "TableAttachIconHoverBackgroundBrush", "#3A224D");
        }
        else
        {
            SetBrush(res, "TableEditIconBackgroundBrush", "#E3F2FD");
            SetBrush(res, "TableEditIconBorderBrush", "#90CAF9");
            SetBrush(res, "TableEditIconForegroundBrush", "#1565C0");
            SetBrush(res, "TableEditIconHoverBackgroundBrush", "#BBDEFB");

            SetBrush(res, "TableDeleteIconBackgroundBrush", "#FFEBEE");
            SetBrush(res, "TableDeleteIconBorderBrush", "#EF9A9A");
            SetBrush(res, "TableDeleteIconForegroundBrush", "#C62828");
            SetBrush(res, "TableDeleteIconHoverBackgroundBrush", "#FFCDD2");

            SetBrush(res, "TableViewIconBackgroundBrush", "#E0F2F1");
            SetBrush(res, "TableViewIconBorderBrush", "#80CBC4");
            SetBrush(res, "TableViewIconForegroundBrush", "#00695C");
            SetBrush(res, "TableViewIconHoverBackgroundBrush", "#B2DFDB");

            SetBrush(res, "TablePrintIconBackgroundBrush", "#FFF3E0");
            SetBrush(res, "TablePrintIconBorderBrush", "#FFCC80");
            SetBrush(res, "TablePrintIconForegroundBrush", "#EF6C00");
            SetBrush(res, "TablePrintIconHoverBackgroundBrush", "#FFE0B2");

            SetBrush(res, "TableSecondaryIconBackgroundBrush", "#ECEFF1");
            SetBrush(res, "TableSecondaryIconBorderBrush", "#B0BEC5");
            SetBrush(res, "TableSecondaryIconForegroundBrush", "#455A64");
            SetBrush(res, "TableSecondaryIconHoverBackgroundBrush", "#CFD8DC");

            SetBrush(res, "TableAttachIconBackgroundBrush", "#F3E5F5");
            SetBrush(res, "TableAttachIconBorderBrush", "#CE93D8");
            SetBrush(res, "TableAttachIconForegroundBrush", "#7B1FA2");
            SetBrush(res, "TableAttachIconHoverBackgroundBrush", "#E1BEE7");
        }
    }

    private static void ApplyPanelChromeBrushes(ResourceDictionary res, bool isDark)
    {
        SetBrush(res, "PanelCloseForegroundBrush", "#90A4AE");
        SetBrush(res, "PanelCloseBorderBrush", isDark ? "#3D4A5C" : "#E0E0E0");
        SetBrush(res, "PanelCloseHoverBackgroundBrush", isDark ? "#3D2226" : "#FFEBEE");
        SetBrush(res, "PanelCloseHoverBorderBrush", isDark ? "#EF5350" : "#EF9A9A");
        SetBrush(res, "PanelCloseHoverForegroundBrush", "#C62828");
        SetBrush(res, "PanelClosePressedBackgroundBrush", isDark ? "#4D2A30" : "#FFCDD2");
    }

    private static void ApplyDashboardBrushes(ResourceDictionary res, bool isDark)
    {
        res["DashboardAmbientBrush"] = CreateDashboardAmbientBrush(isDark);
        res["DashboardHeroBrush"] = CreateDashboardHeroBrush(isDark);
        res["DashboardGlassChipBrush"] = CreateDashboardGlassChipBrush(isDark);

        SetBrush(res, "DashboardTasksPanelBrush", isDark ? "#1A2438" : "#F8FAFC");
        SetBrush(res, "DashboardTasksPanelBorderBrush", isDark ? "#2D3A52" : "#E2E8F0");
        SetBrush(res, "DashboardAlertsPanelBrush", isDark ? "#2A2218" : "#FFFBF0");
        SetBrush(res, "DashboardAlertsPanelBorderBrush", isDark ? "#4A3A22" : "#FFE0B2");
        SetBrush(res, "DashboardItemBrush", isDark ? "#232B3A" : "#FFFFFF");
        SetBrush(res, "DashboardItemBorderBrush", isDark ? "#354155" : "#E2E8F0");
        SetBrush(res, "DashboardItemHoverBrush", isDark ? "#2A3548" : "#F1F5F9");
        SetBrush(res, "DashboardChartWellBrush", isDark ? "#171D28" : "#F8FAFC");
        SetBrush(res, "DashboardChartWellBorderBrush", isDark ? "#2D3544" : "#EEF2F7");
        SetBrush(res, "DashboardSuccessPanelBrush", isDark ? "#1A2E22" : "#E8F5E9");
        SetBrush(res, "DashboardSuccessPanelBorderBrush", isDark ? "#2E4D38" : "#C8E6C9");
        SetBrush(res, "DashboardSuccessForegroundBrush", isDark ? "#81C784" : "#2E7D32");
        SetBrush(res, "DashboardQuickActionBrush", isDark ? "#F0FFFFFF" : "#F5FFFFFF");
        SetBrush(res, "DashboardAlertTitleBrush", isDark ? "#FFB74D" : "#E65100");
        SetBrush(res, "DashboardAlertBodyBrush", isDark ? "#BCAAA4" : "#6D4C41");
        SetBrush(res, "DashboardAlertItemBorderBrush", isDark ? "#5D4037" : "#FFE082");

        SetBrush(res, "DashboardKpiGreenBrush", isDark ? "#81C784" : "#2E7D32");
        SetBrush(res, "DashboardKpiGreenLightBrush", isDark ? "#1B3324" : "#E8F5E9");
        SetBrush(res, "DashboardKpiOrangeBrush", isDark ? "#FFB74D" : "#EF6C00");
        SetBrush(res, "DashboardKpiOrangeLightBrush", isDark ? "#3D2A14" : "#FFF3E0");
        SetBrush(res, "DashboardKpiBlueBrush", isDark ? "#64B5F6" : "#1565C0");
        SetBrush(res, "DashboardKpiBlueLightBrush", isDark ? "#1A2F4A" : "#E3F2FD");
        SetBrush(res, "DashboardKpiRedBrush", isDark ? "#EF5350" : "#C62828");
        SetBrush(res, "DashboardKpiRedLightBrush", isDark ? "#3D2226" : "#FFEBEE");
        SetBrush(res, "DashboardKpiIndigoBrush", isDark ? "#9FA8DA" : "#283593");
        SetBrush(res, "DashboardKpiIndigoLightBrush", isDark ? "#252A45" : "#E8EAF6");
        SetBrush(res, "DashboardKpiPinkBrush", isDark ? "#F48FB1" : "#AD1457");
        SetBrush(res, "DashboardKpiPinkLightBrush", isDark ? "#3D2230" : "#FCE4EC");
        SetBrush(res, "DashboardKpiTealBrush", isDark ? "#4DB6AC" : "#00695C");
        SetBrush(res, "DashboardKpiTealLightBrush", isDark ? "#1A3330" : "#E0F2F1");

        SetBrush(res, "DashboardPriorityHighBrush", isDark ? "#EF5350" : "#C62828");
        SetBrush(res, "DashboardPriorityHighLightBrush", isDark ? "#3D2226" : "#FFEBEE");
        SetBrush(res, "DashboardPriorityMediumBrush", isDark ? "#FFB74D" : "#EF6C00");
        SetBrush(res, "DashboardPriorityMediumLightBrush", isDark ? "#3D2A14" : "#FFF3E0");
        SetBrush(res, "DashboardPriorityLowBrush", isDark ? "#64B5F6" : "#1565C0");
        SetBrush(res, "DashboardPriorityLowLightBrush", isDark ? "#1A2F4A" : "#E3F2FD");

        SetBrush(res, "DashboardIconBadgeBlueBrush", isDark ? "#1A2F4A" : "#E3F2FD");
        SetBrush(res, "DashboardIconBadgeRedBrush", isDark ? "#3D2226" : "#FFEBEE");
        SetBrush(res, "DashboardIconBadgeCyanBrush", isDark ? "#1A3338" : "#E0F7FA");
        SetBrush(res, "DashboardIconBadgeAccentBrush", isDark ? "#1A3338" : "#E0F7FA");
    }

    private static LinearGradientBrush CreateDashboardAmbientBrush(bool isDark)
    {
        var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };
        if (isDark)
        {
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#12151C")!, 0));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#151B26")!, 0.55));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#121820")!, 1));
        }
        else
        {
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#EEF2F9")!, 0));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#E6ECF6")!, 0.55));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#F3F6FB")!, 1));
        }

        return brush;
    }

    private static LinearGradientBrush CreateDashboardHeroBrush(bool isDark)
    {
        var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };
        if (isDark)
        {
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#0A2540")!, 0));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#123A5C")!, 0.45));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#1A4A72")!, 0.82));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#1F5C5A")!, 1));
        }
        else
        {
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#0A3D7A")!, 0));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#1565C0")!, 0.45));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#1E88E5")!, 0.82));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#26A69A")!, 1));
        }

        return brush;
    }

    private static LinearGradientBrush CreateDashboardGlassChipBrush(bool isDark)
    {
        var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
        if (isDark)
        {
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#33FFFFFF")!, 0));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#18FFFFFF")!, 1));
        }
        else
        {
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#40FFFFFF")!, 0));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#24FFFFFF")!, 1));
        }

        return brush;
    }
}
