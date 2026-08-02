using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldFxRatesViewModel : ViewModelBase
{
    private readonly IGoldPricingService _pricingService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private List<GoldFxRate> _allRates = [];

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
        IExportService exportService,
        ICurrentUserService currentUserService)
    {
        _pricingService = pricingService;
        _exportService = exportService;
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

            _allRates = (await _pricingService.GetFxRatesAsync()).ToList();
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
        var filtered = MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters)
            ? ColumnFilterEngine.Apply(_allRates, ColumnFilters)
            : _allRates.ToList();

        Rates.Clear();
        foreach (var rate in filtered)
            Rates.Add(rate);
    }

    protected override void OnColumnFiltersChanged() => ApplyFilters();

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

    [RelayCommand]
    private async Task ExportToExcel()
    {
        try
        {
            if (_allRates.Count == 0)
                await LoadAsync();

            var exportData = _allRates.Select(r => new
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
            IList<object[]> rows = _allRates.Select(r => new object[]
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
