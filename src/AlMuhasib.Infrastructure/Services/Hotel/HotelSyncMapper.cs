using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Infrastructure.Data.Hotel;
using AlMuhasib.Sync.Dtos;
using AlMuhasib.Sync.Requests;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Hotel;

internal static class HotelSyncMapper
{
    public static async Task<SyncDataBundle> BuildPushBundleAsync(HotelDbContext db, DateTime? since, CancellationToken ct)
    {
        var cutoff = since ?? DateTime.MinValue;
        bool ShouldSync(BaseEntity e) =>
            (e.UpdatedAt ?? e.CreatedAt) >= cutoff
            || (e.IsDeleted && (e.DeletedAt ?? e.UpdatedAt ?? e.CreatedAt) >= cutoff);

        var settings = await db.HotelSettings.IgnoreQueryFilters().ToListAsync(ct);
        var floors = await db.Floors.IgnoreQueryFilters().ToListAsync(ct);
        var roomTypes = await db.RoomTypes.IgnoreQueryFilters().ToListAsync(ct);
        var rooms = await db.Rooms.IgnoreQueryFilters().ToListAsync(ct);
        var guests = await db.Guests.IgnoreQueryFilters().ToListAsync(ct);
        var reservations = await db.Reservations.IgnoreQueryFilters().Include(r => r.Guest).Include(r => r.Room).ToListAsync(ct);
        var charges = await db.ReservationCharges.IgnoreQueryFilters().ToListAsync(ct);
        var payments = await db.ReservationPayments.IgnoreQueryFilters().ToListAsync(ct);
        var cashBoxes = await db.HotelCashBoxes.IgnoreQueryFilters().ToListAsync(ct);
        var vouchers = await db.HotelVouchers.IgnoreQueryFilters().ToListAsync(ct);
        var expenseTypes = await db.HotelExpenseTypes.IgnoreQueryFilters().ToListAsync(ct);
        var expenses = await db.HotelExpenses.IgnoreQueryFilters().ToListAsync(ct);
        var ratePlans = await db.RatePlans.IgnoreQueryFilters().ToListAsync(ct);
        var seasons = await db.RatePlanSeasons.IgnoreQueryFilters().ToListAsync(ct);
        var tasks = await db.HousekeepingTasks.IgnoreQueryFilters().ToListAsync(ct);

        var floorMap = floors.ToDictionary(f => f.Id, f => f.SyncId);
        var roomTypeMap = roomTypes.ToDictionary(r => r.Id, r => r.SyncId);
        var roomMap = rooms.ToDictionary(r => r.Id, r => r.SyncId);
        var guestMap = guests.ToDictionary(g => g.Id, g => g.SyncId);
        var reservationMap = reservations.ToDictionary(r => r.Id, r => r.SyncId);
        var cashBoxMap = cashBoxes.ToDictionary(c => c.Id, c => c.SyncId);
        var expenseTypeMap = expenseTypes.ToDictionary(e => e.Id, e => e.SyncId);
        var expenseMap = expenses.ToDictionary(e => e.Id, e => e.SyncId);
        var ratePlanMap = ratePlans.ToDictionary(p => p.Id, p => p.SyncId);

        var changedRooms = rooms.Where(ShouldSync).ToList();
        var changedReservations = reservations.Where(ShouldSync).ToList();
        var referencedFloorIds = changedRooms.Select(r => r.FloorId).ToHashSet();
        var referencedRoomTypeIds = changedRooms.Select(r => r.RoomTypeId)
            .Concat(ratePlans.Where(ShouldSync).Select(p => p.RoomTypeId)).ToHashSet();

        return new SyncDataBundle
        {
            HotelSettings = settings.Where(ShouldSync).Select(MapSettings).ToList(),
            HotelFloors = floors.Where(f => ShouldSync(f) || referencedFloorIds.Contains(f.Id)).Select(MapFloor).ToList(),
            HotelRoomTypes = roomTypes.Where(r => ShouldSync(r) || referencedRoomTypeIds.Contains(r.Id)).Select(MapRoomType).ToList(),
            HotelRooms = changedRooms.Where(r => floorMap.ContainsKey(r.FloorId) && roomTypeMap.ContainsKey(r.RoomTypeId))
                .Select(r => MapRoom(r, floorMap, roomTypeMap)).ToList(),
            HotelGuests = guests.Where(ShouldSync).Select(MapGuest).ToList(),
            HotelReservations = changedReservations.Where(r => guestMap.ContainsKey(r.GuestId))
                .Select(r => MapReservation(r, guestMap, roomMap)).ToList(),
            HotelReservationCharges = charges.Where(ShouldSync).Where(c => reservationMap.ContainsKey(c.ReservationId))
                .Select(c => MapCharge(c, reservationMap)).ToList(),
            HotelReservationPayments = payments.Where(ShouldSync).Where(p => reservationMap.ContainsKey(p.ReservationId))
                .Select(p => MapPayment(p, reservationMap, cashBoxMap)).ToList(),
            HotelCashBoxes = cashBoxes.Where(ShouldSync).Select(MapCashBox).ToList(),
            HotelVouchers = vouchers.Where(ShouldSync).Where(v => cashBoxMap.ContainsKey(v.HotelCashBoxId))
                .Select(v => MapVoucher(v, cashBoxMap, reservationMap, expenseMap)).ToList(),
            HotelExpenseTypes = expenseTypes.Where(ShouldSync).Select(MapExpenseType).ToList(),
            HotelExpenses = expenses.Where(ShouldSync).Where(e => expenseTypeMap.ContainsKey(e.HotelExpenseTypeId))
                .Select(e => MapExpense(e, expenseTypeMap, cashBoxMap)).ToList(),
            HotelRatePlans = ratePlans.Where(ShouldSync).Where(p => roomTypeMap.ContainsKey(p.RoomTypeId))
                .Select(p => MapRatePlan(p, roomTypeMap)).ToList(),
            HotelRatePlanSeasons = seasons.Where(ShouldSync).Where(s => ratePlanMap.ContainsKey(s.RatePlanId))
                .Select(s => MapSeason(s, ratePlanMap)).ToList(),
            HotelHousekeepingTasks = tasks.Where(ShouldSync).Where(t => roomMap.ContainsKey(t.RoomId))
                .Select(t => MapHousekeeping(t, roomMap)).ToList()
        };
    }

