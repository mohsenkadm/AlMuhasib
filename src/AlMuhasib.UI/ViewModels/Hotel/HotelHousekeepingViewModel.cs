using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Core.Models.Hotel;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AlMuhasib.UI.ViewModels.Hotel;

public partial class HotelHousekeepingViewModel : HotelListPreviewViewModelBase
{
    private readonly IHousekeepingService _housekeepingService;
    private readonly IHotelMasterDataService _masterDataService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IToastNotificationService _toast;
    private readonly IExportService _exportService;
    private readonly HotelEntityNavigationHelper _navigation;

    public ObservableCollection<HousekeepingTaskRow> Tasks { get; } = [];
    public ObservableCollection<HotelListStatItem> Stats { get; } = [];

    [ObservableProperty] private HousekeepingTaskRow? _selectedTask;
    [ObservableProperty] private HousekeepingStatus? _statusFilter;
    [ObservableProperty] private string _assignedFilter = string.Empty;
    [ObservableProperty] private HousekeepingTaskRow? _previewTask;

    public IReadOnlyList<HousekeepingStatusFilterOption> StatusFilterOptions { get; } =
    [
        new HousekeepingStatusFilterOption(null, "الكل"),
        new HousekeepingStatusFilterOption(HousekeepingStatus.Pending, "معلق"),
        new HousekeepingStatusFilterOption(HousekeepingStatus.InProgress, "جاري"),
        new HousekeepingStatusFilterOption(HousekeepingStatus.Done, "منجز"),
        new HousekeepingStatusFilterOption(HousekeepingStatus.Verified, "تم التحقق")
    ];

