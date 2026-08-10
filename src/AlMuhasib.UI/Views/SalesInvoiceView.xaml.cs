using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AlMuhasib.UI.Helpers;

namespace AlMuhasib.UI.Views;

public partial class SalesInvoiceView : UserControl
{
    public SalesInvoiceView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        PreviewKeyDown += Root_PreviewKeyDown;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        InvoiceFeatureColumnSync.Attach(
            this,
            ColCustomField1,
            ColCustomField2,
            ColUnit,
            ColBatch,
            expiry: null,
            ColSerial,
            ColPricingType,
            ColUsageInstructions);
    }

    private void Root_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not ViewModels.SalesInvoiceViewModel vm)
            return;

        // Ctrl+S — حفظ
        if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (vm.SaveInvoiceCommand.CanExecute(null))
                vm.SaveInvoiceCommand.Execute(null);
            e.Handled = true;
            return;
        }

        // F4 — إضافة صف / اختيار منتجات
        if (e.Key == Key.F4 && Keyboard.Modifiers == ModifierKeys.None)
        {
            if (vm.OpenProductPickerCommand.CanExecute(null))
                _ = vm.OpenProductPickerCommand.ExecuteAsync(null);
            e.Handled = true;
            return;
        }

        // F5 — فحص الربح
        if (e.Key == Key.F5 && Keyboard.Modifiers == ModifierKeys.None)
        {
            if (vm.CheckProfitCommand.CanExecute(null))
                _ = vm.CheckProfitCommand.ExecuteAsync(null);
            e.Handled = true;
            return;
        }

        // F7 — حاسبة العملة
        if (e.Key == Key.F7 && Keyboard.Modifiers == ModifierKeys.None)
        {
            vm.OpenCurrencyChangeCommand.Execute(null);
            e.Handled = true;
        }
    }
}