    public static async Task ApplyPullBundleAsync(HotelDbContext db, SyncDataBundle data, CancellationToken ct)
    {
        db.IsApplyingSyncPull = true;
        try
        {
            await ApplySettingsAsync(db, data.HotelSettings, ct);
            var floorMap = await ApplyFloorsAsync(db, data.HotelFloors, ct);
            var roomTypeMap = await ApplyRoomTypesAsync(db, data.HotelRoomTypes, ct);
            var roomMap = await ApplyRoomsAsync(db, data.HotelRooms, floorMap, roomTypeMap, ct);
            var guestMap = await ApplyGuestsAsync(db, data.HotelGuests, ct);
            var reservationMap = await ApplyReservationsAsync(db, data.HotelReservations, guestMap, roomMap, ct);
            await ApplyChargesAsync(db, data.HotelReservationCharges, reservationMap, ct);
            var cashBoxMap = await ApplyCashBoxesAsync(db, data.HotelCashBoxes, ct);
            await ApplyPaymentsAsync(db, data.HotelReservationPayments, reservationMap, cashBoxMap, ct);
            var expenseTypeMap = await ApplyExpenseTypesAsync(db, data.HotelExpenseTypes, ct);
            var expenseMap = await ApplyExpensesAsync(db, data.HotelExpenses, expenseTypeMap, cashBoxMap, ct);
            await ApplyVouchersAsync(db, data.HotelVouchers, cashBoxMap, reservationMap, expenseMap, ct);
            var ratePlanMap = await ApplyRatePlansAsync(db, data.HotelRatePlans, roomTypeMap, ct);
            await ApplySeasonsAsync(db, data.HotelRatePlanSeasons, ratePlanMap, ct);
            await ApplyHousekeepingAsync(db, data.HotelHousekeepingTasks, roomMap, ct);
            await db.SaveChangesAsync(ct);
        }
        finally
        {
            db.IsApplyingSyncPull = false;
        }
    }

