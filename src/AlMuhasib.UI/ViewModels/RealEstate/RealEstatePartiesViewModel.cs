using AlMuhasib.Core.Entities.RealEstate;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.RealEstate;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AlMuhasib.UI.ViewModels.RealEstate;

public partial class RealEstatePartiesViewModel : PagedViewModelBase
{
    private readonly IRealEstatePartyService _partyService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IToastNotificationService _toast;
    private readonly IUserPreferencesService _prefs;

    public ObservableCollection<RealEstatePartyListItem> Parties { get; } = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private RealEstatePartyListItem? _selectedParty;
    [ObservableProperty] private string _editName = string.Empty;
    [ObservableProperty] private string _editPhone = string.Empty;
    [ObservableProperty] private string _editAddress = string.Empty;
    [ObservableProperty] private string _editIdNumber = string.Empty;
    [ObservableProperty] private bool _isEditDialogOpen;
    [ObservableProperty] private int _editId;
    [ObservableProperty] private bool _isCardView;
    [ObservableProperty] private string _partiesCountText = "0";
    [ObservableProperty] private string _withDebtCountText = "0";
    [ObservableProperty] private string _totalDebtText = "0";

    public RealEstatePartiesViewModel(
        IRealEstatePartyService partyService,
        ICurrentUserService currentUserService,
        IToastNotificationService toast,
        IUserPreferencesService prefs)
    {
        _partyService = partyService;
        _currentUserService = currentUserService;
        _toast = toast;
        _prefs = prefs;
        PageTitle = "الزبائن";
        IsCardView = ListViewModeHelper.LoadIsCardView(_prefs, "RealEstateParties");
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, RealEstatePermissionRegistry.Parties);
        await LoadAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        CurrentPage = 1;
        _ = LoadAsync();
    }

    partial void OnIsCardViewChanged(bool value) =>
        ListViewModeHelper.SaveIsCardView(_prefs, "RealEstateParties", value);

    protected override Task OnPageChangedAsync() => LoadAsync();

    protected override void OnColumnFiltersChanged()
    {
        CurrentPage = 1;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
            {
                var (allItems, _) = await _partyService.GetPagedAsync(1, int.MaxValue, SearchText);
                var filtered = ColumnFilterEngine.Apply(allItems, ColumnFilters).ToList();
                MasterDataColumnFilterHelper.ApplyClientPagination(
                    filtered, Parties, CurrentPage, PageSize,
                    out var filteredTotal, out var filteredPages, out var filteredText);
                TotalCount = filteredTotal;
                TotalPages = filteredPages;
                PaginationText = filteredText;
                UpdateStats(filtered);
                return;
            }

            var (items, total) = await _partyService.GetPagedAsync(CurrentPage, PageSize, SearchText);
            Parties.Clear();
            foreach (var item in items)
                Parties.Add(item);
            ApplyPaginationStats(total);

            var (allForStats, _) = await _partyService.GetPagedAsync(1, int.MaxValue, SearchText);
            UpdateStats(allForStats);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateStats(IReadOnlyList<RealEstatePartyListItem> rows)
    {
        PartiesCountText = rows.Count.ToString("N0");
        WithDebtCountText = rows.Count(r => r.TotalDebt > 0).ToString("N0");
        TotalDebtText = rows.Sum(r => r.TotalDebt).ToString("N0");
    }

    [RelayCommand]
    private void OpenNew()
    {
        EditId = 0;
        EditName = string.Empty;
        EditPhone = string.Empty;
        EditAddress = string.Empty;
        EditIdNumber = string.Empty;
        IsEditDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEdit(RealEstatePartyListItem? item)
    {
        item ??= SelectedParty;
        if (item is null || !CanEdit) return;
        EditId = item.Id;
        EditName = item.Name;
        EditPhone = item.Phone;
        EditAddress = item.Address;
        EditIdNumber = item.IdNumber;
        IsEditDialogOpen = true;
    }

    [RelayCommand]
    private void CloseEdit() => IsEditDialogOpen = false;

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(EditName))
        {
            _toast.ShowWarning("الاسم مطلوب");
            return;
        }
        try
        {
            await _partyService.SaveAsync(new RealEstateParty
            {
                Id = EditId,
                Name = EditName.Trim(),
                Phone = EditPhone.Trim(),
                Address = EditAddress.Trim(),
                IdNumber = EditIdNumber.Trim()
            });
            IsEditDialogOpen = false;
            _toast.ShowSuccess("تم الحفظ");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(RealEstatePartyListItem? item)
    {
        item ??= SelectedParty;
        if (item is null || !CanDelete) return;
        try
        {
            await _partyService.DeleteAsync(item.Id, _currentUserService.Username ?? "System");
            _toast.ShowSuccess("تم الحذف");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }
}
