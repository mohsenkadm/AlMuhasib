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

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldKaratMovementReportViewModel : GoldReportViewModelBase
{
    private readonly IGoldWarehouseService _warehouseService;
    private List<GoldKaratMovementRow> _allRows = [];

    public ObservableCollection<GoldKaratMovementRow> Rows { get; } = [];
    public ObservableCollection<GoldWarehouse> Warehouses { get; } = [];

    [ObservableProperty] private int? _selectedWarehouseId;
    [ObservableProperty] private string _netMovement = "0";
    [ObservableProperty] private string _soldGrams = "0";
    [ObservableProperty] private string _purchasedGrams = "0";
    [ObservableProperty] private string _closingGrams = "0";

    public GoldKaratMovementReportViewModel(
        IGoldReportService reportService,
        IGoldWarehouseService warehouseService,
        IExportService exportService,
        IToastNotificationService toast,
        ICurrentUserService currentUserService)
        : base(reportService, exportService, toast, currentUserService)
    {
        _warehouseService = warehouseService;
        PageTitle = "حركة العيارات";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(CurrentUserService, GoldShopPermissionRegistry.KaratMovementReport);
        Warehouses.Clear();
        foreach (var w in await _warehouseService.GetAllAsync(activeOnly: true))
            Warehouses.Add(w);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            _allRows = (await ReportService.GetKaratMovementReportAsync(DateFrom, DateTo, SelectedWarehouseId)).ToList();
            NetMovement = _allRows.Sum(r => r.NetMovementGrams).ToString("N3");
            SoldGrams = _allRows.Sum(r => r.SoldGrams).ToString("N3");
            PurchasedGrams = _allRows.Sum(r => r.PurchasedGrams).ToString("N3");
            ClosingGrams = _allRows.Sum(r => r.ClosingGrams).ToString("N3");
            CurrentPage = 1;
            UpdatePagination(_allRows, Rows);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Toast.ShowError(ex.Message);
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected override void OnPageChanged() => UpdatePagination(_allRows, Rows);

    [RelayCommand]
    private void ExportToExcel()
    {
        var cols = new[] { "العيار", "المخزن", "مشتريات", "مبيعات", "وارد تبديل", "صادر تبديل", "تحويل وارد", "تحويل صادر", "صافي", "رصيد" };
        var rows = _allRows.Select(r => new object[]
        {
            r.KaratName, r.WarehouseName, r.PurchasedGrams, r.SoldGrams, r.ExchangeInGrams,
            r.ExchangeOutGrams, r.TransferredInGrams, r.TransferredOutGrams, r.NetMovementGrams, r.ClosingGrams
        }).ToList();
        ExportTable("حركة_العيارات.xlsx", "حركة العيارات", cols, rows);
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "العيار", "المخزن", "مشتريات", "مبيعات", "صافي", "رصيد" };
        var rows = _allRows.Select(r => new object[]
        {
            r.KaratName, r.WarehouseName, r.PurchasedGrams, r.SoldGrams, r.NetMovementGrams, r.ClosingGrams
        }).ToList();
        PrintTable("حركة العيارات", cols, rows);
    }
}
