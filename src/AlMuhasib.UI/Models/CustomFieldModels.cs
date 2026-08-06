using System.Collections.ObjectModel;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Models.CustomFields;
using AlMuhasib.UI.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.Models;

public partial class CustomFieldEditItem : ObservableObject
{
    public int Slot { get; init; }
    public string SlotKey => CustomFieldsHelper.SlotKey(Slot);
    public string Label { get; init; } = string.Empty;
    public CustomFieldValueType FieldType { get; init; }
    public ObservableCollection<string> Choices { get; init; } = [];

    [ObservableProperty]
    private string _textValue = string.Empty;

    [ObservableProperty]
    private bool _boolValue;

    [ObservableProperty]
    private string _numberValue = string.Empty;

    [ObservableProperty]
    private string? _selectedChoice;

    public bool IsText => FieldType == CustomFieldValueType.Text;
    public bool IsBoolean => FieldType == CustomFieldValueType.Boolean;
    public bool IsNumber => FieldType == CustomFieldValueType.Number;
    public bool IsChoice => FieldType == CustomFieldValueType.Choice;

    public static CustomFieldEditItem FromDefinition(CustomFieldDefinition def, string? raw)
    {
        var item = new CustomFieldEditItem
        {
            Slot = def.Slot,
            Label = def.DisplayLabel,
            FieldType = def.FieldType,
            Choices = new ObservableCollection<string>(def.Choices ?? [])
        };

        raw = raw?.Trim() ?? string.Empty;
        switch (def.FieldType)
        {
            case CustomFieldValueType.Boolean:
                item.BoolValue = CustomFieldsHelper.IsTruthy(raw);
                break;
            case CustomFieldValueType.Number:
                item.NumberValue = raw;
                break;
            case CustomFieldValueType.Choice:
                item.SelectedChoice = string.IsNullOrWhiteSpace(raw) ? null : raw;
                break;
            default:
                item.TextValue = raw;
                break;
        }

        return item;
    }

    public string? ToStorageValue() => FieldType switch
    {
        CustomFieldValueType.Boolean => CustomFieldsHelper.FormatBooleanStorage(BoolValue),
        CustomFieldValueType.Number => string.IsNullOrWhiteSpace(NumberValue) ? null : NumberValue.Trim(),
        CustomFieldValueType.Choice => string.IsNullOrWhiteSpace(SelectedChoice) ? null : SelectedChoice.Trim(),
        _ => string.IsNullOrWhiteSpace(TextValue) ? null : TextValue.Trim()
    };
}

public partial class CustomFieldDefinitionItem : ObservableObject
{
    public int Slot { get; init; }

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private string _label = string.Empty;

    [ObservableProperty]
    private CustomFieldValueType _fieldType = CustomFieldValueType.Text;

    public ObservableCollection<string> Choices { get; } = [];

    public bool IsChoiceType => FieldType == CustomFieldValueType.Choice;

    public Action? ChangedCallback { get; set; }

    partial void OnFieldTypeChanged(CustomFieldValueType value)
    {
        OnPropertyChanged(nameof(IsChoiceType));
        ChangedCallback?.Invoke();
    }

    partial void OnIsEnabledChanged(bool value) => ChangedCallback?.Invoke();

    public CustomFieldDefinition ToDefinition() => new()
    {
        Slot = Slot,
        Description = Description?.Trim() ?? string.Empty,
        IsEnabled = IsEnabled,
        Label = Label?.Trim() ?? string.Empty,
        FieldType = FieldType,
        Choices = Choices.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).ToList()
    };

    public static CustomFieldDefinitionItem FromDefinition(CustomFieldDefinition def)
    {
        var item = new CustomFieldDefinitionItem
        {
            Slot = def.Slot,
            Description = def.Description,
            IsEnabled = def.IsEnabled,
            Label = def.Label,
            FieldType = def.FieldType
        };
        foreach (var c in def.Choices ?? [])
            item.Choices.Add(c);
        return item;
    }
}
