using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Controls;

public partial class IraqiCurrencyChangeDialog : Window
{
    private readonly IraqiCurrencyChangeViewModel _vm;

    public IraqiCurrencyChangeDialog(IraqiCurrencyChangeViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = _vm;
        _vm.RequestClose = () => Dispatcher.Invoke(Close);
        _vm.PropertyChanged += OnViewModelPropertyChanged;
        Closed += (_, _) => _vm.PropertyChanged -= OnViewModelPropertyChanged;
    }

    public decimal PaidAmount => _vm.PaidAmount;
    public bool Applied => _vm.Applied;

    /// <summary>
    /// Shows the currency change calculator. Returns paid amount when Apply was used; otherwise null.
    /// </summary>
    public static decimal? Show(decimal invoiceTotal, bool allowApplyPaid = false, Window? owner = null)
    {
        var vm = new IraqiCurrencyChangeViewModel(invoiceTotal, allowApplyPaid);
        var dialog = new IraqiCurrencyChangeDialog(vm)
        {
            Owner = owner
                ?? Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                ?? Application.Current.MainWindow
        };

        dialog.ShowDialog();
        return dialog.Applied ? dialog.PaidAmount : null;
    }

    /// <summary>Opens as a calculator only (no apply). Returns true if dialog was shown.</summary>
    public static void ShowCalculator(decimal invoiceTotal, Window? owner = null)
    {
        Show(invoiceTotal, allowApplyPaid: false, owner);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IraqiCurrencyChangeViewModel.PulseChange) && _vm.PulseChange)
            PlayChangePulse();
    }

    private void PlayChangePulse()
    {
        if (Resources["ChangePulse"] is Storyboard sb)
            sb.Begin(this, true);
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Escape or Key.F7)
        {
            _vm.CloseCommand.Execute(null);
            e.Handled = true;
        }
    }
}
