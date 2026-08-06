using System.Windows;
using System.Windows.Controls;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Views;

public partial class CustomersView : UserControl
{
    public CustomersView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        CustomFieldColumnSync.Attach(
            this,
            CustomersGrid,
            [ColCf1, ColCf2, ColCf3, ColCf4, ColCf5, ColCf6, ColCf7, ColCf8],
            vm => vm is CustomersViewModel c ? c.GetCustomFieldColumnStates() : null,
            nameof(CustomersViewModel.CustomFieldColumnsVersion));
    }
}
