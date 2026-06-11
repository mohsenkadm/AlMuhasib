using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class ProductProfitMarginReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalRevenue = "0";
    [ObservableProperty] private string _totalCost = "0";
    [ObservableProperty] private string _totalGrossProfit = "0";
    [ObservableProperty] private string _averageMarginPercent = "0%";
    [ObservableProperty] private int? _selectedWarehouseId;

    public ObservableCollection<Warehouse> Warehouses { get; } = [];

    private List<ProductProfitMarginRow> _allRows = [];
    public ObservableCollection<ProductProfitMarginRow> Rows { get; } = [];

    public ProductProfitMarginReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "هامش ربح المنتجات";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
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
            var result = await _reportService.GetProductProfitMarginReportAsync(
                DateFrom, DateTo, SelectedWarehouseId);

            TotalRevenue = FormatCurrency(result.TotalRevenue);
            TotalCost = FormatCurrency(result.TotalCost);
            TotalGrossProfit = FormatCurrency(result.TotalGrossProfit);
            AverageMarginPercent = $"{result.AverageMarginPercent}%";

            _allRows = result.Rows;
            CurrentPage = 1;
            UpdatePaginationWithFilters(_allRows, Rows);
        }
        catch (Exception ex) { BeautifulMessageDialog.ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    protected override void OnPageChanged() => UpdatePaginationWithFilters(_allRows, Rows);

    [RelayCommand]
    private void ExportToExcel()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "هامش_ربح_المنتجات.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "المنتج", "الكمية", "الإيراد", "التكلفة", "الربح", "الهامش %" };
        var rows = _allRows.Select(r => new object[]
        {
            r.ProductName, r.QuantitySold.ToString("N0"), r.Revenue.ToString("N0"),
            r.Cost.ToString("N0"), r.GrossProfit.ToString("N0"), r.MarginPercent
        }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "هامش الربح", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "المنتج", "الكمية", "الإيراد", "التكلفة", "الربح", "الهامش %" };
        var rows = _allRows.Select(r => new object[]
        {
            r.ProductName, r.QuantitySold.ToString("N0"), r.Revenue.ToString("N0"),
            r.Cost.ToString("N0"), r.GrossProfit.ToString("N0"), r.MarginPercent
        }).ToList();
        _exportService.PrintTable("هامش ربح المنتجات", cols, rows);
    }
}
