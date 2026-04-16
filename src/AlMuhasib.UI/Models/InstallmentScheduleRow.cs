using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.Models;

/// <summary>
/// Observable row for installment schedule preview in the invoice page.
/// </summary>
public partial class InstallmentScheduleRow : ObservableObject
{
    [ObservableProperty]
    private int _number;

    [ObservableProperty]
    private DateTime _dueDate;

    [ObservableProperty]
    private decimal _amount;
}
