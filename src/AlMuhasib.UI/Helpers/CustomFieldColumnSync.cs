using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using AlMuhasib.Core.Models.CustomFields;
using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Helpers;

/// <summary>
/// يزامن أعمدة الحقول المخصصة الثمانية في DataGrid (Visibility + Header).
/// </summary>
public static class CustomFieldColumnSync
{
    public static void Attach(
        FrameworkElement host,
        DataGrid grid,
        DataGridColumn[] columns,
        Func<INotifyPropertyChanged?, IReadOnlyList<CustomFieldColumnState>?> getStates,
        string? refreshPropertyName = null)
    {
        void Sync()
        {
            var states = getStates(host.DataContext as INotifyPropertyChanged) ?? [];
            for (var i = 0; i < columns.Length; i++)
            {
                var col = columns[i];
                if (col is null) continue;
                var state = states.FirstOrDefault(s => s.Slot == i + 1);
                if (state is null || !state.IsEnabled)
                {
                    col.Visibility = Visibility.Collapsed;
                    continue;
                }

                col.Header = state.Label;
                col.Visibility = Visibility.Visible;
            }
        }

        void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (refreshPropertyName is null
                || e.PropertyName is null
                || e.PropertyName == refreshPropertyName
                || e.PropertyName == "CustomFieldColumnsVersion")
                Sync();
        }

        void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is INotifyPropertyChanged oldVm)
                oldVm.PropertyChanged -= OnVmPropertyChanged;
            if (e.NewValue is INotifyPropertyChanged newVm)
                newVm.PropertyChanged += OnVmPropertyChanged;
            Sync();
        }

        host.DataContextChanged += OnDataContextChanged;
        if (host.DataContext is INotifyPropertyChanged existing)
            existing.PropertyChanged += OnVmPropertyChanged;
        Sync();
    }
}

public sealed class CustomFieldColumnState
{
    public int Slot { get; init; }
    public bool IsEnabled { get; init; }
    public string Label { get; init; } = string.Empty;
}

public static class CustomFieldEditFactory
{
    public static ObservableCollection<CustomFieldEditItem> CreateEditItems(
        IReadOnlyList<CustomFieldDefinition> enabledDefinitions,
        string? existingJson)
    {
        var values = CustomFieldsHelper.Parse(existingJson);
        var items = new ObservableCollection<CustomFieldEditItem>();
        foreach (var def in enabledDefinitions.OrderBy(d => d.Slot))
        {
            values.TryGetValue(def.SlotKey, out var raw);
            items.Add(CustomFieldEditItem.FromDefinition(def, raw));
        }
        return items;
    }

    public static string? SerializeEditItems(IEnumerable<CustomFieldEditItem> items)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            var stored = item.ToStorageValue();
            if (!string.IsNullOrWhiteSpace(stored))
                dict[item.SlotKey] = stored;
        }
        return CustomFieldsHelper.Serialize(dict);
    }
}