    private static void CopyBase(BaseEntity e, SyncDtoBase d)
    {
        d.SyncId = e.SyncId;
        d.CreatedAt = e.CreatedAt;
        d.CreatedBy = e.CreatedBy;
        d.UpdatedAt = e.UpdatedAt;
        d.UpdatedBy = e.UpdatedBy;
        d.IsDeleted = e.IsDeleted;
        d.DeletedAt = e.DeletedAt;
        d.DeletedBy = e.DeletedBy;
        d.RowVersion = e.RowVersion;
    }

    private static void ApplyBase(BaseEntity e, SyncDtoBase d)
    {
        e.SyncId = d.SyncId;
        e.CreatedAt = d.CreatedAt;
        e.CreatedBy = d.CreatedBy;
        e.UpdatedAt = d.UpdatedAt;
        e.UpdatedBy = d.UpdatedBy;
        e.IsDeleted = d.IsDeleted;
        e.DeletedAt = d.DeletedAt;
        e.DeletedBy = d.DeletedBy;
    }

    private static HotelSettingsSyncDto MapSettings(HotelSettings s)
    {
        var d = new HotelSettingsSyncDto(); CopyBase(s, d);
        d.HotelName = s.HotelName; d.Address = s.Address; d.Phone = s.Phone; d.Email = s.Email;
        d.CheckInTime = s.CheckInTime; d.CheckOutTime = s.CheckOutTime; d.CancellationPolicy = s.CancellationPolicy;
        d.Currency = s.Currency; d.IsConfigured = s.IsConfigured;
        return d;
    }

