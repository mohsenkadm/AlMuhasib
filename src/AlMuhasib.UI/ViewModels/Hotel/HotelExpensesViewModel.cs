using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AlMuhasib.UI.ViewModels.Hotel;

public partial class HotelExpensesViewModel : PagedViewModelBase
{
    private readonly IHotelExpenseService _expenseService;
    private readonly IHotelCashService _cashService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IToastNotificationService _toast;
    private readonly IExportService _exportService;

    public ObservableCollection<HotelExpense> Expenses { get; } = [];
    public ObservableCollection<HotelExpenseType> ExpenseTypes { get; } = [];
    public ObservableCollection<HotelCashBox> CashBoxes { get; } = [];
    public ObservableCollection<HotelListStatItem> Stats { get; } = [];

    [ObservableProperty] private HotelExpense? _selectedExpense;
    [ObservableProperty] private HotelExpenseType? _selectedExpenseType;
    [ObservableProperty] private DateTime? _dateFrom;
    [ObservableProperty] private DateTime? _dateTo;
    [ObservableProperty] private int? _typeFilterId;
    [ObservableProperty] private bool _isExpenseDialogOpen;
    [ObservableProperty] private bool _isTypeDialogOpen;
    [ObservableProperty] private bool _isExpenseEditMode;
    [ObservableProperty] private bool _isTypeEditMode;
    [ObservableProperty] private DateTime _editExpenseDate = DateTime.Today;
    [ObservableProperty] private decimal _editExpenseAmount;
    [ObservableProperty] private int? _editExpenseTypeId;
    [ObservableProperty] private int? _editExpenseCashBoxId;
    [ObservableProperty] private string _editExpenseDescription = string.Empty;
    [ObservableProperty] private string _editTypeName = string.Empty;

    private int? _editingExpenseId;
    private int? _editingTypeId;

