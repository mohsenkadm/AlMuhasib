using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using AlMuhasib.UI.ViewModels;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Controls;

public partial class ReportCategoryFlyout : UserControl
{
    private Window? _hostWindow;

    public ReportCategoryFlyout()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => HookViewModel();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _hostWindow = Window.GetWindow(this);
        if (_hostWindow != null)
            _hostWindow.SizeChanged += OnHostWindowSizeChanged;
        ThemeService.ThemeChanged += OnThemeChanged;
        UpdateScrollerMaxHeight();
        RefreshAllCardAccents();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_hostWindow != null)
            _hostWindow.SizeChanged -= OnHostWindowSizeChanged;
        _hostWindow = null;
        ThemeService.ThemeChanged -= OnThemeChanged;
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        ApplyHeaderAccent();
        RefreshAllCardAccents();
    }

    private void OnHostWindowSizeChanged(object sender, SizeChangedEventArgs e) =>
        UpdateScrollerMaxHeight();

    /// <summary>
    /// يوسّع مساحة الكروت لملء ارتفاع النافذة المتاح مع الإبقاء على السكرول للشاشات الصغيرة.
    /// </summary>
    private void UpdateScrollerMaxHeight()
    {
        var windowHeight = _hostWindow?.ActualHeight ?? SystemParameters.WorkArea.Height;
        var max = Math.Max(280, windowHeight - 120);
        CardsScroller.MaxHeight = max;
        RootFlyout.MaxHeight = max + 72;
    }

    private void HookViewModel()
    {
        if (DataContext is INotifyPropertyChanged oldVm)
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;

        if (DataContext is INotifyPropertyChanged newVm)
        {
            newVm.PropertyChanged += OnViewModelPropertyChanged;
            ApplyHeaderAccent();
            UpdateScrollerMaxHeight();
            RefreshAllCardAccents();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainWindowViewModel.ActiveReportCategoryAccent)
            or nameof(MainWindowViewModel.IsReportFlyoutOpen)
            or nameof(MainWindowViewModel.ReportFlyoutItems))
        {
            ApplyHeaderAccent();
            if (e.PropertyName == nameof(MainWindowViewModel.IsReportFlyoutOpen))
                UpdateScrollerMaxHeight();
            if (e.PropertyName is nameof(MainWindowViewModel.ReportFlyoutItems)
                or nameof(MainWindowViewModel.IsReportFlyoutOpen))
                Dispatcher.BeginInvoke(RefreshAllCardAccents, System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }

    private void ApplyHeaderAccent()
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        var color = ParseColor(vm.ActiveReportCategoryAccent);
        // Slightly deepen accent for classic navy dark header
        var end = IsAppDark() ? Darken(color, 0.18) : Lighten(color, 0.22);
        HeaderBar.Background = new LinearGradientBrush(
            color,
            end,
            new Point(0, 0),
            new Point(1, 1));
    }

    private async void ReportCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: ReportMenuEntry entry })
            return;

        if (DataContext is MainWindowViewModel vm)
            await vm.OpenReportFromFlyoutCommand.ExecuteAsync(entry);
    }

    private void ReportCard_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
            ApplyCardAccent(button);
    }

    private void RefreshAllCardAccents()
    {
        foreach (var button in FindVisualChildren<Button>(this))
        {
            if (button.DataContext is ReportMenuEntry)
                ApplyCardAccent(button);
        }
    }

    private void ApplyCardAccent(Button button)
    {
        if (button.DataContext is not ReportMenuEntry entry)
            return;

        // Template may not be applied yet
        if (button.Template is null)
            return;

        button.ApplyTemplate();
        if (button.Template.FindName("IconHost", button) is not Border host)
            return;

        host.Background = ResolveAccentSurface(entry.AccentColor, entry.AccentLightColor);
        if (host.Child is PackIcon icon)
            icon.Foreground = ResolveAccentGlyph(entry.AccentColor);
    }

    private Brush ResolveAccentSurface(string accent, string accentLight)
    {
        if (!IsAppDark())
            return ParseBrush(accentLight);

        // Mix category accent into navy so badges stay tinted, never pastel-white.
        var a = ParseColor(accent);
        var navy = Color.FromRgb(0x14, 0x22, 0x48);
        return new SolidColorBrush(Color.FromRgb(
            Mix(navy.R, a.R, 0.42),
            Mix(navy.G, a.G, 0.42),
            Mix(navy.B, a.B, 0.42)));
    }

    private Brush ResolveAccentGlyph(string accent)
    {
        var a = ParseColor(accent);
        if (!IsAppDark())
            return new SolidColorBrush(a);

        return new SolidColorBrush(Lighten(a, 0.28));
    }

    private static bool IsAppDark()
    {
        if (Application.Current?.TryFindResource("BackgroundBrush") is SolidColorBrush bg)
            return bg.Color.R < 70 && bg.Color.B < 120;
        return false;
    }

    private static byte Mix(byte baseV, byte accentV, double accentWeight) =>
        (byte)Math.Clamp(baseV * (1 - accentWeight) + accentV * accentWeight, 0, 255);

    private static Brush ParseBrush(string color)
    {
        try
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)!);
        }
        catch
        {
            return new SolidColorBrush(Colors.SteelBlue);
        }
    }

    private static Color ParseColor(string color)
    {
        try
        {
            return (Color)ColorConverter.ConvertFromString(color)!;
        }
        catch
        {
            return Color.FromRgb(0x15, 0x65, 0xC0);
        }
    }

    private static Color Lighten(Color c, double amount)
    {
        byte MixChannel(byte v) => (byte)Math.Min(255, v + (255 - v) * amount);
        return Color.FromRgb(MixChannel(c.R), MixChannel(c.G), MixChannel(c.B));
    }

    private static Color Darken(Color c, double amount)
    {
        byte MixChannel(byte v) => (byte)Math.Max(0, v * (1 - amount));
        return Color.FromRgb(MixChannel(c.R), MixChannel(c.G), MixChannel(c.B));
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent is null) yield break;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typed)
                yield return typed;
            foreach (var nested in FindVisualChildren<T>(child))
                yield return nested;
        }
    }
}
