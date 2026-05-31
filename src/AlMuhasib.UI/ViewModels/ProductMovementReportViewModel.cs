using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class ProductMovementReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalIn = "0";
    [ObservableProperty] private string _totalOut = "0";
    [ObservableProperty] private string _productCount = "0";
    [ObservableProperty] private int? _selectedWarehouseId;
    [ObservableProperty] private int? _selectedProductId;

    public ObservableCollection<Warehouse> Warehouses { get; } = [];
    public ObservableCollection<Product> Products { get; } = [];

    private List<ProductMovementRow> _allRows = [];
    public ObservableCollection<ProductMovementRow> Rows { get; } = [];

    public ProductMovementReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "حركة المنتجات";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
        foreach (var w in await _unitOfWork.Warehouses.GetAllAsync())
            Warehouses.Add(w);
        foreach (var p in await _unitOfWork.Products.GetAllAsync())
            Products.Add(p);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _reportService.GetProductMovementReportAsync(
                DateFrom, DateTo, SelectedWarehouseId, SelectedProductId);

            TotalIn = result.TotalQuantityIn.ToString("N0");
            TotalOut = result.TotalQuantityOut.ToString("N0");
            ProductCount = result.ProductCount.ToString("N0");

            _allRows = result.Rows;
            CurrentPage = 1;
            UpdatePagination(_allRows, Rows);
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

    protected override void OnPageChanged() => UpdatePagination(_allRows, Rows);

    [RelayCommand]
    private void ExportToExcel()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Excel|*.xlsx",
            FileName = "حركة_المنتجات.xlsx"
        };
        if (dlg.ShowDialog() != true) return;

        var cols = new[] { "المنتج", "وارد", "صادر", "صافي" };
        var rows = _allRows.Select(r => new object[]
        {
            r.ProductName, r.QuantityIn, r.QuantityOut, r.NetQuantity
        }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "حركة المنتجات", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "المنتج", "وارد", "صادر", "صافي" };
        var rows = _allRows.Select(r => new object[]
        {
            r.ProductName, r.QuantityIn, r.QuantityOut, r.NetQuantity
        }).ToList();
        _exportService.PrintTable("حركة المنتجات", cols, rows);
    }
}
