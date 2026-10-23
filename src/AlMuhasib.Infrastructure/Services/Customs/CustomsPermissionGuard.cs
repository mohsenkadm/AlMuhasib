using AlMuhasib.Core.Constants;
using AlMuhasib.Core.Enums.Customs;
using AlMuhasib.Core.Interfaces;

namespace AlMuhasib.Infrastructure.Services.Customs;

/// <summary>
/// Service-layer permission checks for Customs (not UI-only).
/// Admin bypasses; otherwise uses <see cref="ICurrentUserService"/> session flags.
/// </summary>
public sealed class CustomsPermissionGuard
{
    private readonly ICurrentUserService _currentUser;

    public CustomsPermissionGuard(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    public void EnsureView(string screenName) => Ensure(screenName, "View", () => _currentUser.CanView(screenName));
    public void EnsureAdd(string screenName) => Ensure(screenName, "Add", () => _currentUser.CanAdd(screenName) && !_currentUser.IsViewOnly(screenName));
    public void EnsureEdit(string screenName) => Ensure(screenName, "Edit", () => _currentUser.CanEdit(screenName) && !_currentUser.IsViewOnly(screenName));
    public void EnsureDelete(string screenName) => Ensure(screenName, "Delete", () => _currentUser.CanDelete(screenName) && !_currentUser.IsViewOnly(screenName));
    public void EnsurePrint(string screenName) => Ensure(screenName, "Print", () => _currentUser.CanPrint(screenName));
    public void EnsureExport(string screenName) => Ensure(screenName, "Export", () => _currentUser.CanExport(screenName));

    public void EnsurePrintOrExport(string screenName) =>
        Ensure(screenName, "Print", () => _currentUser.CanPrint(screenName) || _currentUser.CanExport(screenName));

    public bool CanView(string screenName) => _currentUser.IsAdmin || _currentUser.CanView(screenName);
    public bool CanViewExpenses => CanView(CustomsScreenNames.Expenses);
    public bool CanViewProfit => CanView(CustomsScreenNames.ProfitReport);

    public string ScreenForOwner(CustomsAttachmentOwnerType ownerType) => ownerType switch
    {
        CustomsAttachmentOwnerType.Customer => CustomsScreenNames.Customers,
        CustomsAttachmentOwnerType.Receipt => CustomsScreenNames.Receipts,
        CustomsAttachmentOwnerType.Invoice
            or CustomsAttachmentOwnerType.Vehicle
            or CustomsAttachmentOwnerType.Expense => CustomsScreenNames.Invoices,
        _ => CustomsScreenNames.Invoices
    };

    private void Ensure(string screenName, string actionLabel, Func<bool> allowed)
    {
        if (_currentUser.IsAdmin)
            return;

        if (allowed())
            return;

        throw new UnauthorizedAccessException(
            $"ليس لديك صلاحية {ActionArabic(actionLabel)} على «{ScreenArabic(screenName)}».");
    }

    private static string ActionArabic(string action) => action switch
    {
        "View" => "العرض",
        "Add" => "الإضافة",
        "Edit" => "التعديل",
        "Delete" => "الحذف",
        "Print" => "الطباعة",
        "Export" => "التصدير",
        _ => action
    };

    private static string ScreenArabic(string screenName) => screenName switch
    {
        CustomsScreenNames.Dashboard => "لوحة التحكم",
        CustomsScreenNames.Customers => "العملاء",
        CustomsScreenNames.Invoices => "الفواتير",
        CustomsScreenNames.Receipts => "وصولات القبض",
        CustomsScreenNames.Statement => "كشف حساب",
        CustomsScreenNames.Reports => "التقارير",
        CustomsScreenNames.ProfitReport => "تقرير الربح",
        CustomsScreenNames.Expenses => "المصاريف",
        _ => screenName
    };
}
