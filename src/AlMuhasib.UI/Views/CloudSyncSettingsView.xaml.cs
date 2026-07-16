using System.Windows;
using System.Windows.Controls;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Views;

public partial class CloudSyncSettingsView
{
    private bool _suppressPasswordSync;

    public CloudSyncSettingsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) => SyncPasswordBoxFromViewModel();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is CloudSyncSettingsViewModel oldVm)
            oldVm.SettingsLoaded -= SyncPasswordBoxFromViewModel;

        if (e.NewValue is CloudSyncSettingsViewModel newVm)
        {
            newVm.SettingsLoaded += SyncPasswordBoxFromViewModel;
            SyncPasswordBoxFromViewModel();
        }
    }

    private void SyncPasswordBoxFromViewModel()
    {
        if (DataContext is not CloudSyncSettingsViewModel vm)
            return;

        // Keep whatever the user is currently typing; only fill from saved settings.
        if (vm.PasswordEditedByUser)
            return;

        _suppressPasswordSync = true;
        try
        {
            if (PasswordBox.Password != vm.Password)
                PasswordBox.Password = vm.Password ?? string.Empty;
        }
        finally
        {
            _suppressPasswordSync = false;
        }
    }

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_suppressPasswordSync)
            return;

        if (DataContext is CloudSyncSettingsViewModel vm && sender is PasswordBox pb)
        {
            vm.PasswordEditedByUser = true;
            vm.Password = pb.Password;
        }
    }
}
