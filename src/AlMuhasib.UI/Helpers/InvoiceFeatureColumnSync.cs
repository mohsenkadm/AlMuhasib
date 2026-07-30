using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Helpers;

/// <summary>
/// يزامن ظهور أعمدة ميزات الفاتورة يدوياً — ربط Visibility على DataGridColumn غير موثوق في WPF.
/// </summary>
public static class InvoiceFeatureColumnSync
{
    public static void Attach(
        FrameworkElement host,
        DataGridColumn? custom1,
        DataGridColumn? custom2,
        DataGridColumn? unit,
        DataGridColumn? batch,
        DataGridColumn? expiry,
        DataGridColumn? serial,
        DataGridColumn? pricing = null)
    {
        void SyncFromContext()
        {
            switch (host.DataContext)
            {
                case SalesInvoiceViewModel sales:
                    Set(custom1, sales.ShowCustomField1);
                    Set(custom2, sales.ShowCustomField2);
                    Set(unit, sales.ShowUnitsOfMeasure);
                    Set(batch, sales.ShowExpiryTracking);
                    Set(expiry, null);
                    Set(serial, sales.ShowSerialNumbers);
                    Set(pricing, sales.ShowProductPricing);
                    break;
                case PurchaseInvoiceViewModel purchase:
                    Set(custom1, purchase.ShowClothingSizes);
                    Set(custom2, false);
                    Set(unit, purchase.ShowUnitsOfMeasure);
                    Set(batch, purchase.ShowExpiryTracking);
                    Set(expiry, purchase.ShowExpiryTracking);
                    Set(serial, purchase.ShowSerialNumbers);
                    Set(pricing, false);
                    break;
                default:
                    Set(custom1, false);
                    Set(custom2, false);
                    Set(unit, false);
                    Set(batch, false);
                    Set(expiry, false);
                    Set(serial, false);
                    Set(pricing, false);
                    break;
            }
        }

        INotifyPropertyChanged? wired = null;

        void Wire(object? dc)
        {
            if (wired is not null)
                wired.PropertyChanged -= OnVmChanged;

            wired = dc as INotifyPropertyChanged;
            if (wired is not null)
                wired.PropertyChanged += OnVmChanged;

            SyncFromContext();
        }

        void OnVmChanged(object? sender, PropertyChangedEventArgs e)
        {
            // أسماء الخصائص مشتركة بين فواتير البيع/الشراء
            switch (e.PropertyName)
            {
                case "ShowCustomField1":
                case "ShowCustomField2":
                case "ShowUnitsOfMeasure":
                case "ShowExpiryTracking":
                case "ShowSerialNumbers":
                case "ShowProductPricing":
                case "ShowClothingSizes":
                case "CustomField1Header":
                case "CustomField2Header":
                case "ClothingSizeHeader":
                case "MarketTemplateFieldsEnabled":
                case null:
                case "":
                    SyncFromContext();
                    break;
            }
        }

        host.DataContextChanged += (_, args) => Wire(args.NewValue);
        host.Loaded += (_, _) => Wire(host.DataContext);
        Wire(host.DataContext);
    }

    private static void Set(DataGridColumn? column, bool? visible)
    {
        if (column is null) return;
        column.Visibility = visible == true ? Visibility.Visible : Visibility.Collapsed;
    }
}
