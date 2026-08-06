using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities;

/// <summary>إعدادات الحقول المخصصة لكل واجهة (منتجات، عملاء، …).</summary>
public class EntityCustomFieldSettings : BaseEntity
{
    public CustomFieldEntityKind EntityKind { get; set; }

    /// <summary>JSON لمصفوفة تعريفات الحقول (حتى 8).</summary>
    public string DefinitionsJson { get; set; } = "[]";
}
