using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Views;

public partial class PurchaseInvoiceView : UserControl
{
    public PurchaseInvoiceView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        InvoiceFeatureColumnSync.Attach(
            this,
            custom1: ColSize,
            custom2: ColColor,
            ColUnit,
            ColBatch,
            ColExpiry,
            ColSerial,
            pricing: ColPricingType);
        AttachCarShowroomColumns();
    }

    private void AttachCarShowroomColumns()
    {
        void Sync()
        {
            var show = DataContext is PurchaseInvoiceViewModel vm && vm.ShowCarShowroomFields;
            Set(ColVehicleType, show);
            Set(ColChassisNumber, show);
            Set(ColCarModel, show);
            Set(ColVehicleColor, show);
            Set(ColPassengerCount, show);
            Set(ColPlateNumber, show);
            Set(ColPlateType, show);
            ApplyCarShowroomGridLayout(show);
        }

        void OnVmChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(PurchaseInvoiceViewModel.ShowCarShowroomFields) or null)
                Sync();
        }

        void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is INotifyPropertyChanged oldVm)
                oldVm.PropertyChanged -= OnVmChanged;
            if (e.NewValue is INotifyPropertyChanged newVm)
                newVm.PropertyChanged += OnVmChanged;
            Sync();
        }

        DataContextChanged += OnDataContextChanged;
        if (DataContext is INotifyPropertyChanged existing)
            existing.PropertyChanged += OnVmChanged;
        Sync();
    }

    private void ApplyCarShowroomGridLayout(bool showCarShowroom)
    {
        // مع أعمدة المعرض الكثيرة: تمرير أفقي داخل الجدول + تثبيت العمودين الأولين (# والمنتج)
        ScrollViewer.SetHorizontalScrollBarVisibility(
            ItemsGrid,
            showCarShowroom ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled);
        ItemsGrid.FrozenColumnCount = showCarShowroom ? 2 : 0;
    }

    private static void Set(DataGridColumn? column, bool visible)
    {
        if (column is null) return;
        column.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
    }
}
