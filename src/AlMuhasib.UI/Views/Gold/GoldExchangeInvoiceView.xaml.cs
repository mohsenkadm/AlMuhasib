using System.Windows;
using System.Windows.Controls;
using AlMuhasib.UI.ViewModels.Gold;

namespace AlMuhasib.UI.Views.Gold;

public partial class GoldExchangeInvoiceView
{
    public GoldExchangeInvoiceView() => InitializeComponent();

    private void OnInSectionFocus(object sender, RoutedEventArgs e)
    {
        if (DataContext is GoldExchangeInvoiceViewModel vm)
            vm.IsOutSectionActive = false;
    }

    private void OnOutSectionFocus(object sender, RoutedEventArgs e)
    {
        if (DataContext is GoldExchangeInvoiceViewModel vm)
            vm.IsOutSectionActive = true;
    }
}
