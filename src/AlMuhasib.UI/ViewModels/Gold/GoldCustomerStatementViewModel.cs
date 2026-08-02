using System.Collections.ObjectModel;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldCustomerStatementViewModel : ViewModelBase
{
    private readonly IGoldCustomerService _customerService;
    private readonly IToastNotificationService _toast;
    private readonly ICurrentUserService _currentUserService;

    public ObservableCollection<GoldCustomerListItem> Customers { get; } = [];
    public ObservableCollection<GoldInvoiceListItem> Invoices { get; } = [];

    [ObservableProperty] private string _customerSearch = string.Empty;
    [ObservableProperty] private GoldCustomerListItem? _selectedCustomer;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private decimal _totalAmount;
    [ObservableProperty] private decimal _totalPaid;
    [ObservableProperty] private decimal _totalRemaining;
    [ObservableProperty] private decimal _creditBalanceIqd;
    [ObservableProperty] private decimal _creditBalanceUsd;
    [ObservableProperty] private decimal _goldCreditGrams;

    public GoldCustomerStatementViewModel(
        IGoldCustomerService customerService,
        IToastNotificationService toast,
        ICurrentUserService currentUserService)
    {
        _customerService = customerService;
        _toast = toast;
        _currentUserService = currentUserService;
        PageTitle = "كشف حساب زبون";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.CustomerStatement);
        await LoadCustomersAsync();
    }

    [RelayCommand]
    private async Task LoadCustomersAsync()
    {
        IsBusy = true;
        try
        {
            Customers.Clear();
            var (items, _) = await _customerService.GetPagedAsync(1, 500, CustomerSearch, activeOnly: null);
            foreach (var c in items)
                Customers.Add(c);
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

    partial void OnSelectedCustomerChanged(GoldCustomerListItem? value) => _ = LoadStatementAsync();
    partial void OnCustomerSearchChanged(string value) => _ = LoadCustomersAsync();

    [RelayCommand]
    private async Task LoadStatementAsync()
    {
        Invoices.Clear();
        TotalAmount = TotalPaid = TotalRemaining = 0;
        CreditBalanceIqd = CreditBalanceUsd = GoldCreditGrams = 0;

        if (SelectedCustomer is null)
            return;

        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var invoices = await _customerService.GetCustomerInvoicesAsync(SelectedCustomer.Id);
            foreach (var inv in invoices)
                Invoices.Add(inv);

            TotalAmount = invoices.Sum(i => i.TotalAmount);
            TotalPaid = invoices.Sum(i => i.PaidAmount);
            TotalRemaining = invoices.Sum(i => i.RemainingAmount);
            CreditBalanceIqd = SelectedCustomer.CreditBalanceIqd;
            CreditBalanceUsd = SelectedCustomer.CreditBalanceUsd;
            GoldCreditGrams = SelectedCustomer.GoldCreditGrams;
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
