using System.Collections.ObjectModel;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.ViewModels;

public partial class CustomFieldSettingsViewModel : ViewModelBase
{
    private readonly ICustomFieldSettingsService _customFieldSettings;

    public ObservableCollection<CustomFieldEntityTab> EntityTabs { get; } = [];

    [ObservableProperty]
    private CustomFieldEntityTab? _selectedTab;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _saveSuccessPulse;

    [ObservableProperty]
    private bool _isChoicesDialogOpen;

    [ObservableProperty]
    private CustomFieldDefinitionItem? _choicesTargetField;

    [ObservableProperty]
    private string _newChoiceText = string.Empty;

    public ObservableCollection<string> EditingChoices { get; } = [];

    public IReadOnlyList<CustomFieldValueTypeOption> FieldTypeOptions { get; } =
    [
        new(CustomFieldValueType.Text, "نص"),
        new(CustomFieldValueType.Boolean, "نعم / لا"),
        new(CustomFieldValueType.Number, "رقم"),
        new(CustomFieldValueType.Choice, "اختيارات")
    ];

    public CustomFieldSettingsViewModel(
        ICustomFieldSettingsService customFieldSettings,
        ICurrentUserService currentUserService)
    {
        _customFieldSettings = customFieldSettings;
        PageTitle = "إعدادات الحقول المخصصة";
        LoadPermissions(currentUserService, "CustomFieldSettings");

        EntityTabs.Add(new CustomFieldEntityTab(CustomFieldEntityKind.Products, "المنتجات", PackIconKind.PackageVariant, "#2E7D32", "#E8F5E9"));
        EntityTabs.Add(new CustomFieldEntityTab(CustomFieldEntityKind.Customers, "العملاء", PackIconKind.AccountGroup, "#0277BD", "#E1F5FE"));
        EntityTabs.Add(new CustomFieldEntityTab(CustomFieldEntityKind.Suppliers, "الموردون", PackIconKind.Factory, "#EF6C00", "#FFF3E0"));
        EntityTabs.Add(new CustomFieldEntityTab(CustomFieldEntityKind.Investors, "المستثمرون", PackIconKind.TrendingUp, "#558B2F", "#F1F8E9"));
        SelectedTab = EntityTabs[0];
        SyncTabSelection();
    }

    public override async Task InitializeAsync()
    {
        await LoadAllAsync();
    }

    private async Task LoadAllAsync()
    {
        try
        {
            IsBusy = true;
            foreach (var tab in EntityTabs)
            {
                var defs = await _customFieldSettings.GetDefinitionsAsync(tab.EntityKind);
                tab.Fields.Clear();
                foreach (var def in defs)
                {
                    var item = CustomFieldDefinitionItem.FromDefinition(def);
                    item.ChangedCallback = tab.RefreshStats;
                    tab.Fields.Add(item);
                }
                tab.RefreshStats();
            }
            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر تحميل إعدادات الحقول:\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedTabChanged(CustomFieldEntityTab? value) => SyncTabSelection();

    private void SyncTabSelection()
    {
        foreach (var tab in EntityTabs)
            tab.IsSelected = ReferenceEquals(tab, SelectedTab);
    }

    [RelayCommand]
    private void SelectTab(CustomFieldEntityTab? tab)
    {
        if (tab is null) return;
        SelectedTab = tab;
    }

    [RelayCommand]
    private void OpenChoicesDialog(CustomFieldDefinitionItem? field)
    {
        if (field is null) return;
        ChoicesTargetField = field;
        EditingChoices.Clear();
        foreach (var c in field.Choices)
            EditingChoices.Add(c);
        NewChoiceText = string.Empty;
        IsChoicesDialogOpen = true;
    }

    [RelayCommand]
    private void AddChoice()
    {
        var text = NewChoiceText?.Trim() ?? string.Empty;
        if (text.Length == 0) return;
        if (EditingChoices.Any(c => string.Equals(c, text, StringComparison.OrdinalIgnoreCase)))
        {
            BeautifulMessageDialog.ShowWarning("هذا الخيار موجود مسبقاً");
            return;
        }
        EditingChoices.Add(text);
        NewChoiceText = string.Empty;
    }

    [RelayCommand]
    private void RemoveChoice(string? choice)
    {
        if (choice is null) return;
        EditingChoices.Remove(choice);
    }

    [RelayCommand]
    private void SaveChoices()
    {
        if (ChoicesTargetField is null) return;
        ChoicesTargetField.Choices.Clear();
        foreach (var c in EditingChoices)
            ChoicesTargetField.Choices.Add(c);
        ChoicesTargetField.FieldType = CustomFieldValueType.Choice;
        IsChoicesDialogOpen = false;
        SelectedTab?.RefreshStats();
    }

    [RelayCommand]
    private void CancelChoices() => IsChoicesDialogOpen = false;

    [RelayCommand]
    private async Task Save()
    {
        try
        {
            IsBusy = true;
            foreach (var tab in EntityTabs)
            {
                var defs = tab.Fields.Select(f => f.ToDefinition()).ToList();
                foreach (var def in defs.Where(d => d.IsEnabled))
                {
                    if (string.IsNullOrWhiteSpace(def.Label))
                    {
                        BeautifulMessageDialog.ShowWarning(
                            $"في تاب «{tab.Title}» الحقل رقم {def.Slot} مفعّل بدون مسمى — أدخل المسمى أولاً.");
                        SelectedTab = tab;
                        return;
                    }
                    if (def.FieldType == CustomFieldValueType.Choice && def.Choices.Count == 0)
                    {
                        BeautifulMessageDialog.ShowWarning(
                            $"في تاب «{tab.Title}» الحقل «{def.DisplayLabel}» من نوع اختيارات بدون خيارات.");
                        SelectedTab = tab;
                        return;
                    }
                }

                await _customFieldSettings.SaveDefinitionsAsync(tab.EntityKind, defs);
                tab.RefreshStats();
            }

            StatusMessage = "تم حفظ إعدادات الحقول المخصصة بنجاح";
            SaveSuccessPulse = true;
            BeautifulMessageDialog.ShowSuccess("تم حفظ إعدادات الحقول — ستظهر في الجداول ونماذج الإضافة/التعديل");
            await Task.Delay(1200);
            SaveSuccessPulse = false;
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر الحفظ:\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public partial class CustomFieldEntityTab : ObservableObject
{
    public CustomFieldEntityKind EntityKind { get; }
    public string Title { get; }
    public PackIconKind IconKind { get; }
    public string Accent { get; }
    public string AccentLight { get; }
    public ObservableCollection<CustomFieldDefinitionItem> Fields { get; } = [];

    [ObservableProperty]
    private int _enabledCount;

    [ObservableProperty]
    private bool _isSelected;

    public CustomFieldEntityTab(CustomFieldEntityKind kind, string title, PackIconKind icon, string accent, string accentLight)
    {
        EntityKind = kind;
        Title = title;
        IconKind = icon;
        Accent = accent;
        AccentLight = accentLight;
    }

    public void RefreshStats() => EnabledCount = Fields.Count(f => f.IsEnabled);
}

public sealed record CustomFieldValueTypeOption(CustomFieldValueType Type, string Label);
