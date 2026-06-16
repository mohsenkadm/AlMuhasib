using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Core.Models.Hotel;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using System.Linq;
using System.Collections.ObjectModel;

namespace AlMuhasib.UI.ViewModels.Hotel;

public partial class HotelReservationsViewModel : PagedViewModelBase
{
    private readonly IReservationService _reservationService;
    private readonly IReservationPaymentService _paymentService;
    private readonly IHotelInvoicePrintService _printService;
    private readonly IHotelCashService _cashService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IToastNotificationService _toast;
    private readonly MainWindowViewModel _mainWindow;
    private System.Timers.Timer? _debounceTimer;

    public ObservableCollection<ReservationListItem> Reservations { get; } = [];
    public ObservableCollection<HotelCashBoxOption> CashBoxes { get; } = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private DateTime? _checkInFrom;
    [ObservableProperty] private DateTime? _checkInTo;
    [ObservableProperty] private DateTime? _checkOutFrom;
    [ObservableProperty] private DateTime? _checkOutTo;
    [ObservableProperty] private ReservationStatus? _statusFilter;
    [ObservableProperty] private bool _unpaidOnly;
    [ObservableProperty] private int _filteredReservationsCount;
    [ObservableProperty] private decimal _filteredTotalAmount;
    [ObservableProperty] private decimal _filteredRemainingAmount;
    [ObservableProperty] private ReservationListItem? _selectedReservation;
    [ObservableProperty] private bool _isPaymentDialogOpen;
    [ObservableProperty] private decimal _paymentAmount;
    [ObservableProperty] private DateTime _paymentDate = DateTime.Today;
    [ObservableProperty] private string _paymentMethod = "نقد";
    [ObservableProperty] private int? _paymentCashBoxId;
    [ObservableProperty] private string _paymentNotes = string.Empty;
    [ObservableProperty] private string _paymentSummary = string.Empty;
    [ObservableProperty] private bool _isDeleteDialogOpen;
    [ObservableProperty] private ReservationListItem? _reservationToDelete;
    [ObservableProperty] private bool _isDetailDialogOpen;
    [ObservableProperty] private HotelReservationDetailDisplay? _detailReservation;

    public IReadOnlyList<ReservationStatusFilterOption> ReservationStatusFilterOptions { get; } =
    [
        new ReservationStatusFilterOption(null, "كل الحالات"),
        new ReservationStatusFilterOption(ReservationStatus.Confirmed, "مؤكد"),
        new ReservationStatusFilterOption(ReservationStatus.CheckedIn, "مسجل"),
        new ReservationStatusFilterOption(ReservationStatus.CheckedOut, "غادر"),
        new ReservationStatusFilterOption(ReservationStatus.Cancelled, "ملغى"),
        new ReservationStatusFilterOption(ReservationStatus.NoShow, "لم يحضر")
    ];