    public HotelHousekeepingViewModel(
        IHousekeepingService housekeepingService,
        IHotelMasterDataService masterDataService,
        ICurrentUserService currentUserService,
        IToastNotificationService toast,
        IExportService exportService,
        MainWindowViewModel mainWindow)
    {
        _housekeepingService = housekeepingService;
        _masterDataService = masterDataService;
        _currentUserService = currentUserService;
        _toast = toast;
        _exportService = exportService;
        _navigation = new HotelEntityNavigationHelper(mainWindow);
        PageTitle = "النظافة";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, HotelPermissionRegistry.Housekeeping);
        await LoadTasksAsync();
    }

    partial void OnStatusFilterChanged(HousekeepingStatus? value) => _ = LoadTasksAsync();
    partial void OnAssignedFilterChanged(string value) => _ = LoadTasksAsync();

    partial void OnSelectedTaskChanged(HousekeepingTaskRow? value)
    {
        if (value is null)
        {
            ClosePreview();
            PreviewTask = null;
            return;
        }

        PreviewTask = value;
        SetPreviewHeader($"غرفة {value.RoomNumber}", value.StatusLabel, MaterialDesignThemes.Wpf.PackIconKind.Broom);
    }

    protected override void OnPreviewClosed()
    {
        SelectedTask = null;
        PreviewTask = null;
    }

    [RelayCommand]
    private async Task LoadTasksAsync()
    {
        IsBusy = true;
        try
        {
            _allTasks.Clear();
            var filter = new HousekeepingTaskFilter
            {
                Status = StatusFilter,
                AssignedTo = string.IsNullOrWhiteSpace(AssignedFilter) ? null : AssignedFilter.Trim()
            };

            var tasks = await _housekeepingService.GetTasksAsync(filter);
            foreach (var task in tasks.OrderByDescending(t => t.CreatedAt))
            {
                _allTasks.Add(new HousekeepingTaskRow
                {
                    Id = task.Id,
                    RoomId = task.RoomId,
                    RoomNumber = task.Room?.RoomNumber ?? "—",
                    Status = task.Status,
                    PendingStatus = task.Status,
                    StatusLabel = HotelDisplayHelper.GetHousekeepingStatusLabel(task.Status),
                    StatusColor = HotelDisplayHelper.GetHousekeepingStatusColor(task.Status),
                    AssignedTo = string.IsNullOrWhiteSpace(task.AssignedTo) ? "—" : task.AssignedTo,
                    Notes = task.Notes,
                    CreatedAt = task.CreatedAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm")
                });
            }

            ApplyTaskFilters();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private readonly List<HousekeepingTaskRow> _allTasks = [];

    protected override void OnColumnFiltersChanged() => ApplyTaskFilters();

    private void ApplyTaskFilters()
    {
        var items = _allTasks.AsEnumerable();
        if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
            items = ColumnFilterEngine.Apply(items, ColumnFilters);

        Tasks.Clear();
        foreach (var task in items)
            Tasks.Add(task);

        RebuildStats();
    }

    private void RebuildStats()
    {
        Stats.Clear();
        var pending = Tasks.Count(t => t.Status is HousekeepingStatus.Pending or HousekeepingStatus.InProgress);
        var done = Tasks.Count(t => t.Status is HousekeepingStatus.Done or HousekeepingStatus.Verified);

        Stats.Add(new HotelListStatItem { Label = "إجمالي المهام", Value = Tasks.Count.ToString("N0"), AccentColor = "#1565C0" });
        Stats.Add(new HotelListStatItem { Label = "معلقة / جارية", Value = pending.ToString("N0"), AccentColor = "#F57C00" });
        Stats.Add(new HotelListStatItem { Label = "مكتملة", Value = done.ToString("N0"), AccentColor = "#2E7D32" });
    }

    [RelayCommand]
    private async Task MarkInProgressAsync(HousekeepingTaskRow? row)
    {
        row ??= SelectedTask;
        if (row is null || !CanEdit) return;
        await UpdateStatusAsync(row.Id, HousekeepingStatus.InProgress);
    }

    [RelayCommand]
    private async Task MarkDoneAsync(HousekeepingTaskRow? row)
    {
        row ??= SelectedTask;
        if (row is null || !CanEdit) return;
        await UpdateStatusAsync(row.Id, HousekeepingStatus.Done);
    }

    [RelayCommand]
    private async Task MarkVerifiedAsync(HousekeepingTaskRow? row)
    {
        row ??= SelectedTask;
        if (row is null || !CanEdit) return;
        await UpdateStatusAsync(row.Id, HousekeepingStatus.Verified);
    }

    private async Task UpdateStatusAsync(int taskId, HousekeepingStatus status)
    {
        try
        {
            var task = await _housekeepingService.GetTaskByIdAsync(taskId)
                ?? throw new InvalidOperationException("المهمة غير موجودة");

            task.Status = status;
            if (status == HousekeepingStatus.InProgress && !task.StartedAt.HasValue)
                task.StartedAt = DateTime.UtcNow;
            if (status is HousekeepingStatus.Done or HousekeepingStatus.Verified)
                task.CompletedAt = DateTime.UtcNow;

            await _housekeepingService.UpdateTaskAsync(task);
            await SyncRoomStatusAsync(task.RoomId, status);
            _toast.ShowSuccess("تم تحديث المهمة");
            await LoadTasksAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    private async Task SyncRoomStatusAsync(int roomId, HousekeepingStatus status)
    {
        var room = await _masterDataService.GetRoomByIdAsync(roomId);
        if (room is null || room.Status == RoomStatus.Occupied)
            return;

        var newStatus = status switch
        {
            HousekeepingStatus.Pending or HousekeepingStatus.InProgress => RoomStatus.Dirty,
            HousekeepingStatus.Done or HousekeepingStatus.Verified => RoomStatus.Available,
            _ => (RoomStatus?)null
        };

        if (newStatus.HasValue && room.Status != newStatus.Value)
        {
            room.Status = newStatus.Value;
            await _masterDataService.UpdateRoomAsync(room);
        }
    }

    public IReadOnlyList<HousekeepingStatusOption> StatusOptions { get; } =
        Enum.GetValues<HousekeepingStatus>()
            .Select(s => new HousekeepingStatusOption(s, HotelDisplayHelper.GetHousekeepingStatusLabel(s)))
            .ToList();

    [RelayCommand]
    private async Task ChangeRoomStatusAsync(HousekeepingTaskRow? row)
    {
        row ??= SelectedTask;
        if (row is null || !CanEdit)
            return;

        if (row.PendingStatus == row.Status)
            return;

        await UpdateStatusAsync(row.Id, row.PendingStatus);
    }

    [RelayCommand]
    private async Task OpenRoomFromTaskAsync(HousekeepingTaskRow? task)
    {
        task ??= PreviewTask ?? SelectedTask;
        if (task is null)
            return;

        await _navigation.OpenRoomsAsync(task.RoomId);
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        var headers = new[] { "الغرفة", "الحالة", "المسؤول", "ملاحظات" };
        var data = Tasks.Select(t => new object?[] { t.RoomNumber, t.StatusLabel, t.AssignedTo, t.Notes }).ToList();
        ListTableExportHelper.ExportExcel(_exportService, _toast, CanExport, "Housekeeping", "النظافة", headers, data);
    }

    [RelayCommand]
    private void PrintTable()
    {
        var headers = new[] { "الغرفة", "الحالة", "المسؤول", "ملاحظات" };
        var data = Tasks.Select(t => new object?[] { t.RoomNumber, t.StatusLabel, t.AssignedTo, t.Notes }).ToList();
        ListTableExportHelper.Print(_exportService, CanPrint, "مهام النظافة", headers, data);
    }
}

public sealed class HousekeepingTaskRow
{
    public int Id { get; init; }
    public int RoomId { get; init; }
    public string RoomNumber { get; init; } = string.Empty;
    public HousekeepingStatus Status { get; init; }
    public HousekeepingStatus PendingStatus { get; set; }
    public string StatusLabel { get; init; } = string.Empty;
    public string StatusColor { get; init; } = "#616161";
    public string AssignedTo { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
    public string CreatedAt { get; init; } = string.Empty;
}

public sealed record HousekeepingStatusFilterOption(HousekeepingStatus? Value, string Label);

public sealed record HousekeepingStatusOption(HousekeepingStatus Value, string Label);
