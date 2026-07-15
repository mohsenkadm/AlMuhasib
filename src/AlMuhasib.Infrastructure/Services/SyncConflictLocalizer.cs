using System.Text;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Sync.Responses;

namespace AlMuhasib.Infrastructure.Services;

internal static class SyncConflictLocalizer
{
    private static readonly Dictionary<string, string> EntityNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Category"] = "تصنيف",
        ["Product"] = "منتج",
        ["PricingType"] = "نوع تسعير",
        ["ProductPrice"] = "سعر منتج",
        ["BusinessSettings"] = "إعدادات النشاط",
        ["Warehouse"] = "مخزن",
        ["Customer"] = "عميل",
        ["Supplier"] = "مورد",
        ["CashBox"] = "صندوق",
        ["BankAccount"] = "حساب بنكي",
        ["Investor"] = "مستثمر",
        ["ExpenseType"] = "نوع مصروف",
        ["PrintBrandingSettings"] = "إعدادات الطباعة",
        ["WarehouseStock"] = "رصيد مخزن",
        ["Invoice"] = "فاتورة",
        ["InvoiceItem"] = "بند فاتورة",
        ["InstallmentPlan"] = "خطة أقساط",
        ["Installment"] = "قسط",
        ["Voucher"] = "سند",
        ["Expense"] = "مصروف",
        ["Transfer"] = "تحويل",
        ["InvestorTransaction"] = "عملية مستثمر",
        ["ProfitDistribution"] = "توزيع أرباح",
        ["ProfitDistributionDetail"] = "تفصيل توزيع أرباح",
        ["CapitalEntry"] = "قيد رأس مال",
        ["CustomerAttachment"] = "مرفق عميل",
        ["HotelSettings"] = "إعدادات فندق",
        ["HotelFloor"] = "طابق",
        ["HotelRoomType"] = "نوع غرفة",
        ["HotelRoom"] = "غرفة",
        ["HotelGuest"] = "ضيف",
        ["HotelReservation"] = "حجز",
        ["HotelReservationCharge"] = "رسوم حجز",
        ["HotelReservationPayment"] = "دفعة حجز",
        ["HotelCashBox"] = "صندوق فندق",
        ["HotelExpenseType"] = "نوع مصروف فندق",
        ["HotelExpense"] = "مصروف فندق",
        ["HotelVoucher"] = "سند فندق",
        ["HotelRatePlan"] = "خطة أسعار",
        ["HotelRatePlanSeason"] = "موسم أسعار",
        ["HotelHousekeepingTask"] = "مهمة نظافة",
        ["RestaurantIngredient"] = "مكون مطعم",
        ["RestaurantIngredientStock"] = "مخزون مكون",
        ["RestaurantMenuCategory"] = "تصنيف قائمة",
        ["RestaurantRecipe"] = "وصفة",
        ["RestaurantMenuItem"] = "صنف قائمة",
        ["RestaurantRecipeLine"] = "سطر وصفة",
        ["RestaurantTable"] = "طاولة",
        ["RestaurantOrder"] = "طلب مطعم",
        ["RestaurantOrderLine"] = "سطر طلب",
        ["RestaurantOrderPayment"] = "دفعة طلب",
        ["RestaurantStockMovement"] = "حركة مخزون مطعم",
        ["CarSaleContract"] = "عقد بيع سيارة",
        ["CarContractPayment"] = "دفعة عقد سيارة",
        ["CarTradeTransaction"] = "صفقة تجارة سيارات",
        ["CarTradePayment"] = "دفعة تجارة سيارات"
    };

    private static readonly Dictionary<string, string> Reasons = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Server version is newer"] = "نسخة السحابة أحدث من النسخة المحلية — تم رفض الرفع حتى لا تُستبدل بيانات أحدث",
        ["Category not found"] = "التصنيف المرتبط غير موجود على السحابة",
        ["Warehouse not found"] = "المخزن المرتبط غير موجود على السحابة",
        ["Invoice not found"] = "الفاتورة المرتبطة غير موجودة على السحابة",
        ["Plan not found"] = "خطة الأقساط المرتبطة غير موجودة على السحابة — أعد المزامنة بعد تحديث التطبيق",
        ["CashBox not found"] = "الصندوق المرتبط غير موجود على السحابة",
        ["Investor not found"] = "المستثمر المرتبط غير موجود على السحابة",
        ["Customer not found"] = "العميل المرتبط غير موجود على السحابة",
        ["FK not found"] = "سجل مرتبط (مرجع) غير موجود على السحابة",
        ["Account not found"] = "الحساب المرتبط غير موجود على السحابة",
        ["Floor or room type not found"] = "الطابق أو نوع الغرفة غير موجود",
        ["Guest not found"] = "الضيف المرتبط غير موجود",
        ["Reservation not found"] = "الحجز المرتبط غير موجود",
        ["Expense type not found"] = "نوع المصروف غير موجود",
        ["Cash box not found"] = "الصندوق غير موجود",
        ["Room type not found"] = "نوع الغرفة غير موجود",
        ["Rate plan not found"] = "خطة الأسعار غير موجودة",
        ["Room not found"] = "الغرفة غير موجودة",
        ["Ingredient not found"] = "المكون غير موجود",
        ["Recipe or ingredient not found"] = "الوصفة أو المكون غير موجود",
        ["Order or item not found"] = "الطلب أو الصنف غير موجود",
        ["Order not found"] = "الطلب غير موجود",
        ["Transaction not found"] = "الصفقة المرتبطة غير موجودة",
        ["Contract not found"] = "العقد المرتبط غير موجود",
        ["SyncId فارغ — حدّث التطبيق المحلي وأعد المزامنة"] = "معرّف المزامنة فارغ — حدّث التطبيق المحلي وأعد المزامنة"
    };

    public static SyncConflictInfo Map(SyncConflict conflict)
    {
        var entityAr = EntityNames.TryGetValue(conflict.EntityType, out var e) ? e : conflict.EntityType;
        var reasonAr = TranslateReason(conflict.Reason);

        return new SyncConflictInfo
        {
            EntityType = conflict.EntityType,
            EntityTypeArabic = entityAr,
            SyncId = conflict.SyncId,
            Reason = conflict.Reason,
            ReasonArabic = reasonAr
        };
    }

    private static string TranslateReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return "سبب غير معروف";
        if (Reasons.TryGetValue(reason, out var exact))
            return exact;
        if (reason.StartsWith("Plan not found", StringComparison.OrdinalIgnoreCase))
            return "خطة الأقساط المرتبطة غير موجودة على السحابة — أعد المزامنة بعد تحديث التطبيق";
        return reason;
    }

    public static IReadOnlyList<SyncConflictInfo> MapAll(IEnumerable<SyncConflict> conflicts) =>
        conflicts.Select(Map).ToList();

    public static string BuildDiagnostics(
        int acceptedCount,
        IReadOnlyList<SyncConflictInfo> conflicts,
        string apiBaseUrl,
        string username)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== تشخيص مزامنة قيد / AlMuhasib Sync Diagnostics ===");
        sb.AppendLine($"TimeUtc: {DateTime.UtcNow:O}");
        sb.AppendLine($"ApiBaseUrl: {apiBaseUrl}");
        sb.AppendLine($"Username: {username}");
        sb.AppendLine($"AcceptedCount: {acceptedCount}");
        sb.AppendLine($"ConflictCount: {conflicts.Count}");
        sb.AppendLine();
        sb.AppendLine("التعارضات تعني أن بعض السجلات رُفضت على السحابة (نسخة أحدث على السيرفر، أو مرجع مفقود).");
        sb.AppendLine();

        if (conflicts.Count == 0)
        {
            sb.AppendLine("لا توجد تعارضات.");
            return sb.ToString();
        }

        sb.AppendLine("--- Conflicts ---");
        var i = 1;
        foreach (var c in conflicts)
        {
            sb.AppendLine($"{i}. EntityType={c.EntityType} ({c.EntityTypeArabic})");
            sb.AppendLine($"   SyncId={c.SyncId:D}");
            sb.AppendLine($"   Reason={c.Reason}");
            sb.AppendLine($"   ReasonAr={c.ReasonArabic}");
            sb.AppendLine();
            i++;
        }

        return sb.ToString().TrimEnd();
    }
}
