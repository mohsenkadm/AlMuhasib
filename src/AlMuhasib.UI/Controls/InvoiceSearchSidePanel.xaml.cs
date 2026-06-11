using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Controls;

public partial class InvoiceSearchSidePanel : UserControl
{
    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(
            nameof(IsOpen),
            typeof(bool),
            typeof(InvoiceSearchSidePanel),
            new PropertyMetadata(false, OnIsOpenChanged));

    public static readonly DependencyProperty PanelTitleProperty =
        DependencyProperty.Register(
            nameof(PanelTitle),
            typeof(string),
            typeof(InvoiceSearchSidePanel),
            new PropertyMetadata("بحث الفواتير"));

    public static readonly DependencyProperty HeaderColorStartProperty =
        DependencyProperty.Register(
            nameof(HeaderColorStart),
            typeof(Color),
            typeof(InvoiceSearchSidePanel),
            new PropertyMetadata(Color.FromRgb(0x15, 0x65, 0xC0)));

    public static readonly DependencyProperty HeaderColorEndProperty =
        DependencyProperty.Register(
            nameof(HeaderColorEnd),
            typeof(Color),
            typeof(InvoiceSearchSidePanel),
            new PropertyMetadata(Color.FromRgb(0x42, 0xA5, 0xF5)));

    public static readonly DependencyProperty CloseCommandProperty =
        DependencyProperty.Register(
            nameof(CloseCommand),
            typeof(ICommand),
            typeof(InvoiceSearchSidePanel));

    public static readonly DependencyProperty SelectInvoiceCommandProperty =
        DependencyProperty.Register(
            nameof(SelectInvoiceCommand),
            typeof(ICommand),
            typeof(InvoiceSearchSidePanel));

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public string PanelTitle
    {
        get => (string)GetValue(PanelTitleProperty);
        set => SetValue(PanelTitleProperty, value);
    }

    public Color HeaderColorStart
    {
        get => (Color)GetValue(HeaderColorStartProperty);
        set => SetValue(HeaderColorStartProperty, value);
    }

    public Color HeaderColorEnd
    {
        get => (Color)GetValue(HeaderColorEndProperty);
        set => SetValue(HeaderColorEndProperty, value);
    }

    public ICommand? CloseCommand
    {
        get => (ICommand?)GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }

    public ICommand? SelectInvoiceCommand
    {
        get => (ICommand?)GetValue(SelectInvoiceCommandProperty);
        set => SetValue(SelectInvoiceCommandProperty, value);
    }

    public InvoiceSearchSidePanel()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyOpenState(IsOpen, instant: true);
    }

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not InvoiceSearchSidePanel panel)
            return;

        if (!panel.IsLoaded)
            return;

        panel.ApplyOpenState(e.NewValue is true);
    }

    private void ApplyOpenState(bool open, bool instant = false)
    {
        if (open)
            PlayOpenAnimation(instant);
        else
            PlayCloseAnimation(instant);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        if (CloseCommand?.CanExecute(null) == true)
            CloseCommand.Execute(null);
        else
            IsOpen = false;
    }

    private void Backdrop_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (CloseCommand?.CanExecute(null) == true)
            CloseCommand.Execute(null);
        else
            IsOpen = false;
    }

    private void InvoiceItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: InvoiceSearchListItem item })
            return;

        if (SelectInvoiceCommand?.CanExecute(item) == true)
            SelectInvoiceCommand.Execute(item);
    }

    private void PlayOpenAnimation(bool instant = false)
    {
        IsHitTestVisible = true;
        OverlayRoot.Visibility = Visibility.Visible;
        OverlayRoot.IsHitTestVisible = true;

        if (instant)
        {
            Backdrop.Opacity = 1;
            PanelSlide.X = 0;
            return;
        }

        Backdrop.Opacity = 0;
        Backdrop.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(240))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });

        PanelSlide.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(420, 0, TimeSpan.FromMilliseconds(360))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });
    }

    private void PlayCloseAnimation(bool instant = false)
    {
        if (OverlayRoot.Visibility != Visibility.Visible)
        {
            IsHitTestVisible = false;
            return;
        }

        if (instant)
        {
            OverlayRoot.Visibility = Visibility.Collapsed;
            OverlayRoot.IsHitTestVisible = false;
            IsHitTestVisible = false;
            Backdrop.Opacity = 0;
            PanelSlide.X = 420;
            return;
        }

        var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        fade.Completed += (_, _) =>
        {
            OverlayRoot.Visibility = Visibility.Collapsed;
            OverlayRoot.IsHitTestVisible = false;
            IsHitTestVisible = false;
        };
        Backdrop.BeginAnimation(OpacityProperty, fade);

        PanelSlide.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(0, 420, TimeSpan.FromMilliseconds(280))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        });
    }
}
