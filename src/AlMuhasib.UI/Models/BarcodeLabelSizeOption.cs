using AlMuhasib.Core.Enums;

namespace AlMuhasib.UI.Models;

public sealed class BarcodeLabelSizeOption
{
    public BarcodeLabelSizeOption(BarcodeLabelSize size, string displayName)
    {
        Size = size;
        DisplayName = displayName;
    }

    public BarcodeLabelSize Size { get; }
    public string DisplayName { get; }
    public bool IsCustom => Size == BarcodeLabelSize.Custom;

    public static IReadOnlyList<BarcodeLabelSizeOption> CreateAll() =>
        BarcodeLabelSizeHelper.AllSizes
            .Select(s => new BarcodeLabelSizeOption(s, BarcodeLabelSizeHelper.GetDisplayName(s)))
            .ToList();
}

public sealed class BarcodePriceOption
{
    public BarcodePriceOption(int? pricingTypeId, string pricingTypeName, decimal? salePrice, bool isDefault)
    {
        PricingTypeId = pricingTypeId;
        PricingTypeName = pricingTypeName;
        SalePrice = salePrice;
        IsDefault = isDefault;
        DisplayText = salePrice is > 0
            ? $"{salePrice:N0} د.ع — {pricingTypeName}"
            : pricingTypeName;
    }

    public int? PricingTypeId { get; }
    public string PricingTypeName { get; }
    public decimal? SalePrice { get; }
    public bool IsDefault { get; }
    public string DisplayText { get; }
}
