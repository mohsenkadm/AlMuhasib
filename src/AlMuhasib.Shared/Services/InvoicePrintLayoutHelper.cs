using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.Shared.Services;

/// <summary>أعمدة جدول بنود الفاتورة في الطباعة (خصم السطر، المخزن).</summary>
internal static class InvoicePrintLayoutHelper
{
    internal sealed record ItemColumnLayout(
        bool ShowWarehouse,
        bool ShowLineDiscount,
        bool HideAmounts,
        bool Compact);

    internal static ItemColumnLayout Resolve(InvoicePrintModel model, bool compact)
    {
        var hideAmounts = model.HideAmounts;
        var showWarehouse = model.Items.Any(i => !string.IsNullOrWhiteSpace(i.WarehouseName));
        var showDiscount = !hideAmounts && model.ShowLineDiscount;
        return new ItemColumnLayout(showWarehouse, showDiscount, hideAmounts, compact);
    }

    internal static string FormatItemName(InvoicePrintItem item, InvoicePrintModel model, bool includeWarehouseInDescription)
    {
        var name = $"{item.Number}. {item.ItemName}";
        if (includeWarehouseInDescription && !string.IsNullOrWhiteSpace(item.WarehouseName))
            name += $"\nالمخزن: {item.WarehouseName}";
        if (model.PharmacyUsageReceipt && !string.IsNullOrWhiteSpace(item.UsageInstructions))
            name += $"\nطريقة الاستخدام: {item.UsageInstructions}";
        return name;
    }

    internal static string[] BuildColumnTitles(ItemColumnLayout layout, string currency)
    {
        if (layout.HideAmounts)
            return ["الوصف", "الكمية"];

        var titles = new List<string> { "الوصف", "الكمية" };
        if (layout.ShowWarehouse)
            titles.Add("المخزن");
        titles.Add($"سعر الوحدة ({currency})");
        if (layout.ShowLineDiscount)
            titles.Add("خصم %");
        titles.Add($"الإجمالي ({currency})");
        return titles.ToArray();
    }

    internal static double[] BuildNumericWidths(ItemColumnLayout layout)
    {
        if (layout.HideAmounts)
            return [110.0];

        var c = layout.Compact;
        var widths = new List<double> { c ? 58.0 : 66.0 };
        if (layout.ShowWarehouse)
            widths.Add(c ? 68.0 : 78.0);
        widths.Add(c ? 90.0 : 100.0);
        if (layout.ShowLineDiscount)
            widths.Add(c ? 46.0 : 52.0);
        widths.Add(c ? 95.0 : 110.0);
        return widths.ToArray();
    }

    internal static string FormatDiscountPercent(decimal percent) =>
        percent > 0 ? $"{percent:0.##}%" : "—";
}
