using System.Collections.ObjectModel;
using System.Windows;
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

public partial class GoldOpeningCustomerBalanceViewModel : ViewModelBase
{
    private readonly IGoldOpeningBalanceService _openingService;
    private readonly IGoldCustomerService _customerService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private System.Timers.Timer? _debounceTimer;
    private int? _editingCustomerId;

    public ObservableCollection<GoldCustomerListItem> Items { get; } = [];
    public ObservableCollection<GoldCustomerListItem> PartyPickerItems { get; } = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _showWithBalanceOnly = true;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private string _paginationText = string.Empty;
    [ObservableProperty] private GoldCustomerListItem? _selectedItem;

    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private string _dialogTitle = string.Empty;
    [ObservableProperty] private GoldCustomerListItem? _selectedParty;
    [ObservableProperty] private string _partySearchText = string.Empty;
    [ObservableProperty] private decimal _editCreditBalanceIqd;
    [ObservableProperty] private decimal _editCreditBalanceUsd;
    [ObservableProperty] private decimal _editGoldCreditGrams;
    [ObservableProperty] private string _editNotes = string.Empty;
    [ObservableProperty] private string _dialogError = string.Empty;

    [ObservableProperty] private bool _isDeleteDialogOpen;
    [ObservableProperty] private GoldCustomerListItem? _itemToClear;

    public bool CanSaveBalance => CanAdd || CanEdit;

