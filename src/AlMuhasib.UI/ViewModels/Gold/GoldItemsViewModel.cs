using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldItemsViewModel : ViewModelBase
{
    private readonly IGoldInventoryService _inventoryService;
    private readonly IGoldPricingService _pricingService;
    private readonly IGoldPrintService _printService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private System.Timers.Timer? _debounceTimer;
    private int? _editingId;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private string _paginationText = string.Empty;
    [ObservableProperty] private GoldItem? _selectedItem;

    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private string _dialogTitle = string.Empty;
    [ObservableProperty] private string _editName = string.Empty;
    [ObservableProperty] private string _editBarcode = string.Empty;
    [ObservableProperty] private int _editKaratValue = 21;
    [ObservableProperty] private decimal _editWeight;
    [ObservableProperty] private decimal _editMakingCharge;
    [ObservableProperty] private decimal _editCostPerGram;
    [ObservableProperty] private string _editCategory = string.Empty;
    [ObservableProperty] private string _dialogError = string.Empty;

    [ObservableProperty] private bool _isDeleteDialogOpen;
    [ObservableProperty] private GoldItem? _itemToDelete;

    public ObservableCollection<GoldItem> Items { get; } = [];
    public ObservableCollection<GoldKarat> Karats { get; } = [];

    public GoldItemsViewModel(
        IGoldInventoryService inventoryService,
        IGoldPricingService pricingService,
        IGoldPrintService printService,
        IExportService exportService,
        ICurrentUserService currentUserService)
    {
        _inventoryService = inventoryService;
        _pricingService = pricingService;
        _printService = printService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        PageTitle = "أصناف الذهب";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.Items);
        foreach (var karat in await _pricingService.GetKaratsAsync())
            Karats.Add(karat);
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
                var (allItems, _) = await _inventoryService.GetItemsPagedAsync(1, int.MaxValue, search);
                var filtered = ColumnFilterEngine.Apply(allItems, ColumnFilters).ToList();
                MasterDataColumnFilterHelper.ApplyClientPagination(
                    filtered, Items, CurrentPage, PageSize,
                    out var filteredTotal, out var filteredPages, out var filteredText);
                TotalCount = filteredTotal;
                TotalPages = filteredPages;
                PaginationText = filteredText;
                return;
            }

            var (items, totalCount) = await _inventoryService.GetItemsPagedAsync(CurrentPage, PageSize, search);

            TotalCount = totalCount;
            TotalPages = PaginationHelper.ComputeTotalPages(totalCount, PageSize);
            PaginationText = PaginationHelper.BuildPaginationText(totalCount, CurrentPage, PageSize);

            Items.Clear();
            foreach (var item in items)
                Items.Add(item);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر تحميل الأصناف:\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
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
    private void OpenAddDialog()
    {
        _editingId = null;
        IsEditMode = false;
        DialogTitle = "إضافة صنف ذهب";
        EditName = string.Empty;
        EditBarcode = string.Empty;
        EditKaratValue = Karats.FirstOrDefault()?.KaratValue ?? 21;
        EditWeight = 0;
        EditMakingCharge = 0;
        EditCostPerGram = 0;
        EditCategory = string.Empty;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(GoldItem? item)
    {
        if (item is null) return;
        _editingId = item.Id;
        IsEditMode = true;
        DialogTitle = "تعديل صنف الذهب";
        EditName = item.Name;
        EditBarcode = item.Barcode;
        EditKaratValue = item.KaratValue;
        EditWeight = item.WeightGrams;
        EditMakingCharge = item.SuggestedMakingCharge;
        EditCostPerGram = item.CostPerGram;
        EditCategory = item.Category;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void CancelDialog() => IsDialogOpen = false;

    [RelayCommand]
    private async Task SaveItemAsync()
    {
        if (string.IsNullOrWhiteSpace(EditName))
        {
            DialogError = "اسم الصنف مطلوب";
            return;
        }

        if (EditWeight <= 0)
        {
            DialogError = "أدخل وزناً صالحاً";
            return;
        }

        try
        {
            if (IsEditMode && _editingId.HasValue)
            {
                var existing = await _inventoryService.GetItemByIdAsync(_editingId.Value);
                if (existing is null)
                {
                    DialogError = "الصنف غير موجود";
                    return;
                }

                existing.Name = EditName.Trim();
                existing.Barcode = EditBarcode?.Trim() ?? string.Empty;
                existing.KaratValue = EditKaratValue;
                existing.WeightGrams = EditWeight;
                existing.SuggestedMakingCharge = EditMakingCharge;
                existing.CostPerGram = EditCostPerGram;
                existing.Category = EditCategory?.Trim() ?? string.Empty;
                existing.UpdatedBy = _currentUserService.Username;
                await _inventoryService.UpdateItemAsync(existing);
            }
            else
            {
                await _inventoryService.CreateItemAsync(new GoldItem
                {
                    Name = EditName.Trim(),
                    Barcode = EditBarcode?.Trim() ?? string.Empty,
                    KaratValue = EditKaratValue,
                    WeightGrams = EditWeight,
                    SuggestedMakingCharge = EditMakingCharge,
                    CostPerGram = EditCostPerGram,
                    Category = EditCategory?.Trim() ?? string.Empty,
                    Status = GoldItemStatus.InStock,
                    CreatedBy = _currentUserService.Username
                });
            }

            IsDialogOpen = false;
            BeautifulMessageDialog.ShowSuccess("تم حفظ الصنف");
            await LoadItemsAsync();
        }
        catch (Exception ex)
        {
            DialogError = ex.Message;
        }
    }

    [RelayCommand]
    private void ConfirmDelete(GoldItem? item)
    {
        if (item is null || !CanDelete) return;
        ItemToDelete = item;
        IsDeleteDialogOpen = true;
    }

    [RelayCommand]
    private void CancelDelete()
    {
        IsDeleteDialogOpen = false;
        ItemToDelete = null;
    }

    [RelayCommand]
    private async Task ExecuteDeleteAsync()
    {
        if (ItemToDelete is null) return;
        try
        {
            await _inventoryService.DeleteItemAsync(ItemToDelete.Id, _currentUserService.Username);
            IsDeleteDialogOpen = false;
            ItemToDelete = null;
            BeautifulMessageDialog.ShowSuccess("تم حذف الصنف");
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
            var (allItems, _) = await _inventoryService.GetItemsPagedAsync(1, int.MaxValue, null);
            var exportData = allItems.Select(i => new
            {
                الاسم = i.Name,
                الباركود = i.Barcode,
                العيار = i.KaratValue,
                الوزن = i.WeightGrams,
                أجور_الصياغة = i.SuggestedMakingCharge,
                تكلفة_غرام = i.CostPerGram,
                التصنيف = i.Category,
                الحالة = i.Status.ToString()
            });

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"أصناف_الذهب_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "الأصناف");
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
            var (allItems, _) = await _inventoryService.GetItemsPagedAsync(1, int.MaxValue, null);
            var columns = new[] { "الاسم", "الباركود", "العيار", "الوزن", "أجور الصياغة", "تكلفة/غ", "التصنيف", "الحالة" };
            IList<object[]> rows = allItems.Select(i => new object[]
            {
                i.Name,
                i.Barcode,
                i.KaratValue,
                i.WeightGrams.ToString("N2"),
                i.SuggestedMakingCharge.ToString("N0"),
                i.CostPerGram.ToString("N0"),
                i.Category,
                i.Status.ToString()
            }).ToList();
            _exportService.PrintTable("قائمة أصناف الذهب", columns, rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task PrintLabelAsync(GoldItem? item)
    {
        item ??= SelectedItem;
        if (item is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر صنفاً لطباعة الملصق");
            return;
        }

        if (!CanPrint)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية الطباعة");
            return;
        }

        try
        {
            await _printService.PrintItemLabelAsync(item);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر طباعة الملصق:\n{ex.Message}");
        }
    }
}
