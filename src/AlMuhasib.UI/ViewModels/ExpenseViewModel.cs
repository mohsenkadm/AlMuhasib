using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;

namespace AlMuhasib.UI.ViewModels;

public partial class ExpenseViewModel : PagedViewModelBase
{
    private readonly IExpenseService _expenseService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;

    public ExpenseViewModel(IExpenseService expenseService, IUnitOfWork unitOfWork, IExportService exportService, ICurrentUserService currentUserService)
    {
        _expenseService = expenseService;
        _unitOfWork = unitOfWork;
        _exportService = exportService;
        _currentUserService = currentUserService;
        PageTitle = "المصاريف";
    }

    // ══════════════════════════════════════════════════════
    // EXPENSE TYPES
    // ══════════════════════════════════════════════════════

    public ObservableCollection<ExpenseType> ExpenseTypes { get; } = [];

    [ObservableProperty]
    private string _newTypeName = string.Empty;

    [ObservableProperty]
    private ExpenseType? _selectedType;

    [ObservableProperty]
    private string _editTypeName = string.Empty;

    [ObservableProperty]
    private bool _isEditingType;

    partial void OnSelectedTypeChanged(ExpenseType? value)
    {
        if (value is not null)
        {
            EditTypeName = value.Name;
        }
        IsEditingType = false;
    }

