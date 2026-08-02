using System.Collections.ObjectModel;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldCreditReportViewModel : ViewModelBase
{
    private readonly IGoldReportService _reportService;
    private readonly IToastNotificationService _toast;
    private readonly ICurrentUserService _currentUserService;

    public ObservableCollection<GoldCustomerListItem> Rows { get; } = [];

    [ObservableProperty] private bool _overdueOnly;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private int _customerCount;
    [ObservableProperty] private decimal _totalCreditIqd;
    [ObservableProperty] private decimal _totalCreditUsd;
    [ObservableProperty] private int _openInvoiceCount;

    public GoldCreditReportViewModel(
        IGoldReportService reportService,
        IToastNotificationService toast,
        ICurrentUserService currentUserService)
    {
        _reportService = reportService;
        _toast = toast;
        _currentUserService = currentUserService;
        PageTitle = "تقرير الآجل";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.CreditReport);
        await LoadAsync();
    }

    partial void OnOverdueOnlyChanged(bool value) => _ = LoadAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var rows = await _reportService.GetCreditReportAsync(OverdueOnly);
            Rows.Clear();
            foreach (var r in rows)
                Rows.Add(r);

            CustomerCount = rows.Count;
            TotalCreditIqd = rows.Sum(r => r.CreditBalanceIqd);
            TotalCreditUsd = rows.Sum(r => r.CreditBalanceUsd);
            OpenInvoiceCount = rows.Sum(r => r.OpenInvoiceCount);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _toast.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
