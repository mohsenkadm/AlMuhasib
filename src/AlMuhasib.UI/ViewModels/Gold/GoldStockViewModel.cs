using System.Collections.ObjectModel;
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

public partial class GoldStockViewModel : ViewModelBase
{
    private readonly IGoldInventoryService _inventoryService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly MainWindowViewModel _mainWindow;
    private List<GoldStockRow> _allRows = [];

    [ObservableProperty] private GoldStockRow? _selectedRow;
    [ObservableProperty] private int _lowStockCount;
    [ObservableProperty] private decimal _totalGrams;
    [ObservableProperty] private decimal _totalValue;

    public ObservableCollection<GoldStockRow> StockRows { get; } = [];

    public GoldStockViewModel(
        IGoldInventoryService inventoryService,
        IExportService exportService,
        ICurrentUserService currentUserService,
        MainWindowViewModel mainWindow)
    {
        _inventoryService = inventoryService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        _mainWindow = mainWindow;
        PageTitle = "المخزون";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.Stock);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            _allRows = (await _inventoryService.GetStockBalancesAsync()).ToList();
            LowStockCount = _allRows.Count(r => r.IsLowStock);
            TotalGrams = _allRows.Sum(r => r.GramsOnHand);
            TotalValue = _allRows.Sum(r => r.StockValue);
            ApplyFilters();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر تحميل المخزون:\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFilters()
    {
        var filtered = MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters)
            ? ColumnFilterEngine.Apply(_allRows, ColumnFilters)
            : _allRows.ToList();

        StockRows.Clear();
        foreach (var row in filtered)
            StockRows.Add(row);
    }

    protected override void OnColumnFiltersChanged() => ApplyFilters();

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task OpenAdjustmentAsync() =>
        await _mainWindow.OpenTabAsync(
            typeof(GoldStockAdjustmentViewModel),
            "تسوية مخزون",
            MaterialDesignThemes.Wpf.PackIconKind.TuneVerticalVariant);

    [RelayCommand]
    private async Task ExportToExcel()
    {
        try
        {
            if (_allRows.Count == 0)
                await LoadAsync();

            var exportData = _allRows.Select(r => new
            {
                العيار = r.KaratName,
                القيمة = r.KaratValue,
                الوزن = r.GramsOnHand,
                متوسط_التكلفة = r.AverageCostPerGram,
                قيمة_المخزون = r.StockValue,
                عدد_القطع = r.PieceCount,
                منخفض = r.IsLowStock ? "نعم" : "لا"
            });

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"مخزون_الذهب_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "المخزون");
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
            if (_allRows.Count == 0)
                await LoadAsync();

            var columns = new[] { "العيار", "القيمة", "الوزن (غ)", "متوسط التكلفة/غ", "قيمة المخزون", "عدد القطع", "تنبيه" };
            IList<object[]> rows = _allRows.Select(r => new object[]
            {
                r.KaratName,
                r.KaratValue,
                r.GramsOnHand.ToString("N2"),
                r.AverageCostPerGram.ToString("N0"),
                r.StockValue.ToString("N0"),
                r.PieceCount,
                r.IsLowStock ? "منخفض" : ""
            }).ToList();
            _exportService.PrintTable("أرصدة المخزون حسب العيار", columns, rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }
}
