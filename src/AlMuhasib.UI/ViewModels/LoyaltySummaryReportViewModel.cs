using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class LoyaltySummaryReportViewModel : ReportViewModelBase
{
    private readonly ILoyaltyService _loyaltyService;

    [ObservableProperty] private string _earnedPoints = "0";
    [ObservableProperty] private string _redeemedPoints = "0";
    [ObservableProperty] private string _redeemValue = "0";
    [ObservableProperty] private string _activeCustomers = "0";
    [ObservableProperty] private string _transactionsCount = "0";
    [ObservableProperty] private string _adjustedPoints = "0";

    public LoyaltySummaryReportViewModel(
        ILoyaltyService loyaltyService,
        IReportService reportService,
        IUnitOfWork unitOfWork,
        IExportService exportService,
        ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        _loyaltyService = loyaltyService;
        PageTitle = "تقرير ملخص الولاء";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "LoyaltyReports");
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var report = await _loyaltyService.GetSummaryReportAsync(DateFrom, DateTo);
            EarnedPoints = report.TotalEarnedPoints.ToString("N0");
            RedeemedPoints = report.TotalRedeemedPoints.ToString("N0");
            RedeemValue = FormatCurrency(report.TotalRedeemDiscountValue);
            ActiveCustomers = report.ActiveCustomersCount.ToString("N0");
            TransactionsCount = report.TransactionsCount.ToString("N0");
            AdjustedPoints = report.TotalAdjustedPoints.ToString("N0");
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

    protected override void OnPageChanged() { }
}
