using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.ViewModels;

public partial class CustomerStatementViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _customerName = "—";
    [ObservableProperty] private string _totalDebit = "0";
    [ObservableProperty] private string _totalCredit = "0";
    [ObservableProperty] private string _balance = "0";
    [ObservableProperty] private string _transactionCount = "0";

    [ObservableProperty] private int? _selectedCustomerId;
    public ObservableCollection<Customer> Customers { get; } = [];

    private List<CustomerStatementRow> _allRows = [];
    public ObservableCollection<CustomerStatementRow> Rows { get; } = [];

    public CustomerStatementViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    { PageTitle = "كشف حساب عميل"; }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
        foreach (var c in await _unitOfWork.Customers.GetAllAsync()) Customers.Add(c);
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (_selectedCustomerId is null) { BeautifulMessageDialog.ShowWarning("يرجى اختيار عميل"); return; }
        try
        {
            IsBusy = true;
            var result = await _reportService.GetCustomerStatementAsync(_selectedCustomerId.Value, DateFrom, DateTo);

            CustomerName = result.CustomerName;
            TotalDebit = FormatCurrency(result.TotalDebit);
            TotalCredit = FormatCurrency(result.TotalCredit);
            Balance = FormatCurrency(result.Balance);
            TransactionCount = result.TransactionCount.ToString("N0");

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
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = $"كشف_حساب_{CustomerName}.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "التاريخ", "البيان", "مدين", "دائن", "الرصيد" };
        var rows = _allRows.Select(r => new object[] { r.Date.ToString("yyyy/MM/dd"), r.Description, r.Debit, r.Credit, r.RunningBalance }).ToList();
        _exportService.ExportToExcel(dlg.FileName, $"كشف حساب {CustomerName}", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "التاريخ", "البيان", "مدين", "دائن", "الرصيد" };
        var rows = _allRows.Select(r => new object[] { r.Date.ToString("yyyy/MM/dd"), r.Description, r.Debit, r.Credit, r.RunningBalance }).ToList();
        _exportService.PrintTable($"كشف حساب {CustomerName}", cols, rows);
    }
}
