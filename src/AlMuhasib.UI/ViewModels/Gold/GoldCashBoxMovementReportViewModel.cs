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

public partial class GoldCashBoxMovementReportViewModel : GoldReportViewModelBase
{
    private readonly IGoldCashService _cashService;
    private List<GoldCashMovementRow> _allRows = [];

    public ObservableCollection<GoldCashMovementRow> Rows { get; } = [];
    public ObservableCollection<GoldCashBox> CashBoxes { get; } = [];

    [ObservableProperty] private int? _selectedCashBoxId;
    [ObservableProperty] private string _totalIn = "0";
    [ObservableProperty] private string _totalOut = "0";
    [ObservableProperty] private string _net = "0";

    public GoldCashBoxMovementReportViewModel(
        IGoldReportService reportService,
        IGoldCashService cashService,
        IExportService exportService,
        IToastNotificationService toast,
        ICurrentUserService currentUserService)
        : base(reportService, exportService, toast, currentUserService)
    {
        _cashService = cashService;
        PageTitle = "حركة القاصات";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(CurrentUserService, GoldShopPermissionRegistry.CashBoxMovementReport);
        CashBoxes.Clear();
        foreach (var box in await _cashService.GetCashBoxesAsync(activeOnly: false))
            CashBoxes.Add(box);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            _allRows = (await ReportService.GetCashBoxMovementReportAsync(SelectedCashBoxId, DateFrom, DateTo)).ToList();
            var inSum = _allRows.Sum(r => r.AmountIn);
            var outSum = _allRows.Sum(r => r.AmountOut);
            TotalIn = FormatCurrency(inSum);
            TotalOut = FormatCurrency(outSum);
            Net = FormatCurrency(inSum - outSum);
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
        var cols = new[] { "التاريخ", "النوع", "المرجع", "الطرف", "القاصة", "العملة", "وارد", "صادر", "ملاحظات" };
        var rows = _allRows.Select(r => new object[]
        {
            r.Date.ToString("yyyy/MM/dd"), r.MovementType, r.Reference, r.PartyName,
            r.CashBoxName, r.Currency.ToString(), r.AmountIn, r.AmountOut, r.Notes
        }).ToList();
        ExportTable("حركة_القاصات.xlsx", "حركة القاصات", cols, rows);
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "التاريخ", "النوع", "المرجع", "القاصة", "وارد", "صادر" };
        var rows = _allRows.Select(r => new object[]
        {
            r.Date.ToString("yyyy/MM/dd"), r.MovementType, r.Reference,
            r.CashBoxName, r.AmountIn, r.AmountOut
        }).ToList();
        PrintTable("حركة القاصات", cols, rows);
    }
}
