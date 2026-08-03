using System.Windows;
using System.Windows.Controls;

namespace AlMuhasib.UI.Controls;

public partial class ListViewModeToggle
{
    private static int _groupSequence;

    public static readonly DependencyProperty OnDarkBackgroundProperty =
        DependencyProperty.Register(
            nameof(OnDarkBackground),
            typeof(bool),
            typeof(ListViewModeToggle),
            new PropertyMetadata(true));

    public bool OnDarkBackground
    {
        get => (bool)GetValue(OnDarkBackgroundProperty);
        set => SetValue(OnDarkBackgroundProperty, value);
    }

    public ListViewModeToggle()
    {
        InitializeComponent();
        // Unique group per instance — shared GroupName across multiple toggles
        // (e.g. InvestorsView tabs) causes RadioButton mutual-exclusion loops → StackOverflow.
        var group = $"ListViewMode_{Interlocked.Increment(ref _groupSequence)}";
        TableButton.GroupName = group;
        CardButton.GroupName = group;
    }
}
