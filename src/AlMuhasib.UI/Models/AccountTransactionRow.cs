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
    private string _partyName = string.Empty;

    [ObservableProperty]
    private decimal _credit;

    [ObservableProperty]
    private decimal _debit;

    [ObservableProperty]
    private decimal _runningBalance;

    [ObservableProperty]
    private string _reference = string.Empty;

    [ObservableProperty]
    private int? _voucherId;

    [ObservableProperty]
    private bool _isReconciled;

    [ObservableProperty]
    private bool _isVoucher;

    [ObservableProperty]
    private string _sourceType = string.Empty;

    [ObservableProperty]
    private int? _sourceId;

    [ObservableProperty]
    private bool _canReverse;
}
