using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class ExpiryReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _expiredCount = "0";
    [ObservableProperty] private string _criticalCount = "0";
    [ObservableProperty] private string _warningCount = "0";
    [ObservableProperty] private string _affectedQuantity = "0";
    [ObservableProperty] private int? _selectedWarehouseId;
    [ObservableProperty] private int? _selectedProductId;
    [ObservableProperty] private string _productSearch = string.Empty;
    [ObservableProperty] private DateTime? _expiryFrom;
    [ObservableProperty] private DateTime? _expiryTo;
    [ObservableProperty] private ExpiryStatusFilter _selectedFilter = ExpiryStatusFilter.All;
    [ObservableProperty] private bool _hideZeroQuantity = true;

    public ObservableCollection<Warehouse> Warehouses { get; } = [];
    public ObservableCollection<Product> Products { get; } = [];

    public List<ExpiryFilterItem> Filters { get; } =
    [
        new("الكل", ExpiryStatusFilter.All),
        new("منتهي", ExpiryStatusFilter.Expired),
        new("خلال 30 يوم", ExpiryStatusFilter.Within30Days),
        new("خلال 90 يوم", ExpiryStatusFilter.Within90Days),
        new("صالح", ExpiryStatusFilter.Valid)
    ];

    private List<ExpiryReportRow> _allRows = [];
    public ObservableCollection<ExpiryReportRow> Rows { get; } = [];

    public ExpiryReportViewModel(
        IReportService reportService,
        IUnitOfWork unitOfWork,
        IExportService exportService,
        ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "تقرير الصلاحية";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
        foreach (var w in await _unitOfWork.Warehouses.GetAllAsync())
            Warehouses.Add(w);
        foreach (var p in (await _unitOfWork.Products.GetAllAsync()).OrderBy(x => x.Name))
            Products.Add(p);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _reportService.GetExpiryReportAsync(
                SelectedWarehouseId,
                SelectedProductId,
                string.IsNullOrWhiteSpace(ProductSearch) ? null : ProductSearch.Trim(),
                ExpiryFrom,
                ExpiryTo,
                SelectedFilter,
                HideZeroQuantity);

            ExpiredCount = result.ExpiredCount.ToString("N0");
            CriticalCount = result.CriticalCount.ToString("N0");
            WarningCount = result.WarningCount.ToString("N0");
            AffectedQuantity = result.AffectedQuantity.ToString("N0");

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
    private void ExportToExcel()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Excel|*.xlsx",
            FileName = "تقرير_الصلاحية.xlsx"
        };
        if (dlg.ShowDialog() != true) return;

        var cols = new[]
        {
            "المنتج", "الباركود", "المخزن", "رقم الدفعة", "تاريخ الانتهاء",
            "الكمية", "الأيام المتبقية", "الحالة"
        };
        var rows = _allRows.Select(r => new object[]
        {
            r.ProductName,
            r.ProductBarcode ?? "—",
            r.WarehouseName,
            r.BatchNumberDisplay,
            r.ExpiryDateDisplay,
            r.Quantity,
            r.DaysRemainingDisplay,
            r.StatusDisplay
        }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "تقرير الصلاحية", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[]
        {
            "المنتج", "المخزن", "الدفعة", "الانتهاء", "الكمية", "الأيام", "الحالة"
        };
        var rows = _allRows.Select(r => new object[]
        {
            r.ProductName,
            r.WarehouseName,
            r.BatchNumberDisplay,
            r.ExpiryDateDisplay,
            r.Quantity,
            r.DaysRemainingDisplay,
            r.StatusDisplay
        }).ToList();
        _exportService.PrintTable("تقرير الصلاحية", cols, rows);
    }

    public record ExpiryFilterItem(string Label, ExpiryStatusFilter Value);
}
