using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class DamageInvoicesReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalDamagedQuantity = "0";
    [ObservableProperty] private string _totalDamagedCost = "0";
    [ObservableProperty] private string _invoiceCount = "0";
    [ObservableProperty] private string _lineCount = "0";

    [ObservableProperty] private DateTime? _dateFrom;
    [ObservableProperty] private DateTime? _dateTo;
    [ObservableProperty] private int? _selectedWarehouseId;
    public ObservableCollection<Warehouse> Warehouses { get; } = [];

    private List<DamageInvoiceReportRow> _allRows = [];
    public ObservableCollection<DamageInvoiceReportRow> Rows { get; } = [];

    public DamageInvoicesReportViewModel(
        IReportService reportService,
        IUnitOfWork unitOfWork,
        IExportService exportService,
        ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "تقرير فواتير التلف";
        DateFrom = DateTime.Today.AddMonths(-1);
        DateTo = DateTime.Today;
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, ScreenPermissionRegistry.DamageInvoicesReport);
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
            var result = await _reportService.GetDamageInvoicesReportAsync(DateFrom, DateTo, SelectedWarehouseId);
            TotalDamagedQuantity = result.TotalDamagedQuantity.ToString("N0");
            TotalDamagedCost = FormatCurrency(result.TotalDamagedCost);
            InvoiceCount = result.InvoiceCount.ToString("N0");
            LineCount = result.LineCount.ToString("N0");
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
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "تقرير_التلف.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "رقم الفاتورة", "التاريخ", "المخزن", "المنتج", "الكمية", "تكلفة الوحدة", "إجمالي التكلفة", "ملاحظات" };
        var rows = _allRows.Select(r => new object[]
        {
            r.InvoiceNumber, r.Date.ToString("yyyy/MM/dd"), r.WarehouseName, r.ProductName,
            r.Quantity, r.UnitCost, r.TotalCost, r.Notes ?? ""
        }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "التلف", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "رقم الفاتورة", "التاريخ", "المخزن", "المنتج", "الكمية", "التكلفة" };
        var rows = _allRows.Select(r => new object[]
        {
            r.InvoiceNumber, r.Date.ToString("yyyy/MM/dd"), r.WarehouseName, r.ProductName, r.Quantity, r.TotalCost
        }).ToList();
        _exportService.PrintTable("تقرير فواتير التلف", cols, rows);
    }
}
