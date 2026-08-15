using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class PackagingStockReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalBaseQuantity = "0";
    [ObservableProperty] private string _productCount = "0";
    [ObservableProperty] private string _rowCount = "0";

    [ObservableProperty] private int? _selectedWarehouseId;
    [ObservableProperty] private int? _selectedProductId;
    public ObservableCollection<Warehouse> Warehouses { get; } = [];
    public ObservableCollection<Product> Products { get; } = [];

    private List<PackagingStockReportRow> _allRows = [];
    public ObservableCollection<PackagingStockReportRow> Rows { get; } = [];

    public PackagingStockReportViewModel(
        IReportService reportService,
        IUnitOfWork unitOfWork,
        IExportService exportService,
        ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "كميات حسب التعبئة";
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
            var result = await _reportService.GetPackagingStockReportAsync(SelectedWarehouseId, SelectedProductId);
            TotalBaseQuantity = result.TotalBaseQuantity.ToString("N0");
            ProductCount = result.ProductCount.ToString("N0");
            RowCount = result.RowCount.ToString("N0");
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
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "كميات_التعبئة.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "المنتج", "المخزن", "الكمية الأساسية", "نوع التعبئة", "معامل التحويل", "المكافئ بالتعبئة" };
        var rows = _allRows.Select(r => new object[]
        {
            r.ProductName, r.WarehouseName, r.BaseQuantity, r.PackagingTypeName, r.ConversionFactor, r.EquivalentPackQuantity
        }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "التعبئة", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "المنتج", "المخزن", "الأساسي", "التعبئة", "المعامل", "المكافئ" };
        var rows = _allRows.Select(r => new object[]
        {
            r.ProductName, r.WarehouseName, r.BaseQuantity, r.PackagingTypeName, r.ConversionFactor, r.EquivalentPackQuantity
        }).ToList();
        _exportService.PrintTable("كميات حسب التعبئة", cols, rows);
    }
}
