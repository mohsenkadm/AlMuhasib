using System.Windows;
using System.Windows.Controls;

namespace AlMuhasib.UI.Controls;

public partial class PriceLabelPrintDialog : UserControl
{
    private bool? _compact;

    public PriceLabelPrintDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => Dispatcher.BeginInvoke(ApplyResponsive);
        SizeChanged += (_, _) => ApplyResponsive();
    }

    private void ApplyResponsive()
    {
        if (ActualWidth < 100)
            return;

        var compact = ActualWidth < 1100 || ActualHeight < 680;
        if (_compact == compact)
            return;

        _compact = compact;
        if (compact)
            UseStackedLayout();
        else
            UseCenteredLayout();
    }

    private void UseCenteredLayout()
    {
        MoveTo(LeftCards, Stage);
        MoveTo(RightCards, Stage);
        CardsScroll.Visibility = Visibility.Collapsed;

        C0.Width = new GridLength(1, GridUnitType.Star);
        C1.Width = new GridLength(14);
        C2.Width = new GridLength(1.45, GridUnitType.Star);
        C3.Width = new GridLength(14);
        C4.Width = new GridLength(1, GridUnitType.Star);
        R0.Height = new GridLength(1, GridUnitType.Star);
        R1.Height = new GridLength(0);
        R2.Height = new GridLength(0);

        Grid.SetRow(LeftCards, 0);
        Grid.SetColumn(LeftCards, 0);
        Grid.SetColumnSpan(LeftCards, 1);
        Grid.SetRow(PreviewPane, 0);
        Grid.SetColumn(PreviewPane, 2);
        Grid.SetColumnSpan(PreviewPane, 1);
        Grid.SetRow(RightCards, 0);
        Grid.SetColumn(RightCards, 4);
        Grid.SetColumnSpan(RightCards, 1);
    }

    private void UseStackedLayout()
    {
        MoveTo(LeftCards, CardsHost);
        MoveTo(RightCards, CardsHost);
        CardsScroll.Visibility = Visibility.Visible;

        C0.Width = new GridLength(1, GridUnitType.Star);
        C1.Width = new GridLength(0);
        C2.Width = new GridLength(0);
        C3.Width = new GridLength(0);
        C4.Width = new GridLength(0);
        R0.Height = new GridLength(Math.Clamp(ActualHeight * 0.46, 240, 460));
        R1.Height = new GridLength(10);
        R2.Height = new GridLength(1, GridUnitType.Star);

        Grid.SetRow(PreviewPane, 0);
        Grid.SetColumn(PreviewPane, 0);
        Grid.SetColumnSpan(PreviewPane, 5);
        Grid.SetRow(CardsScroll, 2);
        Grid.SetColumn(CardsScroll, 0);
        Grid.SetColumnSpan(CardsScroll, 5);
    }

    private static void MoveTo(FrameworkElement element, Panel destination)
    {
        if (ReferenceEquals(element.Parent, destination))
            return;

        if (element.Parent is Panel parent)
            parent.Children.Remove(element);
        destination.Children.Add(element);
    }
}
