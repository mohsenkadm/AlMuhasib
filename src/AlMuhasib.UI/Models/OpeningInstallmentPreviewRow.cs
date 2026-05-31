using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.Models;

public partial class OpeningInstallmentPreviewRow : ObservableObject
{
    [ObservableProperty]
    private int _number;

    [ObservableProperty]
    private DateTime _dueDate;

    [ObservableProperty]
    private decimal _amount;

    [ObservableProperty]
    private bool _isPaid;

    public string StatusText => IsPaid ? "مسدد (رصيد سابق)" : DueDate.Date < DateTime.Today ? "متأخر" : "غير مسدد";
}
