using System.Windows;
using System.Windows.Controls;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Views;

public partial class InvestorsView : UserControl
{
    public InvestorsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        CustomFieldColumnSync.Attach(
            this,
            InvestorsGrid,
            [ColCf1, ColCf2, ColCf3, ColCf4, ColCf5, ColCf6, ColCf7, ColCf8],
            vm => vm is InvestorsViewModel i ? i.GetCustomFieldColumnStates() : null,
            nameof(InvestorsViewModel.CustomFieldColumnsVersion));
    }
}
