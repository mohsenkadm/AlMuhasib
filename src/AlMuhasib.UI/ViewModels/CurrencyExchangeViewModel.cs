using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class CurrencyExchangeViewModel : ViewModelBase
{
    private readonly ICurrencyExchangeService _exchangeService;
    private readonly ICashBankService _cashBankService;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly IFeatureFlagService _featureFlags;
    private readonly ICurrentUserService _currentUserService;

    public ObservableCollection<CashBox> FromCashBoxes { get; } = [];
    public ObservableCollection<CashBox> ToCashBoxes { get; } = [];
    public ObservableCollection<CashBox> FilterCashBoxes { get; } = [];
    public ObservableCollection<CurrencyExchangeListItem> History { get; } = [];

    [ObservableProperty] private CashBox? _selectedFromCashBox;
    [ObservableProperty] private CashBox? _selectedToCashBox;
    [ObservableProperty] private decimal _fromAmount;
    [ObservableProperty] private decimal _fxRate;
    [ObservableProperty] private decimal _toAmountPreview;
    [ObservableProperty] private string _toAmountPreviewDisplay = "—";
    [ObservableProperty] private DateTime _exchangeDate = DateTime.Today;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private string _latestRateDisplay = string.Empty;
    [ObservableProperty] private bool _multiCurrencyEnabled;

    [ObservableProperty] private DateTime? _filterFromDate;
    [ObservableProperty] private DateTime? _filterToDate;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private CashBox? _filterCashBox;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private CurrencyExchangeListItem? _selectedExchange;

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public bool CanGoPrev => CurrentPage > 1;
    public bool CanGoNext => CurrentPage < TotalPages;

    public CurrencyExchangeViewModel(
        ICurrencyExchangeService exchangeService,
        ICashBankService cashBankService,
        IExchangeRateService exchangeRateService,
        IFeatureFlagService featureFlags,
        ICurrentUserService currentUserService)
    {
        _exchangeService = exchangeService;
        _cashBankService = cashBankService;
        _exchangeRateService = exchangeRateService;
        _featureFlags = featureFlags;
        _currentUserService = currentUserService;
        PageTitle = "صيرفة";
        FilterFromDate = DateTime.Today.AddMonths(-1);
        FilterToDate = DateTime.Today;
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "CurrencyExchange");
        MultiCurrencyEnabled = _featureFlags.MultiCurrency;
        if (!MultiCurrencyEnabled)
        {
            BeautifulMessageDialog.ShowWarning("فعّل «تنوع العملات» من إعدادات الميزات لاستخدام شاشة الصيرفة");
            return;
        }

        await LoadCashBoxesAsync();
        await LoadLatestRateAsync();
        await LoadHistoryAsync();
    }

    private async Task LoadCashBoxesAsync()
    {
        var boxes = (await _cashBankService.GetAllCashBoxesAsync()).ToList();
        FilterCashBoxes.Clear();
        FilterCashBoxes.Add(new CashBox { Id = 0, Name = "كل القاصات" });
        foreach (var b in boxes)
            FilterCashBoxes.Add(b);
        FilterCashBox = FilterCashBoxes[0];

        RebuildFromToLists(boxes);
    }

    private void RebuildFromToLists(IReadOnlyList<CashBox>? all = null)
    {
        all ??= FilterCashBoxes.Where(b => b.Id > 0).ToList();
        FromCashBoxes.Clear();
        ToCashBoxes.Clear();
        foreach (var b in all.Where(x => x.Id > 0).OrderBy(x => x.Name))
        {
            FromCashBoxes.Add(b);
            ToCashBoxes.Add(b);
        }
    }

    private async Task LoadLatestRateAsync()
    {
        try
        {
            var latest = await _exchangeRateService.GetLatestAsync();
            if (latest is not null && latest.UsdToIqd > 0)
            {
                FxRate = latest.UsdToIqd;
                LatestRateDisplay = $"آخر سعر: 1 $ = {latest.UsdToIqd:N0} د.ع — {latest.RateDate:yyyy/MM/dd}";
            }
            else
            {
                FxRate = 0;
                LatestRateDisplay = "لا يوجد سعر صرف مسجّل — أدخله يدوياً أو من شاشة أسعار الصرف";
            }
        }
        catch
        {
            LatestRateDisplay = "تعذر تحميل سعر الصرف";
        }
        RefreshPreview();
    }

    partial void OnSelectedFromCashBoxChanged(CashBox? value) => RefreshPreview();
    partial void OnSelectedToCashBoxChanged(CashBox? value) => RefreshPreview();
    partial void OnFromAmountChanged(decimal value) => RefreshPreview();
    partial void OnFxRateChanged(decimal value) => RefreshPreview();
    partial void OnSearchTextChanged(string value) => _ = ReloadHistoryFromFirstPageAsync();
    partial void OnFilterFromDateChanged(DateTime? value) => _ = ReloadHistoryFromFirstPageAsync();
    partial void OnFilterToDateChanged(DateTime? value) => _ = ReloadHistoryFromFirstPageAsync();
    partial void OnFilterCashBoxChanged(CashBox? value) => _ = ReloadHistoryFromFirstPageAsync();

    private void RefreshPreview()
    {
        if (SelectedFromCashBox is null || SelectedToCashBox is null
            || SelectedFromCashBox.Currency == SelectedToCashBox.Currency
            || FromAmount <= 0 || FxRate <= 0)
        {
            ToAmountPreview = 0;
            ToAmountPreviewDisplay = "—";
            return;
        }

        try
        {
            ToAmountPreview = AccountingCurrencyHelper.Convert(
                FromAmount,
                SelectedFromCashBox.Currency,
                SelectedToCashBox.Currency,
                FxRate);
            ToAmountPreviewDisplay = AccountingCurrencyHelper.Format(ToAmountPreview, SelectedToCashBox.Currency);
        }
        catch
        {
            ToAmountPreview = 0;
            ToAmountPreviewDisplay = "—";
        }
    }

    [RelayCommand]
    private async Task SaveExchangeAsync()
    {
        if (!CanAdd) return;
        if (SelectedFromCashBox is null || SelectedToCashBox is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر قاصة المصدر وقاصة الوجهة");
            return;
        }
        if (SelectedFromCashBox.Currency == SelectedToCashBox.Currency)
        {
            BeautifulMessageDialog.ShowWarning("يجب أن تكون القاصتان بعملتين مختلفتين");
            return;
        }
        if (FromAmount <= 0)
        {
            BeautifulMessageDialog.ShowWarning("أدخل المبلغ المراد تحويله");
            return;
        }
        if (FxRate <= 0)
        {
            BeautifulMessageDialog.ShowWarning("أدخل سعر الصرف");
            return;
        }

        try
        {
            IsBusy = true;
            var created = await _exchangeService.CreateAsync(
                SelectedFromCashBox.Id,
                SelectedToCashBox.Id,
                FromAmount,
                FxRate,
                ExchangeDate,
                Notes);

            BeautifulMessageDialog.ShowSuccess(
                $"تمت الصيرفة — {AccountingCurrencyHelper.Format(created.FromAmount, created.FromCurrency)} → {AccountingCurrencyHelper.Format(created.ToAmount, created.ToCurrency)}");

            FromAmount = 0;
            Notes = string.Empty;
            await LoadCashBoxesAsync();
            await LoadHistoryAsync();
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

    [RelayCommand]
    private async Task ReverseSelectedAsync()
    {
        if (SelectedExchange is null || !CanDelete) return;
        if (!BeautifulMessageDialog.ShowConfirm($"هل تريد التراجع عن عملية الصيرفة رقم {SelectedExchange.Id}؟"))
            return;

        try
        {
            IsBusy = true;
            await _exchangeService.ReverseAsync(SelectedExchange.Id);
            BeautifulMessageDialog.ShowSuccess("تم التراجع عن عملية الصيرفة");
            await LoadCashBoxesAsync();
            await LoadHistoryAsync();
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

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadCashBoxesAsync();
        await LoadLatestRateAsync();
        await LoadHistoryAsync();
    }

    [RelayCommand]
    private async Task ApplyFiltersAsync() => await ReloadHistoryFromFirstPageAsync();

    [RelayCommand]
    private async Task PrevPageAsync()
    {
        if (!CanGoPrev) return;
        CurrentPage--;
        await LoadHistoryAsync();
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (!CanGoNext) return;
        CurrentPage++;
        await LoadHistoryAsync();
    }

    private async Task ReloadHistoryFromFirstPageAsync()
    {
        CurrentPage = 1;
        await LoadHistoryAsync();
    }

    private async Task LoadHistoryAsync()
    {
        if (!MultiCurrencyEnabled) return;
        try
        {
            var cashBoxId = FilterCashBox is { Id: > 0 } ? FilterCashBox.Id : (int?)null;
            var (items, total) = await _exchangeService.GetPagedAsync(
                CurrentPage,
                PageSize,
                FilterFromDate,
                FilterToDate,
                cashBoxId,
                SearchText);

            History.Clear();
            foreach (var item in items)
                History.Add(item);

            TotalCount = total;
            OnPropertyChanged(nameof(TotalPages));
            OnPropertyChanged(nameof(CanGoPrev));
            OnPropertyChanged(nameof(CanGoNext));
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر تحميل سجل الصيرفة:\n{ex.Message}");
        }
    }
}
