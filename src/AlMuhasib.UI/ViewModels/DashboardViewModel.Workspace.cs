using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.ViewModels;

public partial class DashboardViewModel
{
    private readonly IUserPreferencesService _userPreferences;
    private readonly ICurrentUserService _currentUserService;

    [ObservableProperty] private bool _showDashboardQuickSales = true;
    [ObservableProperty] private bool _showDashboardQuickPurchase = true;
    [ObservableProperty] private bool _showDashboardQuickInstallment = true;
    [ObservableProperty] private bool _showCollectionDashboard;
    [ObservableProperty] private bool _showTodayPurchases = true;
    [ObservableProperty] private bool _showNetProfit = true;
    [ObservableProperty] private bool _showInvestorStats = true;
    [ObservableProperty] private bool _showFinanceCharts = true;

    private void ApplyDashboardProfile()
    {
        switch (_userPreferences.Current.WorkspaceProfile)
        {
            case WorkspaceProfile.Cashier:
                ShowDashboardQuickPurchase = false;
                ShowDashboardQuickInstallment = true;
                ShowTodayPurchases = false;
                ShowNetProfit = false;
                ShowInvestorStats = false;
                ShowFinanceCharts = false;
                break;
            case WorkspaceProfile.Accountant:
                ShowDashboardQuickPurchase = true;
                ShowDashboardQuickInstallment = false;
                ShowTodayPurchases = true;
                ShowNetProfit = true;
                ShowInvestorStats = false;
                ShowFinanceCharts = true;
                break;
            default:
                ShowDashboardQuickPurchase = true;
                ShowDashboardQuickInstallment = true;
                ShowTodayPurchases = true;
                ShowNetProfit = true;
                ShowInvestorStats = true;
                ShowFinanceCharts = true;
                break;
        }

        ShowDashboardQuickSales = _currentUserService.CanView("SaleInvoice");
        ShowDashboardQuickPurchase = ShowDashboardQuickPurchase && _currentUserService.CanView("PurchaseInvoice");
        ShowDashboardQuickInstallment = ShowDashboardQuickInstallment && _currentUserService.CanView("InstallmentInvoice");
        ShowCollectionDashboard = _currentUserService.CanView("Installments");
    }
}
