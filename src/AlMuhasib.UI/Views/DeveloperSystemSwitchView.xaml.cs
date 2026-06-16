using System.Windows;
using System.Windows.Controls;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Views;

public partial class DeveloperSystemSwitchView
{
    public DeveloperSystemSwitchView() => InitializeComponent();

    private void DevPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is DeveloperSystemSwitchViewModel vm && sender is PasswordBox pb)
            vm.Password = pb.Password;
    }

    private void NewDevPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is DeveloperSystemSwitchViewModel vm && sender is PasswordBox pb)
            vm.NewPassword = pb.Password;
    }
}
