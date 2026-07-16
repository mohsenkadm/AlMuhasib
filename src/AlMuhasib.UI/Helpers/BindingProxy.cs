using System.Windows;

namespace AlMuhasib.UI.Helpers;

/// <summary>
/// يتيح ربط خصائص DataContext بأعمدة DataGrid (ليست جزءاً من الشجرة البصرية).
/// </summary>
public sealed class BindingProxy : Freezable
{
    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register(nameof(Data), typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));

    public object? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    protected override Freezable CreateInstanceCore() => new BindingProxy();
}
