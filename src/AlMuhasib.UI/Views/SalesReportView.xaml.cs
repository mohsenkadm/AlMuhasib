using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Views;

public partial class SalesReportView : UserControl
{
    private INotifyPropertyChanged? _wiredVm;

    public SalesReportView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) => SyncWarrantyColumns();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_wiredVm is not null)
            _wiredVm.PropertyChanged -= OnVmPropertyChanged;

        _wiredVm = e.NewValue as INotifyPropertyChanged;
        if (_wiredVm is not null)
            _wiredVm.PropertyChanged += OnVmPropertyChanged;

        SyncWarrantyColumns();
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null or "" or nameof(SalesReportViewModel.ShowMobileShopColumns))
            SyncWarrantyColumns();
    }

    private void SyncWarrantyColumns()
    {
        var show = DataContext is SalesReportViewModel vm && vm.ShowMobileShopColumns;
        var visibility = show ? Visibility.Visible : Visibility.Collapsed;
        if (ColWarrantyEnd is not null) ColWarrantyEnd.Visibility = visibility;
        if (ColWarrantyStatus is not null) ColWarrantyStatus.Visibility = visibility;
        if (ColWarrantyRemaining is not null) ColWarrantyRemaining.Visibility = visibility;
    }
}
