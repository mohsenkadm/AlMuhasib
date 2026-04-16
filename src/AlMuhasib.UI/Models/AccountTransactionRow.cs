using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.Models;

/// <summary>
/// Unified transaction row for CashBox/Bank transaction history display.
/// </summary>
public partial class AccountTransactionRow : ObservableObject
{
    [ObservableProperty]
    private DateTime _date;

    [ObservableProperty]
    private string _type = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private decimal _credit; // In

    [ObservableProperty]
    private decimal _debit;  // Out

    [ObservableProperty]
    private string _reference = string.Empty;
}
