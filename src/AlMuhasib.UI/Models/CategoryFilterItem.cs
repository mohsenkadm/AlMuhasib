using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.Models;

/// <summary>خيار صنف في فلتر متعدد الاختيار.</summary>
public partial class CategoryFilterItem : ObservableObject
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;

    [ObservableProperty] private bool _isChecked = true;

    public event Action? Changed;

    partial void OnIsCheckedChanged(bool value) => Changed?.Invoke();
}
