using System.Windows.Controls;
using System.Windows.Input;
using AlMuhasib.UI.ViewModels.Hotel;

namespace AlMuhasib.UI.Views.Hotel;

public partial class RestaurantPosView : UserControl
{
    public RestaurantPosView() => InitializeComponent();

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (DataContext is RestaurantPosViewModel vm)
            vm.AddItemFromSearchCommand.Execute(null);
        e.Handled = true;
    }
}
