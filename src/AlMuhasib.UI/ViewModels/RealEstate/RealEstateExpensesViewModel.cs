using AlMuhasib.Core.Entities.RealEstate;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.RealEstate;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;

namespace AlMuhasib.UI.ViewModels.RealEstate;

public partial class RealEstateExpensesViewModel : PagedViewModelBase
{
    private readonly IRealEstateExpenseService _expenseService;
    private readonly IRealEstateContractService _contractService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IToastNotificationService _toast;

    public ObservableCollection<RealEstateExpenseListItem> Expenses { get; } = [];
    public ObservableCollection<RealEstateExpenseType> ExpenseTypes { get; } = [];
    public ObservableCollection<RealEstateContractListItem> ContractOptions { get; } = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private DateTime? _dateFrom = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime? _dateTo = DateTime.Today;
    [ObservableProperty] private RealEstateExpenseType? _typeFilter;
    [ObservableProperty] private decimal _periodTotal;
    [ObservableProperty] private int _periodCount;

    [ObservableProperty] private bool _isExpenseDialogOpen;
    [ObservableProperty] private bool _isTypeDialogOpen;
    [ObservableProperty] private int _editExpenseId;
    [ObservableProperty] private DateTime _editExpenseDate = DateTime.Today;
    [ObservableProperty] private decimal _editExpenseAmount;
    [ObservableProperty] private int? _editExpenseTypeId;
    [ObservableProperty] private string _editExpenseDescription = string.Empty;
    [ObservableProperty] private string _editExpenseNotes = string.Empty;
    [ObservableProperty] private int? _editRelatedContractId;

    [ObservableProperty] private int _editTypeId;
    [ObservableProperty] private string _editTypeName = string.Empty;
    [ObservableProperty] private string _editTypeNotes = string.Empty;
    [ObservableProperty] private bool _editTypeIsActive = true;

