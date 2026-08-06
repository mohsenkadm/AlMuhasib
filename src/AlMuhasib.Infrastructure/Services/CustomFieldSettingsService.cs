using System.Text.Json;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.CustomFields;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class CustomFieldSettingsService : ICustomFieldSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public CustomFieldSettingsService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<CustomFieldDefinition>> GetDefinitionsAsync(CustomFieldEntityKind entityKind)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var row = await context.EntityCustomFieldSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.EntityKind == entityKind);

        return Normalize(Deserialize(row?.DefinitionsJson));
    }

    public async Task<IReadOnlyList<CustomFieldDefinition>> GetEnabledDefinitionsAsync(CustomFieldEntityKind entityKind)
    {
        var all = await GetDefinitionsAsync(entityKind);
        return all.Where(d => d.IsEnabled && !string.IsNullOrWhiteSpace(d.DisplayLabel)).ToList();
    }

    public async Task SaveDefinitionsAsync(CustomFieldEntityKind entityKind, IReadOnlyList<CustomFieldDefinition> definitions)
    {
        var normalized = Normalize(definitions);
        var json = JsonSerializer.Serialize(normalized, JsonOptions);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var row = await context.EntityCustomFieldSettings
            .FirstOrDefaultAsync(s => s.EntityKind == entityKind);

        if (row is null)
        {
            context.EntityCustomFieldSettings.Add(new EntityCustomFieldSettings
            {
                EntityKind = entityKind,
                DefinitionsJson = json,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system"
            });
        }
        else
        {
            row.DefinitionsJson = json;
            row.UpdatedAt = DateTime.UtcNow;
            row.UpdatedBy = "system";
        }

        await context.SaveChangesAsync();
    }

    private static List<CustomFieldDefinition> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<CustomFieldDefinition>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static List<CustomFieldDefinition> Normalize(IEnumerable<CustomFieldDefinition>? source)
    {
        var map = (source ?? [])
            .Where(d => d.Slot is >= 1 and <= CustomFieldDefinition.MaxFieldsPerEntity)
            .GroupBy(d => d.Slot)
            .ToDictionary(g => g.Key, g => g.First());

        var result = new List<CustomFieldDefinition>(CustomFieldDefinition.MaxFieldsPerEntity);
        for (var slot = 1; slot <= CustomFieldDefinition.MaxFieldsPerEntity; slot++)
        {
            if (map.TryGetValue(slot, out var existing))
            {
                existing.Slot = slot;
                existing.Description = existing.Description?.Trim() ?? string.Empty;
                existing.Label = existing.Label?.Trim() ?? string.Empty;
                existing.Choices = (existing.Choices ?? [])
                    .Select(c => c?.Trim() ?? string.Empty)
                    .Where(c => c.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (existing.FieldType != CustomFieldValueType.Choice)
                    existing.Choices = [];
                result.Add(existing);
            }
            else
            {
                result.Add(new CustomFieldDefinition
                {
                    Slot = slot,
                    Description = $"حقل {slot}",
                    Label = $"حقل {slot}",
                    IsEnabled = false,
                    FieldType = CustomFieldValueType.Text
                });
            }
        }

        return result;
    }
}
