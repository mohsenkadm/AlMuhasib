using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Customs;
using AlMuhasib.Core.Models.Customs;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels.Customs;

public partial class CustomsStatementViewModel : ViewModelBase
{
    private readonly ICustomsStatementService _statementService;
    private readonly ICustomsCustomerService _customerService;
    private readonly ICustomsPrintService _printService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private System.Timers.Timer? _customerSearchTimer;

    [ObservableProperty] private DateTime? _dateFrom;
    [ObservableProperty] private DateTime? _dateTo;
    [ObservableProperty] private string _customerSearchText = string.Empty;
    [ObservableProperty] private CustomsCustomerListItem? _selectedCustomer;
    [ObservableProperty] private CustomsStatementTransactionType _transactionTypeFilter = CustomsStatementTransactionType.All;
    [ObservableProperty] private CustomsStatementInvoiceOption? _selectedInvoiceOption;

    [ObservableProperty] private string _customerName = "—";
    [ObservableProperty] private string _totalDebitIqd = "0";
    [ObservableProperty] private string _totalCreditIqd = "0";
    [ObservableProperty] private string _balanceIqd = "0";
    [ObservableProperty] private string _totalDebitUsd = "0";
    [ObservableProperty] private string _totalCreditUsd = "0";
    [ObservableProperty] private string _balanceUsd = "0";
    [ObservableProperty] private string _transactionCount = "0";
    [ObservableProperty] private string _periodLabel = "جميع الفترات";

    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 25;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private int _totalRecords;
    [ObservableProperty] private string _paginationText = string.Empty;

    private List<CustomsStatementRow> _allRows = [];
    private CustomsStatementResult? _lastResult;

    public ObservableCollection<CustomsCustomerListItem> Customers { get; } = [];
    public ObservableCollection<CustomsCustomerListItem> FilteredCustomers { get; } = [];
    public ObservableCollection<CustomsStatementInvoiceOption> InvoiceOptions { get; } = [];
    public ObservableCollection<CustomsStatementRow> Rows { get; } = [];

    public IReadOnlyList<CustomsStatementTypeOption> TransactionTypeOptions { get; } =
    [
        new(CustomsStatementTransactionType.All, "الكل"),
        new(CustomsStatementTransactionType.Invoice, "فواتير"),
        new(CustomsStatementTransactionType.Receipt, "قبض")
    ];

    public CustomsStatementViewModel(
        ICustomsStatementService statementService,
        ICustomsCustomerService customerService,
        ICustomsPrintService printService,
        IExportService exportService,
        ICurrentUserService currentUserService)
    {
        _statementService = statementService;
        _customerService = customerService;
        _printService = printService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        PageTitle = "كشف حساب عميل";
        DateFrom = null;
        DateTo = null;
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, CustomsPermissionRegistry.Statement);
        await LoadCustomersAsync();

