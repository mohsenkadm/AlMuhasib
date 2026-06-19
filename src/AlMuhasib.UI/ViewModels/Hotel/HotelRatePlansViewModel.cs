using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;

namespace AlMuhasib.UI.ViewModels.Hotel;

public partial class HotelRatePlansViewModel : PagedViewModelBase
{
    private readonly IRatePlanService _ratePlanService;
    private readonly IHotelMasterDataService _masterDataService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IToastNotificationService _toast;
    private readonly IUserPreferencesService _userPreferences;

    private List<RatePlan> _allRatePlans = [];

    public ObservableCollection<RatePlan> RatePlans { get; } = [];
    public ObservableCollection<HotelListStatItem> Stats { get; } = [];
    public ObservableCollection<RatePlanSeason> Seasons { get; } = [];
    public ObservableCollection<RoomTypeOption> RoomTypes { get; } = [];

    [ObservableProperty] private RatePlan? _selectedRatePlan;
    [ObservableProperty] private RatePlanSeason? _selectedSeason;
    [ObservableProperty] private bool _isCardView;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isPlanDialogOpen;
    [ObservableProperty] private bool _isSeasonDialogOpen;
    [ObservableProperty] private bool _isPlanEditMode;
    [ObservableProperty] private bool _isSeasonEditMode;
    [ObservableProperty] private string _editPlanName = string.Empty;
    [ObservableProperty] private int? _editPlanRoomTypeId;
    [ObservableProperty] private decimal _editPlanBasePrice;
    [ObservableProperty] private bool _editPlanIsActive = true;
    [ObservableProperty] private string _editPlanNotes = string.Empty;
    [ObservableProperty] private string _editSeasonName = string.Empty;
    [ObservableProperty] private DateTime _editSeasonStart = DateTime.Today;
    [ObservableProperty] private DateTime _editSeasonEnd = DateTime.Today.AddMonths(1);
    [ObservableProperty] private decimal _editSeasonPrice;

    private int? _editingPlanId;
    private int? _editingSeasonId;

    public HotelRatePlansViewModel(
        IRatePlanService ratePlanService,
        IHotelMasterDataService masterDataService,
        ICurrentUserService currentUserService,
        IToastNotificationService toast,
        IUserPreferencesService userPreferences)
    {
        _ratePlanService = ratePlanService;
        _masterDataService = masterDataService;
        _currentUserService = currentUserService;
        _toast = toast;
        _userPreferences = userPreferences;
        PageTitle = "خطط الأسعار";
        IsCardView = ListViewModeHelper.LoadIsCardView(_userPreferences, ListViewModeKeys.HotelRatePlans);
    }

