using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.UI.Windows;

public partial class SystemSelectionWindow : Window
{
    public ApplicationSystemType? SelectedSystem { get; private set; }

    public SystemSelectionWindow()
    {
        InitializeComponent();
    }

    private void OnAccountingSelected(object sender, MouseButtonEventArgs e) =>
        SelectSystem(ApplicationSystemType.Accounting, AccountingCard, "#1565C0", "#E3F2FD");

    private void OnCarSelected(object sender, MouseButtonEventArgs e) =>
        SelectSystem(ApplicationSystemType.CarContracts, CarCard, "#2E7D32", "#E8F5E9");

    private void OnCarTradeSelected(object sender, MouseButtonEventArgs e) =>
        SelectSystem(ApplicationSystemType.CarTrading, CarTradeCard, "#E65100", "#FFF3E0");

    private void OnHotelSelected(object sender, MouseButtonEventArgs e) =>
        SelectSystem(ApplicationSystemType.HotelManagement, HotelCard, "#6A1B9A", "#F3E5F5");

    private void OnRealEstateSelected(object sender, MouseButtonEventArgs e) =>
        SelectSystem(ApplicationSystemType.RealEstateContracts, RealEstateCard, "#00695C", "#E0F2F1");

    private void SelectSystem(ApplicationSystemType system, Border card, string accent, string bg)
    {
        SelectedSystem = system;
        ContinueButton.IsEnabled = true;

        ResetCard(AccountingCard);
        ResetCard(CarCard);
        ResetCard(CarTradeCard);
        ResetCard(HotelCard);
        ResetCard(RealEstateCard);

        card.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(accent)!);
        card.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg)!);
    }

    private static void ResetCard(Border card)
    {
        card.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")!);
        card.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FAFAFA")!);
    }

    private void OnContinueClick(object sender, RoutedEventArgs e)
    {
        if (SelectedSystem is null)
            return;

        DialogResult = true;
        Close();
    }
}
