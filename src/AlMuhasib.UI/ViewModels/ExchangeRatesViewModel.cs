using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels;

public partial class ExchangeRatesViewModel : ViewModelBase
{
    private readonly IExchangeRateService _exchangeRateService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private List<ExchangeRate> _allRates = [];

    [ObservableProperty] private decimal? _latestUsdToIqd;
    [ObservableProperty] private DateTime? _latestRateDate;
    [ObservableProperty] private string _latestRateDisplay = "لا يوجد سعر صرف";
    [ObservableProperty] private decimal _newUsdToIqd;
    [ObservableProperty] private DateTime _newRateDate = DateTime.Today;
    [ObservableProperty] private string _newNotes = string.Empty;
    [ObservableProperty] private ExchangeRate? _selectedRate;
    [ObservableProperty] private DateTime? _filterFromDate;
    [ObservableProperty] private DateTime? _filterToDate;
    [ObservableProperty] private string _searchText = string.Empty;

    public ObservableCollection<ExchangeRate> Rates { get; } = [];

    public ExchangeRatesViewModel(
        IExchangeRateService exchangeRateService,
        IExportService exportService,
        ICurrentUserService currentUserService)
    {
        _exchangeRateService = exchangeRateService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        PageTitle = "أسعار الصرف";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "ExchangeRates");
        await LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync(bool force = false)
    {
        if (IsBusy && !force) return;
        IsBusy = true;
        try
        {
            var latest = await _exchangeRateService.GetLatestAsync();
            LatestUsdToIqd = latest?.UsdToIqd;
            LatestRateDate = latest?.RateDate;
            LatestRateDisplay = latest is null
                ? "لا يوجد سعر صرف مسجّل"
                : $"1 USD = {latest.UsdToIqd:N0} د.ع  —  {latest.RateDate:yyyy/MM/dd}";

            if (latest is not null)
                NewUsdToIqd = latest.UsdToIqd;

            _allRates = (await _exchangeRateService.GetAllAsync()).ToList();
            ApplyFilters();
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

    private void ApplyFilters()
    {
        IEnumerable<ExchangeRate> filtered = _allRates;

        if (FilterFromDate.HasValue)
            filtered = filtered.Where(r => r.RateDate.Date >= FilterFromDate.Value.Date);
        if (FilterToDate.HasValue)
            filtered = filtered.Where(r => r.RateDate.Date <= FilterToDate.Value.Date);

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var q = SearchText.Trim();
            filtered = filtered.Where(r =>
                (r.Notes?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                || r.UsdToIqd.ToString("N0").Contains(q, StringComparison.OrdinalIgnoreCase)
                || r.RateDate.ToString("yyyy/MM/dd").Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
            filtered = ColumnFilterEngine.Apply(filtered.ToList(), ColumnFilters);

        Rates.Clear();
        foreach (var rate in filtered
                     .OrderByDescending(r => r.RateDate)
                     .ThenByDescending(r => r.Id))
            Rates.Add(rate);
    }

    protected override void OnColumnFiltersChanged() => ApplyFilters();

    partial void OnFilterFromDateChanged(DateTime? value) => ApplyFilters();
    partial void OnFilterToDateChanged(DateTime? value) => ApplyFilters();
    partial void OnSearchTextChanged(string value) => ApplyFilters();

    [RelayCommand]
    private async Task Refresh() => await LoadAsync(force: true);

    [RelayCommand]
    private void ClearSearchFilters()
    {
        FilterFromDate = null;
        FilterToDate = null;
        SearchText = string.Empty;
        ApplyFilters();
    }

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
            await _exchangeRateService.SaveAsync(new ExchangeRate
            {
                RateDate = NewRateDate.Date,
                UsdToIqd = NewUsdToIqd,
                Notes = NewNotes?.Trim() ?? string.Empty,
                CreatedBy = _currentUserService.Username
            });
            BeautifulMessageDialog.ShowSuccess("تم حفظ سعر الصرف");
            NewNotes = string.Empty;
            await LoadAsync(force: true);
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
    private async Task DeleteSelectedAsync()
    {
        if (!CanDelete)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية حذف سعر الصرف");
            return;
        }

        if (SelectedRate is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر سجلاً للحذف");
            return;
        }

        if (!BeautifulMessageDialog.ShowConfirm(
                $"حذف سعر الصرف لتاريخ {SelectedRate.RateDate:yyyy/MM/dd}؟",
                "تأكيد الحذف"))
            return;

        try
        {
            IsBusy = true;
            await _exchangeRateService.DeleteAsync(SelectedRate.Id);
            BeautifulMessageDialog.ShowSuccess("تم الحذف");
            await LoadAsync(force: true);
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
    private async Task ExportToExcel()
    {
        try
        {
            if (_allRates.Count == 0)
                await LoadAsync();

            var exportData = Rates.Select(r => new
            {
                التاريخ = r.RateDate.ToString("yyyy/MM/dd"),
                السعر = r.UsdToIqd,
                ملاحظات = r.Notes,
                أُنشئ = r.CreatedAt.ToString("yyyy/MM/dd HH:mm")
            });

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"أسعار_الصرف_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "أسعار الصرف");
                BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
            }
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء التصدير: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task PrintTable()
    {
        try
        {
            if (_allRates.Count == 0)
                await LoadAsync();

            var columns = new[] { "التاريخ", "USD → IQD", "ملاحظات", "أُنشئ" };
            IList<object[]> rows = Rates.Select(r => new object[]
            {
                r.RateDate.ToString("yyyy/MM/dd"),
                r.UsdToIqd.ToString("N0"),
                r.Notes,
                r.CreatedAt.ToString("yyyy/MM/dd HH:mm")
            }).ToList();
            _exportService.PrintTable("سجل أسعار الصرف", columns, rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }
}