    partial void OnIsCardViewChanged(bool value) =>
        ListViewModeHelper.SaveIsCardView(_userPreferences, ListViewModeKeys.HotelRatePlans, value);

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, HotelPermissionRegistry.RatePlans);
        await LoadLookupsAsync();
        await LoadAllPlansAsync();
        await ReloadPlansPageAsync();
    }

    partial void OnSelectedRatePlanChanged(RatePlan? value) => _ = LoadSeasonsAsync();

    [RelayCommand]
    private async Task LoadAllPlansAsync()
    {
        IsBusy = true;
        try
        {
            _allRatePlans = (await _ratePlanService.GetRatePlansAsync(activeOnly: false)).ToList();
        }
        finally
        {
            IsBusy = false;
        }

        if (SelectedRatePlan is not null)
        {
            var selectedId = SelectedRatePlan.Id;
            SelectedRatePlan = _allRatePlans.FirstOrDefault(p => p.Id == selectedId);
        }

        await LoadSeasonsAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        CurrentPage = 1;
        _ = ReloadPlansPageAsync();
    }

    protected override Task OnPageChangedAsync() => ReloadPlansPageAsync();

    protected override void OnColumnFiltersChanged() =>
        _ = ReloadFromFirstPageAsync();

    private async Task ReloadFromFirstPageAsync()
    {
        CurrentPage = 1;
        await ReloadPlansPageAsync();
    }

    private Task ReloadPlansPageAsync()
    {
        var search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();

        var items = _allRatePlans
            .Where(p => search is null
                        || p.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                        || (p.Notes != null && p.Notes.Contains(search, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(p => p.Name)
            .ToList();

        if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
            items = ColumnFilterEngine.Apply(items, ColumnFilters).ToList();

        MasterDataColumnFilterHelper.ApplyClientPagination(
            items, RatePlans, CurrentPage, PageSize,
            out var filteredTotal, out _, out _);

        ApplyPaginationStats(filteredTotal);
        RebuildStats(items);
        return Task.CompletedTask;
    }

    private void RebuildStats(IReadOnlyList<RatePlan> allItems)
    {
        Stats.Clear();
        var total = allItems.Count;
        var avgPrice = total == 0 ? 0m : allItems.Average(x => x.BasePrice);

        Stats.Add(new HotelListStatItem { Label = "إجمالي الخطط", Value = total.ToString("N0"), AccentColor = "#1565C0" });
        Stats.Add(new HotelListStatItem { Label = "متوسط السعر", Value = avgPrice.ToString("N0"), AccentColor = "#2E7D32" });
    }

    private async Task LoadLookupsAsync()
    {
        RoomTypes.Clear();
        foreach (var rt in await _masterDataService.GetRoomTypesAsync())
            RoomTypes.Add(new RoomTypeOption(rt.Id, rt.Name, rt.Capacity, rt.BasePrice));
    }

    [ObservableProperty] private string _seasonSearchText = string.Empty;
    [ObservableProperty] private bool _isSeasonColumnFilterPanelOpen;
    [ObservableProperty] private int _seasonActiveColumnFilterCount;

    private readonly Dictionary<string, string> _seasonColumnFilters = new(StringComparer.OrdinalIgnoreCase);

    partial void OnSeasonSearchTextChanged(string value) => ApplySeasonFilters();

    private readonly List<RatePlanSeason> _allSeasons = [];

    private async Task LoadSeasonsAsync()
    {
        _allSeasons.Clear();
        if (SelectedRatePlan is null)
        {
            Seasons.Clear();
            return;
        }

        foreach (var season in await _ratePlanService.GetSeasonsAsync(SelectedRatePlan.Id))
            _allSeasons.Add(season);

        ApplySeasonFilters();
    }

    private void ApplySeasonFilters()
    {
        var search = string.IsNullOrWhiteSpace(SeasonSearchText) ? null : SeasonSearchText.Trim();
        var items = _allSeasons
            .Where(s => search is null || s.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            .AsEnumerable();

        if (MasterDataColumnFilterHelper.HasActiveColumnFilters(_seasonColumnFilters))
            items = ColumnFilterEngine.Apply(items, _seasonColumnFilters);

        Seasons.Clear();
        foreach (var season in items)
            Seasons.Add(season);
    }

    [RelayCommand]
    private void ApplySeasonColumnFilters(Dictionary<string, string>? filters)
    {
        _seasonColumnFilters.Clear();
        if (filters is not null)
        {
            foreach (var kv in filters)
                _seasonColumnFilters[kv.Key] = kv.Value;
        }

        SeasonActiveColumnFilterCount = _seasonColumnFilters.Count(kv => !string.IsNullOrWhiteSpace(kv.Value));
        ApplySeasonFilters();
    }

    [RelayCommand]
    private void ClearSeasonColumnFilters()
    {
        _seasonColumnFilters.Clear();
        SeasonActiveColumnFilterCount = 0;
        ApplySeasonFilters();
    }

    [RelayCommand]
    private void OpenAddPlanDialog()
    {
        if (!CanAdd) return;
        _editingPlanId = null;
        IsPlanEditMode = false;
        EditPlanName = string.Empty;
        EditPlanRoomTypeId = RoomTypes.FirstOrDefault()?.Id;
        EditPlanBasePrice = 0;
        EditPlanIsActive = true;
        EditPlanNotes = string.Empty;
        IsPlanDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditPlanDialog(RatePlan? plan)
    {
        plan ??= SelectedRatePlan;
        if (plan is null || !CanEdit) return;
        _editingPlanId = plan.Id;
        IsPlanEditMode = true;
        EditPlanName = plan.Name;
        EditPlanRoomTypeId = plan.RoomTypeId;
        EditPlanBasePrice = plan.BasePrice;
        EditPlanIsActive = plan.IsActive;
        EditPlanNotes = plan.Notes;
        IsPlanDialogOpen = true;
    }

    [RelayCommand]
    private void ClosePlanDialog() => IsPlanDialogOpen = false;

    [RelayCommand]
    private async Task SavePlanAsync()
    {
        if (string.IsNullOrWhiteSpace(EditPlanName) || !EditPlanRoomTypeId.HasValue)
        {
            _toast.ShowWarning("أكمل بيانات الخطة");
            return;
        }

        try
        {
            if (_editingPlanId.HasValue)
            {
                var plan = await _ratePlanService.GetRatePlanByIdAsync(_editingPlanId.Value, includeSeasons: false)
                    ?? throw new InvalidOperationException("الخطة غير موجودة");
                plan.Name = EditPlanName.Trim();
                plan.RoomTypeId = EditPlanRoomTypeId.Value;
                plan.BasePrice = EditPlanBasePrice;
                plan.IsActive = EditPlanIsActive;
                plan.Notes = EditPlanNotes;
                await _ratePlanService.UpdateRatePlanAsync(plan);
            }
            else
            {
                await _ratePlanService.CreateRatePlanAsync(new RatePlan
                {
                    Name = EditPlanName.Trim(),
                    RoomTypeId = EditPlanRoomTypeId.Value,
                    BasePrice = EditPlanBasePrice,
                    IsActive = EditPlanIsActive,
                    Notes = EditPlanNotes
                });
            }

            IsPlanDialogOpen = false;
            _toast.ShowSuccess("تم الحفظ");
            await LoadAllPlansAsync();
            await ReloadPlansPageAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeletePlanAsync(RatePlan? plan)
    {
        plan ??= SelectedRatePlan;
        if (plan is null || !CanDelete) return;
        if (!RequestSensitiveApproval($"حذف خطة «{plan.Name}»؟")) return;

        try
        {
            await _ratePlanService.DeleteRatePlanAsync(plan.Id, _currentUserService.Username ?? "System");
            _toast.ShowSuccess("تم الحذف");
            SelectedRatePlan = null;
            await LoadAllPlansAsync();
            await ReloadPlansPageAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void OpenAddSeasonDialog()
    {
        if (!CanAdd || SelectedRatePlan is null) return;
        _editingSeasonId = null;
        IsSeasonEditMode = false;
        EditSeasonName = string.Empty;
        EditSeasonStart = DateTime.Today;
        EditSeasonEnd = DateTime.Today.AddMonths(1);
        EditSeasonPrice = SelectedRatePlan.BasePrice;
        IsSeasonDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditSeasonDialog(RatePlanSeason? season)
    {
        season ??= SelectedSeason;
        if (season is null || !CanEdit) return;
        _editingSeasonId = season.Id;
        IsSeasonEditMode = true;
        EditSeasonName = season.Name;
        EditSeasonStart = season.StartDate;
        EditSeasonEnd = season.EndDate;
        EditSeasonPrice = season.PricePerNight;
        IsSeasonDialogOpen = true;
    }

    [RelayCommand]
    private void CloseSeasonDialog() => IsSeasonDialogOpen = false;

    [RelayCommand]
    private async Task SaveSeasonAsync()
    {
        if (SelectedRatePlan is null || string.IsNullOrWhiteSpace(EditSeasonName))
        {
            _toast.ShowWarning("أكمل بيانات الموسم");
            return;
        }

        try
        {
            if (_editingSeasonId.HasValue)
            {
                var season = await _ratePlanService.GetSeasonByIdAsync(_editingSeasonId.Value)
                    ?? throw new InvalidOperationException("الموسم غير موجود");
                season.Name = EditSeasonName.Trim();
                season.StartDate = EditSeasonStart.Date;
                season.EndDate = EditSeasonEnd.Date;
                season.PricePerNight = EditSeasonPrice;
                await _ratePlanService.UpdateSeasonAsync(season);
            }
            else
            {
                await _ratePlanService.CreateSeasonAsync(new RatePlanSeason
                {
                    RatePlanId = SelectedRatePlan.Id,
                    Name = EditSeasonName.Trim(),
                    StartDate = EditSeasonStart.Date,
                    EndDate = EditSeasonEnd.Date,
                    PricePerNight = EditSeasonPrice
                });
            }

            IsSeasonDialogOpen = false;
            _toast.ShowSuccess("تم الحفظ");
            await LoadSeasonsAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteSeasonAsync(RatePlanSeason? season)
    {
        season ??= SelectedSeason;
        if (season is null || !CanDelete) return;
        if (!RequestSensitiveApproval($"حذف موسم «{season.Name}»؟")) return;

        try
        {
            await _ratePlanService.DeleteSeasonAsync(season.Id, _currentUserService.Username ?? "System");
            _toast.ShowSuccess("تم الحذف");
            await LoadSeasonsAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }
}