    private static HotelFloorSyncDto MapFloor(Floor f) { var d = new HotelFloorSyncDto(); CopyBase(f, d); d.Name = f.Name; d.SortOrder = f.SortOrder; return d; }
    private static HotelRoomTypeSyncDto MapRoomType(RoomType r) { var d = new HotelRoomTypeSyncDto(); CopyBase(r, d); d.Name = r.Name; d.Description = r.Description; d.Capacity = r.Capacity; d.BasePrice = r.BasePrice; d.SortOrder = r.SortOrder; return d; }
    private static HotelRoomSyncDto MapRoom(Room r, Dictionary<int, Guid> floors, Dictionary<int, Guid> types) { var d = new HotelRoomSyncDto(); CopyBase(r, d); d.RoomNumber = r.RoomNumber; d.FloorSyncId = floors[r.FloorId]; d.RoomTypeSyncId = types[r.RoomTypeId]; d.Status = r.Status; d.Notes = r.Notes; return d; }
    private static HotelGuestSyncDto MapGuest(Guest g) { var d = new HotelGuestSyncDto(); CopyBase(g, d); d.FullName = g.FullName; d.IdNumber = g.IdNumber; d.Phone = g.Phone; d.Email = g.Email; d.Notes = g.Notes; return d; }
    private static HotelReservationSyncDto MapReservation(Reservation r, Dictionary<int, Guid> guests, Dictionary<int, Guid> rooms)
    {
        var d = new HotelReservationSyncDto(); CopyBase(r, d);
        d.ReservationNumber = r.ReservationNumber; d.GuestSyncId = guests[r.GuestId];
        d.RoomSyncId = r.RoomId.HasValue ? rooms.GetValueOrDefault(r.RoomId.Value) : null;
        d.GuestName = r.Guest?.FullName ?? string.Empty; d.RoomNumber = r.Room?.RoomNumber;
        d.CheckInDate = r.CheckInDate; d.CheckOutDate = r.CheckOutDate; d.ActualCheckIn = r.ActualCheckIn; d.ActualCheckOut = r.ActualCheckOut;
        d.GuestCount = r.GuestCount; d.Status = r.Status; d.TotalAmount = r.TotalAmount; d.AmountPaid = r.AmountPaid;
        d.RemainingAmount = r.RemainingAmount; d.Notes = r.Notes;
        return d;
    }
    private static HotelReservationChargeSyncDto MapCharge(ReservationCharge c, Dictionary<int, Guid> reservations) { var d = new HotelReservationChargeSyncDto(); CopyBase(c, d); d.ReservationSyncId = reservations[c.ReservationId]; d.Description = c.Description; d.Amount = c.Amount; d.ChargeDate = c.ChargeDate; d.Notes = c.Notes; return d; }
    private static HotelReservationPaymentSyncDto MapPayment(ReservationPayment p, Dictionary<int, Guid> reservations, Dictionary<int, Guid> cashBoxes) { var d = new HotelReservationPaymentSyncDto(); CopyBase(p, d); d.ReservationSyncId = reservations[p.ReservationId]; d.PaymentDate = p.PaymentDate; d.Amount = p.Amount; d.PaymentMethod = p.PaymentMethod; d.Notes = p.Notes; d.HotelCashBoxSyncId = p.HotelCashBoxId.HasValue ? cashBoxes.GetValueOrDefault(p.HotelCashBoxId.Value) : null; return d; }
    private static HotelCashBoxSyncDto MapCashBox(HotelCashBox c) { var d = new HotelCashBoxSyncDto(); CopyBase(c, d); d.Name = c.Name; d.IsBank = c.IsBank; d.OpeningBalance = c.OpeningBalance; d.CurrentBalance = c.CurrentBalance; d.IsActive = c.IsActive; d.Notes = c.Notes; return d; }
    private static HotelVoucherSyncDto MapVoucher(HotelVoucher v, Dictionary<int, Guid> cashBoxes, Dictionary<int, Guid> reservations, Dictionary<int, Guid> expenses) { var d = new HotelVoucherSyncDto(); CopyBase(v, d); d.VoucherNumber = v.VoucherNumber; d.VoucherDate = v.VoucherDate; d.Type = v.Type; d.Amount = v.Amount; d.HotelCashBoxSyncId = cashBoxes[v.HotelCashBoxId]; d.ReservationSyncId = v.ReservationId.HasValue ? reservations.GetValueOrDefault(v.ReservationId.Value) : null; d.HotelExpenseSyncId = v.HotelExpenseId.HasValue ? expenses.GetValueOrDefault(v.HotelExpenseId.Value) : null; d.Description = v.Description; d.Notes = v.Notes; return d; }
    private static HotelExpenseTypeSyncDto MapExpenseType(HotelExpenseType e) { var d = new HotelExpenseTypeSyncDto(); CopyBase(e, d); d.Name = e.Name; d.Notes = e.Notes; return d; }
    private static HotelExpenseSyncDto MapExpense(HotelExpense e, Dictionary<int, Guid> types, Dictionary<int, Guid> cashBoxes) { var d = new HotelExpenseSyncDto(); CopyBase(e, d); d.HotelExpenseTypeSyncId = types[e.HotelExpenseTypeId]; d.ExpenseDate = e.ExpenseDate; d.Amount = e.Amount; d.Description = e.Description; d.Notes = e.Notes; d.HotelCashBoxSyncId = e.HotelCashBoxId.HasValue ? cashBoxes.GetValueOrDefault(e.HotelCashBoxId.Value) : null; return d; }
    private static HotelRatePlanSyncDto MapRatePlan(RatePlan p, Dictionary<int, Guid> types) { var d = new HotelRatePlanSyncDto(); CopyBase(p, d); d.Name = p.Name; d.RoomTypeSyncId = types[p.RoomTypeId]; d.BasePrice = p.BasePrice; d.IsActive = p.IsActive; d.Notes = p.Notes; return d; }
    private static HotelRatePlanSeasonSyncDto MapSeason(RatePlanSeason s, Dictionary<int, Guid> plans) { var d = new HotelRatePlanSeasonSyncDto(); CopyBase(s, d); d.RatePlanSyncId = plans[s.RatePlanId]; d.Name = s.Name; d.StartDate = s.StartDate; d.EndDate = s.EndDate; d.PricePerNight = s.PricePerNight; return d; }
    private static HotelHousekeepingTaskSyncDto MapHousekeeping(HousekeepingTask t, Dictionary<int, Guid> rooms) { var d = new HotelHousekeepingTaskSyncDto(); CopyBase(t, d); d.RoomSyncId = rooms[t.RoomId]; d.Status = t.Status; d.AssignedTo = t.AssignedTo; d.StartedAt = t.StartedAt; d.CompletedAt = t.CompletedAt; d.Notes = t.Notes; return d; }

