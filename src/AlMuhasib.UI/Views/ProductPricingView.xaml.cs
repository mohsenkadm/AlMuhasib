using System.Windows;
using System.Windows.Controls;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Views;

public partial class ProductPricingView : UserControl
{
    public ProductPricingView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) => SyncCurrencyColumns();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ProductPricingViewModel oldVm)
            oldVm.PropertyChanged -= OnVmPropertyChanged;
        if (e.NewValue is ProductPricingViewModel newVm)
            newVm.PropertyChanged += OnVmPropertyChanged;
        SyncCurrencyColumns();
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null or nameof(ProductPricingViewModel.ShowMultiCurrency))
            SyncCurrencyColumns();
    }

    private void SyncCurrencyColumns()
    {
        var show = DataContext is ProductPricingViewModel vm && vm.ShowMultiCurrency;
        var visibility = show ? Visibility.Visible : Visibility.Collapsed;
        if (ColSalePriceUsd is not null)
            ColSalePriceUsd.Visibility = visibility;
        if (ColPurchasePriceUsd is not null)
            ColPurchasePriceUsd.Visibility = visibility;
    }
}
