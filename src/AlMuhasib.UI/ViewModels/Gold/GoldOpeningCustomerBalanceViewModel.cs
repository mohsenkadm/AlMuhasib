using System.Collections.ObjectModel;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldOpeningCustomerBalanceViewModel : ViewModelBase
{
    private readonly IGoldOpeningBalanceService _openingService;
    private readonly IGoldCustomerService _customerService;
    private readonly ICurrentUserService _currentUserService;

    public ObservableCollection<GoldCustomerListItem> Customers { get; } = [];

    [ObservableProperty] private GoldCustomerListItem? _selectedCustomer;
    [ObservableProperty] private decimal _creditBalanceIqd;
    [ObservableProperty] private decimal _creditBalanceUsd;
    [ObservableProperty] private decimal _goldCreditGrams;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private string _formError = string.Empty;
    [ObservableProperty] private string _searchText = string.Empty;

    public GoldOpeningCustomerBalanceViewModel(
        IGoldOpeningBalanceService openingService,
        IGoldCustomerService customerService,
        ICurrentUserService currentUserService)
    {
        _openingService = openingService;
        _customerService = customerService;
        _currentUserService = currentUserService;
        PageTitle = "رصيد افتتاحي للزبون";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.OpeningCustomerBalance);
        await LoadCustomersAsync();
    }

    [RelayCommand]
    private async Task LoadCustomersAsync()
    {
        IsBusy = true;
        try
        {
            Customers.Clear();
            var (items, _) = await _customerService.GetPagedAsync(1, 500, SearchText, activeOnly: true);
            foreach (var c in items)
                Customers.Add(c);
        }
        catch (Exception ex)
        {
            FormError = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSearchTextChanged(string value) => _ = LoadCustomersAsync();

    partial void OnSelectedCustomerChanged(GoldCustomerListItem? value)
    {
        if (value is null)
        {
            CreditBalanceIqd = CreditBalanceUsd = GoldCreditGrams = 0;
            return;
        }

        CreditBalanceIqd = value.CreditBalanceIqd;
        CreditBalanceUsd = value.CreditBalanceUsd;
        GoldCreditGrams = value.GoldCreditGrams;
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (!CanAdd && !CanEdit)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية تعيين رصيد الافتتاح");
            return;
        }

        if (SelectedCustomer is null)
        {
            FormError = "اختر الزبون";
            return;
        }

        if (CreditBalanceIqd < 0 || CreditBalanceUsd < 0 || GoldCreditGrams < 0)
        {
            FormError = "الأرصدة لا يمكن أن تكون سالبة";
            return;
        }

        if (!BeautifulMessageDialog.ShowConfirm(
                $"تعيين أرصدة افتتاحية للزبون «{SelectedCustomer.Name}»؟\n" +
                $"د.ع: {CreditBalanceIqd:N0} | $: {CreditBalanceUsd:N2} | ذهب: {GoldCreditGrams:N3} غ",
                "رصيد الافتتاح"))
            return;

        try
        {
            IsBusy = true;
            FormError = string.Empty;
            await _openingService.SetCustomerOpeningBalanceAsync(new GoldOpeningCustomerBalanceRequest
            {
                CustomerId = SelectedCustomer.Id,
                CreditBalanceIqd = CreditBalanceIqd,
                CreditBalanceUsd = CreditBalanceUsd,
                GoldCreditGrams = GoldCreditGrams,
                Notes = Notes
            });

            BeautifulMessageDialog.ShowSuccess("تم تعيين أرصدة الافتتاح");
            Notes = string.Empty;
            await LoadCustomersAsync();
            SelectedCustomer = Customers.FirstOrDefault(c => c.Id == SelectedCustomer.Id);
        }
        catch (Exception ex)
        {
            FormError = ex.Message;
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
