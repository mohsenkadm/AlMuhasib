using System.Windows;
using System.Windows.Media;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Services;

public sealed class ThemeService
{
    /// <summary>Fired after palette, brushes, and chart theme are updated.</summary>
    public static event EventHandler? ThemeChanged;

    // Classic blue-dark — matched to SmarterASP V5 navy (#0A1136 family)
    private const string DarkBg = "#0A1136";
    private const string DarkCard = "#121B42";
    private const string DarkTopBar = "#0E1538";
    private const string DarkBorder = "#2A3558";
    private const string DarkText = "#FFFFFF";
    private const string DarkTextSecondary = "#A8B0C4";
    private const string DarkHint = "#8890A8";
    private const string DarkMutedIcon = "#1A2448";
    private const string DarkAlt = "#161F46";
    private const string DarkHover = "#1E2A52";
    private const string DarkSelected = "#1A3A6C";
    private const string DarkHighlight = "#5BA3D9";
    private const string DarkHighlightLight = "#163A6C";
    private const string DarkHighlightBorder = "#337AB7";
    private const string DarkAccent = "#337AB7";
    private const string DarkAccentLight = "#143A5C";
    private const string DarkAccentDark = "#286090";
    private const string DarkChromeInactive = "#1A2448";
    private const string DarkChromeHover = "#1E2A52";
    private const string DarkSearchBorder = "#3A4570";
    // Soft inset that sits inside cards (not a darker “hole”)
    private const string DarkChartWell = "#161F46";

    private readonly IUserPreferencesService _preferences;
    private readonly ISystemProfileService _systemProfile;

