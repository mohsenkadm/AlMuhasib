using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class CustomerStatementViewModel : ReportViewModelBase
{
    private readonly IWhatsAppShareService _whatsAppShare;

    [ObservableProperty] private string _customerName = "—";
    [ObservableProperty] private string _totalDebit = "0";
    [ObservableProperty] private string _totalCredit = "0";
    [ObservableProperty] private string _balance = "0";
    [ObservableProperty] private string _transactionCount = "0";
    [ObservableProperty] private string _periodLabel = "جميع الفترات";

    [ObservableProperty] private Customer? _selectedCustomer;
    [ObservableProperty] private string _customerSearchText = string.Empty;

    public ObservableCollection<Customer> Customers { get; } = [];
    public ObservableCollection<Customer> FilteredCustomers { get; } = [];

    private List<CustomerStatementRow> _allRows = [];
    public ObservableCollection<CustomerStatementRow> Rows { get; } = [];

    public CustomerStatementViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService,
        IWhatsAppShareService whatsAppShare)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        _whatsAppShare = whatsAppShare;
        PageTitle = "كشف حساب عميل";
        DateFrom = null;
        DateTo = null;
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
        Customers.Clear();
        FilteredCustomers.Clear();
        foreach (var c in (await _unitOfWork.Customers.GetAllAsync()).OrderBy(c => c.Name))
        {
            Customers.Add(c);
            FilteredCustomers.Add(c);
        }
    }

    partial void OnSelectedCustomerChanged(Customer? value)
    {
        if (value is not null)
            CustomerSearchText = value.Name;
    }

    partial void OnCustomerSearchTextChanged(string value)
    {
        if (SelectedCustomer is not null && SelectedCustomer.Name == value)
            return;

        SelectedCustomer = null;
        CustomerComboBoxFilter.Apply(Customers, FilteredCustomers, value);
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (SelectedCustomer is null)
        {
            BeautifulMessageDialog.ShowWarning("يرجى اختيار عميل من القائمة");
            return;
        }

        if (DateFrom.HasValue && DateTo.HasValue && DateFrom.Value.Date > DateTo.Value.Date)
        {
            BeautifulMessageDialog.ShowWarning("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");
            return;
        }

        try
        {
            IsBusy = true;
            var result = await _reportService.GetCustomerStatementAsync(SelectedCustomer.Id, DateFrom, DateTo);

            CustomerName = result.CustomerName;
            TotalDebit = FormatCurrency(result.TotalDebit);
            TotalCredit = FormatCurrency(result.TotalCredit);
            Balance = FormatCurrency(result.Balance);
            TransactionCount = result.TransactionCount.ToString("N0");
            PeriodLabel = BuildPeriodLabel();

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
        if (_allRows.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("لا توجد بيانات للطباعة");
            return;
        }

        var cols = new[] { "التاريخ", "البيان", "مدين", "دائن", "الرصيد" };
        var rows = _allRows.Select(r => new object[] { r.Date.ToString("yyyy/MM/dd"), r.Description, r.Debit.ToString("N0"), r.Credit.ToString("N0"), r.RunningBalance.ToString("N0") }).ToList();
        var title = $"كشف حساب {CustomerName} — {PeriodLabel}";
        var summary = new List<string>
        {
            $"الفترة: {PeriodLabel}",
            $"عدد الحركات: {TransactionCount}",
            $"إجمالي المدين: {TotalDebit}",
            $"إجمالي الدائن: {TotalCredit}",
            $"الرصيد: {Balance}"
        };
        _exportService.PrintTable(title, cols, rows, summary);
    }

    [RelayCommand]
    private void ShareWhatsApp()
    {
        if (SelectedCustomer is null || _allRows.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("يرجى تحميل كشف الحساب أولاً");
            return;
        }

        var model = new StatementPrintModel
        {
            Title = $"كشف حساب — {CustomerName}",
            PartyName = CustomerName,
            PartyPhone = SelectedCustomer.Phone,
            FromDate = DateFrom,
            ToDate = DateTo,
            Columns = ["التاريخ", "البيان", "مدين", "دائن", "الرصيد"],
            Rows = _allRows.Select(r => new object[]
            {
                r.Date.ToString("yyyy/MM/dd"),
                r.Description,
                r.Debit,
                r.Credit,
                r.RunningBalance
            }).ToList(),
            SummaryLines =
            [
                $"الفترة: {PeriodLabel}",
                $"عدد الحركات: {TransactionCount}",
                $"إجمالي المدين: {TotalDebit}",
                $"إجمالي الدائن: {TotalCredit}",
                $"الرصيد: {Balance}"
            ]
        };

        _whatsAppShare.ShareStatement(model, SelectedCustomer.Phone, CustomerName);
    }

    private string BuildPeriodLabel()
    {
        if (!DateFrom.HasValue && !DateTo.HasValue)
            return "جميع الفترات";
        if (DateFrom.HasValue && DateTo.HasValue)
            return $"{DateFrom:yyyy/MM/dd} — {DateTo:yyyy/MM/dd}";
        if (DateFrom.HasValue)
            return $"من {DateFrom:yyyy/MM/dd}";
        return $"حتى {DateTo:yyyy/MM/dd}";
    }
}
