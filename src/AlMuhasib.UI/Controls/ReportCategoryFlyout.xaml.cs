using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.ViewModels;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Controls;

public partial class ReportCategoryFlyout : UserControl
{
    public ReportCategoryFlyout()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => HookViewModel();
    }

    private void HookViewModel()
    {
        if (DataContext is INotifyPropertyChanged oldVm)
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;

        if (DataContext is INotifyPropertyChanged newVm)
        {
            newVm.PropertyChanged += OnViewModelPropertyChanged;
            ApplyHeaderAccent();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainWindowViewModel.ActiveReportCategoryAccent)
            or nameof(MainWindowViewModel.IsReportFlyoutOpen))
            ApplyHeaderAccent();
    }

    private void ApplyHeaderAccent()
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        var color = ParseColor(vm.ActiveReportCategoryAccent);
        HeaderBar.Background = new LinearGradientBrush(
            color,
            Lighten(color, 0.22),
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
        if (sender is not Button button || button.DataContext is not ReportMenuEntry entry)
            return;

        if (button.Template.FindName("IconHost", button) is Border host)
        {
            host.Background = ParseBrush(entry.AccentLightColor);
            if (host.Child is PackIcon icon)
                icon.Foreground = ParseBrush(entry.AccentColor);
        }
    }

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
        byte Mix(byte v) => (byte)Math.Min(255, v + (255 - v) * amount);
        return Color.FromRgb(Mix(c.R), Mix(c.G), Mix(c.B));
    }
}
