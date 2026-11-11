using AlMuhasib.Core.Intelligence;
using AlMuhasib.UI.Services;

namespace AlMuhasib.UI.Services;

/// <summary>
/// يفتح المساعد فقط بصلاحية «المساعد الذكي»، ويمنع أي مجال لا تغطيه صلاحية الشاشة الأصلية أيضاً.
/// </summary>
public sealed class AssistantAccessGuard : IAssistantAccessGuard
{
    private readonly CurrentUserService _user;

    public AssistantAccessGuard(CurrentUserService user) => _user = user;

    public bool CanOpen => IsGranted(ScreenPermissionRegistry.AiAssistant);

    public bool CanAccess(AssistantDataDomain domain)
    {
        var (section, businessScreens) = Map(domain);
        return IsGranted(section) && businessScreens.Any(IsBusinessGranted);
    }

    private bool IsGranted(string screen)
    {
        if (!_user.IsPermissionRecorded(screen))
            return _user.IsAdmin;
        return _user.CanView(screen);
    }

    private bool IsBusinessGranted(string screen)
    {
        if (!_user.IsPermissionRecorded(screen))
            return _user.IsAdmin;
        return _user.CanView(screen);
    }

    private static (string Section, string[] Business) Map(AssistantDataDomain domain) => domain switch
    {
        AssistantDataDomain.Sales => (ScreenPermissionRegistry.AiSales, ["SaleInvoice", ScreenPermissionRegistry.Reports, ScreenPermissionRegistry.CashierPos]),
        AssistantDataDomain.Purchases => (ScreenPermissionRegistry.AiPurchases, ["PurchaseInvoice", ScreenPermissionRegistry.Reports]),
        AssistantDataDomain.Expenses => (ScreenPermissionRegistry.AiExpenses, ["Expenses", ScreenPermissionRegistry.Reports]),
        AssistantDataDomain.Profit => (ScreenPermissionRegistry.AiProfit, [ScreenPermissionRegistry.Reports]),
        AssistantDataDomain.CustomerBalances => (ScreenPermissionRegistry.AiCustomerBalances, ["Customers", ScreenPermissionRegistry.Reports]),
        AssistantDataDomain.Suppliers => (ScreenPermissionRegistry.AiSuppliers, ["Suppliers", ScreenPermissionRegistry.Reports]),
        AssistantDataDomain.Cash => (ScreenPermissionRegistry.AiCash, ["CashAndBank", ScreenPermissionRegistry.Reports]),
        AssistantDataDomain.Inventory => (ScreenPermissionRegistry.AiInventory, ["Products", "Warehouses", ScreenPermissionRegistry.MinimumQuantityReport]),
        AssistantDataDomain.FinancialReports => (ScreenPermissionRegistry.AiFinancialReports, [ScreenPermissionRegistry.Reports, ScreenPermissionRegistry.BalanceSheet]),
        _ => (ScreenPermissionRegistry.AiAssistant, [])
    };
}
