using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.SalesRep;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class SalesRepCustomersReportViewModel : ReportViewModelBase
{
    private readonly ISalesRepService _salesRepService;

    public ObservableCollection<SalesRepresentative> Representatives { get; } = [];
    public ObservableCollection<SalesRepCustomerRow> Rows { get; } = [];
    private List<SalesRepCustomerRow> _allRows = [];

    [ObservableProperty] private SalesRepresentative? _selectedSalesRep;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _totalSales = "0";
    [ObservableProperty] private string _totalPaid = "0";
    [ObservableProperty] private string _totalRemaining = "0";
    [ObservableProperty] private string _customerCount = "0";

    public SalesRepCustomersReportViewModel(
        ISalesRepService salesRepService,
        IReportService reportService,
        IUnitOfWork unitOfWork,
        IExportService exportService,
        ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        _salesRepService = salesRepService;
        PageTitle = "عملاء المندوب";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "SalesRepCustomers");
        Representatives.Clear();
        foreach (var r in (await _unitOfWork.SalesRepresentatives.GetAllAsync()).OrderBy(x => x.Name))
            Representatives.Add(r);
        SelectedSalesRep = Representatives.FirstOrDefault();
        if (SelectedSalesRep is not null)
            await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (SelectedSalesRep is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر مندوباً أولاً");
            return;
        }

        try
        {
            IsBusy = true;
            var data = await _salesRepService.GetCustomersByRepAsync(SelectedSalesRep.Id, DateFrom, DateTo);
            _allRows = data.ToList();

            TotalSales = FormatCurrency(_allRows.Sum(r => r.TotalSales));
            TotalPaid = FormatCurrency(_allRows.Sum(r => r.PaidAmount));
            TotalRemaining = FormatCurrency(_allRows.Sum(r => r.RemainingAmount));
            CustomerCount = _allRows.Count.ToString("N0");

            CurrentPage = 1;
            ApplyFilterAndPage();
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

    partial void OnSearchTextChanged(string value)
    {
        CurrentPage = 1;
        ApplyFilterAndPage();
    }

    protected override void OnPageChanged() => ApplyFilterAndPage();

    private void ApplyFilterAndPage()
    {
        IEnumerable<SalesRepCustomerRow> filtered = _allRows;
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            filtered = filtered.Where(r =>
                r.CustomerName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (r.Phone?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || (r.LastInvoiceNumber?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        UpdatePaginationWithFilters(filtered.ToList(), Rows);
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        try
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel|*.xlsx",
                FileName = $"عملاء_مندوب_{SelectedSalesRep?.Name}_{DateTime.Now:yyyyMMdd}.xlsx"
            };
            if (dlg.ShowDialog() != true) return;

            var cols = new[]
            {
                "العميل", "الهاتف", "المبيعات", "المدفوع", "المتبقي",
                "آخر فاتورة", "تاريخ آخر فاتورة", "آخر دفعة", "مبلغ آخر دفعة"
            };
            var rows = _allRows.Select(r => new object[]
            {
                r.CustomerName, r.Phone ?? "",
                r.TotalSales, r.PaidAmount, r.RemainingAmount,
                r.LastInvoiceNumber ?? "",
                r.LastInvoiceDate?.ToString("yyyy/MM/dd") ?? "",
                r.LastPaymentDate?.ToString("yyyy/MM/dd") ?? "",
                r.LastPaymentAmount
            }).ToList();
            _exportService.ExportToExcel(dlg.FileName, "العملاء", cols, rows);
            BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void Print()
    {
        try
        {
            var cols = new[] { "العميل", "الهاتف", "المبيعات", "المدفوع", "المتبقي", "آخر فاتورة" };
            IList<object[]> rows = _allRows.Select(r => new object[]
            {
                r.CustomerName, r.Phone ?? "",
                r.TotalSales.ToString("N0"), r.PaidAmount.ToString("N0"),
                r.RemainingAmount.ToString("N0"),
                r.LastInvoiceNumber ?? ""
            }).ToList();
            _exportService.PrintTable($"عملاء المندوب — {SelectedSalesRep?.Name}", cols, rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }
}