    public HotelExpensesViewModel(
        IHotelExpenseService expenseService,
        IHotelCashService cashService,
        ICurrentUserService currentUserService,
        IToastNotificationService toast,
        IExportService exportService)
    {
        _expenseService = expenseService;
        _cashService = cashService;
        _currentUserService = currentUserService;
        _toast = toast;
        _exportService = exportService;
        PageTitle = "المصاريف";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, HotelPermissionRegistry.HotelExpenses);
        await LoadLookupsAsync();
        await LoadExpensesAsync();
    }

    partial void OnDateFromChanged(DateTime? value) => _ = ReloadExpensesAsync();
    partial void OnDateToChanged(DateTime? value) => _ = ReloadExpensesAsync();
    partial void OnTypeFilterIdChanged(int? value) => _ = ReloadExpensesAsync();

    protected override void OnColumnFiltersChanged() => _ = ReloadExpensesAsync();

    protected override Task OnPageChangedAsync() => LoadExpensesAsync();

    private async Task ReloadExpensesAsync()
    {
        CurrentPage = 1;
        await LoadExpensesAsync();
    }

    private async Task LoadLookupsAsync()
    {
        ExpenseTypes.Clear();
        CashBoxes.Clear();
        foreach (var t in await _expenseService.GetExpenseTypesAsync())
            ExpenseTypes.Add(t);
        foreach (var b in await _cashService.GetCashBoxesAsync())
            CashBoxes.Add(b);
    }

    [RelayCommand]
    private async Task LoadExpensesAsync()
    {
        IsBusy = true;
        try
        {
            if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
            {
                var (allItems, _) = await _expenseService.GetExpensesPagedAsync(
                    1, int.MaxValue, DateFrom, DateTo, TypeFilterId);
                var filtered = ColumnFilterEngine.Apply(allItems, ColumnFilters).ToList();
                Expenses.Clear();
                MasterDataColumnFilterHelper.ApplyClientPagination(
                    filtered, Expenses, CurrentPage, PageSize,
                    out var filteredTotal, out _, out _);
                ApplyPaginationStats(filteredTotal);
                RebuildStats(filtered);
                return;
            }

            var (items, total) = await _expenseService.GetExpensesPagedAsync(
                CurrentPage, PageSize, DateFrom, DateTo, TypeFilterId);
            Expenses.Clear();
            foreach (var e in items)
                Expenses.Add(e);
            ApplyPaginationStats(total);
            RebuildStats(items);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RebuildStats(IEnumerable<HotelExpense> items)
    {
        var list = items.ToList();
        Stats.Clear();
        Stats.Add(new HotelListStatItem { Label = "عدد المصاريف", Value = list.Count.ToString("N0"), AccentColor = "#1565C0" });
        Stats.Add(new HotelListStatItem { Label = "إجمالي المبالغ", Value = list.Sum(e => e.Amount).ToString("N0"), AccentColor = "#C62828" });
    }

    [RelayCommand]
    private void OpenAddExpenseDialog()
    {
        if (!CanAdd) return;
        _editingExpenseId = null;
        IsExpenseEditMode = false;
        EditExpenseDate = DateTime.Today;
        EditExpenseAmount = 0;
        EditExpenseTypeId = ExpenseTypes.FirstOrDefault()?.Id;
        EditExpenseCashBoxId = CashBoxes.FirstOrDefault()?.Id;
        EditExpenseDescription = string.Empty;
        IsExpenseDialogOpen = true;
    }

    [RelayCommand]
    private async Task OpenEditExpenseDialogAsync(HotelExpense? expense)
    {
        expense ??= SelectedExpense;
        if (expense is null || !CanEdit) return;

        var entity = await _expenseService.GetExpenseByIdAsync(expense.Id);
        if (entity is null) return;

        _editingExpenseId = entity.Id;
        IsExpenseEditMode = true;
        EditExpenseDate = entity.ExpenseDate;
        EditExpenseAmount = entity.Amount;
        EditExpenseTypeId = entity.HotelExpenseTypeId;
        EditExpenseCashBoxId = entity.HotelCashBoxId;
        EditExpenseDescription = entity.Description;
        IsExpenseDialogOpen = true;
    }

    [RelayCommand]
    private void CloseExpenseDialog() => IsExpenseDialogOpen = false;

    [RelayCommand]
    private async Task SaveExpenseAsync()
    {
        if (!EditExpenseTypeId.HasValue || EditExpenseAmount <= 0)
        {
            _toast.ShowWarning("أكمل بيانات المصروف");
            return;
        }

        try
        {
            if (_editingExpenseId.HasValue)
            {
                var entity = await _expenseService.GetExpenseByIdAsync(_editingExpenseId.Value)
                    ?? throw new InvalidOperationException("المصروف غير موجود");
                entity.ExpenseDate = EditExpenseDate;
                entity.Amount = EditExpenseAmount;
                entity.HotelExpenseTypeId = EditExpenseTypeId.Value;
                entity.HotelCashBoxId = EditExpenseCashBoxId;
                entity.Description = EditExpenseDescription.Trim();
                await _expenseService.UpdateExpenseAsync(entity);
            }
            else
            {
                await _expenseService.CreateExpenseAsync(new HotelExpense
                {
                    ExpenseDate = EditExpenseDate,
                    Amount = EditExpenseAmount,
                    HotelExpenseTypeId = EditExpenseTypeId.Value,
                    HotelCashBoxId = EditExpenseCashBoxId,
                    Description = EditExpenseDescription.Trim()
                });
            }

            IsExpenseDialogOpen = false;
            _toast.ShowSuccess("تم الحفظ");
            await LoadExpensesAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteExpenseAsync(HotelExpense? expense)
    {
        expense ??= SelectedExpense;
        if (expense is null || !CanDelete) return;
        if (!RequestSensitiveApproval("حذف هذا المصروف؟")) return;

        try
        {
            await _expenseService.DeleteExpenseAsync(expense.Id, _currentUserService.Username ?? "System");
            _toast.ShowSuccess("تم الحذف");
            await LoadExpensesAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void OpenAddTypeDialog()
    {
        if (!CanAdd) return;
        _editingTypeId = null;
        IsTypeEditMode = false;
        EditTypeName = string.Empty;
        IsTypeDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditTypeDialog(HotelExpenseType? type)
    {
        type ??= SelectedExpenseType;
        if (type is null || !CanEdit) return;
        _editingTypeId = type.Id;
        IsTypeEditMode = true;
        EditTypeName = type.Name;
        IsTypeDialogOpen = true;
    }

    [RelayCommand]
    private void CloseTypeDialog() => IsTypeDialogOpen = false;

    [RelayCommand]
    private async Task SaveTypeAsync()
    {
        if (string.IsNullOrWhiteSpace(EditTypeName))
        {
            _toast.ShowWarning("أدخل اسم النوع");
            return;
        }

        try
        {
            if (_editingTypeId.HasValue)
            {
                var entity = await _expenseService.GetExpenseTypeByIdAsync(_editingTypeId.Value)
                    ?? throw new InvalidOperationException("النوع غير موجود");
                entity.Name = EditTypeName.Trim();
                await _expenseService.UpdateExpenseTypeAsync(entity);
            }
            else
            {
                await _expenseService.CreateExpenseTypeAsync(new HotelExpenseType { Name = EditTypeName.Trim() });
            }

            IsTypeDialogOpen = false;
            _toast.ShowSuccess("تم الحفظ");
            await LoadLookupsAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        var headers = new[] { "التاريخ", "النوع", "المبلغ", "الوصف" };
        var data = Expenses.Select(e => new object?[]
        {
            e.ExpenseDate.ToString("yyyy/MM/dd"), e.ExpenseType?.Name, e.Amount, e.Description
        }).ToList();
        ListTableExportHelper.ExportExcel(_exportService, _toast, CanExport, "HotelExpenses", "المصاريف", headers, data);
    }

    [RelayCommand]
    private void PrintTable()
    {
        var headers = new[] { "التاريخ", "النوع", "المبلغ", "الوصف" };
        var data = Expenses.Select(e => new object?[]
        {
            e.ExpenseDate.ToString("yyyy/MM/dd"), e.ExpenseType?.Name, e.Amount, e.Description
        }).ToList();
        ListTableExportHelper.Print(_exportService, CanPrint, "سجل مصاريف الفندق", headers, data);
    }
}
