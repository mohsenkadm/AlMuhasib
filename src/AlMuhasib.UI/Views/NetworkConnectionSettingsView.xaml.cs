namespace AlMuhasib.UI.Views;

public partial class NetworkConnectionSettingsView
{
    public NetworkConnectionSettingsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.NetworkConnectionSettingsViewModel vm && !string.IsNullOrEmpty(vm.SqlPassword))
            SqlPasswordBox.Password = vm.SqlPassword;
    }

    private void SqlPasswordBox_OnPasswordChanged(object sender, System.Windows.Controls.PasswordChangedEventArgs e)
    {
        if (DataContext is ViewModels.NetworkConnectionSettingsViewModel vm && sender is System.Windows.Controls.PasswordBox pb)
            vm.SqlPassword = pb.Password;
    }
}
