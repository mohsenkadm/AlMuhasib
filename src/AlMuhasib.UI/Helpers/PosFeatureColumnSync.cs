using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Helpers;

/// <summary>
/// يزامن ظهور أعمدة ميزات سلة POS — ربط Visibility على DataGridColumn غير موثوق في WPF.
/// </summary>
public static class PosFeatureColumnSync
{
    public static void Attach(
        FrameworkElement host,
        DataGridColumn? size,
        DataGridColumn? color,
        DataGridColumn? custom1,
        DataGridColumn? custom2,
        DataGridColumn? unit,
        DataGridColumn? batch,
        DataGridColumn? serial,
        DataGridColumn? pricing,
        DataGridColumn? discount)
    {
        void SyncFromContext()
        {
            if (host.DataContext is not PosQuickSaleViewModel pos)
                return;

            Set(size, pos.ShowClothingSizes);
            Set(color, pos.ShowClothingSizes);
            Set(custom1, pos.ShowCustomField1);
            Set(custom2, pos.ShowCustomField2);
            Set(unit, pos.ShowUnitsOfMeasure);
            Set(batch, pos.ShowExpiryTracking);
            Set(serial, pos.ShowSerialNumbers);
            Set(pricing, pos.ShowProductPricing);
            Set(discount, pos.ShowProductDiscount);
        }

        void OnVmChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(PosQuickSaleViewModel.ShowClothingSizes):
                case nameof(PosQuickSaleViewModel.ShowCustomField1):
                case nameof(PosQuickSaleViewModel.ShowCustomField2):
                case nameof(PosQuickSaleViewModel.ShowUnitsOfMeasure):
                case nameof(PosQuickSaleViewModel.ShowExpiryTracking):
                case nameof(PosQuickSaleViewModel.ShowSerialNumbers):
                case nameof(PosQuickSaleViewModel.ShowProductPricing):
                case nameof(PosQuickSaleViewModel.ShowProductDiscount):
                case nameof(PosQuickSaleViewModel.CustomField1Header):
                case nameof(PosQuickSaleViewModel.CustomField2Header):
                case null:
                case "":
                    SyncFromContext();
                    break;
            }
        }

        void Wire(object? dc)
        {
            if (dc is INotifyPropertyChanged npc)
                npc.PropertyChanged += OnVmChanged;
            SyncFromContext();
        }

        host.DataContextChanged += (_, args) =>
        {
            if (args.OldValue is INotifyPropertyChanged oldNpc)
                oldNpc.PropertyChanged -= OnVmChanged;
            Wire(args.NewValue);
        };
        host.Loaded += (_, _) => Wire(host.DataContext);
        Wire(host.DataContext);
    }

    private static void Set(DataGridColumn? column, bool visible)
    {
        if (column is null) return;
        column.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
    }
}
