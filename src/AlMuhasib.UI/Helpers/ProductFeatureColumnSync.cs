using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Helpers;

/// <summary>
/// يزامن ظهور أعمدة الصيدلية — ربط Visibility على DataGridColumn غير موثوق في WPF.
/// </summary>
public static class ProductFeatureColumnSync
{
    public static void Attach(FrameworkElement host, DataGridColumn? scientificName, DataGridColumn? usageInstructions = null)
    {
        void SyncFromContext()
        {
            if (host.DataContext is ProductsViewModel products)
            {
                Set(scientificName, products.ShowScientificName);
                Set(usageInstructions, products.ShowUsageInstructions);
            }
        }

        void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is INotifyPropertyChanged oldVm)
                oldVm.PropertyChanged -= OnVmPropertyChanged;
            if (e.NewValue is INotifyPropertyChanged newVm)
                newVm.PropertyChanged += OnVmPropertyChanged;
            SyncFromContext();
        }

        void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(ProductsViewModel.ShowScientificName)
                or nameof(ProductsViewModel.ShowUsageInstructions)
                or null)
                SyncFromContext();
        }

        host.DataContextChanged += OnDataContextChanged;
        if (host.DataContext is INotifyPropertyChanged existing)
            existing.PropertyChanged += OnVmPropertyChanged;
        SyncFromContext();
    }

    private static void Set(DataGridColumn? column, bool visible)
    {
        if (column is null) return;
        column.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
    }
}
