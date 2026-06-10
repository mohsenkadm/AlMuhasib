using System.Windows;
using System.Windows.Controls;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Views;

public partial class CloudSyncSettingsView
{
    public CloudSyncSettingsView() => InitializeComponent();

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is CloudSyncSettingsViewModel vm && sender is PasswordBox pb)
            vm.Password = pb.Password;
    }
}
