using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.ViewModels;

public partial class SupplierStatementViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _supplierName = "—";
    [ObservableProperty] private string _totalDebit = "0";
    [ObservableProperty] private string _totalCredit = "0";
    [ObservableProperty] private string _balance = "0";
    [ObservableProperty] private string _invoiceCount = "0";

    [ObservableProperty] private int? _selectedSupplierId;
    public ObservableCollection<Supplier> Suppliers { get; } = [];

    private List<SupplierStatementRow> _allRows = [];
    public ObservableCollection<SupplierStatementRow> Rows { get; } = [];

    public SupplierStatementViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    { PageTitle = "كشف حساب مورد"; }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
        foreach (var s in await _unitOfWork.Suppliers.GetAllAsync()) Suppliers.Add(s);
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (_selectedSupplierId is null) { BeautifulMessageDialog.ShowWarning("يرجى اختيار مورد"); return; }
        try
        {
            IsBusy = true;
            var result = await _reportService.GetSupplierStatementAsync(_selectedSupplierId.Value, DateFrom, DateTo);

            SupplierName = result.SupplierName;
            TotalDebit = FormatCurrency(result.TotalDebit);
            TotalCredit = FormatCurrency(result.TotalCredit);
            Balance = FormatCurrency(result.Balance);
            InvoiceCount = result.InvoiceCount.ToString("N0");

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
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = $"كشف_حساب_{SupplierName}.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "التاريخ", "البيان", "مدين", "دائن", "الرصيد" };
        var rows = _allRows.Select(r => new object[] { r.Date.ToString("yyyy/MM/dd"), r.Description, r.Debit, r.Credit, r.RunningBalance }).ToList();
        _exportService.ExportToExcel(dlg.FileName, $"كشف حساب {SupplierName}", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "التاريخ", "البيان", "مدين", "دائن", "الرصيد" };
        var rows = _allRows.Select(r => new object[] { r.Date.ToString("yyyy/MM/dd"), r.Description, r.Debit, r.Credit, r.RunningBalance }).ToList();
        _exportService.PrintTable($"كشف حساب {SupplierName}", cols, rows);
    }
}
