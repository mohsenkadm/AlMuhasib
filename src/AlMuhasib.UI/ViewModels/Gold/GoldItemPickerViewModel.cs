using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces.Services.Gold;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldItemPickerDisplayItem : ObservableObject
{
    public GoldItemPickerDisplayItem(GoldItem item)
    {
        Item = item;
    }

    public GoldItem Item { get; }
    public int Id => Item.Id;
    public string Name => Item.Name;
    public string Barcode => Item.Barcode;
    public int KaratValue => Item.KaratValue;
    public decimal WeightGrams => Item.WeightGrams;
    public decimal SuggestedMakingCharge => Item.SuggestedMakingCharge;
    public string StatusLabel => GoldItemStatusDisplay.ToArabic(Item.Status);
    public string Summary => $"عيار {KaratValue} — {WeightGrams:N3} غ";

    [ObservableProperty] private bool _isSelected;
}

public partial class GoldItemPickerViewModel : ObservableObject
{
    private readonly IGoldInventoryService _inventoryService;
    private List<GoldItem> _allInStock = [];

    public ObservableCollection<GoldItemPickerDisplayItem> VisibleItems { get; } = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private int _selectedCount;

    public event Action<IReadOnlyList<GoldItem>>? Confirmed;
    public event Action? Cancelled;

    public GoldItemPickerViewModel(IGoldInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            var (items, _) = await _inventoryService.GetItemsPagedAsync(
                page: 1,
                pageSize: 200,
                search: null,
                karatValue: null,
                status: GoldItemStatus.InStock);
            _allInStock = items.ToList();
            ApplyFilter();
            StatusMessage = _allInStock.Count == 0
                ? "لا توجد قطع متوفرة في المخزن"
                : $"{_allInStock.Count} قطعة متوفرة";
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var term = SearchText?.Trim() ?? string.Empty;
        IEnumerable<GoldItem> query = _allInStock;
        if (!string.IsNullOrEmpty(term))
        {
            query = _allInStock.Where(i =>
                i.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                i.Barcode.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                i.Id.ToString() == term ||
                i.KaratValue.ToString() == term ||
                i.Category.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var previous = VisibleItems.Where(v => v.IsSelected).Select(v => v.Id).ToHashSet();
        VisibleItems.Clear();
        foreach (var item in query.Take(150))
        {
            var row = new GoldItemPickerDisplayItem(item)
            {
                IsSelected = previous.Contains(item.Id)
            };
            row.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(GoldItemPickerDisplayItem.IsSelected))
                    RefreshSelectedCount();
            };
            VisibleItems.Add(row);
        }

        RefreshSelectedCount();
    }

    private void RefreshSelectedCount() =>
        SelectedCount = VisibleItems.Count(i => i.IsSelected);

    [RelayCommand]
    private void ToggleItem(GoldItemPickerDisplayItem? item)
    {
        if (item is null) return;
        item.IsSelected = !item.IsSelected;
    }

    [RelayCommand]
    private void Confirm()
    {
        var selected = VisibleItems.Where(i => i.IsSelected).Select(i => i.Item).ToList();
        if (selected.Count == 0)
        {
            StatusMessage = "اختر قطعة واحدة على الأقل";
            return;
        }

        Confirmed?.Invoke(selected);
    }

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke();
}
