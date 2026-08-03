using System.Windows;
using System.Windows.Input;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Controls;

public partial class InvoiceProfitCheckDialog : Window
{
    public InvoiceProfitCheckViewModel ViewModel => (InvoiceProfitCheckViewModel)DataContext;

    public InvoiceProfitCheckDialog(InvoiceProfitCheckViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public static bool? Show(
        Window? owner,
        InvoiceProfitCheckViewModel viewModel)
    {
        var dialog = new InvoiceProfitCheckDialog(viewModel)
        {
            Owner = owner ?? Application.Current.MainWindow
        };
        return dialog.ShowDialog();
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void Scrim_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ApplyDiscountCommand.Execute(null);
        DialogResult = true;
        Close();
    }
}
