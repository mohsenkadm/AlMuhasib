using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyPropertyChanged oldVm)
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;

        if (e.NewValue is INotifyPropertyChanged newVm)
            newVm.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DashboardViewModel.IsLoaded) &&
            sender is DashboardViewModel vm && vm.IsLoaded)
        {
            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    PlayStoryboard("FadeInRow0");
                    PlayStoryboard("FadeInRow1");
                    PlayStoryboard("FadeInRow2");
                    PlayStoryboard("FadeInRow3");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Dashboard animation error: {ex.Message}");
                }
            });
        }
    }

    private void PlayStoryboard(string key)
    {
        if (Resources.Contains(key) && Resources[key] is Storyboard sb)
            sb.Begin(this, true);
    }
}