    public ThemeService(IUserPreferencesService preferences, ISystemProfileService systemProfile)
    {
        _preferences = preferences;
        _systemProfile = systemProfile;
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
        var isGold = _systemProfile.ActiveSystem == ApplicationSystemType.GoldShop;
        var palette = new PaletteHelper();
        var theme = palette.GetTheme();
        theme.SetBaseTheme(isDark ? BaseTheme.Dark : BaseTheme.Light);
        if (isGold)
        {
            theme.SetPrimaryColor((Color)ColorConverter.ConvertFromString("#B8860B")!);
            theme.SetSecondaryColor((Color)ColorConverter.ConvertFromString("#D4AF37")!);
        }
        palette.SetTheme(theme);

        if (Application.Current is null) return;

        var res = Application.Current.Resources;

        SetBrush(res, "BackgroundBrush", isDark ? DarkBg : "#F5F7FA");
        SetBrush(res, "CardBrush", isDark ? DarkCard : "#FFFFFF");
        SetBrush(res, "ContentBackground", isDark ? DarkBg : "#F5F7FA");
        SetBrush(res, "TopBarBackground", isDark ? DarkTopBar : "#FFFFFF");
        SetBrush(res, "AppTitleBarBackground", isDark ? DarkTopBar : "#FAFCFF");
        SetBrush(res, "SubtleBorderBrush", isDark ? DarkBorder : "#E8EDF2");
        SetBrush(res, "TextSecondaryBrush", isDark ? DarkTextSecondary : "#757575");
        SetBrush(res, "TextPrimaryBrush", isDark ? DarkText : "#212121");
        SetBrush(res, "HintForegroundBrush", isDark ? DarkHint : "#9E9E9E");
        SetBrush(res, "CardBorderBrush", isDark ? DarkBorder : "#E8EEF5");
        SetBrush(res, "MutedIconBackgroundBrush", isDark ? DarkMutedIcon : "#ECEFF1");
        SetBrush(res, "ChartEmptyIconBrush", isDark ? "#5A7390" : "#BDBDBD");

        // Accent / warning — classic blue in dark (replace cyan/purple)
        SetBrush(res, "AccentBrush", isDark ? DarkAccent : "#00ACC1");
        SetBrush(res, "AccentLightBrush", isDark ? DarkAccentLight : "#E0F7FA");
        SetBrush(res, "AccentDarkBrush", isDark ? DarkAccentDark : "#00838F");
        SetBrush(res, "WarningBrush", isDark ? DarkHighlight : "#7E57C2");

        // Semantic badge surfaces (Paid / Overdue / Pending)
        SetBrush(res, "BadgePaidBackgroundBrush", isDark ? "#1A3328" : "#E8F5E9");
        SetBrush(res, "BadgePaidForegroundBrush", isDark ? "#81C784" : "#2E7D32");
        SetBrush(res, "BadgeOverdueBackgroundBrush", isDark ? "#3D2226" : "#FFEBEE");
        SetBrush(res, "BadgeOverdueForegroundBrush", isDark ? "#EF9A9A" : "#C62828");
        SetBrush(res, "BadgePendingBackgroundBrush", isDark ? DarkMutedIcon : "#F5F5F5");
        SetBrush(res, "BadgePendingForegroundBrush", isDark ? DarkTextSecondary : "#616161");

        // Glass / notepad paper
        SetBrush(res, "GlassCardBrush", isDark ? "#33132338" : "#EEFFFFFF");
        SetBrush(res, "NotepadPaperBrush", isDark ? "#1A2A3C" : "#FFF8E1");
        SetBrush(res, "TitleBarButtonHoverBrush", isDark ? DarkHover : "#ECEFF1");

        // Secondary flyout menu surfaces
        SetBrush(res, "FlyoutPanelBrush", isDark ? DarkTopBar : "#F5F7FA");
        SetBrush(res, "FlyoutCardBrush", isDark ? DarkCard : "#FFFFFF");
        SetBrush(res, "FlyoutCardHoverBrush", isDark ? DarkHover : "#E3F2FD");

        if (isGold)
        {
            SetBrush(res, "PrimaryHueMidBrush", isDark ? "#D4AF37" : "#B8860B");
            SetBrush(res, "PrimaryHueMidForegroundBrush", "#FFFFFF");
            SetBrush(res, "PrimaryHueLightBrush", isDark ? "#3D2E14" : "#FFF8E1");
            SetBrush(res, "PrimaryHueLightForegroundBrush", isDark ? DarkText : "#212121");
            SetBrush(res, "PrimaryHueDarkBrush", isDark ? "#FFF8E1" : "#6D4C00");
            SetBrush(res, "PrimaryHueDarkForegroundBrush", isDark ? "#212121" : "#FFFFFF");
            SetBrush(res, "ChromeTabSelectedBorderBrush", isDark ? "#D4AF37" : "#C9A227");
            SetBrush(res, "ChromeTabSelectedForegroundBrush", isDark ? "#FFF8E1" : "#6D4C00");
            ApplyGoldInvoiceTotalsBrushes(res, isDark);
        }
        else
        {
            SetBrush(res, "PrimaryHueLightBrush", isDark ? DarkHighlightLight : "#E3F2FD");
            SetBrush(res, "PrimaryHueLightForegroundBrush", isDark ? DarkText : "#212121");
            SetBrush(res, "PrimaryHueDarkBrush", isDark ? "#BBDEFB" : "#0D47A1");
            SetBrush(res, "ChromeTabSelectedBorderBrush", isDark ? DarkHighlight : "#90CAF9");
            SetBrush(res, "ChromeTabSelectedForegroundBrush", isDark ? "#BBDEFB" : "#1565C0");
        }

        SetBrush(res, "SearchPanelBackgroundBrush", isDark ? DarkCard : "#FAFCFE");
        SetBrush(res, "SearchPanelBorderBrush", isDark ? DarkSearchBorder : "#D5E3F0");
        SetBrush(res, "SearchResultHoverBrush", isDark ? DarkHover : "#E8F4FD");
        SetBrush(res, "SearchResultTitleBrush", isDark ? DarkText : "#263238");
        SetBrush(res, "SearchResultSubtitleBrush", isDark ? DarkHint : "#757575");

        res["ChromeTabStripBackground"] = CreateChromeTabStripBrush(isDark);

        // Chrome tabs
        SetBrush(res, "ChromeTabInactiveBrush", isDark ? DarkChromeInactive : "#D5E0EC");
        SetBrush(res, "ChromeTabInactiveBorderBrush", isDark ? DarkSearchBorder : "#B8C9DB");
        SetBrush(res, "ChromeTabHoverBrush", isDark ? DarkChromeHover : "#EBF4FC");
        SetBrush(res, "ChromeTabSelectedBrush", isDark ? DarkCard : "#FFFFFF");
        SetBrush(res, "ChromeTabForegroundBrush", isDark ? "#C5D4E4" : "#37474F");

        // DataGrids
        SetBrush(res, "DataGridBackgroundBrush", isDark ? DarkCard : "#FFFFFF");
        SetBrush(res, "DataGridRowBrush", isDark ? DarkCard : "#FFFFFF");
        SetBrush(res, "DataGridRowAltBrush", isDark ? DarkAlt : "#F7F9FC");
        SetBrush(res, "DataGridRowHoverBrush", isDark ? DarkHover : "#E8F4FD");
        SetBrush(res, "DataGridRowSelectedBrush", isDark ? DarkSelected : "#D6EBFC");
        SetBrush(res, "DataGridHeaderBackgroundBrush", isDark ? DarkAlt : "#EEF3F8");
        SetBrush(res, "DataGridHeaderHoverBrush", isDark ? DarkHover : "#E3F2FD");
        SetBrush(res, "DataGridHeaderForegroundBrush", isDark ? DarkTextSecondary : "#37474F");
        SetBrush(res, "DataGridCellForegroundBrush", isDark ? DarkText : "#263238");
        SetBrush(res, "DataGridGridLineBrush", isDark ? DarkBorder : "#ECEFF1");

        ApplyTableActionBrushes(res, isDark);
        ApplyPanelChromeBrushes(res, isDark);
        ApplyDashboardBrushes(res, isDark);

        if (!isGold)
            SetBrush(res, "PrimaryHueDarkBrush", isDark ? "#BBDEFB" : "#0D47A1");

        // Highlight — classic blue (not purple)
        SetBrush(res, "HighlightLightBrush", isDark ? DarkHighlightLight : "#E3F2FD");
        SetBrush(res, "HighlightBorderBrush", isDark ? DarkHighlightBorder : "#90CAF9");
        SetBrush(res, "HighlightBrush", isDark ? DarkHighlight : "#1565C0");

        ApplyMaterialDesignCompatBrushes(res, isDark);

        res["NormalFontSize"] = 14.0 * fontScale;
        res["SmallFontSize"] = 12.0 * fontScale;
        res["LargeFontSize"] = 16.0 * fontScale;

        Charts.ChartThemeConfig.ApplyTheme(isDark);
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Force MaterialDesign legacy/compat brushes to follow classic blue-dark so
    /// cards, paper surfaces, and body text never stay white/black after toggle.
    /// </summary>
    private static void ApplyMaterialDesignCompatBrushes(ResourceDictionary res, bool isDark)
    {
        SetBrush(res, "MaterialDesignPaper", isDark ? DarkCard : "#FFFFFF");
        SetBrush(res, "MaterialDesignBody", isDark ? DarkText : "#212121");
        SetBrush(res, "MaterialDesignBodyLight", isDark ? DarkTextSecondary : "#757575");
        SetBrush(res, "MaterialDesignDivider", isDark ? DarkBorder : "#E0E0E0");
        SetBrush(res, "MaterialDesignToolBarBackground", isDark ? DarkTopBar : "#FFFFFF");

        // MaterialDesignThemes 5 resource keys
        SetBrush(res, "MaterialDesign.Brush.Background", isDark ? DarkBg : "#F5F7FA");
        SetBrush(res, "MaterialDesign.Brush.CardBackground", isDark ? DarkCard : "#FFFFFF");
        SetBrush(res, "MaterialDesign.Brush.Foreground", isDark ? DarkText : "#212121");
        SetBrush(res, "MaterialDesign.Brush.Foreground.Light", isDark ? DarkTextSecondary : "#757575");
        SetBrush(res, "MaterialDesign.Brush.Chip.Background", isDark ? DarkMutedIcon : "#ECEFF1");
        SetBrush(res, "MaterialDesign.Brush.Chip.Background.Outline", isDark ? DarkMutedIcon : "#FFFFFF");
        SetBrush(res, "MaterialDesign.Brush.TextBox.HoverBackground", isDark ? DarkHover : "#EEEEEE");
        SetBrush(res, "MaterialDesign.Brush.TextBox.OutlineInactive", isDark ? DarkBorder : "#89000000");
        SetBrush(res, "MaterialDesign.Brush.CheckBox", isDark ? DarkHighlight : "#89000000");
    }

    private static void SetBrush(ResourceDictionary res, string key, string colorHex)
    {
        res[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)!);
    }

