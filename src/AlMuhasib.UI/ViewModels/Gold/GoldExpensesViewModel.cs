using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldExpensesViewModel : ViewModelBase
{
    private readonly IGoldExpenseService _expenseService;
    private readonly IGoldCashService _cashService;
    private readonly IGoldWarehouseService _warehouseService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private bool _isClearing;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private string _paginationText = string.Empty;
    [ObservableProperty] private decimal _totalFiltered;
    [ObservableProperty] private GoldExpenseListItem? _selectedExpense;

    [ObservableProperty] private GoldExpenseType? _filterExpenseType;
    [ObservableProperty] private GoldCashBox? _filterCashBox;
    [ObservableProperty] private GoldCurrency? _filterCurrency;
    [ObservableProperty] private DateTime? _filterFromDate;
    [ObservableProperty] private DateTime? _filterToDate;

    [ObservableProperty] private GoldExpenseType? _formExpenseType;
    [ObservableProperty] private decimal _formAmount;
    [ObservableProperty] private DateTime _formDate = DateTime.Today;
    [ObservableProperty] private GoldCashBox? _formCashBox;
    [ObservableProperty] private GoldCurrency _formCurrency = GoldCurrency.IQD;
    [ObservableProperty] private GoldWarehouse? _formWarehouse;
    [ObservableProperty] private string _formNotes = string.Empty;

    public ObservableCollection<GoldExpenseListItem> Expenses { get; } = [];
    public ObservableCollection<GoldExpenseType> ExpenseTypes { get; } = [];
    public ObservableCollection<GoldCashBox> CashBoxes { get; } = [];
    public ObservableCollection<GoldWarehouse> Warehouses { get; } = [];

    public IReadOnlyList<GoldCurrencyOption> Currencies { get; } =
    [
        new(GoldCurrency.IQD, "دينار عراقي"),
        new(GoldCurrency.USD, "دولار أمريكي")
    ];

    public GoldExpensesViewModel(
        IGoldExpenseService expenseService,
        IGoldCashService cashService,
        IGoldWarehouseService warehouseService,
        IExportService exportService,
        ICurrentUserService currentUserService)
    {
        _expenseService = expenseService;
        _cashService = cashService;
        _warehouseService = warehouseService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        PageTitle = "المصاريف";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.Expenses);
        await LoadLookupsAsync();
        await LoadExpensesAsync();
    }

    private async Task LoadLookupsAsync()
    {
        ExpenseTypes.Clear();
        foreach (var t in await _expenseService.GetExpenseTypesAsync(activeOnly: true))
            ExpenseTypes.Add(t);

        CashBoxes.Clear();
        foreach (var box in await _cashService.GetCashBoxesAsync(activeOnly: true))
            CashBoxes.Add(box);

        FormCashBox = CashBoxes.FirstOrDefault(b => b.IsDefault) ?? CashBoxes.FirstOrDefault();
        if (FormCashBox is not null)
            FormCurrency = FormCashBox.Currency;

        Warehouses.Clear();
        foreach (var w in await _warehouseService.GetAllAsync(activeOnly: true))
            Warehouses.Add(w);

        FormWarehouse = Warehouses.FirstOrDefault(w => w.IsDefault) ?? Warehouses.FirstOrDefault();
    }

    private async Task LoadExpensesAsync(bool force = false)
    {
        if (IsBusy && !force) return;
        IsBusy = true;
        try
        {
            var search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();

            if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
            {
                var (allItems, _) = await _expenseService.GetExpensesPagedAsync(
                    1, int.MaxValue, search,
                    FilterExpenseType?.Id, FilterCashBox?.Id, null, FilterCurrency,
                    FilterFromDate, FilterToDate);
                var filtered = ColumnFilterEngine.Apply(allItems, ColumnFilters).ToList();
                MasterDataColumnFilterHelper.ApplyClientPagination(
                    filtered, Expenses, CurrentPage, PageSize,
                    out var filteredTotal, out var filteredPages, out var filteredText);
                TotalCount = filteredTotal;
                TotalPages = filteredPages;
                PaginationText = filteredText;
                TotalFiltered = filtered.Sum(e => e.Amount);
                return;
            }

            var (items, totalCount) = await _expenseService.GetExpensesPagedAsync(
                CurrentPage, PageSize, search,
                FilterExpenseType?.Id, FilterCashBox?.Id, null, FilterCurrency,
                FilterFromDate, FilterToDate);

            TotalCount = totalCount;
            TotalPages = PaginationHelper.ComputeTotalPages(totalCount, PageSize);
            PaginationText = PaginationHelper.BuildPaginationText(totalCount, CurrentPage, PageSize);
            TotalFiltered = items.Sum(e => e.Amount);

            Expenses.Clear();
            foreach (var item in items)
                Expenses.Add(item);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر تحميل المصاريف:\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected override void OnColumnFiltersChanged()
    {
        CurrentPage = 1;
        _ = LoadExpensesAsync();
    }

    partial void OnFilterExpenseTypeChanged(GoldExpenseType? value) { if (!_isClearing) { CurrentPage = 1; _ = LoadExpensesAsync(); } }
    partial void OnFilterCashBoxChanged(GoldCashBox? value) { if (!_isClearing) { CurrentPage = 1; _ = LoadExpensesAsync(); } }
    partial void OnFilterCurrencyChanged(GoldCurrency? value) { if (!_isClearing) { CurrentPage = 1; _ = LoadExpensesAsync(); } }
    partial void OnFilterFromDateChanged(DateTime? value) { if (!_isClearing) { CurrentPage = 1; _ = LoadExpensesAsync(); } }
    partial void OnFilterToDateChanged(DateTime? value) { if (!_isClearing) { CurrentPage = 1; _ = LoadExpensesAsync(); } }

    partial void OnFormCashBoxChanged(GoldCashBox? value)
    {
        if (value is not null)
            FormCurrency = value.Currency;
    }

    [RelayCommand]
    private async Task FirstPage() { CurrentPage = 1; await LoadExpensesAsync(); }

    [RelayCommand]
    private async Task PreviousPage() { if (CurrentPage > 1) { CurrentPage--; await LoadExpensesAsync(); } }

    [RelayCommand]
    private async Task NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; await LoadExpensesAsync(); } }

    [RelayCommand]
    private async Task LastPage() { CurrentPage = TotalPages; await LoadExpensesAsync(); }

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
        FilterCurrency = null;
        FilterFromDate = null;
        FilterToDate = null;
        SearchText = string.Empty;
        CurrentPage = 1;
        _isClearing = false;
        await LoadExpensesAsync();
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadLookupsAsync();
        CurrentPage = 1;
        await LoadExpensesAsync();
    }

    [RelayCommand]
    private async Task AddExpenseAsync()
    {
        if (!CanAdd) return;

        if (FormExpenseType is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر نوع المصروف");
            return;
        }

        if (FormAmount <= 0)
        {
            BeautifulMessageDialog.ShowWarning("أدخل مبلغاً صحيحاً");
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
            await _expenseService.CreateExpenseAsync(new GoldExpense
            {
                ExpenseDate = FormDate.Date,
                ExpenseTypeId = FormExpenseType.Id,
                Amount = FormAmount,
                Currency = FormCurrency,
                CashBoxId = FormCashBox.Id,
                WarehouseId = FormWarehouse?.Id,
                Notes = FormNotes?.Trim() ?? string.Empty,
                CreatedBy = _currentUserService.Username
            });

            FormExpenseType = null;
            FormAmount = 0;
            FormDate = DateTime.Today;
            FormNotes = string.Empty;

            BeautifulMessageDialog.ShowSuccess("تم إضافة المصروف بنجاح");
            CurrentPage = 1;
            await LoadLookupsAsync();
            await LoadExpensesAsync(force: true);
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

    [RelayCommand]
    private async Task DeleteExpenseAsync(GoldExpenseListItem? expense)
    {
        if (expense is null || !CanDelete) return;

        if (!BeautifulMessageDialog.ShowConfirm(
                $"هل تريد حذف المصروف بمبلغ {expense.Amount:N0}؟ سيتم إرجاع المبلغ للقاصة."))
            return;

        try
        {
            IsBusy = true;
            await _expenseService.DeleteExpenseAsync(expense.Id, _currentUserService.Username);
            BeautifulMessageDialog.ShowSuccess("تم حذف المصروف");
            CurrentPage = 1;
            await LoadLookupsAsync();
            await LoadExpensesAsync(force: true);
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

    [RelayCommand]
    private async Task ExportToExcel()
    {
        try
        {
            var search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();
            var (allItems, _) = await _expenseService.GetExpensesPagedAsync(
                1, int.MaxValue, search,
                FilterExpenseType?.Id, FilterCashBox?.Id, null, FilterCurrency,
                FilterFromDate, FilterToDate);

            var exportData = allItems.Select(e => new
            {
                التاريخ = e.ExpenseDate.ToString("yyyy/MM/dd"),
                النوع = e.ExpenseTypeName,
                المبلغ = e.Amount,
                العملة = e.Currency.ToString(),
                القاصة = e.CashBoxName ?? "",
                المخزن = e.WarehouseName ?? "",
                ملاحظات = e.Notes
            });

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"مصاريف_الذهب_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "المصاريف");
                BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
            }
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء التصدير: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task PrintTable()
    {
        try
        {
            var search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();
            var (allItems, _) = await _expenseService.GetExpensesPagedAsync(
                1, int.MaxValue, search,
                FilterExpenseType?.Id, FilterCashBox?.Id, null, FilterCurrency,
                FilterFromDate, FilterToDate);

            var columns = new[] { "التاريخ", "النوع", "المبلغ", "العملة", "القاصة", "المخزن", "ملاحظات" };
            IList<object[]> rows = allItems.Select(e => new object[]
            {
                e.ExpenseDate.ToString("yyyy/MM/dd"),
                e.ExpenseTypeName,
                e.Amount.ToString("N0"),
                e.Currency.ToString(),
                e.CashBoxName ?? "",
                e.WarehouseName ?? "",
                e.Notes
            }).ToList();
            _exportService.PrintTable("مصاريف محل الذهب", columns, rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }
}
