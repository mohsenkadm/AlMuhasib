using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Controls;

public partial class CustomFieldsEditor : UserControl
{
    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(
            nameof(Items),
            typeof(ObservableCollection<CustomFieldEditItem>),
            typeof(CustomFieldsEditor),
            new PropertyMetadata(null));

    public ObservableCollection<CustomFieldEditItem>? Items
    {
        get => (ObservableCollection<CustomFieldEditItem>?)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public CustomFieldsEditor()
    {
        InitializeComponent();
    }
}
