using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels;

public partial class EmployeesViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserPreferencesService _userPreferences;

    public ObservableCollection<EmployeeListRow> Employees { get; } = [];
    public ObservableCollection<CurrencyOption> CurrencyOptions { get; } = new(CurrencyOption.All);

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages;
    [ObservableProperty] private string _paginationText = string.Empty;
    [ObservableProperty] private EmployeeListRow? _selectedEmployee;
    [ObservableProperty] private bool _isCardView;
    [ObservableProperty] private string _statusFilter = "الكل";

    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private string _dialogTitle = string.Empty;
    [ObservableProperty] private string _editName = string.Empty;
    [ObservableProperty] private string _editPhone = string.Empty;
    [ObservableProperty] private string _editAddress = string.Empty;
    [ObservableProperty] private string _editJobTitle = string.Empty;
    [ObservableProperty] private DateTime _editHireDate = DateTime.Today;
    [ObservableProperty] private bool _editIsActive = true;
    [ObservableProperty] private string _editOpeningBalance = string.Empty;
    [ObservableProperty] private CurrencyOption? _editOpeningCurrencyOption;
    [ObservableProperty] private string _editNotes = string.Empty;
    [ObservableProperty] private string _dialogError = string.Empty;

    [ObservableProperty] private bool _isDeleteDialogOpen;
    [ObservableProperty] private EmployeeListRow? _employeeToDelete;

    [ObservableProperty] private int _activeCount;
    [ObservableProperty] private int _inactiveCount;
    [ObservableProperty] private string _totalAdvances = "0";

    private int? _editingId;
    private System.Timers.Timer? _debounceTimer;
    private decimal _totalAdvancesIqd;
    private decimal _totalAdvancesUsd;

    public string[] StatusFilters { get; } = ["الكل", "فعال", "غير فعال"];

    public EmployeesViewModel(
        IUnitOfWork unitOfWork,
        IExportService exportService,
        ICurrentUserService currentUserService,
        IUserPreferencesService userPreferences)
    {
        _unitOfWork = unitOfWork;
        _exportService = exportService;
        _currentUserService = currentUserService;
        _userPreferences = userPreferences;
        IsCardView = ListViewModeHelper.LoadIsCardView(_userPreferences, ListViewModeKeys.Employees);
        PageTitle = "الموظفون";
        EditOpeningCurrencyOption = CurrencyOptions.FirstOrDefault(c => c.Currency == AccountingCurrency.IQD);
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            LoadPermissions(_currentUserService, "Employees");
            await LoadAsync(refreshTotals: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadAsync(bool refreshTotals = false)
    {
        var filter = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();
        System.Linq.Expressions.Expression<Func<Employee, bool>>? searchPredicate = filter is null
            ? null
            : e => e.Name.Contains(filter)
                   || (e.Phone != null && e.Phone.Contains(filter))
                   || (e.JobTitle != null && e.JobTitle.Contains(filter))
                   || (e.Address != null && e.Address.Contains(filter));

        if (StatusFilter == "فعال")
        {
            searchPredicate = filter is null
                ? e => e.IsActive
                : e => e.IsActive && (e.Name.Contains(filter!)
                                      || (e.Phone != null && e.Phone.Contains(filter!))
                                      || (e.JobTitle != null && e.JobTitle.Contains(filter!))
                                      || (e.Address != null && e.Address.Contains(filter!)));
        }
        else if (StatusFilter == "غير فعال")
        {
            searchPredicate = filter is null
                ? e => !e.IsActive
                : e => !e.IsActive && (e.Name.Contains(filter!)
                                       || (e.Phone != null && e.Phone.Contains(filter!))
                                       || (e.JobTitle != null && e.JobTitle.Contains(filter!))
                                       || (e.Address != null && e.Address.Contains(filter!)));
        }

        var (items, totalCount) = await _unitOfWork.Employees.GetPagedAsync(
            CurrentPage, PageSize, searchPredicate, q => q.OrderByDescending(e => e.CreatedAt));
        var itemsList = items as IList<Employee> ?? items.ToList();

        TotalCount = totalCount;
        TotalPages = PaginationHelper.ComputeTotalPages(totalCount, PageSize);
        PaginationText = PaginationHelper.BuildPaginationText(totalCount, CurrentPage, PageSize);

        ActiveCount = await _unitOfWork.Employees.CountAsync(e => e.IsActive);
        InactiveCount = await _unitOfWork.Employees.CountAsync(e => !e.IsActive);

        // سندات الصفحة الحالية فقط — تجنّب تحميل كل السندات عند كل حفظ/تحديث
        var pageIds = itemsList.Select(e => e.Id).ToList();
        var pageVouchers = pageIds.Count == 0
            ? []
            : (await _unitOfWork.Vouchers.FindAsync(v =>
                v.EmployeeId != null && pageIds.Contains(v.EmployeeId.Value))).ToList();
        var vouchersByEmp = GroupVouchersByEmployee(pageVouchers);

        Employees.Clear();
        foreach (var emp in itemsList)
        {
            var rows = vouchersByEmp.GetValueOrDefault(emp.Id);
            var balance = EmployeeBalanceHelper.ComputeAdvanceBalances(
                emp.OpeningBalance, emp.OpeningBalanceCurrency, rows ?? EmptyVoucherRows);
            Employees.Add(new EmployeeListRow(emp, balance));
        }

        if (refreshTotals)
            await RefreshAdvanceTotalsAsync();
    }

    private static readonly List<(AccountingCurrency Currency, VoucherType Type, decimal Amount)> EmptyVoucherRows = [];

    private static Dictionary<int, List<(AccountingCurrency Currency, VoucherType Type, decimal Amount)>> GroupVouchersByEmployee(
        IEnumerable<Voucher> vouchers) =>
        vouchers
            .Where(v => v.EmployeeId is not null)
            .GroupBy(v => v.EmployeeId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.Select(v => (v.Currency, v.VoucherType, v.Amount)).ToList());

    private async Task RefreshAdvanceTotalsAsync()
    {
        var all = (await _unitOfWork.Employees.GetAllAsync()).ToList();
        if (all.Count == 0)
        {
            SetTotalAdvances(0, 0);
            return;
        }

        var allVouchers = (await _unitOfWork.Vouchers.FindAsync(v => v.EmployeeId != null)).ToList();
        var vouchersByEmp = GroupVouchersByEmployee(allVouchers);

        decimal totalIqd = 0;
        decimal totalUsd = 0;
        foreach (var emp in all)
        {
            var rows = vouchersByEmp.GetValueOrDefault(emp.Id);
            var bal = EmployeeBalanceHelper.ComputeAdvanceBalances(
                emp.OpeningBalance, emp.OpeningBalanceCurrency, rows ?? EmptyVoucherRows);
            totalIqd += bal.Iqd;
            totalUsd += bal.Usd;
        }

        SetTotalAdvances(totalIqd, totalUsd);
    }

    private void SetTotalAdvances(decimal totalIqd, decimal totalUsd)
    {
        _totalAdvancesIqd = totalIqd;
        _totalAdvancesUsd = totalUsd;
        TotalAdvances = totalUsd == 0
            ? AccountingCurrencyHelper.Format(totalIqd, AccountingCurrency.IQD)
            : $"{AccountingCurrencyHelper.Format(totalIqd, AccountingCurrency.IQD)} | {AccountingCurrencyHelper.Format(totalUsd, AccountingCurrency.USD)}";
    }

    private void AdjustTotalAdvances(DualCurrencyBalance? previous, DualCurrencyBalance next)
    {
        if (previous is not null)
        {
            _totalAdvancesIqd -= previous.Value.Iqd;
            _totalAdvancesUsd -= previous.Value.Usd;
        }

        _totalAdvancesIqd += next.Iqd;
        _totalAdvancesUsd += next.Usd;
        SetTotalAdvances(_totalAdvancesIqd, _totalAdvancesUsd);
    }

    private async Task<DualCurrencyBalance> ComputeBalanceForEmployeeAsync(Employee emp)
    {
        var vouchers = (await _unitOfWork.Vouchers.FindAsync(v => v.EmployeeId == emp.Id)).ToList();
        var rows = vouchers.Select(v => (v.Currency, v.VoucherType, v.Amount));
        return EmployeeBalanceHelper.ComputeAdvanceBalances(
            emp.OpeningBalance, emp.OpeningBalanceCurrency, rows);
    }

    private void UpsertEmployeeRow(Employee emp, DualCurrencyBalance balance)
    {
        var row = new EmployeeListRow(emp, balance);
        for (var i = 0; i < Employees.Count; i++)
        {
            if (Employees[i].Id != emp.Id) continue;
            Employees[i] = row;
            if (SelectedEmployee?.Id == emp.Id)
                SelectedEmployee = row;
            return;
        }

        // موظف جديد: يظهر فوراً في الصفحة الأولى إن كان يطابق الفلتر
        if (CurrentPage == 1 && MatchesCurrentFilter(emp))
            Employees.Insert(0, row);
    }

    private bool MatchesCurrentFilter(Employee emp)
    {
        if (StatusFilter == "فعال" && !emp.IsActive) return false;
        if (StatusFilter == "غير فعال" && emp.IsActive) return false;

        if (string.IsNullOrWhiteSpace(SearchText)) return true;
        var filter = SearchText.Trim();
        return emp.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
               || (emp.Phone?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)
               || (emp.JobTitle?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)
               || (emp.Address?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    partial void OnSearchTextChanged(string value)
    {
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();
        _debounceTimer = new System.Timers.Timer(400);
        _debounceTimer.Elapsed += async (_, _) =>
        {
            _debounceTimer?.Stop();
            CurrentPage = 1;
            await Application.Current.Dispatcher.InvokeAsync(async () => await LoadAsync());
        };
        _debounceTimer.AutoReset = false;
        _debounceTimer.Start();
    }

    partial void OnStatusFilterChanged(string value)
    {
        CurrentPage = 1;
        _ = LoadAsync();
    }

    [RelayCommand] private async Task FirstPage() { CurrentPage = 1; await LoadAsync(); }
    [RelayCommand] private async Task PreviousPage() { if (CurrentPage > 1) { CurrentPage--; await LoadAsync(); } }
    [RelayCommand] private async Task NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; await LoadAsync(); } }
    [RelayCommand] private async Task LastPage() { CurrentPage = TotalPages; await LoadAsync(); }

    [RelayCommand]
    private async Task Refresh()
    {
        CurrentPage = 1;
        SearchText = string.Empty;
        StatusFilter = "الكل";
        await LoadAsync(refreshTotals: true);
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        _editingId = null;
        IsEditMode = false;
        DialogTitle = "إضافة موظف جديد";
        EditName = string.Empty;
        EditPhone = string.Empty;
        EditAddress = string.Empty;
        EditJobTitle = string.Empty;
        EditHireDate = DateTime.Today;
        EditIsActive = true;
        EditOpeningBalance = string.Empty;
        EditOpeningCurrencyOption = CurrencyOptions.FirstOrDefault(c => c.Currency == AccountingCurrency.IQD);
        EditNotes = string.Empty;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(EmployeeListRow? row)
    {
        if (row?.Employee is null) return;
        var emp = row.Employee;
        _editingId = emp.Id;
        IsEditMode = true;
        DialogTitle = "تعديل بيانات الموظف";
        EditName = emp.Name;
        EditPhone = emp.Phone ?? string.Empty;
        EditAddress = emp.Address ?? string.Empty;
        EditJobTitle = emp.JobTitle ?? string.Empty;
        EditHireDate = emp.HireDate;
        EditIsActive = emp.IsActive;
        EditOpeningBalance = emp.OpeningBalance.ToString("0.##");
        EditOpeningCurrencyOption = CurrencyOptions.FirstOrDefault(c => c.Currency == emp.OpeningBalanceCurrency)
            ?? CurrencyOptions.FirstOrDefault(c => c.Currency == AccountingCurrency.IQD);
        EditNotes = emp.Notes ?? string.Empty;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task SaveEmployee()
    {
        if (string.IsNullOrWhiteSpace(EditName))
        {
            DialogError = "اسم الموظف مطلوب";
            return;
        }

        decimal opening = 0;
        if (!string.IsNullOrWhiteSpace(EditOpeningBalance))
        {
            if (!decimal.TryParse(EditOpeningBalance.Replace(",", ""), out opening) || opening < 0)
            {
                DialogError = "الرصيد الافتتاحي غير صالح";
                return;
            }
        }

        var openingCurrency = EditOpeningCurrencyOption?.Currency ?? AccountingCurrency.IQD;
        opening = AccountingCurrencyHelper.NormalizeAmount(opening, openingCurrency);

        DialogError = string.Empty;
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            DualCurrencyBalance? previousBalance = null;
            bool? wasActive = null;
            Employee saved;

            if (IsEditMode && _editingId.HasValue)
            {
                var emp = await _unitOfWork.Employees.GetByIdAsync(_editingId.Value);
                if (emp is null) return;

                wasActive = emp.IsActive;
                var oldRow = Employees.FirstOrDefault(r => r.Id == emp.Id);
                if (oldRow is not null)
                    previousBalance = new DualCurrencyBalance(oldRow.AdvanceBalanceIqd, oldRow.AdvanceBalanceUsd);

                ApplyFields(emp, opening, openingCurrency);
                emp.UpdatedAt = DateTime.UtcNow;
                emp.UpdatedBy = _currentUserService.Username;

                // Update يحفظ بشكل متزامن — خارج خيط الواجهة لتفادي التجمد
                await Task.Run(() => _unitOfWork.Employees.Update(emp));
                await _unitOfWork.SaveChangesAsync();
                saved = emp;
            }
            else
            {
                var emp = new Employee { CreatedBy = _currentUserService.Username };
                ApplyFields(emp, opening, openingCurrency);
                await _unitOfWork.Employees.AddAsync(emp);
                await _unitOfWork.SaveChangesAsync();
                saved = emp;
                TotalCount++;
                TotalPages = PaginationHelper.ComputeTotalPages(TotalCount, PageSize);
                PaginationText = PaginationHelper.BuildPaginationText(TotalCount, CurrentPage, PageSize);
            }

            IsDialogOpen = false;

            if (wasActive is not null && wasActive.Value != saved.IsActive)
            {
                if (saved.IsActive) { ActiveCount++; InactiveCount = Math.Max(0, InactiveCount - 1); }
                else { InactiveCount++; ActiveCount = Math.Max(0, ActiveCount - 1); }
            }
            else if (wasActive is null)
            {
                if (saved.IsActive) ActiveCount++;
                else InactiveCount++;
            }

            // سندات هذا الموظف فقط + تحديث الصف محلياً (بدون إعادة تحميل الصفحة/كل السندات)
            var balance = await ComputeBalanceForEmployeeAsync(saved);
            UpsertEmployeeRow(saved, balance);
            AdjustTotalAdvances(previousBalance, balance);

            // إن أصبح لا يطابق فلتر الحالة الحالي أزِله من الجدول
            if (!MatchesCurrentFilter(saved))
            {
                var stale = Employees.FirstOrDefault(r => r.Id == saved.Id);
                if (stale is not null) Employees.Remove(stale);
            }
        }
        catch (Exception ex)
        {
            DialogError = $"حدث خطأ: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFields(Employee emp, decimal opening, AccountingCurrency openingCurrency)
    {
        emp.Name = EditName.Trim();
        emp.Phone = string.IsNullOrWhiteSpace(EditPhone) ? null : EditPhone.Trim();
        emp.Address = string.IsNullOrWhiteSpace(EditAddress) ? null : EditAddress.Trim();
        emp.JobTitle = string.IsNullOrWhiteSpace(EditJobTitle) ? null : EditJobTitle.Trim();
        emp.HireDate = EditHireDate.Date;
        emp.IsActive = EditIsActive;
        emp.OpeningBalance = opening;
        emp.OpeningBalanceCurrency = openingCurrency;
        emp.Notes = string.IsNullOrWhiteSpace(EditNotes) ? null : EditNotes.Trim();
    }

    [RelayCommand] private void CancelDialog() => IsDialogOpen = false;

    [RelayCommand]
    private void ConfirmDelete(EmployeeListRow? row)
    {
        if (row is null) return;
        EmployeeToDelete = row;
        IsDeleteDialogOpen = true;
    }

    [RelayCommand]
    private async Task ExecuteDelete()
    {
        if (EmployeeToDelete?.Employee is null) return;
        var row = EmployeeToDelete;
        try
        {
            await Task.Run(() =>
                _unitOfWork.Employees.SoftDelete(row.Employee, _currentUserService.Username));
            await _unitOfWork.SaveChangesAsync();
            IsDeleteDialogOpen = false;
            EmployeeToDelete = null;

            Employees.Remove(row);
            if (row.IsActive) ActiveCount = Math.Max(0, ActiveCount - 1);
            else InactiveCount = Math.Max(0, InactiveCount - 1);
            AdjustTotalAdvances(
                new DualCurrencyBalance(row.AdvanceBalanceIqd, row.AdvanceBalanceUsd),
                default);
            TotalCount = Math.Max(0, TotalCount - 1);
            TotalPages = PaginationHelper.ComputeTotalPages(TotalCount, PageSize);
            PaginationText = PaginationHelper.BuildPaginationText(TotalCount, CurrentPage, PageSize);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الحذف: {ex.Message}");
        }
    }

    [RelayCommand]
    private void CancelDelete()
    {
        IsDeleteDialogOpen = false;
        EmployeeToDelete = null;
    }

    [RelayCommand]
    private async Task ExportToExcel()
    {
        try
        {
            var exportData = Employees.Select(r => new
            {
                الاسم = r.Name,
                الهاتف = r.Phone ?? "",
                المسمى = r.JobTitle ?? "",
                تاريخ_التعيين = r.HireDate.ToString("yyyy/MM/dd"),
                الحالة = r.IsActive ? "فعال" : "غير فعال",
                رصيد_السلفة_دينار = r.AdvanceBalanceIqd.ToString("N0"),
                رصيد_السلفة_دولار = r.AdvanceBalanceUsd.ToString("N2"),
                ملاحظات = r.Notes ?? ""
            });

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"الموظفون_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "الموظفون");
                BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
            }
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء التصدير: {ex.Message}");
        }
    }

    [RelayCommand]
    private void PrintTable()
    {
        try
        {
            var columns = new[] { "الاسم", "الهاتف", "المسمى", "تاريخ التعيين", "الحالة", "سلفة د.ع", "سلفة $" };
            IList<object[]> rows = Employees.Select(r => new object[]
            {
                r.Name,
                r.Phone ?? "",
                r.JobTitle ?? "",
                r.HireDate.ToString("yyyy/MM/dd"),
                r.IsActive ? "فعال" : "غير فعال",
                r.AdvanceBalanceIqd.ToString("N0"),
                r.AdvanceBalanceUsd.ToString("N2")
            }).ToList();
            _exportService.PrintTable("قائمة الموظفين", columns, rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }

    partial void OnIsCardViewChanged(bool value) =>
        ListViewModeHelper.SaveIsCardView(_userPreferences, ListViewModeKeys.Employees, value);
}

public sealed class EmployeeListRow
{
    public EmployeeListRow(Employee employee, DualCurrencyBalance advanceBalance)
    {
        Employee = employee;
        AdvanceBalanceIqd = advanceBalance.Iqd;
        AdvanceBalanceUsd = advanceBalance.Usd;
    }

    public Employee Employee { get; }
    public int Id => Employee.Id;
    public string Name => Employee.Name;
    public string? Phone => Employee.Phone;
    public string? JobTitle => Employee.JobTitle;
    public string? Address => Employee.Address;
    public DateTime HireDate => Employee.HireDate;
    public bool IsActive => Employee.IsActive;
    public string StatusDisplay => Employee.IsActive ? "فعال" : "غير فعال";
    public string? Notes => Employee.Notes;
    public decimal OpeningBalance => Employee.OpeningBalance;
    public AccountingCurrency OpeningBalanceCurrency => Employee.OpeningBalanceCurrency;
    public decimal AdvanceBalanceIqd { get; }
    public decimal AdvanceBalanceUsd { get; }

    /// <summary>عرض مختصر للجدول — دينار، ويُلحق الدولار إن وُجد.</summary>
    public string AdvanceBalanceDisplay =>
        AdvanceBalanceUsd == 0
            ? AdvanceBalanceIqd.ToString("N0")
            : $"{AdvanceBalanceIqd:N0} د.ع | {AdvanceBalanceUsd:N2} $";
}
