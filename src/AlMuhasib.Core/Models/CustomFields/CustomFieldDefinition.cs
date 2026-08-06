using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Models.CustomFields;

/// <summary>تعريف حقل مخصص واحد (من أصل 8 لكل واجهة).</summary>
public sealed class CustomFieldDefinition
{
    public const int MaxFieldsPerEntity = 8;

    /// <summary>رقم الحقل من 1 إلى 8.</summary>
    public int Slot { get; set; }

    /// <summary>البيان / وصف داخلي للحقل.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>هل الحقل مفعّل ويظهر في الجداول والنماذج.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>المسمى الظاهر في الجداول والنماذج.</summary>
    public string Label { get; set; } = string.Empty;

    public CustomFieldValueType FieldType { get; set; } = CustomFieldValueType.Text;

    /// <summary>خيارات القائمة المنسدلة عند النوع = اختيارات.</summary>
    public List<string> Choices { get; set; } = [];

    public string SlotKey => $"cf{Slot}";

    public string DisplayLabel =>
        string.IsNullOrWhiteSpace(Label)
            ? (string.IsNullOrWhiteSpace(Description) ? $"حقل {Slot}" : Description.Trim())
            : Label.Trim();
}