        var pending = CustomsStatementNavigationBridge.TakePendingCustomerId();
        if (pending is int customerId)
        {
            SelectedCustomer = Customers.FirstOrDefault(c => c.Id == customerId)
                ?? FilteredCustomers.FirstOrDefault(c => c.Id == customerId);
            if (SelectedCustomer is not null)
            {
                CustomerSearchText = SelectedCustomer.Name;
                await LoadInvoiceOptionsAsync();
                await LoadDataAsync();
            }
        }
    }

    private async Task LoadCustomersAsync()
    {
        var (items, _) = await _customerService.GetPagedAsync(1, 500, activeOnly: true);
        Customers.Clear();
        FilteredCustomers.Clear();
        foreach (var c in items.OrderBy(x => x.Name))
        {
            Customers.Add(c);
            FilteredCustomers.Add(c);
        }
    }

    partial void OnSelectedCustomerChanged(CustomsCustomerListItem? value)
    {
        if (value is not null)
            CustomerSearchText = value.Name;
        _ = LoadInvoiceOptionsAsync();
    }

    partial void OnCustomerSearchTextChanged(string value)
    {
        if (SelectedCustomer is not null && SelectedCustomer.Name == value)
            return;

        SelectedCustomer = null;
        _customerSearchTimer?.Stop();
        _customerSearchTimer?.Dispose();
        _customerSearchTimer = new System.Timers.Timer(250) { AutoReset = false };
        _customerSearchTimer.Elapsed += (_, _) =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                FilteredCustomers.Clear();
                var term = (value ?? string.Empty).Trim();
                IEnumerable<CustomsCustomerListItem> q = Customers;
                if (!string.IsNullOrEmpty(term))
                {
                    q = Customers.Where(c =>
                        c.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        (c.Phone?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (c.CompanyName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
                }

                foreach (var c in q)
                    FilteredCustomers.Add(c);
            });
        };
        _customerSearchTimer.Start();
    }

    private async Task LoadInvoiceOptionsAsync()
    {
        InvoiceOptions.Clear();
        SelectedInvoiceOption = null;
        if (SelectedCustomer is null) return;

        try
        {
            var options = await _statementService.GetInvoiceOptionsAsync(SelectedCustomer.Id);
            foreach (var o in options)
                InvoiceOptions.Add(o);
            SelectedInvoiceOption = InvoiceOptions.FirstOrDefault();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
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
            var result = await _statementService.GetStatementAsync(
                SelectedCustomer.Id,
                DateFrom,
                DateTo,
                TransactionTypeFilter,
                SelectedInvoiceOption?.Id);

            _lastResult = result;
            CustomerName = result.CustomerName;
            TotalDebitIqd = result.TotalDebitIqd.ToString("N0");
            TotalCreditIqd = result.TotalCreditIqd.ToString("N0");
            BalanceIqd = result.BalanceIqd.ToString("N0");
            TotalDebitUsd = result.TotalDebitUsd.ToString("N2");
            TotalCreditUsd = result.TotalCreditUsd.ToString("N2");
            BalanceUsd = result.BalanceUsd.ToString("N2");
            TransactionCount = result.TransactionCount.ToString("N0");
            PeriodLabel = BuildPeriodLabel();

            _allRows = result.Rows;
            CurrentPage = 1;
            ApplyPage();
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

    private void ApplyPage()
    {
        var filtered = ColumnFilterEngine.Apply(_allRows, ColumnFilters);
        PaginationHelper.ApplyPage(filtered, Rows, CurrentPage, PageSize,
            out var totalRecords, out var totalPages, out var paginationText);
        TotalRecords = totalRecords;
        TotalPages = Math.Max(1, totalPages);
        PaginationText = paginationText;
    }

    protected override void OnColumnFiltersChanged()
    {
        CurrentPage = 1;
        ApplyPage();
    }

    [RelayCommand]
    private void FirstPage() { CurrentPage = 1; ApplyPage(); }

    [RelayCommand]
    private void PreviousPage() { if (CurrentPage > 1) { CurrentPage--; ApplyPage(); } }

    [RelayCommand]
    private void NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; ApplyPage(); } }

    [RelayCommand]
    private void LastPage() { CurrentPage = TotalPages; ApplyPage(); }

    [RelayCommand]
    private void ExportToExcel()
    {
        if (_allRows.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("لا توجد بيانات للتصدير");
            return;
        }

        var cols = new[] { "التاريخ", "البيان", "النوع", "العملة", "مدين", "دائن", "الرصيد" };
        var rows = _allRows.Select(r => new object[]
        {
            r.Date.ToString("yyyy/MM/dd"),
            r.Description,
            r.TransactionTypeLabel,
            r.CurrencyLabel,
            r.Debit,
            r.Credit,
            r.RunningBalance
        }).ToList();
        ListTableExportHelper.ExportExcel(
            _exportService, toast: null, CanExport,
            $"كشف_حساب_تخليص_{CustomerName}",
            $"كشف حساب {CustomerName}",
            cols,
            rows);
    }

    [RelayCommand]
    private void Print()
    {
        if (!CanPrint)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية الطباعة");
            return;
        }

        if (_allRows.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("لا توجد بيانات للطباعة");
            return;
        }

        var cols = new[] { "التاريخ", "البيان", "النوع", "العملة", "مدين", "دائن", "الرصيد" };
        IList<object[]> rows = _allRows.Select(r => new object[]
        {
            r.Date.ToString("yyyy/MM/dd"),
            r.Description,
            r.TransactionTypeLabel,
            r.CurrencyLabel,
            r.Debit.ToString("N0"),
            r.Credit.ToString("N0"),
            r.RunningBalance.ToString("N0")
        }).ToList();

        var model = _printService.BuildCustomerStatement(
            CustomerName,
            SelectedCustomer?.Phone,
            DateFrom,
            DateTo,
            cols,
            rows,
            BuildSummaryLines());
        model.Title = $"{model.Title} — {PeriodLabel}";
        _printService.PrintCustomerStatement(model);
    }

    [RelayCommand]
    private void ExportPdf()
    {
        if (!CanExport)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية التصدير");
            return;
        }

        if (_allRows.Count == 0 || SelectedCustomer is null)
        {
            BeautifulMessageDialog.ShowWarning("يرجى تحميل كشف الحساب أولاً");
            return;
        }

        try
        {
            var cols = new[] { "التاريخ", "البيان", "النوع", "العملة", "مدين", "دائن", "الرصيد" };
            IList<object[]> rows = _allRows.Select(r => new object[]
            {
                r.Date.ToString("yyyy/MM/dd"),
                r.Description,
                r.TransactionTypeLabel,
                r.CurrencyLabel,
                r.Debit,
                r.Credit,
                r.RunningBalance
            }).ToList();

            var model = _printService.BuildCustomerStatement(
                CustomerName,
                SelectedCustomer.Phone,
                DateFrom,
                DateTo,
                cols,
                rows,
                BuildSummaryLines());

            var temp = _printService.ExportCustomerStatementPdf(model);
            var dlg = new SaveFileDialog
            {
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = $"كشف_حساب_{CustomerName}_{DateTime.Now:yyyyMMdd}.pdf",
                DefaultExt = ".pdf"
            };
            if (dlg.ShowDialog() == true)
            {
                File.Copy(temp, dlg.FileName, overwrite: true);
                BeautifulMessageDialog.ShowSuccess("تم حفظ ملف PDF");
            }
            else
            {
                Process.Start(new ProcessStartInfo { FileName = temp, UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    private List<string> BuildSummaryLines() =>
    [
        $"الفترة: {PeriodLabel}",
        $"عدد الحركات: {TransactionCount}",
        $"مدين د.ع: {TotalDebitIqd} — دائن د.ع: {TotalCreditIqd} — الرصيد د.ع: {BalanceIqd}",
        $"مدين $: {TotalDebitUsd} — دائن $: {TotalCreditUsd} — الرصيد $: {BalanceUsd}"
    ];

    private string BuildPeriodLabel()
    {
        if (DateFrom is null && DateTo is null) return "جميع الفترات";
        if (DateFrom is not null && DateTo is not null)
            return $"{DateFrom:yyyy/MM/dd} — {DateTo:yyyy/MM/dd}";
        if (DateFrom is not null) return $"من {DateFrom:yyyy/MM/dd}";
        return $"حتى {DateTo:yyyy/MM/dd}";
    }
}

public sealed record CustomsStatementTypeOption(CustomsStatementTransactionType Value, string Label);
