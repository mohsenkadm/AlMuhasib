using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Models.CustomFields;

namespace AlMuhasib.Core.Interfaces.Services;

public interface ICustomFieldSettingsService
{
    Task<IReadOnlyList<CustomFieldDefinition>> GetDefinitionsAsync(CustomFieldEntityKind entityKind);
    Task<IReadOnlyList<CustomFieldDefinition>> GetEnabledDefinitionsAsync(CustomFieldEntityKind entityKind);
    Task SaveDefinitionsAsync(CustomFieldEntityKind entityKind, IReadOnlyList<CustomFieldDefinition> definitions);
}
