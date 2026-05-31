using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AlMuhasib.UI.Controls;

public partial class GlobalSearchOverlay : UserControl
{
    public GlobalSearchOverlay()
    {
        InitializeComponent();
        Loaded += (_, _) => SearchBox.Focus();
    }

    public void FocusSearch()
    {
        SearchBox.Focus();
        SearchBox.SelectAll();
    }

    private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (DataContext is ViewModels.MainWindowViewModel vm)
                vm.CloseGlobalSearchCommand.Execute(null);
            e.Handled = true;
        }
    }
}