    private static async Task ApplySettingsAsync(HotelDbContext db, List<HotelSettingsSyncDto> items, CancellationToken ct)
    {
        foreach (var dto in items)
        {
            var entity = await db.HotelSettings.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.SyncId == dto.SyncId, ct)
                ?? new HotelSettings();
            if (entity.Id == 0) db.HotelSettings.Add(entity);
            ApplyBase(entity, dto);
            entity.HotelName = dto.HotelName; entity.Address = dto.Address; entity.Phone = dto.Phone; entity.Email = dto.Email;
            entity.CheckInTime = dto.CheckInTime; entity.CheckOutTime = dto.CheckOutTime; entity.CancellationPolicy = dto.CancellationPolicy;
            entity.Currency = dto.Currency; entity.IsConfigured = dto.IsConfigured;
        }
    }

    private static async Task<Dictionary<Guid, int>> ApplyFloorsAsync(HotelDbContext db, List<HotelFloorSyncDto> items, CancellationToken ct)
    {
        foreach (var dto in items)
        {
            var entity = await db.Floors.IgnoreQueryFilters().FirstOrDefaultAsync(f => f.SyncId == dto.SyncId, ct) ?? new Floor();
            if (entity.Id == 0) db.Floors.Add(entity);
            ApplyBase(entity, dto); entity.Name = dto.Name; entity.SortOrder = dto.SortOrder;
        }
        await db.SaveChangesAsync(ct);
        return await db.Floors.IgnoreQueryFilters().ToDictionaryAsync(f => f.SyncId, f => f.Id, ct);
    }

    private static async Task<Dictionary<Guid, int>> ApplyRoomTypesAsync(HotelDbContext db, List<HotelRoomTypeSyncDto> items, CancellationToken ct)
    {
        foreach (var dto in items)
        {
            var entity = await db.RoomTypes.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.SyncId == dto.SyncId, ct) ?? new RoomType();
            if (entity.Id == 0) db.RoomTypes.Add(entity);
            ApplyBase(entity, dto); entity.Name = dto.Name; entity.Description = dto.Description; entity.Capacity = dto.Capacity;
            entity.BasePrice = dto.BasePrice; entity.SortOrder = dto.SortOrder;
        }
        await db.SaveChangesAsync(ct);
        return await db.RoomTypes.IgnoreQueryFilters().ToDictionaryAsync(r => r.SyncId, r => r.Id, ct);
    }

    private static async Task<Dictionary<Guid, int>> ApplyRoomsAsync(HotelDbContext db, List<HotelRoomSyncDto> items, Dictionary<Guid, int> floors, Dictionary<Guid, int> types, CancellationToken ct)
    {
        foreach (var dto in items)
        {
            if (!floors.TryGetValue(dto.FloorSyncId, out var floorId) || !types.TryGetValue(dto.RoomTypeSyncId, out var typeId)) continue;
            var entity = await db.Rooms.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.SyncId == dto.SyncId, ct) ?? new Room();
            if (entity.Id == 0) db.Rooms.Add(entity);
            ApplyBase(entity, dto); entity.RoomNumber = dto.RoomNumber; entity.FloorId = floorId; entity.RoomTypeId = typeId;
            entity.Status = dto.Status; entity.Notes = dto.Notes;
        }
        await db.SaveChangesAsync(ct);
        return await db.Rooms.IgnoreQueryFilters().ToDictionaryAsync(r => r.SyncId, r => r.Id, ct);
    }

    private static async Task<Dictionary<Guid, int>> ApplyGuestsAsync(HotelDbContext db, List<HotelGuestSyncDto> items, CancellationToken ct)
    {
        foreach (var dto in items)
        {
            var entity = await db.Guests.IgnoreQueryFilters().FirstOrDefaultAsync(g => g.SyncId == dto.SyncId, ct) ?? new Guest();
            if (entity.Id == 0) db.Guests.Add(entity);
            ApplyBase(entity, dto); entity.FullName = dto.FullName; entity.IdNumber = dto.IdNumber; entity.Phone = dto.Phone;
            entity.Email = dto.Email; entity.Notes = dto.Notes;
        }
        await db.SaveChangesAsync(ct);
        return await db.Guests.IgnoreQueryFilters().ToDictionaryAsync(g => g.SyncId, g => g.Id, ct);
    }

    private static async Task<Dictionary<Guid, int>> ApplyReservationsAsync(HotelDbContext db, List<HotelReservationSyncDto> items, Dictionary<Guid, int> guests, Dictionary<Guid, int> rooms, CancellationToken ct)
    {
        foreach (var dto in items)
        {
            if (!guests.TryGetValue(dto.GuestSyncId, out var guestId)) continue;
            int? roomId = dto.RoomSyncId.HasValue && rooms.TryGetValue(dto.RoomSyncId.Value, out var rid) ? rid : null;
            var entity = await db.Reservations.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.SyncId == dto.SyncId, ct) ?? new Reservation();
            if (entity.Id == 0) db.Reservations.Add(entity);
            ApplyBase(entity, dto); entity.ReservationNumber = dto.ReservationNumber; entity.GuestId = guestId; entity.RoomId = roomId;
            entity.CheckInDate = dto.CheckInDate; entity.CheckOutDate = dto.CheckOutDate; entity.ActualCheckIn = dto.ActualCheckIn;
            entity.ActualCheckOut = dto.ActualCheckOut; entity.GuestCount = dto.GuestCount; entity.Status = dto.Status;
            entity.TotalAmount = dto.TotalAmount; entity.AmountPaid = dto.AmountPaid; entity.RemainingAmount = dto.RemainingAmount; entity.Notes = dto.Notes;
        }
        await db.SaveChangesAsync(ct);
        return await db.Reservations.IgnoreQueryFilters().ToDictionaryAsync(r => r.SyncId, r => r.Id, ct);
    }

    private static async Task ApplyChargesAsync(HotelDbContext db, List<HotelReservationChargeSyncDto> items, Dictionary<Guid, int> reservations, CancellationToken ct)
    {
        foreach (var dto in items)
        {
            if (!reservations.TryGetValue(dto.ReservationSyncId, out var reservationId)) continue;
            var entity = await db.ReservationCharges.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.SyncId == dto.SyncId, ct) ?? new ReservationCharge();
            if (entity.Id == 0) db.ReservationCharges.Add(entity);
            ApplyBase(entity, dto); entity.ReservationId = reservationId; entity.Description = dto.Description;
            entity.Amount = dto.Amount; entity.ChargeDate = dto.ChargeDate; entity.Notes = dto.Notes;
        }
    }

    private static async Task<Dictionary<Guid, int>> ApplyCashBoxesAsync(HotelDbContext db, List<HotelCashBoxSyncDto> items, CancellationToken ct)
    {
        foreach (var dto in items)
        {
            var entity = await db.HotelCashBoxes.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.SyncId == dto.SyncId, ct) ?? new HotelCashBox();
            if (entity.Id == 0) db.HotelCashBoxes.Add(entity);
            ApplyBase(entity, dto); entity.Name = dto.Name; entity.IsBank = dto.IsBank; entity.OpeningBalance = dto.OpeningBalance;
            entity.CurrentBalance = dto.CurrentBalance; entity.IsActive = dto.IsActive; entity.Notes = dto.Notes;
        }
        await db.SaveChangesAsync(ct);
        return await db.HotelCashBoxes.IgnoreQueryFilters().ToDictionaryAsync(c => c.SyncId, c => c.Id, ct);
    }

    private static async Task ApplyPaymentsAsync(HotelDbContext db, List<HotelReservationPaymentSyncDto> items, Dictionary<Guid, int> reservations, Dictionary<Guid, int> cashBoxes, CancellationToken ct)
    {
        foreach (var dto in items)
        {
            if (!reservations.TryGetValue(dto.ReservationSyncId, out var reservationId)) continue;
            int? cashBoxId = dto.HotelCashBoxSyncId.HasValue && cashBoxes.TryGetValue(dto.HotelCashBoxSyncId.Value, out var cb) ? cb : null;
            var entity = await db.ReservationPayments.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.SyncId == dto.SyncId, ct) ?? new ReservationPayment();
            if (entity.Id == 0) db.ReservationPayments.Add(entity);
            ApplyBase(entity, dto); entity.ReservationId = reservationId; entity.PaymentDate = dto.PaymentDate; entity.Amount = dto.Amount;
            entity.PaymentMethod = dto.PaymentMethod; entity.Notes = dto.Notes; entity.HotelCashBoxId = cashBoxId;
        }
    }

    private static async Task<Dictionary<Guid, int>> ApplyExpenseTypesAsync(HotelDbContext db, List<HotelExpenseTypeSyncDto> items, CancellationToken ct)
    {
        foreach (var dto in items)
        {
            var entity = await db.HotelExpenseTypes.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.SyncId == dto.SyncId, ct) ?? new HotelExpenseType();
            if (entity.Id == 0) db.HotelExpenseTypes.Add(entity);
            ApplyBase(entity, dto); entity.Name = dto.Name; entity.Notes = dto.Notes;
        }
        await db.SaveChangesAsync(ct);
        return await db.HotelExpenseTypes.IgnoreQueryFilters().ToDictionaryAsync(e => e.SyncId, e => e.Id, ct);
    }

    private static async Task<Dictionary<Guid, int>> ApplyExpensesAsync(HotelDbContext db, List<HotelExpenseSyncDto> items, Dictionary<Guid, int> types, Dictionary<Guid, int> cashBoxes, CancellationToken ct)
    {
        foreach (var dto in items)
        {
            if (!types.TryGetValue(dto.HotelExpenseTypeSyncId, out var typeId)) continue;
            int? cashBoxId = dto.HotelCashBoxSyncId.HasValue && cashBoxes.TryGetValue(dto.HotelCashBoxSyncId.Value, out var cb) ? cb : null;
            var entity = await db.HotelExpenses.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.SyncId == dto.SyncId, ct) ?? new HotelExpense();
            if (entity.Id == 0) db.HotelExpenses.Add(entity);
            ApplyBase(entity, dto); entity.HotelExpenseTypeId = typeId; entity.ExpenseDate = dto.ExpenseDate; entity.Amount = dto.Amount;
            entity.Description = dto.Description; entity.Notes = dto.Notes; entity.HotelCashBoxId = cashBoxId;
        }
        await db.SaveChangesAsync(ct);
        return await db.HotelExpenses.IgnoreQueryFilters().ToDictionaryAsync(e => e.SyncId, e => e.Id, ct);
    }

    private static async Task ApplyVouchersAsync(HotelDbContext db, List<HotelVoucherSyncDto> items, Dictionary<Guid, int> cashBoxes, Dictionary<Guid, int> reservations, Dictionary<Guid, int> expenses, CancellationToken ct)
    {
        foreach (var dto in items)
        {
            if (!cashBoxes.TryGetValue(dto.HotelCashBoxSyncId, out var cashBoxId)) continue;
            int? reservationId = dto.ReservationSyncId.HasValue && reservations.TryGetValue(dto.ReservationSyncId.Value, out var rid) ? rid : null;
            int? expenseId = dto.HotelExpenseSyncId.HasValue && expenses.TryGetValue(dto.HotelExpenseSyncId.Value, out var eid) ? eid : null;
            var entity = await db.HotelVouchers.IgnoreQueryFilters().FirstOrDefaultAsync(v => v.SyncId == dto.SyncId, ct) ?? new HotelVoucher();
            if (entity.Id == 0) db.HotelVouchers.Add(entity);
            ApplyBase(entity, dto); entity.VoucherNumber = dto.VoucherNumber; entity.VoucherDate = dto.VoucherDate; entity.Type = dto.Type;
            entity.Amount = dto.Amount; entity.HotelCashBoxId = cashBoxId; entity.ReservationId = reservationId; entity.HotelExpenseId = expenseId;
            entity.Description = dto.Description; entity.Notes = dto.Notes;
        }
    }

    private static async Task<Dictionary<Guid, int>> ApplyRatePlansAsync(HotelDbContext db, List<HotelRatePlanSyncDto> items, Dictionary<Guid, int> roomTypes, CancellationToken ct)
    {
        foreach (var dto in items)
        {
            if (!roomTypes.TryGetValue(dto.RoomTypeSyncId, out var typeId)) continue;
            var entity = await db.RatePlans.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.SyncId == dto.SyncId, ct) ?? new RatePlan();
            if (entity.Id == 0) db.RatePlans.Add(entity);
            ApplyBase(entity, dto); entity.Name = dto.Name; entity.RoomTypeId = typeId; entity.BasePrice = dto.BasePrice;
            entity.IsActive = dto.IsActive; entity.Notes = dto.Notes;
        }
        await db.SaveChangesAsync(ct);
        return await db.RatePlans.IgnoreQueryFilters().ToDictionaryAsync(p => p.SyncId, p => p.Id, ct);
    }

    private static async Task ApplySeasonsAsync(HotelDbContext db, List<HotelRatePlanSeasonSyncDto> items, Dictionary<Guid, int> plans, CancellationToken ct)
    {
        foreach (var dto in items)
        {
            if (!plans.TryGetValue(dto.RatePlanSyncId, out var planId)) continue;
            var entity = await db.RatePlanSeasons.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.SyncId == dto.SyncId, ct) ?? new RatePlanSeason();
            if (entity.Id == 0) db.RatePlanSeasons.Add(entity);
            ApplyBase(entity, dto); entity.RatePlanId = planId; entity.Name = dto.Name; entity.StartDate = dto.StartDate;
            entity.EndDate = dto.EndDate; entity.PricePerNight = dto.PricePerNight;
        }
    }

    private static async Task ApplyHousekeepingAsync(HotelDbContext db, List<HotelHousekeepingTaskSyncDto> items, Dictionary<Guid, int> rooms, CancellationToken ct)
    {
        foreach (var dto in items)
        {
            if (!rooms.TryGetValue(dto.RoomSyncId, out var roomId)) continue;
            var entity = await db.HousekeepingTasks.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.SyncId == dto.SyncId, ct) ?? new HousekeepingTask();
            if (entity.Id == 0) db.HousekeepingTasks.Add(entity);
            ApplyBase(entity, dto); entity.RoomId = roomId; entity.Status = dto.Status; entity.AssignedTo = dto.AssignedTo;
            entity.StartedAt = dto.StartedAt; entity.CompletedAt = dto.CompletedAt; entity.Notes = dto.Notes;
        }
    }
}
