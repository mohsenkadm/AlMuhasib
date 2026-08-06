using System.Text.Json;
using AlMuhasib.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.Models;

/// <summary>صف إدخال منتج في واجهة الإضافة المتعددة (شبيه Excel).</summary>
public partial class BulkProductEntryRow : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _barcode = string.Empty;
    [ObservableProperty] private string _categoryName = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private string _scientificName = string.Empty;
    [ObservableProperty] private string _usageInstructions = string.Empty;
    [ObservableProperty] private decimal _weight;
    [ObservableProperty] private string _weightUnit = "كغ";
    [ObservableProperty] private string _discountTypeText = "بدون";
    [ObservableProperty] private decimal _discountValue;
    [ObservableProperty] private string _discountExpiresText = string.Empty;
    [ObservableProperty] private decimal _salePrice;
    [ObservableProperty] private decimal _purchasePrice;
    [ObservableProperty] private string _customField1 = string.Empty;
    [ObservableProperty] private string _customField2 = string.Empty;
    [ObservableProperty] private string _customField3 = string.Empty;
    [ObservableProperty] private string _customField4 = string.Empty;
    [ObservableProperty] private string _customField5 = string.Empty;
    [ObservableProperty] private string _customField6 = string.Empty;
    [ObservableProperty] private string _customField7 = string.Empty;
    [ObservableProperty] private string _customField8 = string.Empty;

    [ObservableProperty] private string _rowStatus = string.Empty;

    public bool HasName => !string.IsNullOrWhiteSpace(Name);

    public bool IsReadyToSave => HasName;

    partial void OnNameChanged(string value)
    {
        OnPropertyChanged(nameof(HasName));
        OnPropertyChanged(nameof(IsReadyToSave));
        RowChanged?.Invoke(this);
    }

    partial void OnBarcodeChanged(string value) => RowChanged?.Invoke(this);
    partial void OnCategoryNameChanged(string value) => RowChanged?.Invoke(this);

    public event Action<BulkProductEntryRow>? RowChanged;

    public DiscountType ParseDiscountType()
    {
        var value = DiscountTypeText?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(value) || value is "بدون" or "لا" or "none" or "0")
            return DiscountType.None;
        if (value.Contains("نسب", StringComparison.OrdinalIgnoreCase) || value.Contains('%') || value == "1")
            return DiscountType.Percentage;
        if (value.Contains("ثابت", StringComparison.OrdinalIgnoreCase)
            || value.Contains("قيمة", StringComparison.OrdinalIgnoreCase)
            || value == "2")
            return DiscountType.FixedAmount;
        return DiscountType.None;
    }

    public DateTime? ParseDiscountExpiry()
    {
        var value = DiscountExpiresText?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(value)) return null;
        if (DateTime.TryParse(value, out var dt))
            return dt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dt, DateTimeKind.Local).ToUniversalTime()
                : dt.ToUniversalTime();
        return null;
    }

    public string? BuildCustomFieldsJson(IReadOnlyList<(int Slot, string Label)> enabledSlots)
    {
        if (enabledSlots.Count == 0) return null;
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (slot, _) in enabledSlots)
        {
            var value = GetCustomField(slot)?.Trim();
            if (!string.IsNullOrEmpty(value))
                dict[$"cf{slot}"] = value;
        }

        return dict.Count == 0 ? null : JsonSerializer.Serialize(dict);
    }

    public string GetCustomField(int slot) => slot switch
    {
        1 => CustomField1,
        2 => CustomField2,
        3 => CustomField3,
        4 => CustomField4,
        5 => CustomField5,
        6 => CustomField6,
        7 => CustomField7,
        8 => CustomField8,
        _ => string.Empty
    };
}
