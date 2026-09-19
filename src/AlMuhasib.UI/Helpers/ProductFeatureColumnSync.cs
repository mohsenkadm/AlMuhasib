using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Helpers;

/// <summary>
/// يزامن ظهور أعمدة الصيدلية ومعرض السيارات — ربط Visibility على DataGridColumn غير موثوق في WPF.
/// </summary>
public static class ProductFeatureColumnSync
{
    public static void Attach(
        FrameworkElement host,
        DataGridColumn? scientificName,
        DataGridColumn? usageInstructions = null,
        DataGridColumn? vehicleType = null,
        DataGridColumn? chassisNumber = null,
        DataGridColumn? vehicleColor = null,
        DataGridColumn? passengerCount = null,
        DataGridColumn? plateNumber = null,
        DataGridColumn? plateType = null)
    {
        void SyncFromContext()
        {
            if (host.DataContext is ProductsViewModel products)
            {
                Set(scientificName, products.ShowScientificName);
                Set(usageInstructions, products.ShowUsageInstructions);
                var showCar = products.ShowCarShowroomFields;
                Set(vehicleType, showCar);
                Set(chassisNumber, showCar);
                Set(vehicleColor, showCar);
                Set(passengerCount, showCar);
                Set(plateNumber, showCar);
                Set(plateType, showCar);
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
                or nameof(ProductsViewModel.ShowCarShowroomFields)
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
