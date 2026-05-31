using System.Windows;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class ProfitComparisonReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _periodLabel = string.Empty;
    [ObservableProperty] private string _previousPeriodLabel = string.Empty;

    [ObservableProperty] private string _currentSales = "0";
    [ObservableProperty] private string _previousSales = "0";
    [ObservableProperty] private string _salesChange = "0%";

    [ObservableProperty] private string _currentGrossProfit = "0";
    [ObservableProperty] private string _previousGrossProfit = "0";
    [ObservableProperty] private string _grossProfitChange = "0%";

    [ObservableProperty] private string _currentNetProfit = "0";
    [ObservableProperty] private string _previousNetProfit = "0";
    [ObservableProperty] private string _netProfitChange = "0%";

    public ProfitComparisonReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "مقارنة الأرباح";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _reportService.GetProfitComparisonAsync(DateFrom, DateTo);

            PeriodLabel = $"{result.CurrentFrom:yyyy/MM/dd} — {result.CurrentTo:yyyy/MM/dd}";
            PreviousPeriodLabel = $"{result.PreviousFrom:yyyy/MM/dd} — {result.PreviousTo:yyyy/MM/dd}";

            CurrentSales = FormatCurrency(result.Current.TotalSales);
            PreviousSales = FormatCurrency(result.Previous.TotalSales);
            SalesChange = FormatChange(result.SalesChangePercent);

            CurrentGrossProfit = FormatCurrency(result.Current.GrossProfit);
            PreviousGrossProfit = FormatCurrency(result.Previous.GrossProfit);
            GrossProfitChange = FormatChange(result.GrossProfitChangePercent);

            CurrentNetProfit = FormatCurrency(result.Current.NetProfit);
            PreviousNetProfit = FormatCurrency(result.Previous.NetProfit);
            NetProfitChange = FormatChange(result.NetProfitChangePercent);
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

    private static string FormatChange(decimal percent) =>
        percent > 0 ? $"+{percent:N1}%" : $"{percent:N1}%";
}
