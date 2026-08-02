using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldFxRatesViewModel : ViewModelBase
{
    private readonly IGoldPricingService _pricingService;
    private readonly ICurrentUserService _currentUserService;

    [ObservableProperty] private decimal? _latestUsdToIqd;
    [ObservableProperty] private DateTime? _latestRateDate;
    [ObservableProperty] private string _latestRateDisplay = "لا يوجد سعر صرف";
    [ObservableProperty] private decimal _newUsdToIqd;
    [ObservableProperty] private DateTime _newRateDate = DateTime.Today;
    [ObservableProperty] private string _newNotes = string.Empty;
    [ObservableProperty] private GoldFxRate? _selectedRate;

    public ObservableCollection<GoldFxRate> Rates { get; } = [];

    public GoldFxRatesViewModel(
        IGoldPricingService pricingService,
        ICurrentUserService currentUserService)
    {
        _pricingService = pricingService;
        _currentUserService = currentUserService;
        PageTitle = "أسعار الصرف";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.FxRates);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var latest = await _pricingService.GetLatestFxRateAsync();
            LatestUsdToIqd = latest?.UsdToIqd;
            LatestRateDate = latest?.RateDate;
            LatestRateDisplay = latest is null
                ? "لا يوجد سعر صرف مسجّل"
                : $"1 USD = {latest.UsdToIqd:N0} IQD  —  {latest.RateDate:yyyy/MM/dd}";

            if (latest is not null)
                NewUsdToIqd = latest.UsdToIqd;

            Rates.Clear();
            foreach (var rate in await _pricingService.GetFxRatesAsync())
                Rates.Add(rate);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر تحميل أسعار الصرف:\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task SaveRateAsync()
    {
        if (!CanAdd && !CanEdit)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية حفظ سعر الصرف");
            return;
        }

        if (NewUsdToIqd <= 0)
        {
            BeautifulMessageDialog.ShowWarning("أدخل سعر صرف صالحاً (دولار → دينار)");
            return;
        }

        try
        {
            IsBusy = true;
            await _pricingService.SaveFxRateAsync(new GoldFxRate
            {
                RateDate = NewRateDate.Date,
                UsdToIqd = NewUsdToIqd,
                Notes = NewNotes?.Trim() ?? string.Empty,
                CreatedBy = _currentUserService.Username
            });
            BeautifulMessageDialog.ShowSuccess("تم حفظ سعر الصرف");
            NewNotes = string.Empty;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