    public GoldOpeningCustomerBalanceViewModel(
        IGoldOpeningBalanceService openingService,
        IGoldCustomerService customerService,
        IExportService exportService,
        ICurrentUserService currentUserService)
    {
        _openingService = openingService;
        _customerService = customerService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        PageTitle = "أرصدة الزبائن الافتتاحية";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.OpeningCustomerBalance);
        OnPropertyChanged(nameof(CanSaveBalance));
        await LoadItemsAsync();
    }

    private async Task LoadItemsAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();

            if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
            {
                var (allItems, _) = await _customerService.GetPagedAsync(
                    1, int.MaxValue, search, activeOnly: null, creditOnly: ShowWithBalanceOnly);
                var filtered = ColumnFilterEngine.Apply(allItems, ColumnFilters).ToList();
                MasterDataColumnFilterHelper.ApplyClientPagination(
                    filtered, Items, CurrentPage, PageSize,
                    out var filteredTotal, out var filteredPages, out var filteredText);
                TotalCount = filteredTotal;
                TotalPages = filteredPages;
                PaginationText = filteredText;
                return;
            }

            var (items, totalCount) = await _customerService.GetPagedAsync(
                CurrentPage, PageSize, search, activeOnly: null, creditOnly: ShowWithBalanceOnly);

            TotalCount = totalCount;
            TotalPages = PaginationHelper.ComputeTotalPages(totalCount, PageSize);
            PaginationText = PaginationHelper.BuildPaginationText(totalCount, CurrentPage, PageSize);

            Items.Clear();
            foreach (var item in items)
                Items.Add(item);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر تحميل الأرصدة الافتتاحية:\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadPartyPickerAsync(string? search = null)
    {
        try
        {
            PartyPickerItems.Clear();
            var (items, _) = await _customerService.GetPagedAsync(1, 300, search, activeOnly: true);
            foreach (var item in items)
                PartyPickerItems.Add(item);
        }
        catch (Exception ex)
        {
            DialogError = ex.Message;
        }
    }

    protected override void OnColumnFiltersChanged()
    {
        CurrentPage = 1;
        _ = LoadItemsAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();
        _debounceTimer = new System.Timers.Timer(400);
        _debounceTimer.Elapsed += (_, _) =>
        {
            _debounceTimer?.Stop();
            Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                CurrentPage = 1;
                await LoadItemsAsync();
            });
        };
        _debounceTimer.AutoReset = false;
        _debounceTimer.Start();
    }

    partial void OnShowWithBalanceOnlyChanged(bool value)
    {
        CurrentPage = 1;
        _ = LoadItemsAsync();
    }

    partial void OnPartySearchTextChanged(string value)
    {
        if (!IsDialogOpen || IsEditMode) return;
        _ = LoadPartyPickerAsync(string.IsNullOrWhiteSpace(value) ? null : value.Trim());
    }

    partial void OnSelectedPartyChanged(GoldCustomerListItem? value)
    {
        if (value is null || IsEditMode) return;
        EditCreditBalanceIqd = value.CreditBalanceIqd;
        EditCreditBalanceUsd = value.CreditBalanceUsd;
        EditGoldCreditGrams = value.GoldCreditGrams;
    }

    [RelayCommand]
    private async Task FirstPage() { CurrentPage = 1; await LoadItemsAsync(); }

    [RelayCommand]
    private async Task PreviousPage() { if (CurrentPage > 1) { CurrentPage--; await LoadItemsAsync(); } }

    [RelayCommand]
    private async Task NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; await LoadItemsAsync(); } }

    [RelayCommand]
    private async Task LastPage() { CurrentPage = TotalPages; await LoadItemsAsync(); }

    [RelayCommand]
    private async Task Refresh()
    {
        CurrentPage = 1;
        await LoadItemsAsync();
    }

    [RelayCommand]
    private async Task OpenAddDialog()
    {
        if (!CanAdd && !CanEdit)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية إضافة رصيد افتتاحي");
            return;
        }

        _editingCustomerId = null;
        IsEditMode = false;
        DialogTitle = "تعيين رصيد افتتاحي لزبون";
        SelectedParty = null;
        PartySearchText = string.Empty;
        EditCreditBalanceIqd = 0;
        EditCreditBalanceUsd = 0;
        EditGoldCreditGrams = 0;
        EditNotes = string.Empty;
        DialogError = string.Empty;
        await LoadPartyPickerAsync();
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task OpenEditDialog(GoldCustomerListItem? item)
    {
        if (item is null) return;
        if (!CanEdit && !CanAdd)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية تعديل الرصيد الافتتاحي");
            return;
        }

        try
        {
            var entity = await _customerService.GetByIdAsync(item.Id);
            if (entity is null)
            {
                BeautifulMessageDialog.ShowWarning("الزبون غير موجود");
                return;
            }

            _editingCustomerId = entity.Id;
            IsEditMode = true;
            DialogTitle = $"تعديل رصيد افتتاحي — {entity.Name}";
            SelectedParty = item;
            PartySearchText = entity.Name;
            EditCreditBalanceIqd = entity.CreditBalanceIqd;
            EditCreditBalanceUsd = entity.CreditBalanceUsd;
            EditGoldCreditGrams = entity.GoldCreditGrams;
            EditNotes = string.Empty;
            DialogError = string.Empty;
            PartyPickerItems.Clear();
            PartyPickerItems.Add(item);
            IsDialogOpen = true;
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void CancelDialog() => IsDialogOpen = false;

    [RelayCommand]
    private async Task SaveBalanceAsync()
    {
        if (!CanAdd && !CanEdit)
        {
            DialogError = "ليس لديك صلاحية حفظ الرصيد";
            return;
        }

        var customerId = IsEditMode ? _editingCustomerId : SelectedParty?.Id;
        if (customerId is null or <= 0)
        {
            DialogError = "اختر الزبون";
            return;
        }

        if (EditCreditBalanceIqd < 0 || EditCreditBalanceUsd < 0 || EditGoldCreditGrams < 0)
        {
            DialogError = "الأرصدة لا يمكن أن تكون سالبة";
            return;
        }

        try
        {
            IsBusy = true;
            DialogError = string.Empty;
            await _openingService.SetCustomerOpeningBalanceAsync(new GoldOpeningCustomerBalanceRequest
            {
                CustomerId = customerId.Value,
                CreditBalanceIqd = EditCreditBalanceIqd,
                CreditBalanceUsd = EditCreditBalanceUsd,
                GoldCreditGrams = EditGoldCreditGrams,
                Notes = EditNotes
            });

            IsDialogOpen = false;
            BeautifulMessageDialog.ShowSuccess(IsEditMode ? "تم تعديل الرصيد الافتتاحي" : "تم تعيين الرصيد الافتتاحي");
            await LoadItemsAsync();
        }
        catch (Exception ex)
        {
            DialogError = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ConfirmClear(GoldCustomerListItem? item)
    {
        if (item is null || !CanDelete) return;
        ItemToClear = item;
        IsDeleteDialogOpen = true;
    }

    [RelayCommand]
    private void CancelClear()
    {
        IsDeleteDialogOpen = false;
        ItemToClear = null;
    }

    [RelayCommand]
    private async Task ExecuteClearAsync()
    {
        if (ItemToClear is null || !CanDelete) return;
        try
        {
            await _openingService.ClearCustomerOpeningBalanceAsync(ItemToClear.Id);
            IsDeleteDialogOpen = false;
            ItemToClear = null;
            BeautifulMessageDialog.ShowSuccess("تم تصفير الرصيد الافتتاحي");
            await LoadItemsAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task ExportToExcel()
    {
        try
        {
            var search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();
            var (allItems, _) = await _customerService.GetPagedAsync(
                1, int.MaxValue, search, activeOnly: null, creditOnly: ShowWithBalanceOnly);
            var rows = MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters)
                ? ColumnFilterEngine.Apply(allItems, ColumnFilters).ToList()
                : allItems.ToList();

            var exportData = rows.Select(c => new
            {
                الاسم = c.Name,
                الهاتف = c.Phone,
                رصيد_آجل_د_ع = c.CreditBalanceIqd,
                رصيد_آجل_دولار = c.CreditBalanceUsd,
                ذهب_آجل_غرام = c.GoldCreditGrams,
                فواتير_مفتوحة = c.OpenInvoiceCount,
                نشط = c.IsActive ? "نعم" : "لا"
            });

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"ارصدة_افتتاحية_زبائن_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "أرصدة الزبائن");
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
            var (allItems, _) = await _customerService.GetPagedAsync(
                1, int.MaxValue, search, activeOnly: null, creditOnly: ShowWithBalanceOnly);
            var rowsData = MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters)
                ? ColumnFilterEngine.Apply(allItems, ColumnFilters).ToList()
                : allItems.ToList();

            var columns = new[] { "الاسم", "الهاتف", "رصيد آجل د.ع", "رصيد آجل $", "ذهب آجل (غ)", "فواتير مفتوحة", "نشط" };
            IList<object[]> rows = rowsData.Select(c => new object[]
            {
                c.Name,
                c.Phone,
                c.CreditBalanceIqd.ToString("N0"),
                c.CreditBalanceUsd.ToString("N2"),
                c.GoldCreditGrams.ToString("N3"),
                c.OpenInvoiceCount,
                c.IsActive ? "نعم" : "لا"
            }).ToList();
            _exportService.PrintTable("أرصدة الزبائن الافتتاحية", columns, rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }
}
