using System.Windows.Controls;
using System.Windows.Input;

namespace AlMuhasib.UI.Views;

public partial class PosQuickSaleView : UserControl
{
    public PosQuickSaleView() => InitializeComponent();

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (DataContext is ViewModels.PosQuickSaleViewModel vm)
            vm.AddProductFromSearchCommand.Execute(null);
        e.Handled = true;
    }
}
