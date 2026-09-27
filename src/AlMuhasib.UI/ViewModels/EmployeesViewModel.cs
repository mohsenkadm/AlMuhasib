using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
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
    [ObservableProperty] private string _editNotes = string.Empty;
    [ObservableProperty] private string _dialogError = string.Empty;

    [ObservableProperty] private bool _isDeleteDialogOpen;
    [ObservableProperty] private EmployeeListRow? _employeeToDelete;

    [ObservableProperty] private int _activeCount;
    [ObservableProperty] private int _inactiveCount;
    [ObservableProperty] private decimal _totalAdvances;

    private int? _editingId;
    private System.Timers.Timer? _debounceTimer;

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
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            LoadPermissions(_currentUserService, "Employees");
            await LoadAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadAsync()
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

        TotalCount = totalCount;
        TotalPages = PaginationHelper.ComputeTotalPages(totalCount, PageSize);
        PaginationText = PaginationHelper.BuildPaginationText(totalCount, CurrentPage, PageSize);

        var vouchers = await _unitOfWork.Vouchers.FindAsync(v => v.EmployeeId != null);
        var voucherList = vouchers.ToList();

        Employees.Clear();
        foreach (var emp in items)
        {
            var payments = voucherList
                .Where(v => v.EmployeeId == emp.Id && v.VoucherType == VoucherType.Payment)
                .Sum(v => v.Amount);
            var receipts = voucherList
                .Where(v => v.EmployeeId == emp.Id && v.VoucherType == VoucherType.Receipt)
                .Sum(v => v.Amount);
            var balance = EmployeeBalanceHelper.ComputeAdvanceBalance(emp.OpeningBalance, payments, receipts);
            Employees.Add(new EmployeeListRow(emp, balance));
        }

        var all = (await _unitOfWork.Employees.GetAllAsync()).ToList();
        ActiveCount = all.Count(e => e.IsActive);
        InactiveCount = all.Count(e => !e.IsActive);
        TotalAdvances = all.Sum(emp =>
        {
            var payments = voucherList
                .Where(v => v.EmployeeId == emp.Id && v.VoucherType == VoucherType.Payment)
                .Sum(v => v.Amount);
            var receipts = voucherList
                .Where(v => v.EmployeeId == emp.Id && v.VoucherType == VoucherType.Receipt)
                .Sum(v => v.Amount);
            return EmployeeBalanceHelper.ComputeAdvanceBalance(emp.OpeningBalance, payments, receipts);
        });
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
        await LoadAsync();
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
        EditOpeningBalance = emp.OpeningBalance.ToString("0");
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

        DialogError = string.Empty;
        try
        {
            if (IsEditMode && _editingId.HasValue)
            {
                var emp = await _unitOfWork.Employees.GetByIdAsync(_editingId.Value);
                if (emp is null) return;
                ApplyFields(emp, opening);
                emp.UpdatedAt = DateTime.UtcNow;
                emp.UpdatedBy = _currentUserService.Username;
                _unitOfWork.Employees.Update(emp);
                await _unitOfWork.SaveChangesAsync();
            }
            else
            {
                var emp = new Employee { CreatedBy = _currentUserService.Username };
                ApplyFields(emp, opening);
                await _unitOfWork.Employees.AddAsync(emp);
                await _unitOfWork.SaveChangesAsync();
            }

            IsDialogOpen = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            DialogError = $"حدث خطأ: {ex.Message}";
        }
    }

    private void ApplyFields(Employee emp, decimal opening)
    {
        emp.Name = EditName.Trim();
        emp.Phone = string.IsNullOrWhiteSpace(EditPhone) ? null : EditPhone.Trim();
        emp.Address = string.IsNullOrWhiteSpace(EditAddress) ? null : EditAddress.Trim();
        emp.JobTitle = string.IsNullOrWhiteSpace(EditJobTitle) ? null : EditJobTitle.Trim();
        emp.HireDate = EditHireDate.Date;
        emp.IsActive = EditIsActive;
        emp.OpeningBalance = opening;
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
        try
        {
            _unitOfWork.Employees.SoftDelete(EmployeeToDelete.Employee, _currentUserService.Username);
            await _unitOfWork.SaveChangesAsync();
            IsDeleteDialogOpen = false;
            EmployeeToDelete = null;
            await LoadAsync();
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
                رصيد_السلفة = r.AdvanceBalance.ToString("N0"),
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
            var columns = new[] { "الاسم", "الهاتف", "المسمى", "تاريخ التعيين", "الحالة", "رصيد السلفة" };
            IList<object[]> rows = Employees.Select(r => new object[]
            {
                r.Name,
                r.Phone ?? "",
                r.JobTitle ?? "",
                r.HireDate.ToString("yyyy/MM/dd"),
                r.IsActive ? "فعال" : "غير فعال",
                r.AdvanceBalance.ToString("N0")
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
    public EmployeeListRow(Employee employee, decimal advanceBalance)
    {
        Employee = employee;
        AdvanceBalance = advanceBalance;
    }

    public Employee Employee { get; }
    public int Id => Employee.Id;
    public string Name => Employee.Name;
    public string? Phone => Employee.Phone;
    public string? JobTitle => Employee.JobTitle;
    public string? Address => Employee.Address;
    public DateTime HireDate => Employee.HireDate;
    public bool IsActive => Employee.IsActive;
    public string? Notes => Employee.Notes;
    public decimal OpeningBalance => Employee.OpeningBalance;
    public decimal AdvanceBalance { get; }
}
