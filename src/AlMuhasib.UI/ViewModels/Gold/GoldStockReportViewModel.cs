using System.Collections.ObjectModel;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldStockReportViewModel : ViewModelBase
{
    private readonly IGoldReportService _reportService;
    private readonly IToastNotificationService _toast;
    private readonly ICurrentUserService _currentUserService;

    public ObservableCollection<GoldStockRow> Rows { get; } = [];

    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private decimal _totalGrams;
    [ObservableProperty] private decimal _totalValue;
    [ObservableProperty] private int _pieceCount;
    [ObservableProperty] private int _lowStockCount;

    public GoldStockReportViewModel(
        IGoldReportService reportService,
        IToastNotificationService toast,
        ICurrentUserService currentUserService)
    {
        _reportService = reportService;
        _toast = toast;
        _currentUserService = currentUserService;
        PageTitle = "تقرير المخزون";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.StockReport);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var rows = await _reportService.GetStockReportAsync();
            Rows.Clear();
            foreach (var r in rows)
                Rows.Add(r);

            TotalGrams = rows.Sum(r => r.GramsOnHand);
            TotalValue = rows.Sum(r => r.StockValue);
            PieceCount = rows.Sum(r => r.PieceCount);
            LowStockCount = rows.Count(r => r.IsLowStock);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _toast.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
