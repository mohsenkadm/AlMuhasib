using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class SuppliersOverviewReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalPurchases = "0";
    [ObservableProperty] private string _totalPaid = "0";
    [ObservableProperty] private string _totalOutstanding = "0";
    [ObservableProperty] private string _supplierCount = "0";

    private List<SupplierOverviewRow> _allRows = [];
    public ObservableCollection<SupplierOverviewRow> Rows { get; } = [];

    public SuppliersOverviewReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "ملخص الموردين";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _reportService.GetSuppliersOverviewReportAsync(DateFrom, DateTo);

            TotalPurchases = FormatCurrency(result.TotalPurchases);
            TotalPaid = FormatCurrency(result.TotalPaid);
            TotalOutstanding = FormatCurrency(result.TotalOutstanding);
            SupplierCount = result.SupplierCount.ToString("N0");

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
            FileName = "ملخص_الموردين.xlsx"
        };
        if (dlg.ShowDialog() != true) return;

        var cols = new[] { "المورد", "الهاتف", "فواتير", "المشتريات", "المدفوع", "المستحق" };
        var rows = _allRows.Select(r => new object[]
        {
            r.SupplierName, r.Phone, r.InvoiceCount, r.PurchaseAmount, r.PaidAmount, r.OutstandingBalance
        }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "ملخص الموردين", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "المورد", "الهاتف", "فواتير", "المشتريات", "المدفوع", "المستحق" };
        var rows = _allRows.Select(r => new object[]
        {
            r.SupplierName, r.Phone, r.InvoiceCount, r.PurchaseAmount, r.PaidAmount, r.OutstandingBalance
        }).ToList();
        _exportService.PrintTable("ملخص الموردين", cols, rows);
    }
}