    [RelayCommand]
    private async Task AddExpenseTypeAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTypeName)) return;
        try
        {
            IsBusy = true;
            var created = await _expenseService.AddExpenseTypeAsync(NewTypeName.Trim());
            ExpenseTypes.Add(created);
            NewTypeName = string.Empty;
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void StartEditType()
    {
        if (SelectedType is null) return;
        EditTypeName = SelectedType.Name;
        IsEditingType = true;
    }

    [RelayCommand]
    private async Task SaveEditTypeAsync()
    {
        if (SelectedType is null || string.IsNullOrWhiteSpace(EditTypeName)) return;
        try
        {
            IsBusy = true;
            await _expenseService.UpdateExpenseTypeAsync(SelectedType.Id, EditTypeName.Trim());
            SelectedType.Name = EditTypeName.Trim();

            // Refresh list to update display
            var index = ExpenseTypes.IndexOf(SelectedType);
            if (index >= 0)
            {
                var updated = SelectedType;
                ExpenseTypes.RemoveAt(index);
                ExpenseTypes.Insert(index, updated);
                SelectedType = updated;
            }
            IsEditingType = false;
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void CancelEditType()
    {
        IsEditingType = false;
        if (SelectedType is not null)
            EditTypeName = SelectedType.Name;
    }

    [RelayCommand]
    private async Task DeleteExpenseTypeAsync()
    {
        if (SelectedType is null) return;
            var confirmed = BeautifulMessageDialog.ShowConfirm(
                $"هل تريد حذف نوع المصروف \"{SelectedType.Name}\"؟");
            if (!confirmed) return;
        try
        {
            IsBusy = true;
            await _expenseService.DeleteExpenseTypeAsync(SelectedType.Id);
            ExpenseTypes.Remove(SelectedType);
            SelectedType = null;
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally { IsBusy = false; }
    }

    // ══════════════════════════════════════════════════════
    // ADD EXPENSE FORM
    // ══════════════════════════════════════════════════════

    public ObservableCollection<CashBox> CashBoxes { get; } = [];

    [ObservableProperty]
    private ExpenseType? _formExpenseType;

    [ObservableProperty]
    private decimal _formAmount;

    [ObservableProperty]
    private DateTime _formDate = DateTime.Now;

    [ObservableProperty]
    private CashBox? _formCashBox;

    [ObservableProperty]
    private string _formNotes = string.Empty;

    [RelayCommand]
    private async Task AddExpenseAsync()
    {
        if (FormExpenseType is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر نوع المصروف");
            return;
        }
        if (FormAmount <= 0)
        {
            BeautifulMessageDialog.ShowWarning("أدخل مبلغ صحيح");
            return;
        }
        if (FormCashBox is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر القاصة");
            return;
        }

        try
        {
            IsBusy = true;
            await _expenseService.AddExpenseAsync(
                FormExpenseType.Id,
                FormAmount,
                FormDate,
                FormCashBox.Id,
                string.IsNullOrWhiteSpace(FormNotes) ? null : FormNotes.Trim());

            // Reset form
            FormExpenseType = null;
            FormAmount = 0;
            FormDate = DateTime.Now;
            FormNotes = string.Empty;
            // Keep FormCashBox selected for convenience

            // Refresh list & cash boxes
            await LoadExpensesAsync();
            await LoadCashBoxesAsync();

            BeautifulMessageDialog.ShowSuccess("تم إضافة المصروف بنجاح");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally { IsBusy = false; }
    }

    // ══════════════════════════════════════════════════════
    // EXPENSES LIST WITH FILTERS
    // ══════════════════════════════════════════════════════

    public ObservableCollection<Expense> Expenses { get; } = [];

    [ObservableProperty]
    private ExpenseType? _filterExpenseType;

    [ObservableProperty]
    private CashBox? _filterCashBox;

    [ObservableProperty]
    private DateTime? _filterFromDate;

    [ObservableProperty]
    private DateTime? _filterToDate;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private decimal _totalFiltered;

    private bool _isClearing;

    partial void OnFilterExpenseTypeChanged(ExpenseType? value) { if (!_isClearing) _ = LoadExpensesAsync(); }
    partial void OnFilterCashBoxChanged(CashBox? value) { if (!_isClearing) _ = LoadExpensesAsync(); }
    partial void OnFilterFromDateChanged(DateTime? value) { if (!_isClearing) _ = LoadExpensesAsync(); }
    partial void OnFilterToDateChanged(DateTime? value) { if (!_isClearing) _ = LoadExpensesAsync(); }

    [RelayCommand]
    private async Task SearchAsync()
    {
        CurrentPage = 1;
        await LoadExpensesAsync();
    }

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        _isClearing = true;
        FilterExpenseType = null;
        FilterCashBox = null;
        FilterFromDate = null;
        FilterToDate = null;
        SearchText = string.Empty;
        CurrentPage = 1;
        _isClearing = false;
        await LoadExpensesAsync();
    }

    protected override Task OnPageChangedAsync() => LoadExpensesAsync();

    [RelayCommand]
    private async Task DeleteExpenseAsync(Expense? expense)
    {
        if (expense is null || !CanDelete) return;

        if (!BeautifulMessageDialog.ShowConfirm(
                $"هل تريد حذف المصروف بمبلغ {expense.Amount:N0}؟ سيتم إرجاع المبلغ للقاصة ولن يظهر في التقارير."))
            return;

        IsBusy = true;
        try
        {
            await _expenseService.DeleteExpenseAsync(expense.Id);
            BeautifulMessageDialog.ShowSuccess("تم حذف المصروف بنجاح");
            await LoadCashBoxesAsync();
            await LoadExpensesAsync();
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

    private async Task LoadExpensesAsync()
    {
        try
        {
            var (items, totalCount) = await _expenseService.GetPagedExpensesAsync(
                CurrentPage, PageSize,
                FilterExpenseType?.Id,
                FilterCashBox?.Id,
                FilterFromDate,
                FilterToDate,
                string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim());

            Expenses.Clear();
            foreach (var item in items)
                Expenses.Add(item);

            ApplyPaginationStats(totalCount);
            TotalFiltered = items.Sum(e => e.Amount);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    // ══════════════════════════════════════════════════════
    // EXPORT & PRINT
    // ══════════════════════════════════════════════════════

    [RelayCommand]
    private void ExportExpenses()
    {
        var columns = new[] { "النوع", "المبلغ", "التاريخ", "القاصة", "ملاحظات" };
        var rows = Expenses.Select(e => new object[]
        {
            e.ExpenseType?.Name ?? "",
            e.Amount.ToString("N0"),
            e.Date.ToString("yyyy/MM/dd"),
            e.CashBox?.Name ?? "",
            e.Notes ?? ""
        }).ToList();

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Excel Files|*.xlsx",
            FileName = $"المصاريف_{DateTime.Now:yyyyMMdd}.xlsx"
        };
        if (dialog.ShowDialog() == true)
        {
            _exportService.ExportToExcel(dialog.FileName, "المصاريف", columns, (IList<object[]>)rows);
            BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
        }
    }

    [RelayCommand]
    private void PrintExpenses()
    {
        var columns = new[] { "النوع", "المبلغ", "التاريخ", "القاصة", "ملاحظات" };
        var rows = Expenses.Select(e => new object[]
        {
            e.ExpenseType?.Name ?? "",
            e.Amount.ToString("N0"),
            e.Date.ToString("yyyy/MM/dd"),
            e.CashBox?.Name ?? "",
            e.Notes ?? ""
        }).ToList();

        _exportService.PrintTable("تقرير المصاريف", columns, (IList<object[]>)rows);
    }

    // ══════════════════════════════════════════════════════
    // INITIALIZATION
    // ══════════════════════════════════════════════════════

    public override async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            LoadPermissions(_currentUserService, "Expenses");

            await LoadExpenseTypesAsync();
            await LoadCashBoxesAsync();
            await LoadExpensesAsync();
        }
        finally { IsBusy = false; }
    }

    private async Task LoadExpenseTypesAsync()
    {
        var types = await _expenseService.GetAllExpenseTypesAsync();
        ExpenseTypes.Clear();
        foreach (var t in types)
            ExpenseTypes.Add(t);
    }

    private async Task LoadCashBoxesAsync()
    {
        var cashBoxes = await _unitOfWork.CashBoxes.GetAllAsync();
        CashBoxes.Clear();
        foreach (var cb in cashBoxes)
            CashBoxes.Add(cb);
    }
}