    public HotelReservationsViewModel(
        IReservationService reservationService,
        IReservationPaymentService paymentService,
        IHotelInvoicePrintService printService,
        IHotelCashService cashService,
        ICurrentUserService currentUserService,
        IToastNotificationService toast,
        MainWindowViewModel mainWindow)
    {
        _reservationService = reservationService;
        _paymentService = paymentService;
        _printService = printService;
        _cashService = cashService;
        _currentUserService = currentUserService;
        _toast = toast;
        _mainWindow = mainWindow;
        PageTitle = "الحجوزات";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, HotelPermissionRegistry.Reservations);
        await LoadCashBoxesAsync();
        await LoadReservationsAsync();
    }

    protected override void OnColumnFiltersChanged()
    {
        _ = ReloadFromFirstPageAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();
        _debounceTimer = new System.Timers.Timer(350) { AutoReset = false };
        _debounceTimer.Elapsed += async (_, _) =>
        {
            await App.Current.Dispatcher.InvokeAsync(async () =>
            {
                CurrentPage = 1;
                await LoadReservationsAsync();
            });
        };
        _debounceTimer.Start();
    }

    partial void OnCheckInFromChanged(DateTime? value) => _ = ReloadFromFirstPageAsync();
    partial void OnCheckInToChanged(DateTime? value) => _ = ReloadFromFirstPageAsync();
    partial void OnCheckOutFromChanged(DateTime? value) => _ = ReloadFromFirstPageAsync();
    partial void OnCheckOutToChanged(DateTime? value) => _ = ReloadFromFirstPageAsync();
    partial void OnStatusFilterChanged(ReservationStatus? value) => _ = ReloadFromFirstPageAsync();
    partial void OnUnpaidOnlyChanged(bool value) => _ = ReloadFromFirstPageAsync();

    protected override Task OnPageChangedAsync() => LoadReservationsAsync();

    private async Task ReloadFromFirstPageAsync()
    {
        CurrentPage = 1;
        await LoadReservationsAsync();
    }

    private async Task LoadCashBoxesAsync()
    {
        CashBoxes.Clear();
        var boxes = await _cashService.GetCashBoxesAsync();
        foreach (var box in boxes)
            CashBoxes.Add(new HotelCashBoxOption(box.Id, box.Name));
    }

    [RelayCommand]
    private async Task LoadReservationsAsync()
    {
        IsBusy = true;
        try
        {
            var filter = BuildFilter();

            if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
            {
                var (allItems, _) = await _reservationService.SearchPagedAsync(filter, 1, int.MaxValue);
                var filteredAll = ColumnFilterEngine.Apply(allItems, ColumnFilters).ToList();

                Reservations.Clear();
                MasterDataColumnFilterHelper.ApplyClientPagination(
                    filteredAll, Reservations,
                    CurrentPage, PageSize,
                    out var filteredTotalCount, out _, out _
                );

                ApplyPaginationStats(filteredTotalCount);
                FilteredReservationsCount = Reservations.Count;
                FilteredTotalAmount = Reservations.Sum(i => i.TotalAmount);
                FilteredRemainingAmount = Reservations.Sum(i => i.RemainingAmount);
                return;
            }

            var (items, total) = await _reservationService.SearchPagedAsync(filter, CurrentPage, PageSize);
            Reservations.Clear();
            foreach (var item in items)
                Reservations.Add(item);

            ApplyPaginationStats(total);
            FilteredReservationsCount = items.Count;
            FilteredTotalAmount = items.Sum(i => i.TotalAmount);
            FilteredRemainingAmount = items.Sum(i => i.RemainingAmount);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenNewReservationAsync() =>
        await _mainWindow.OpenTabAsync(typeof(HotelReservationFormViewModel), "حجز جديد", PackIconKind.CalendarPlus, activateIfExists: false);

    [RelayCommand]
    private async Task EditReservationAsync(ReservationListItem? item)
    {
        item ??= SelectedReservation;
        if (item is null || !CanEdit)
            return;

        HotelReservationNavigationBridge.PendingEditReservationId = item.Id;
        await _mainWindow.OpenTabAsync(typeof(HotelReservationFormViewModel), $"تعديل {item.ReservationNumber}", PackIconKind.FileDocumentEdit, activateIfExists: false);
    }

    [RelayCommand]
    private async Task ViewDetailsAsync(ReservationListItem? item)
    {
        item ??= SelectedReservation;
        if (item is null)
            return;

        var reservation = await _reservationService.GetByIdAsync(item.Id);
        if (reservation is null)
        {
            _toast.ShowError("الحجز غير موجود");
            return;
        }

        SelectedReservation = item;
        DetailReservation = HotelReservationDetailDisplay.FromEntity(reservation);
        IsDetailDialogOpen = true;
    }

    [RelayCommand]
    private void CloseDetailDialog()
    {
        IsDetailDialogOpen = false;
        DetailReservation = null;
    }

    [RelayCommand]
    private async Task DetailPrintAsync()
    {
        if (DetailReservation is null)
            return;

        await PrintReservationAsync(SelectedReservation);
    }

    [RelayCommand]
    private async Task DetailEditAsync()
    {
        if (SelectedReservation is null)
            return;

        IsDetailDialogOpen = false;
        await EditReservationAsync(SelectedReservation);
    }

    [RelayCommand]
    private async Task DetailOpenCheckInOutAsync()
    {
        IsDetailDialogOpen = false;
        await _mainWindow.OpenTabAsync(typeof(HotelCheckInOutViewModel), "تسجيل دخول/خروج", PackIconKind.Login);
    }

    [RelayCommand]
    private void DetailOpenPayment()
    {
        if (SelectedReservation is null)
            return;

        IsDetailDialogOpen = false;
        OpenPaymentDialog(SelectedReservation);
    }

    [RelayCommand]
    private async Task DetailOpenRoomAsync()
    {
        IsDetailDialogOpen = false;
        await _mainWindow.OpenTabAsync(typeof(HotelRoomsViewModel), "الغرف", PackIconKind.Door);
    }

    [RelayCommand]
    private void ConfirmDelete(ReservationListItem? item)
    {
        if (item is null || !CanDelete)
            return;

        ReservationToDelete = item;
        IsDeleteDialogOpen = true;
    }

    [RelayCommand]
    private void CancelDelete() => IsDeleteDialogOpen = false;

    [RelayCommand]
    private async Task DeleteConfirmedAsync()
    {
        if (ReservationToDelete is null)
            return;

        try
        {
            await _reservationService.DeleteAsync(ReservationToDelete.Id, _currentUserService.Username ?? "System");
            IsDeleteDialogOpen = false;
            ReservationToDelete = null;
            _toast.ShowSuccess("تم حذف الحجز");
            await LoadReservationsAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void OpenPaymentDialog(ReservationListItem? item)
    {
        item ??= SelectedReservation;
        if (item is null || !CanEdit)
            return;

        SelectedReservation = item;
        PaymentAmount = item.RemainingAmount;
        PaymentDate = DateTime.Today;
        PaymentNotes = string.Empty;
        PaymentSummary = $"حجز {item.ReservationNumber} — {item.GuestName} (متبقي: {item.RemainingAmount:N0})";
        IsPaymentDialogOpen = true;
    }

    [RelayCommand]
    private void ClosePaymentDialog() => IsPaymentDialogOpen = false;

    [RelayCommand]
    private async Task SubmitPaymentAsync()
    {
        if (SelectedReservation is null)
            return;

        if (PaymentAmount <= 0)
        {
            _toast.ShowWarning("أدخل مبلغاً صحيحاً");
            return;
        }

        try
        {
            await _paymentService.AddPaymentAsync(
                SelectedReservation.Id,
                PaymentAmount,
                PaymentDate,
                PaymentMethod,
                PaymentCashBoxId,
                PaymentNotes);
            IsPaymentDialogOpen = false;
            _toast.ShowSuccess("تم تسجيل الدفعة بنجاح");
            await LoadReservationsAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task PrintReservationAsync(ReservationListItem? item)
    {
        item ??= SelectedReservation;
        if (item is null || !CanPrint)
            return;

        var reservation = await _reservationService.GetByIdAsync(item.Id);
        if (reservation is not null)
            _printService.PrintReservationInvoice(reservation);
    }

    private ReservationFilter BuildFilter() => new()
    {
        SearchText = SearchText,
        CheckInFrom = CheckInFrom,
        CheckInTo = CheckInTo,
        CheckOutFrom = CheckOutFrom,
        CheckOutTo = CheckOutTo,
        Status = StatusFilter,
        UnpaidOnly = UnpaidOnly
    };
}

public sealed record HotelCashBoxOption(int Id, string Name);
public sealed record ReservationStatusFilterOption(ReservationStatus? Value, string Label);
