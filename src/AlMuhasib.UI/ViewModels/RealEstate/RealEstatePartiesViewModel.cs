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

    public ObservableCollection<RealEstatePartyListItem> Parties { get; } = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private RealEstatePartyListItem? _selectedParty;
    [ObservableProperty] private string _editName = string.Empty;
    [ObservableProperty] private string _editPhone = string.Empty;
    [ObservableProperty] private string _editAddress = string.Empty;
    [ObservableProperty] private string _editIdNumber = string.Empty;
    [ObservableProperty] private bool _isEditDialogOpen;
    [ObservableProperty] private int _editId;

    public RealEstatePartiesViewModel(
        IRealEstatePartyService partyService,
        ICurrentUserService currentUserService,
        IToastNotificationService toast)
    {
        _partyService = partyService;
        _currentUserService = currentUserService;
        _toast = toast;
        PageTitle = "الزبائن";
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

    protected override Task OnPageChangedAsync() => LoadAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var (items, total) = await _partyService.GetPagedAsync(CurrentPage, PageSize, SearchText);
            Parties.Clear();
            foreach (var item in items)
                Parties.Add(item);
            ApplyPaginationStats(total);
        }
        finally
        {
            IsBusy = false;
        }
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
