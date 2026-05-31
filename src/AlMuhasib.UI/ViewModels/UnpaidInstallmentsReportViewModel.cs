using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Charts;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.ViewModels;

public partial class UnpaidInstallmentsReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalUnpaid = "0";
    [ObservableProperty] private string _unpaidCount = "0";
    [ObservableProperty] private string _customerCount = "0";
    [ObservableProperty] private string _oldestOverdueDays = "0";

    [ObservableProperty] private int? _selectedCustomerId;
    public ObservableCollection<Customer> Customers { get; } = [];

    [ObservableProperty] private ISeries[] _customerSeries = [];

    private List<UnpaidInstallmentRow> _allRows = [];
    public ObservableCollection<UnpaidInstallmentRow> Rows { get; } = [];

    public UnpaidInstallmentsReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService,
        IInvoiceService invoiceService, IInstallmentService installmentService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "الأقساط غير المسددة";
        InitReportActionServices(invoiceService, installmentService);
        RegisterThemeChartReload(LoadDataAsync);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
        foreach (var c in await _unitOfWork.Customers.GetAllAsync()) Customers.Add(c);
        await LoadBulkPayCashBoxesAsync();
        await LoadDataAsync();
    }

    protected override async Task OnAfterBulkInstallmentPayAsync()
    {
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _reportService.GetUnpaidInstallmentsAsync(DateFrom, DateTo, _selectedCustomerId);

            TotalUnpaid = FormatCurrency(result.TotalUnpaid);
            UnpaidCount = result.UnpaidCount.ToString("N0");
            CustomerCount = result.CustomerCount.ToString("N0");
            OldestOverdueDays = $"{result.OldestOverdueDays} يوم";

            if (result.ByCustomerChart.Count > 0)
                CustomerSeries = ChartThemeConfig.PieFromNameAmount(result.ByCustomerChart);

            _allRows = result.Rows;
            CurrentPage = 1;
            UpdatePagination(_allRows, Rows);
        }
        catch (Exception ex) { BeautifulMessageDialog.ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    protected override void OnPageChanged() => UpdatePagination(_allRows, Rows);

    [RelayCommand]
    private void ExportToExcel()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "الأقساط_غير_المسددة.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "العميل", "رقم الخطة", "تاريخ الاستحقاق", "المبلغ", "المتبقي", "أيام التأخير" };
        var rows = _allRows.Select(r => new object[] { r.CustomerName, r.PlanNumber, r.DueDate.ToString("yyyy/MM/dd"), r.Amount, r.RemainingAmount, r.OverdueDays }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "الأقساط غير المسددة", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "العميل", "رقم الخطة", "تاريخ الاستحقاق", "المبلغ", "المتبقي", "أيام التأخير" };
        var rows = _allRows.Select(r => new object[] { r.CustomerName, r.PlanNumber, r.DueDate.ToString("yyyy/MM/dd"), r.Amount, r.RemainingAmount, r.OverdueDays }).ToList();
        _exportService.PrintTable("الأقساط غير المسددة", cols, rows);
    }
}
