using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Charts;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace AlMuhasib.UI.ViewModels;

public partial class WarehouseTransfersReportViewModel : ReportViewModelBase
{
    private readonly IFeatureFlagService _featureFlags;

    [ObservableProperty] private string _transferCount = "0";
    [ObservableProperty] private string _totalLineCount = "0";
    [ObservableProperty] private string _totalQuantity = "0";
    [ObservableProperty] private string _warehouseCount = "0";

    [ObservableProperty] private int? _selectedFromWarehouseId;
    [ObservableProperty] private int? _selectedToWarehouseId;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _showMultiCurrency;

    public ObservableCollection<Warehouse> Warehouses { get; } = [];

    [ObservableProperty] private ISeries[] _dailySeries = [];
    [ObservableProperty] private Axis[] _dailyXAxes = [];
    [ObservableProperty] private Axis[] _dailyYAxes = [];
    [ObservableProperty] private ISeries[] _fromWarehouseSeries = [];

    private List<WarehouseTransferReportRow> _allRows = [];
    public ObservableCollection<WarehouseTransferReportRow> Rows { get; } = [];

    public WarehouseTransfersReportViewModel(
        IReportService reportService,
        IUnitOfWork unitOfWork,
        IExportService exportService,
        ICurrentUserService currentUserService,
        IFeatureFlagService featureFlags)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        _featureFlags = featureFlags;
        PageTitle = "تقرير نقل المخازن";
        RegisterThemeChartReload(LoadDataAsync);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
        ShowMultiCurrency = _featureFlags.MultiCurrency;

        if (!_featureFlags.WarehouseTransfers)
        {
            BeautifulMessageDialog.ShowWarning("فعّل «نقل بين مخازن» من إعدادات الميزات");
            return;
        }

        Warehouses.Clear();
        Warehouses.Add(new Warehouse { Id = 0, Name = "الكل" });
        foreach (var w in await _unitOfWork.Warehouses.GetAllAsync())
            Warehouses.Add(w);

        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var fromId = SelectedFromWarehouseId is > 0 ? SelectedFromWarehouseId : null;
            var toId = SelectedToWarehouseId is > 0 ? SelectedToWarehouseId : null;
            var result = await _reportService.GetWarehouseTransfersReportAsync(
                DateFrom, DateTo, fromId, toId, SearchText);

            TransferCount = result.TransferCount.ToString("N0");
            TotalLineCount = result.TotalLineCount.ToString("N0");
            TotalQuantity = result.TotalQuantity.ToString("N2");
            WarehouseCount = result.WarehouseCount.ToString("N0");

            if (result.DailyChart.Count > 0)
            {
                DailySeries = [ChartThemeConfig.Column(result.DailyChart.Select(d => d.Amount).ToArray(), "الكميات", 0)];
                DailyXAxes = [ChartThemeConfig.CreateXAxis(result.DailyChart.Select(d => d.Date.ToString("MM/dd")).ToArray())];
                DailyYAxes = [ChartThemeConfig.CreateYAxis()];
            }
            else
            {
                DailySeries = [];
                DailyXAxes = [];
                DailyYAxes = [];
            }

            FromWarehouseSeries = result.ByFromWarehouseChart.Count > 0
                ? ChartThemeConfig.PieFromNameAmount(result.ByFromWarehouseChart)
                : [];

            _allRows = result.Rows;
            CurrentPage = 1;
            UpdatePaginationWithFilters(_allRows, Rows);
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

    protected override void OnPageChanged() => UpdatePaginationWithFilters(_allRows, Rows);

    [RelayCommand]
    private async Task OpenTransferDetailFromReportRowAsync(object? rowObj)
    {
        if (rowObj is not WarehouseTransferReportRow row) return;
        try
        {
            IsBusy = true;
            var detail = await _reportService.GetWarehouseTransferDetailAsync(row.Id);
            if (detail is null)
            {
                BeautifulMessageDialog.ShowWarning("لم يتم العثور على فاتورة النقل");
                return;
            }

            WarehouseTransferDetailDialog.Show(detail);
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
    private void ExportToExcel()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "تقرير_نقل_المخازن.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "رقم النقل", "التاريخ", "من مخزن", "إلى مخزن", "عدد البنود", "إجمالي الكمية", "ملاحظات", "بواسطة" };
        var rows = _allRows.Select(r => new object[]
        {
            r.TransferNumber, r.Date.ToString("yyyy/MM/dd"), r.FromWarehouseName, r.ToWarehouseName,
            r.LineCount, r.TotalQuantity, r.Notes, r.CreatedBy
        }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "نقل المخازن", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "رقم النقل", "التاريخ", "من مخزن", "إلى مخزن", "عدد البنود", "إجمالي الكمية", "ملاحظات", "بواسطة" };
        var rows = _allRows.Select(r => new object[]
        {
            r.TransferNumber, r.Date.ToString("yyyy/MM/dd"), r.FromWarehouseName, r.ToWarehouseName,
            r.LineCount, r.TotalQuantity, r.Notes, r.CreatedBy
        }).ToList();
        _exportService.PrintTable("تقرير نقل المخازن", cols, rows);
    }
}
