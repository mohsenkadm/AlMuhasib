using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AlMuhasib.UI.Controls;

public partial class HotelEntityLink : UserControl
{
    public HotelEntityLink()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty LinkTextProperty =
        DependencyProperty.Register(nameof(LinkText), typeof(string), typeof(HotelEntityLink), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(HotelEntityLink));

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(HotelEntityLink));

    public static readonly DependencyProperty ToolTipTextProperty =
        DependencyProperty.Register(nameof(ToolTipText), typeof(string), typeof(HotelEntityLink), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty MaxLinkWidthProperty =
        DependencyProperty.Register(nameof(MaxLinkWidth), typeof(double), typeof(HotelEntityLink), new PropertyMetadata(220.0));

    public string LinkText
    {
        get => (string)GetValue(LinkTextProperty);
        set => SetValue(LinkTextProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public string ToolTipText
    {
        get => (string)GetValue(ToolTipTextProperty);
        set => SetValue(ToolTipTextProperty, value);
    }

    public double MaxLinkWidth
    {
        get => (double)GetValue(MaxLinkWidthProperty);
        set => SetValue(MaxLinkWidthProperty, value);
    }
}
