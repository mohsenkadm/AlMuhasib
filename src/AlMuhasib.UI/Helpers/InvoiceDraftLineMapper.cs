using AlMuhasib.Core.Entities;
using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Helpers;

/// <summary>تحويل بنود المسودة/قائمة الانتظار من وإلى صفوف الفاتورة مع التعبئة والتسعير.</summary>
public static class InvoiceDraftLineMapper
{
    public static SalesInvoiceDraftLine ToDraftLine(InvoiceItemRow row) => new()
    {
        ProductId = row.ProductId ?? 0,
        ProductName = row.ItemName,
        Quantity = row.Quantity,
        UnitPrice = row.UnitPrice,
        DiscountAmount = row.DiscountAmount,
        PricingTypeId = row.PricingTypeId,
        PricingTypeName = row.PricingTypeName ?? string.Empty,
        SelectedUnitName = row.SelectedUnitName ?? string.Empty,
        UnitConversionFactor = row.UnitConversionFactor <= 0 ? 1m : row.UnitConversionFactor,
        CustomField1 = NullIfEmpty(row.CustomField1),
        CustomField2 = NullIfEmpty(row.CustomField2),
        CustomField1Label = NullIfEmpty(row.CustomField1Label),
        CustomField2Label = NullIfEmpty(row.CustomField2Label),
        SizeName = NullIfEmpty(row.SizeName),
        ProductSizeId = row.ProductSizeId,
        ColorName = NullIfEmpty(row.ColorName),
        ProductColorId = row.ProductColorId,
        SerialNumber = NullIfEmpty(row.SerialNumber),
        BatchNumber = NullIfEmpty(row.BatchNumber),
        BatchId = row.BatchId,
        ExpiryDate = row.ExpiryDate
    };

    public static InvoiceItemRow ToRow(SalesInvoiceDraftLine line, IEnumerable<Product> products)
    {
        Product? product = null;
        if (line.ProductId > 0)
            product = products.FirstOrDefault(p => p.Id == line.ProductId);

        var row = new InvoiceItemRow
        {
            ProductId = line.ProductId > 0 ? line.ProductId : null,
            ItemName = string.IsNullOrWhiteSpace(line.ProductName) && product is not null
                ? product.Name
                : line.ProductName,
            Quantity = line.Quantity,
            UnitPrice = line.UnitPrice,
            DiscountAmount = line.DiscountAmount,
            PricingTypeId = line.PricingTypeId,
            PricingTypeName = line.PricingTypeName ?? string.Empty,
            SelectedUnitName = line.SelectedUnitName ?? string.Empty,
            UnitConversionFactor = line.UnitConversionFactor <= 0 ? 1m : line.UnitConversionFactor,
            CustomField1 = line.CustomField1 ?? string.Empty,
            CustomField2 = line.CustomField2 ?? string.Empty,
            CustomField1Label = line.CustomField1Label ?? string.Empty,
            CustomField2Label = line.CustomField2Label ?? string.Empty,
            SizeName = line.SizeName ?? string.Empty,
            ProductSizeId = line.ProductSizeId,
            ColorName = line.ColorName ?? string.Empty,
            ProductColorId = line.ProductColorId,
            SerialNumber = line.SerialNumber ?? string.Empty,
            BatchNumber = line.BatchNumber ?? string.Empty,
            BatchId = line.BatchId,
            ExpiryDate = line.ExpiryDate
        };

        if (product is not null)
            row.AttachProductSilent(product);

        if (line.PricingTypeId is int pricingTypeId && pricingTypeId > 0)
        {
            row.SetSelectedPricingOptionWithoutPrice(new ProductPricingOption
            {
                PricingTypeId = pricingTypeId,
                Name = line.PricingTypeName ?? string.Empty,
                Price = line.UnitPrice
            });
        }

        // أعد القيم المحفوظة بعد أي آثار جانبية لتعيين المنتج
        row.ItemName = string.IsNullOrWhiteSpace(line.ProductName) && product is not null
            ? product.Name
            : line.ProductName;
        row.Quantity = line.Quantity;
        row.UnitPrice = line.UnitPrice;
        row.DiscountAmount = line.DiscountAmount;
        row.SelectedUnitName = line.SelectedUnitName ?? string.Empty;
        row.UnitConversionFactor = line.UnitConversionFactor <= 0 ? 1m : line.UnitConversionFactor;
        row.PricingTypeId = line.PricingTypeId;
        row.PricingTypeName = line.PricingTypeName ?? string.Empty;

        return row;
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
