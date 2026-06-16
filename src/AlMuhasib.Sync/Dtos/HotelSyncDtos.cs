using AlMuhasib.Core.Enums;

namespace AlMuhasib.Sync.Dtos;

public sealed class HotelSettingsSyncDto : SyncDtoBase
{
    public string HotelName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public TimeSpan CheckInTime { get; set; } = new(14, 0, 0);
    public TimeSpan CheckOutTime { get; set; } = new(12, 0, 0);
    public string CancellationPolicy { get; set; } = string.Empty;
    public string Currency { get; set; } = "IQD";
    public bool IsConfigured { get; set; }
}

public sealed class HotelFloorSyncDto : SyncDtoBase
{
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public sealed class HotelRoomTypeSyncDto : SyncDtoBase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Capacity { get; set; } = 2;
    public decimal BasePrice { get; set; }
    public int SortOrder { get; set; }
}

public sealed class HotelRoomSyncDto : SyncDtoBase
{
    public string RoomNumber { get; set; } = string.Empty;
    public Guid FloorSyncId { get; set; }
    public Guid RoomTypeSyncId { get; set; }
    public RoomStatus Status { get; set; } = RoomStatus.Available;
    public string Notes { get; set; } = string.Empty;
}

public sealed class HotelGuestSyncDto : SyncDtoBase
{
    public string FullName { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public sealed class HotelReservationSyncDto : SyncDtoBase
{
    public string ReservationNumber { get; set; } = string.Empty;
    public Guid GuestSyncId { get; set; }
    public Guid? RoomSyncId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string? RoomNumber { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public DateTime? ActualCheckIn { get; set; }
    public DateTime? ActualCheckOut { get; set; }
    public int GuestCount { get; set; } = 1;
    public ReservationStatus Status { get; set; } = ReservationStatus.Confirmed;
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class HotelReservationChargeSyncDto : SyncDtoBase
{
    public Guid ReservationSyncId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ChargeDate { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class HotelReservationPaymentSyncDto : SyncDtoBase
{
    public Guid ReservationSyncId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "نقد";
    public string Notes { get; set; } = string.Empty;
    public Guid? HotelCashBoxSyncId { get; set; }
}

public sealed class HotelCashBoxSyncDto : SyncDtoBase
{
    public string Name { get; set; } = string.Empty;
    public bool IsBank { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public bool IsActive { get; set; } = true;
    public string Notes { get; set; } = string.Empty;
}

public sealed class HotelVoucherSyncDto : SyncDtoBase
{
    public string VoucherNumber { get; set; } = string.Empty;
    public DateTime VoucherDate { get; set; }
    public HotelVoucherType Type { get; set; }
    public decimal Amount { get; set; }
    public Guid HotelCashBoxSyncId { get; set; }
    public Guid? ReservationSyncId { get; set; }
    public Guid? HotelExpenseSyncId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public sealed class HotelExpenseTypeSyncDto : SyncDtoBase
{
    public string Name { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public sealed class HotelExpenseSyncDto : SyncDtoBase
{
    public Guid HotelExpenseTypeSyncId { get; set; }
    public DateTime ExpenseDate { get; set; }
    public decimal Amount { get; set; }
    public Guid? HotelCashBoxSyncId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public sealed class HotelRatePlanSyncDto : SyncDtoBase
{
    public string Name { get; set; } = string.Empty;
    public Guid RoomTypeSyncId { get; set; }
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; } = true;
    public string Notes { get; set; } = string.Empty;
}

public sealed class HotelRatePlanSeasonSyncDto : SyncDtoBase
{
    public Guid RatePlanSyncId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal PricePerNight { get; set; }
}

public sealed class HotelHousekeepingTaskSyncDto : SyncDtoBase
{
    public Guid RoomSyncId { get; set; }
    public HousekeepingStatus Status { get; set; } = HousekeepingStatus.Pending;
    public string AssignedTo { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Notes { get; set; } = string.Empty;
}
