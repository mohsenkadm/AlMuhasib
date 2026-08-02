using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities.Gold;
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

public partial class GoldWarehouseTransferViewModel : ViewModelBase
{
    private readonly IGoldWarehouseService _warehouseService;
    private readonly IGoldPricingService _pricingService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;

    [ObservableProperty] private DateTime _transferDate = DateTime.Today;
    [ObservableProperty] private GoldWarehouse? _fromWarehouse;
    [ObservableProperty] private GoldWarehouse? _toWarehouse;
    [ObservableProperty] private int _karatValue = 21;
    [ObservableProperty] private decimal _weightGrams;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private string _formError = string.Empty;

    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private string _paginationText = string.Empty;
    [ObservableProperty] private GoldWarehouseTransfer? _selectedTransfer;

    public ObservableCollection<GoldWarehouse> Warehouses { get; } = [];
    public ObservableCollection<GoldKarat> Karats { get; } = [];
    public ObservableCollection<GoldWarehouseTransfer> Transfers { get; } = [];

    public GoldWarehouseTransferViewModel(
        IGoldWarehouseService warehouseService,
        IGoldPricingService pricingService,
        IExportService exportService,
        ICurrentUserService currentUserService)
    {
        _warehouseService = warehouseService;
        _pricingService = pricingService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        PageTitle = "نقل مخازن";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.WarehouseTransfer);
        await LoadLookupsAsync();
        await LoadTransfersAsync();
    }

    private async Task LoadLookupsAsync()
    {
        Warehouses.Clear();
        foreach (var w in await _warehouseService.GetAllAsync(activeOnly: true))
            Warehouses.Add(w);

        FromWarehouse = Warehouses.FirstOrDefault(w => w.IsDefault) ?? Warehouses.FirstOrDefault();
        ToWarehouse = Warehouses.FirstOrDefault(w => w.Id != FromWarehouse?.Id);

        Karats.Clear();
        foreach (var k in await _pricingService.GetKaratsAsync())
            Karats.Add(k);

        if (Karats.Count > 0)
            KaratValue = Karats[0].KaratValue;
    }

    private async Task LoadTransfersAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
            {
                var (allItems, _) = await _warehouseService.GetTransfersPagedAsync(1, int.MaxValue);
                var filtered = ColumnFilterEngine.Apply(allItems, ColumnFilters).ToList();
                MasterDataColumnFilterHelper.ApplyClientPagination(
                    filtered, Transfers, CurrentPage, PageSize,
                    out var filteredTotal, out var filteredPages, out var filteredText);
                TotalCount = filteredTotal;
                TotalPages = filteredPages;
                PaginationText = filteredText;
                return;
            }

            var (items, totalCount) = await _warehouseService.GetTransfersPagedAsync(CurrentPage, PageSize);
            TotalCount = totalCount;
            TotalPages = PaginationHelper.ComputeTotalPages(totalCount, PageSize);
            PaginationText = PaginationHelper.BuildPaginationText(totalCount, CurrentPage, PageSize);

            Transfers.Clear();
            foreach (var item in items)
                Transfers.Add(item);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر تحميل النقلات:\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected override void OnColumnFiltersChanged()
    {
        CurrentPage = 1;
        _ = LoadTransfersAsync();
    }

    [RelayCommand]
    private async Task FirstPage() { CurrentPage = 1; await LoadTransfersAsync(); }

    [RelayCommand]
    private async Task PreviousPage() { if (CurrentPage > 1) { CurrentPage--; await LoadTransfersAsync(); } }

    [RelayCommand]
    private async Task NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; await LoadTransfersAsync(); } }

    [RelayCommand]
    private async Task LastPage() { CurrentPage = TotalPages; await LoadTransfersAsync(); }

    [RelayCommand]
    private async Task Refresh()
    {
        CurrentPage = 1;
        await LoadLookupsAsync();
        await LoadTransfersAsync();
    }

    [RelayCommand]
    private async Task SubmitTransferAsync()
    {
        if (!CanAdd)
        {
            FormError = "ليس لديك صلاحية النقل";
            return;
        }

        if (FromWarehouse is null || ToWarehouse is null)
        {
            FormError = "اختر المخزن المصدر والوجهة";
            return;
        }

        if (FromWarehouse.Id == ToWarehouse.Id)
        {
            FormError = "لا يمكن النقل إلى نفس المخزن";
            return;
        }

        if (KaratValue <= 0)
        {
            FormError = "اختر العيار";
            return;
        }

        if (WeightGrams <= 0)
        {
            FormError = "أدخل وزناً صحيحاً";
            return;
        }

        if (!BeautifulMessageDialog.ShowConfirm(
                $"نقل {WeightGrams:N2} غ عيار {KaratValue} من «{FromWarehouse.Name}» إلى «{ToWarehouse.Name}»؟",
                "تأكيد النقل"))
            return;

        try
        {
            IsBusy = true;
            FormError = string.Empty;
            await _warehouseService.TransferAsync(new GoldTransferRequest
            {
                TransferDate = TransferDate.Date,
                FromWarehouseId = FromWarehouse.Id,
                ToWarehouseId = ToWarehouse.Id,
                KaratValue = KaratValue,
                WeightGrams = WeightGrams,
                Notes = Notes?.Trim() ?? string.Empty
            });

            BeautifulMessageDialog.ShowSuccess("تم تنفيذ النقل");
            WeightGrams = 0;
            Notes = string.Empty;
            CurrentPage = 1;
            await LoadTransfersAsync();
        }
        catch (Exception ex)
        {
            FormError = ex.Message;
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ResetForm()
    {
        TransferDate = DateTime.Today;
        WeightGrams = 0;
        Notes = string.Empty;
        FormError = string.Empty;
        FromWarehouse = Warehouses.FirstOrDefault(w => w.IsDefault) ?? Warehouses.FirstOrDefault();
        ToWarehouse = Warehouses.FirstOrDefault(w => w.Id != FromWarehouse?.Id);
        if (Karats.Count > 0)
            KaratValue = Karats[0].KaratValue;
    }

    [RelayCommand]
    private async Task ExportToExcel()
    {
        try
        {
            var (allItems, _) = await _warehouseService.GetTransfersPagedAsync(1, int.MaxValue);
            var exportData = allItems.Select(t => new
            {
                التاريخ = t.TransferDate.ToString("yyyy/MM/dd"),
                من = t.FromWarehouse?.Name ?? "",
                إلى = t.ToWarehouse?.Name ?? "",
                العيار = t.KaratValue,
                الوزن = t.WeightGrams,
                ملاحظات = t.Notes
            });

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"نقل_مخازن_الذهب_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "النقلات");
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
            var (allItems, _) = await _warehouseService.GetTransfersPagedAsync(1, int.MaxValue);
            var columns = new[] { "التاريخ", "من", "إلى", "العيار", "الوزن (غ)", "ملاحظات" };
            IList<object[]> rows = allItems.Select(t => new object[]
            {
                t.TransferDate.ToString("yyyy/MM/dd"),
                t.FromWarehouse?.Name ?? "",
                t.ToWarehouse?.Name ?? "",
                t.KaratValue,
                t.WeightGrams.ToString("N2"),
                t.Notes
            }).ToList();
            _exportService.PrintTable("نقل مخازن الذهب", columns, rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }
}
