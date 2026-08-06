using System.Windows;
using System.Windows.Controls;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Views;

public partial class BulkProductsEntryView
{
    public BulkProductsEntryView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) => SyncColumns();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is BulkProductsEntryViewModel oldVm)
            oldVm.PropertyChanged -= OnVmPropertyChanged;
        if (e.NewValue is BulkProductsEntryViewModel newVm)
            newVm.PropertyChanged += OnVmPropertyChanged;
        SyncColumns();
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null
            || e.PropertyName.StartsWith("Show", StringComparison.Ordinal)
            || e.PropertyName.Contains("CustomField", StringComparison.Ordinal))
        {
            SyncColumns();
        }
    }

    private void SyncColumns()
    {
        if (DataContext is not BulkProductsEntryViewModel vm)
            return;

        SetColumn("ColPharmacyScientific", vm.ShowPharmacyFields);
        SetColumn("ColPharmacyUsage", vm.ShowPharmacyFields);
        SetColumn("ColWeight", vm.ShowWeightFields);
        SetColumn("ColWeightUnit", vm.ShowWeightFields);
        SetColumn("ColDiscountType", vm.ShowDiscountFields);
        SetColumn("ColDiscountValue", vm.ShowDiscountFields);
        SetColumn("ColDiscountExpires", vm.ShowDiscountFields);
        SetColumn("ColSalePrice", vm.ShowPricingFields);
        SetColumn("ColPurchasePrice", vm.ShowPricingFields);

        SetNamedColumn("ColCf1", vm.ShowCustomField1, vm.CustomField1Header);
        SetNamedColumn("ColCf2", vm.ShowCustomField2, vm.CustomField2Header);
        SetNamedColumn("ColCf3", vm.ShowCustomField3, vm.CustomField3Header);
        SetNamedColumn("ColCf4", vm.ShowCustomField4, vm.CustomField4Header);
        SetNamedColumn("ColCf5", vm.ShowCustomField5, vm.CustomField5Header);
        SetNamedColumn("ColCf6", vm.ShowCustomField6, vm.CustomField6Header);
        SetNamedColumn("ColCf7", vm.ShowCustomField7, vm.CustomField7Header);
        SetNamedColumn("ColCf8", vm.ShowCustomField8, vm.CustomField8Header);
    }

    private void SetColumn(string name, bool visible)
    {
        if (FindName(name) is DataGridColumn col)
            col.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SetNamedColumn(string name, bool visible, string header)
    {
        if (FindName(name) is DataGridColumn col)
        {
            col.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            col.Header = header;
        }
    }
}
