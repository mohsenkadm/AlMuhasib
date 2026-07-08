using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.ViewModels;

public partial class DashboardViewModel
{
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
        // ملف العمل (كاشير/محاسب) يخصّص شريط المساعد السريع فقط.
        // لوحة التحكم تعرض كل الإحصائيات والإجراءات؛ الصلاحيات تحدّد الظهور.
        ShowDashboardQuickSales = _currentUserService.CanView("SaleInvoice");
        ShowDashboardQuickPurchase = _currentUserService.CanView("PurchaseInvoice");
        ShowDashboardQuickInstallment = _currentUserService.CanView("InstallmentInvoice");
        ShowCollectionDashboard = _currentUserService.CanView("Installments");

        ShowTodayPurchases = true;
        ShowNetProfit = true;
        ShowInvestorStats = true;
        ShowFinanceCharts = true;
    }
}