    private static void ApplyGoldInvoiceTotalsBrushes(ResourceDictionary res, bool isDark)
    {
        res["GoldInvoiceTotalsBackgroundBrush"] = CreateGoldInvoiceTotalsBrush(isDark);
        SetBrush(res, "GoldInvoiceTotalsTextBrush", isDark ? DarkText : "#5D4037");
        SetBrush(res, "GoldInvoiceTotalsAccentBrush", isDark ? "#D4AF37" : "#8B6914");
        SetBrush(res, "GoldInvoiceTotalsMutedBrush", isDark ? "#BCAAA4" : "#8D6E63");
    }

    private static LinearGradientBrush CreateGoldInvoiceTotalsBrush(bool isDark)
    {
        var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };
        if (isDark)
        {
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#2A2418")!, 0));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#1A160E")!, 1));
        }
        else
        {
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#FFF8E7")!, 0));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#F5E6C8")!, 1));
        }

        return brush;
    }

    private static LinearGradientBrush CreateChromeTabStripBrush(bool isDark)
    {
        var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
        if (isDark)
        {
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(DarkCard)!, 0));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(DarkTopBar)!, 1));
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

            SetBrush(res, "TableSecondaryIconBackgroundBrush", DarkMutedIcon);
            SetBrush(res, "TableSecondaryIconBorderBrush", "#78909C");
            SetBrush(res, "TableSecondaryIconForegroundBrush", DarkTextSecondary);
            SetBrush(res, "TableSecondaryIconHoverBackgroundBrush", DarkHover);

            SetBrush(res, "TableAttachIconBackgroundBrush", "#1A3048");
            SetBrush(res, "TableAttachIconBorderBrush", "#64B5F6");
            SetBrush(res, "TableAttachIconForegroundBrush", "#90CAF9");
            SetBrush(res, "TableAttachIconHoverBackgroundBrush", DarkHover);
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

            SetBrush(res, "TableAttachIconBackgroundBrush", "#E3F2FD");
            SetBrush(res, "TableAttachIconBorderBrush", "#90CAF9");
            SetBrush(res, "TableAttachIconForegroundBrush", "#1565C0");
            SetBrush(res, "TableAttachIconHoverBackgroundBrush", "#BBDEFB");
        }
    }

    private static void ApplyPanelChromeBrushes(ResourceDictionary res, bool isDark)
    {
        SetBrush(res, "PanelCloseForegroundBrush", isDark ? DarkTextSecondary : "#90A4AE");
        SetBrush(res, "PanelCloseBorderBrush", isDark ? DarkSearchBorder : "#E0E0E0");
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

        SetBrush(res, "DashboardTasksPanelBrush", isDark ? "#152A40" : "#F8FAFC");
        SetBrush(res, "DashboardTasksPanelBorderBrush", isDark ? DarkBorder : "#E2E8F0");
        SetBrush(res, "DashboardAlertsPanelBrush", isDark ? "#2A2218" : "#FFFBF0");
        SetBrush(res, "DashboardAlertsPanelBorderBrush", isDark ? "#4A3A22" : "#FFE0B2");
        SetBrush(res, "DashboardItemBrush", isDark ? DarkCard : "#FFFFFF");
        SetBrush(res, "DashboardItemBorderBrush", isDark ? DarkBorder : "#E2E8F0");
        SetBrush(res, "DashboardItemHoverBrush", isDark ? DarkHover : "#F1F5F9");
        SetBrush(res, "DashboardChartWellBrush", isDark ? DarkChartWell : "#F3F6FB");
        SetBrush(res, "DashboardChartWellBorderBrush", isDark ? "#243056" : "#E6ECF5");
        SetBrush(res, "DashboardChartCardBrush", isDark ? DarkCard : "#FFFFFF");
        SetBrush(res, "DashboardChartHeaderBrush", isDark ? "#141D48" : "#FAFBFD");
        SetBrush(res, "DashboardSuccessPanelBrush", isDark ? "#1A2E22" : "#E8F5E9");
        SetBrush(res, "DashboardSuccessPanelBorderBrush", isDark ? "#2E4D38" : "#C8E6C9");
        SetBrush(res, "DashboardSuccessForegroundBrush", isDark ? "#81C784" : "#2E7D32");
        // Glass on hero: translucent dark navy in dark (not near-white).
        SetBrush(res, "DashboardQuickActionBrush", isDark ? "#D0121B42" : "#F5FFFFFF");
        SetBrush(res, "DashboardQuickActionTitleBrush", isDark ? DarkText : "#212121");
        SetBrush(res, "DashboardQuickActionSubtitleBrush", isDark ? DarkTextSecondary : "#616161");
        SetBrush(res, "DashboardAlertTitleBrush", isDark ? "#FFB74D" : "#E65100");
        SetBrush(res, "DashboardAlertBodyBrush", isDark ? "#BCAAA4" : "#6D4C41");
        SetBrush(res, "DashboardAlertItemBorderBrush", isDark ? "#5D4037" : "#FFE082");

        // Invoice / price-check surfaces that were hardcoded pastel/white.
        res["InvoiceSalesHeaderBrush"] = CreateInvoiceSalesHeaderBrush(isDark);
        res["InvoiceSalesTotalsBrush"] = CreateInvoiceSalesTotalsBrush(isDark);
        res["InvoicePurchaseHeaderBrush"] = CreateInvoicePurchaseHeaderBrush(isDark);
        res["InvoicePurchaseTotalsBrush"] = CreateInvoicePurchaseTotalsBrush(isDark);
        SetBrush(res, "PriceCheckSurfaceBrush", isDark ? "#152A22" : "#F1F8E9");
        SetBrush(res, "PriceCheckTitleBrush", isDark ? "#A5D6A7" : "#33691E");
        SetBrush(res, "PriceCheckBodyBrush", isDark ? "#81C784" : "#558B2F");
        SetBrush(res, "PriceCheckProductBrush", isDark ? DarkText : "#1B5E20");
        SetBrush(res, "HelpListItemHoverBrush", isDark ? DarkHover : "#F5FAFF");
        SetBrush(res, "HelpListItemSelectedBrush", isDark ? DarkSelected : "#E3F2FD");
        SetBrush(res, "HelpVideoTitleBrush", isDark ? DarkHighlight : "#1A237E");

        SetBrush(res, "DashboardKpiGreenBrush", isDark ? "#81C784" : "#2E7D32");
        SetBrush(res, "DashboardKpiGreenLightBrush", isDark ? "#1B3324" : "#E8F5E9");
        SetBrush(res, "DashboardKpiOrangeBrush", isDark ? "#FFB74D" : "#EF6C00");
        SetBrush(res, "DashboardKpiOrangeLightBrush", isDark ? "#3D2A14" : "#FFF3E0");
        SetBrush(res, "DashboardKpiBlueBrush", isDark ? "#64B5F6" : "#1565C0");
        SetBrush(res, "DashboardKpiBlueLightBrush", isDark ? DarkHighlightLight : "#E3F2FD");
        SetBrush(res, "DashboardKpiRedBrush", isDark ? "#EF5350" : "#C62828");
        SetBrush(res, "DashboardKpiRedLightBrush", isDark ? "#3D2226" : "#FFEBEE");
        SetBrush(res, "DashboardKpiIndigoBrush", isDark ? "#90CAF9" : "#1565C0");
        SetBrush(res, "DashboardKpiIndigoLightBrush", isDark ? "#1A3048" : "#E3F2FD");
        SetBrush(res, "DashboardKpiPinkBrush", isDark ? "#F48FB1" : "#AD1457");
        SetBrush(res, "DashboardKpiPinkLightBrush", isDark ? "#3D2230" : "#FCE4EC");
        SetBrush(res, "DashboardKpiTealBrush", isDark ? "#4DB6AC" : "#00695C");
        SetBrush(res, "DashboardKpiTealLightBrush", isDark ? "#1A3330" : "#E0F2F1");

        SetBrush(res, "DashboardPriorityHighBrush", isDark ? "#EF5350" : "#C62828");
        SetBrush(res, "DashboardPriorityHighLightBrush", isDark ? "#3D2226" : "#FFEBEE");
        SetBrush(res, "DashboardPriorityMediumBrush", isDark ? "#FFB74D" : "#EF6C00");
        SetBrush(res, "DashboardPriorityMediumLightBrush", isDark ? "#3D2A14" : "#FFF3E0");
        SetBrush(res, "DashboardPriorityLowBrush", isDark ? "#64B5F6" : "#1565C0");
        SetBrush(res, "DashboardPriorityLowLightBrush", isDark ? DarkHighlightLight : "#E3F2FD");

        SetBrush(res, "DashboardIconBadgeBlueBrush", isDark ? DarkHighlightLight : "#E3F2FD");
        SetBrush(res, "DashboardIconBadgeRedBrush", isDark ? "#3D2226" : "#FFEBEE");
        SetBrush(res, "DashboardIconBadgeCyanBrush", isDark ? "#143A5C" : "#E0F7FA");
        SetBrush(res, "DashboardIconBadgeAccentBrush", isDark ? "#143A5C" : "#E0F7FA");
    }

    private static LinearGradientBrush CreateDashboardAmbientBrush(bool isDark)
    {
        var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };
        if (isDark)
        {
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#0A1136")!, 0));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#0C1234")!, 0.55));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#0E1538")!, 1));
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
            // Classic navy → SmarterASP blue accent
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#0A1136")!, 0));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#123A5C")!, 0.45));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#286090")!, 0.82));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#337AB7")!, 1));
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

    private static LinearGradientBrush CreateInvoiceSalesHeaderBrush(bool isDark)
    {
        var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
        if (isDark)
        {
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#1A2E22")!, 0));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(DarkCard)!, 1));
        }
        else
        {
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#F1F8F4")!, 0));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#FFFFFF")!, 1));
        }

        return brush;
    }

    private static LinearGradientBrush CreateInvoiceSalesTotalsBrush(bool isDark)
    {
        var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };
        if (isDark)
        {
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#1A2E22")!, 0));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#243D2E")!, 1));
        }
        else
        {
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#E8F5E9")!, 0));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#C8E6C9")!, 1));
        }

        return brush;
    }

    private static LinearGradientBrush CreateInvoicePurchaseHeaderBrush(bool isDark)
    {
        var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
        if (isDark)
        {
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(DarkHighlightLight)!, 0));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(DarkCard)!, 1));
        }
        else
        {
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#E3F2FD")!, 0));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#FFFFFF")!, 1));
        }

        return brush;
    }

    private static LinearGradientBrush CreateInvoicePurchaseTotalsBrush(bool isDark)
    {
        var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };
        if (isDark)
        {
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(DarkHighlightLight)!, 0));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#1A3048")!, 1));
        }
        else
        {
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#E3F2FD")!, 0));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#BBDEFB")!, 1));
        }

        return brush;
    }
}
