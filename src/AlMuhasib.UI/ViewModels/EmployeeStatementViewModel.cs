using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class EmployeeStatementViewModel : ReportViewModelBase
{
    private readonly IWhatsAppShareService _whatsAppShare;

    [ObservableProperty] private string _employeeName = "—";
    [ObservableProperty] private string _totalPayments = "0";
    [ObservableProperty] private string _totalReceipts = "0";
    [ObservableProperty] private string _balance = "0";
    [ObservableProperty] private string _transactionCount = "0";
    [ObservableProperty] private string _periodLabel = "جميع الفترات";

    [ObservableProperty] private Employee? _selectedEmployee;
    [ObservableProperty] private string _employeeSearchText = string.Empty;

    public ObservableCollection<Employee> Employees { get; } = [];
    public ObservableCollection<Employee> FilteredEmployees { get; } = [];
    public ObservableCollection<EmployeeStatementRow> Rows { get; } = [];

    private List<EmployeeStatementRow> _allRows = [];

    public EmployeeStatementViewModel(
        IReportService reportService,
        IUnitOfWork unitOfWork,
        IExportService exportService,
        ICurrentUserService currentUserService,
        IWhatsAppShareService whatsAppShare)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        _whatsAppShare = whatsAppShare;
        PageTitle = "كشف حساب موظف";
        DateFrom = null;
        DateTo = null;
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "EmployeeStatement");
        Employees.Clear();
        FilteredEmployees.Clear();
        foreach (var e in (await _unitOfWork.Employees.GetAllAsync()).OrderBy(x => x.Name))
        {
            Employees.Add(e);
            FilteredEmployees.Add(e);
        }
    }

    partial void OnSelectedEmployeeChanged(Employee? value)
    {
        if (value is not null)
            EmployeeSearchText = value.Name;
    }

    partial void OnEmployeeSearchTextChanged(string value)
    {
        if (SelectedEmployee is not null && SelectedEmployee.Name == value)
            return;

        SelectedEmployee = null;
        FilteredEmployees.Clear();
        var term = value?.Trim() ?? string.Empty;
        foreach (var e in Employees.Where(x =>
                     string.IsNullOrEmpty(term) ||
                     x.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                     (x.Phone != null && x.Phone.Contains(term))))
            FilteredEmployees.Add(e);
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (SelectedEmployee is null)
        {
            BeautifulMessageDialog.ShowWarning("يرجى اختيار موظف من القائمة");
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
            var emp = SelectedEmployee;
            var vouchers = (await _unitOfWork.Vouchers.FindAsync(v => v.EmployeeId == emp.Id)).ToList();

            if (DateFrom.HasValue)
                vouchers = vouchers.Where(v => v.Date.Date >= DateFrom.Value.Date).ToList();
            if (DateTo.HasValue)
                vouchers = vouchers.Where(v => v.Date.Date <= DateTo.Value.Date).ToList();

            vouchers = vouchers.OrderBy(v => v.Date).ThenBy(v => v.Id).ToList();

            var dual = EmployeeBalanceHelper.ComputeAdvanceBalances(
                emp.OpeningBalance,
                emp.OpeningBalanceCurrency,
                vouchers.Select(v => (v.Currency, v.VoucherType, v.Amount)));

            var rows = new List<EmployeeStatementRow>();
            decimal paymentsIqd = 0, paymentsUsd = 0, receiptsIqd = 0, receiptsUsd = 0;

            foreach (var currency in new[] { AccountingCurrency.IQD, AccountingCurrency.USD })
            {
                var opening = emp.OpeningBalanceCurrency == currency ? emp.OpeningBalance : 0m;
                var currencyVouchers = vouchers.Where(v => v.Currency == currency).ToList();
                if (opening == 0 && currencyVouchers.Count == 0)
                    continue;

                var running = opening;
                rows.Add(new EmployeeStatementRow
                {
                    Date = DateFrom ?? emp.HireDate,
                    Description = $"رصيد افتتاحي (سلفة) — {AccountingCurrencyHelper.GetDisplayName(currency)}",
                    Debit = opening > 0 ? opening : 0,
                    Credit = opening < 0 ? Math.Abs(opening) : 0,
                    Balance = running,
                    VoucherNumber = "—",
                    Currency = currency,
                    CurrencyLabel = AccountingCurrencyHelper.GetLabel(currency)
                });

                foreach (var v in currencyVouchers)
                {
                    if (EmployeeBalanceHelper.IsEmployeePayment(v.VoucherType))
                    {
                        if (currency == AccountingCurrency.IQD) paymentsIqd += v.Amount;
                        else paymentsUsd += v.Amount;
                        running += v.Amount;
                        rows.Add(new EmployeeStatementRow
                        {
                            Date = v.Date,
                            Description = "سند دفع (سلفة)",
                            Debit = v.Amount,
                            Credit = 0,
                            Balance = running,
                            VoucherNumber = v.VoucherNumber,
                            Notes = v.Notes,
                            Currency = currency,
                            CurrencyLabel = AccountingCurrencyHelper.GetLabel(currency)
                        });
                    }
                    else if (EmployeeBalanceHelper.IsEmployeeReceipt(v.VoucherType))
                    {
                        if (currency == AccountingCurrency.IQD) receiptsIqd += v.Amount;
                        else receiptsUsd += v.Amount;
                        running -= v.Amount;
                        rows.Add(new EmployeeStatementRow
                        {
                            Date = v.Date,
                            Description = "سند قبض من موظف",
                            Debit = 0,
                            Credit = v.Amount,
                            Balance = running,
                            VoucherNumber = v.VoucherNumber,
                            Notes = v.Notes,
                            Currency = currency,
                            CurrencyLabel = AccountingCurrencyHelper.GetLabel(currency)
                        });
                    }
                }
            }

            _allRows = rows;
            Rows.Clear();
            foreach (var r in _allRows)
                Rows.Add(r);

            EmployeeName = emp.Name;
            TotalPayments = FormatDual(
                paymentsIqd + (emp.OpeningBalanceCurrency == AccountingCurrency.IQD ? Math.Max(0, emp.OpeningBalance) : 0),
                paymentsUsd + (emp.OpeningBalanceCurrency == AccountingCurrency.USD ? Math.Max(0, emp.OpeningBalance) : 0));
            TotalReceipts = FormatDual(receiptsIqd, receiptsUsd);
            Balance = FormatDual(dual.Iqd, dual.Usd);
            TransactionCount = vouchers.Count.ToString("N0");
            PeriodLabel = BuildPeriodLabel();
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

    private static string FormatDual(decimal iqd, decimal usd)
    {
        if (usd == 0)
            return AccountingCurrencyHelper.Format(iqd, AccountingCurrency.IQD);
        if (iqd == 0)
            return AccountingCurrencyHelper.Format(usd, AccountingCurrency.USD);
        return $"{AccountingCurrencyHelper.Format(iqd, AccountingCurrency.IQD)} | {AccountingCurrencyHelper.Format(usd, AccountingCurrency.USD)}";
    }

    [RelayCommand]
    private void ShareWhatsApp()
    {
        if (SelectedEmployee is null || _allRows.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("حمّل الكشف أولاً");
            return;
        }

        var model = new StatementPrintModel
        {
            Title = $"كشف حساب موظف — {EmployeeName}",
            PartyName = SelectedEmployee.Name,
            PartyPhone = SelectedEmployee.Phone,
            FromDate = DateFrom,
            ToDate = DateTo,
            Columns = ["التاريخ", "العملة", "البيان", "مدين", "دائن", "الرصيد", "رقم السند"],
            Rows = _allRows.Select(r => new object[]
            {
                r.Date.ToString("yyyy/MM/dd"),
                r.CurrencyLabel,
                r.Description,
                r.Debit,
                r.Credit,
                r.Balance,
                r.VoucherNumber
            }).ToList(),
            SummaryLines =
            [
                $"الفترة: {PeriodLabel}",
                $"عدد الحركات: {TransactionCount}",
                $"إجمالي الدفع (سلف): {TotalPayments}",
                $"إجمالي القبض: {TotalReceipts}",
                $"رصيد السلفة: {Balance}"
            ]
        };

        _whatsAppShare.ShareStatement(model, SelectedEmployee.Phone, SelectedEmployee.Name);
    }

    [RelayCommand]
    private void Print()
    {
        if (_allRows.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("حمّل الكشف أولاً");
            return;
        }

        var columns = new[] { "التاريخ", "العملة", "الوصف", "مدين", "دائن", "الرصيد", "رقم السند" };
        IList<object[]> rows = _allRows.Select(r => new object[]
        {
            r.Date.ToString("yyyy/MM/dd"),
            r.CurrencyLabel,
            r.Description,
            r.Currency == AccountingCurrency.USD ? r.Debit.ToString("N2") : r.Debit.ToString("N0"),
            r.Currency == AccountingCurrency.USD ? r.Credit.ToString("N2") : r.Credit.ToString("N0"),
            r.Currency == AccountingCurrency.USD ? r.Balance.ToString("N2") : r.Balance.ToString("N0"),
            r.VoucherNumber
        }).ToList();
        _exportService.PrintTable($"كشف حساب موظف — {EmployeeName}", columns, rows);
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

public sealed class EmployeeStatementRow
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
    public string VoucherNumber { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public AccountingCurrency Currency { get; set; } = AccountingCurrency.IQD;
    public string CurrencyLabel { get; set; } = AccountingCurrencyHelper.IqdLabel;
}