    public RealEstateExpensesViewModel(
        IRealEstateExpenseService expenseService,
        IRealEstateContractService contractService,
        IExportService exportService,
        ICurrentUserService currentUserService,
        IToastNotificationService toast)
    {
        _expenseService = expenseService;
        _contractService = contractService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        _toast = toast;
        PageTitle = "المصاريف";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, RealEstatePermissionRegistry.Expenses);
        await _expenseService.EnsureDefaultTypesAsync();
        await LoadTypesAsync();
        await LoadContractOptionsAsync();
        await LoadExpensesAsync();
    }

    partial void OnSearchTextChanged(string value) => _ = ReloadAsync();
    partial void OnDateFromChanged(DateTime? value) => _ = ReloadAsync();
    partial void OnDateToChanged(DateTime? value) => _ = ReloadAsync();
    partial void OnTypeFilterChanged(RealEstateExpenseType? value) => _ = ReloadAsync();

    protected override void OnColumnFiltersChanged() => _ = ReloadAsync();
    protected override Task OnPageChangedAsync() => LoadExpensesAsync();

    private async Task ReloadAsync()
    {
        CurrentPage = 1;
        await LoadExpensesAsync();
    }

    private async Task LoadTypesAsync()
    {
        var types = await _expenseService.GetTypesAsync();
        ExpenseTypes.Clear();
        foreach (var t in types)
            ExpenseTypes.Add(t);
    }

    private async Task LoadContractOptionsAsync()
    {
        var (items, _) = await _contractService.GetPagedAsync(1, 200, new RealEstateContractFilter());
        ContractOptions.Clear();
        ContractOptions.Add(new RealEstateContractListItem { Id = 0, ContractNumber = "— بدون ربط —" });
        foreach (var item in items)
            ContractOptions.Add(item);
    }

    [RelayCommand]
    private async Task LoadExpensesAsync()
    {
        IsBusy = true;
        try
        {
            var filter = new RealEstateExpenseFilter
            {
                SearchText = SearchText,
                DateFrom = DateFrom,
                DateTo = DateTo,
                ExpenseTypeId = TypeFilter?.Id
            };

            if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
            {
                var (all, _, totalAmt) = await _expenseService.GetPagedAsync(1, int.MaxValue, filter);
                var filtered = ColumnFilterEngine.Apply(all, ColumnFilters).ToList();
                MasterDataColumnFilterHelper.ApplyClientPagination(
                    filtered, Expenses, CurrentPage, PageSize,
                    out var filteredTotal, out var filteredPages, out var filteredText);
                TotalCount = filteredTotal;
                TotalPages = filteredPages;
                PaginationText = filteredText;
                PeriodCount = filtered.Count;
                PeriodTotal = filtered.Sum(e => e.Amount);
                return;
            }

            var (items, total, amount) = await _expenseService.GetPagedAsync(CurrentPage, PageSize, filter);
            Expenses.Clear();
            foreach (var item in items)
                Expenses.Add(item);
            ApplyPaginationStats(total);
            PeriodCount = total;
            PeriodTotal = amount;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void OpenNewExpense()
    {
        EditExpenseId = 0;
        EditExpenseDate = DateTime.Today;
        EditExpenseAmount = 0;
        EditExpenseTypeId = ExpenseTypes.FirstOrDefault(t => t.IsActive)?.Id;
        EditExpenseDescription = string.Empty;
        EditExpenseNotes = string.Empty;
        EditRelatedContractId = 0;
        IsExpenseDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditExpense(RealEstateExpenseListItem? item)
    {
        if (item is null || !CanEdit) return;
        EditExpenseId = item.Id;
        EditExpenseDate = item.ExpenseDate;
        EditExpenseAmount = item.Amount;
        EditExpenseTypeId = item.ExpenseTypeId;
        EditExpenseDescription = item.Description;
        EditExpenseNotes = item.Notes;
        EditRelatedContractId = item.RelatedContractId ?? 0;
        IsExpenseDialogOpen = true;
    }

    [RelayCommand]
    private void CloseExpenseDialog() => IsExpenseDialogOpen = false;

    [RelayCommand]
    private async Task SaveExpenseAsync()
    {
        if (!EditExpenseTypeId.HasValue || EditExpenseTypeId <= 0)
        {
            _toast.ShowWarning("اختر نوع المصروف");
            return;
        }

        try
        {
            await _expenseService.SaveAsync(new RealEstateExpense
            {
                Id = EditExpenseId,
                ExpenseTypeId = EditExpenseTypeId.Value,
                ExpenseDate = EditExpenseDate,
                Amount = EditExpenseAmount,
                Description = EditExpenseDescription.Trim(),
                Notes = EditExpenseNotes.Trim(),
                RelatedContractId = EditRelatedContractId is > 0 ? EditRelatedContractId : null
            });
            IsExpenseDialogOpen = false;
            _toast.ShowSuccess("تم حفظ المصروف");
            await LoadExpensesAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteExpenseAsync(RealEstateExpenseListItem? item)
    {
        if (item is null || !CanDelete) return;
        try
        {
            await _expenseService.DeleteAsync(item.Id, _currentUserService.Username ?? "System");
            _toast.ShowSuccess("تم حذف المصروف");
            await LoadExpensesAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void OpenNewType()
    {
        EditTypeId = 0;
        EditTypeName = string.Empty;
        EditTypeNotes = string.Empty;
        EditTypeIsActive = true;
        IsTypeDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditType(RealEstateExpenseType? type)
    {
        type ??= TypeFilter;
        if (type is null) return;
        EditTypeId = type.Id;
        EditTypeName = type.Name;
        EditTypeNotes = type.Notes;
        EditTypeIsActive = type.IsActive;
        IsTypeDialogOpen = true;
    }

    [RelayCommand]
    private void CloseTypeDialog() => IsTypeDialogOpen = false;

    [RelayCommand]
    private async Task SaveTypeAsync()
    {
        try
        {
            await _expenseService.SaveTypeAsync(new RealEstateExpenseType
            {
                Id = EditTypeId,
                Name = EditTypeName,
                Notes = EditTypeNotes,
                IsActive = EditTypeIsActive
            });
            IsTypeDialogOpen = false;
            _toast.ShowSuccess("تم حفظ نوع المصروف");
            await LoadTypesAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteTypeAsync(RealEstateExpenseType? type)
    {
        if (type is null || !CanDelete) return;
        try
        {
            await _expenseService.DeleteTypeAsync(type.Id, _currentUserService.Username ?? "System");
            _toast.ShowSuccess("تم حذف نوع المصروف");
            if (TypeFilter?.Id == type.Id) TypeFilter = null;
            await LoadTypesAsync();
            await LoadExpensesAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        if (!CanExport) return;
        var filter = new RealEstateExpenseFilter
        {
            SearchText = SearchText,
            DateFrom = DateFrom,
            DateTo = DateTo,
            ExpenseTypeId = TypeFilter?.Id
        };
        var (rows, _, _) = await _expenseService.GetPagedAsync(1, int.MaxValue, filter);
        var dialog = new SaveFileDialog
        {
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"RealEstateExpenses_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };
        if (dialog.ShowDialog() != true) return;

        var headers = new[] { "التاريخ", "النوع", "المبلغ", "البيان", "العقد", "ملاحظات" };
        var data = rows.Select(r => new object?[]
        {
            r.ExpenseDate.ToString("yyyy/MM/dd"), r.ExpenseTypeName, r.Amount,
            r.Description, r.RelatedContractNumber, r.Notes
        }).ToList();
        _exportService.ExportToExcel(dialog.FileName, "المصاريف", headers, data);
        _toast.ShowSuccess("تم التصدير");
    }

    [RelayCommand]
    private async Task PrintTableAsync()
    {
        if (!CanPrint) return;
        var filter = new RealEstateExpenseFilter
        {
            SearchText = SearchText,
            DateFrom = DateFrom,
            DateTo = DateTo,
            ExpenseTypeId = TypeFilter?.Id
        };
        var (rows, _, total) = await _expenseService.GetPagedAsync(1, int.MaxValue, filter);
        var headers = new[] { "التاريخ", "النوع", "المبلغ", "البيان", "العقد" };
        var data = rows.Select(r => new object?[]
        {
            r.ExpenseDate.ToString("yyyy/MM/dd"), r.ExpenseTypeName, r.Amount,
            r.Description, r.RelatedContractNumber
        }).ToList();
        _exportService.PrintTable(
            "كشف مصاريف مكتب العقارات",
            headers,
            data,
            [$"الفترة: {DateFrom:yyyy/MM/dd} — {DateTo:yyyy/MM/dd}", $"الإجمالي: {total:N0}"]);
    }
}
