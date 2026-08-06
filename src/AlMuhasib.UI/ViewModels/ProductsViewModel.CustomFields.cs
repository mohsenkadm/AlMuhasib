using System.Collections.ObjectModel;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.CustomFields;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.ViewModels;

public partial class ProductsViewModel
{
    private ICustomFieldSettingsService? _customFieldSettings;

    public ObservableCollection<CustomFieldEditItem> EditCustomFields { get; } = [];

    public ObservableCollection<CustomFieldColumnState> CustomFieldColumns { get; } = [];

    [ObservableProperty]
    private int _customFieldColumnsVersion;

    [ObservableProperty]
    private bool _hasCustomFields;

    private void ConfigureCustomFields(ICustomFieldSettingsService customFieldSettings) =>
        _customFieldSettings = customFieldSettings;

    private async Task LoadCustomFieldDefinitionsAsync()
    {
        if (_customFieldSettings is null) return;

        var enabled = await _customFieldSettings.GetEnabledDefinitionsAsync(CustomFieldEntityKind.Products);
        CustomFieldColumns.Clear();
        foreach (var def in enabled)
        {
            CustomFieldColumns.Add(new CustomFieldColumnState
            {
                Slot = def.Slot,
                IsEnabled = true,
                Label = def.DisplayLabel
            });
        }

        HasCustomFields = CustomFieldColumns.Count > 0;
        CustomFieldColumnsVersion++;
    }

    private async Task ResetCustomFieldEditorsAsync(string? existingJson)
    {
        EditCustomFields.Clear();
        if (_customFieldSettings is null) return;

        var enabled = await _customFieldSettings.GetEnabledDefinitionsAsync(CustomFieldEntityKind.Products);
        foreach (var item in CustomFieldEditFactory.CreateEditItems(enabled, existingJson))
            EditCustomFields.Add(item);
        HasCustomFields = EditCustomFields.Count > 0;
    }

    private string? SerializeCustomFieldsFromEditors() =>
        CustomFieldEditFactory.SerializeEditItems(EditCustomFields);

    public IReadOnlyList<CustomFieldColumnState> GetCustomFieldColumnStates() => CustomFieldColumns.ToList();
}
