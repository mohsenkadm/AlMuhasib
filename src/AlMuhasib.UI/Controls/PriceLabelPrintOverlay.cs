using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Controls;

public sealed class PriceLabelPrintOverlay
{
    private PriceLabelPrintDialog? _dialog;

    public void Show(PriceLabelPrintEditor editor)
    {
        var host = Host();
        if (host is null)
            return;

        if (_dialog is null || !host.Children.Contains(_dialog))
        {
            host.Children.Clear();
            host.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0x99, 0x00, 0x00, 0x00))
            });
            _dialog = new PriceLabelPrintDialog
            {
                DataContext = editor,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            host.Children.Add(_dialog);
        }
        else
        {
            _dialog.DataContext = editor;
        }

        host.Visibility = Visibility.Visible;
    }

    public void Hide()
    {
        var host = Host();
        if (host is not null)
            host.Visibility = Visibility.Collapsed;
    }

    private static Grid? Host() =>
        Application.Current?.MainWindow is MainWindow window ? window.FullScreenHost : null;
}
