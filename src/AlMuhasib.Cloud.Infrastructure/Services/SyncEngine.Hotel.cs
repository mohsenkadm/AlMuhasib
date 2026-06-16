using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Sync.Dtos;
using AlMuhasib.Sync.Requests;
using AlMuhasib.Sync.Responses;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Cloud.Infrastructure.Services;

public sealed partial class SyncEngine
{
    private async Task<SyncPushResponse> PushHotelAsync(int tenantId, SyncPushRequest request, CancellationToken ct)
    {
        var resolver = new SyncIdResolver(_db, tenantId);
        var response = new SyncPushResponse { ServerTime = DateTime.UtcNow };
        var accepted = 0;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            foreach (var dto in request.Data.HotelSettings)
                accepted += await UpsertHotelSettingsAsync(tenantId, dto, response, ct);
            await _db.SaveChangesAsync(ct);

            foreach (var dto in request.Data.HotelFloors)
                accepted += await UpsertHotelFloorAsync(tenantId, dto, response, ct);
            await FlushAndCacheAsync(_db.HotelFloors, tenantId, request.Data.HotelFloors.Select(f => f.SyncId), resolver, ct);

            foreach (var dto in request.Data.HotelRoomTypes)
                accepted += await UpsertHotelRoomTypeAsync(tenantId, dto, response, ct);
            await FlushAndCacheAsync(_db.HotelRoomTypes, tenantId, request.Data.HotelRoomTypes.Select(r => r.SyncId), resolver, ct);

            foreach (var dto in request.Data.HotelRooms)
            {
                var floorId = await resolver.ResolveHotelFloorAsync(dto.FloorSyncId, ct);
                var roomTypeId = await resolver.ResolveHotelRoomTypeAsync(dto.RoomTypeSyncId, ct);
                if (floorId is null || roomTypeId is null)
                {
                    AddConflict(response, "HotelRoom", dto.SyncId, "Floor or room type not found");
                    continue;
                }
                accepted += await UpsertHotelRoomAsync(tenantId, dto, floorId.Value, roomTypeId.Value, response, ct);
            }
            await FlushAndCacheAsync(_db.HotelRooms, tenantId, request.Data.HotelRooms.Select(r => r.SyncId), resolver, ct);

            foreach (var dto in request.Data.HotelGuests)
                accepted += await UpsertHotelGuestAsync(tenantId, dto, response, ct);
            await FlushAndCacheAsync(_db.HotelGuests, tenantId, request.Data.HotelGuests.Select(g => g.SyncId), resolver, ct);

            foreach (var dto in request.Data.HotelReservations)
            {
                var guestId = await resolver.ResolveHotelGuestAsync(dto.GuestSyncId, ct);
                if (guestId is null)
                {
                    AddConflict(response, "HotelReservation", dto.SyncId, "Guest not found");
                    continue;
                }
                var roomId = await resolver.ResolveHotelRoomAsync(dto.RoomSyncId, ct);
                accepted += await UpsertHotelReservationAsync(tenantId, dto, guestId.Value, roomId, response, ct);
            }
            await FlushAndCacheAsync(_db.HotelReservations, tenantId, request.Data.HotelReservations.Select(r => r.SyncId), resolver, ct);

            foreach (var dto in request.Data.HotelReservationCharges)
            {
                var reservationId = await resolver.ResolveHotelReservationAsync(dto.ReservationSyncId, ct);
                if (reservationId is null)
                {
                    AddConflict(response, "HotelReservationCharge", dto.SyncId, "Reservation not found");
                    continue;
                }
                accepted += await UpsertHotelReservationChargeAsync(tenantId, dto, reservationId.Value, response, ct);
            }

            foreach (var dto in request.Data.HotelReservationPayments)
            {
                var reservationId = await resolver.ResolveHotelReservationAsync(dto.ReservationSyncId, ct);
                if (reservationId is null)
                {
                    AddConflict(response, "HotelReservationPayment", dto.SyncId, "Reservation not found");
                    continue;
                }
                var cashBoxId = await resolver.ResolveHotelCashBoxAsync(dto.HotelCashBoxSyncId, ct);
                accepted += await UpsertHotelReservationPaymentAsync(tenantId, dto, reservationId.Value, cashBoxId, response, ct);
            }

            foreach (var dto in request.Data.HotelCashBoxes)
                accepted += await UpsertHotelCashBoxAsync(tenantId, dto, response, ct);
            await FlushAndCacheAsync(_db.HotelCashBoxes, tenantId, request.Data.HotelCashBoxes.Select(c => c.SyncId), resolver, ct);

            foreach (var dto in request.Data.HotelExpenseTypes)
                accepted += await UpsertHotelExpenseTypeAsync(tenantId, dto, response, ct);
            await FlushAndCacheAsync(_db.HotelExpenseTypes, tenantId, request.Data.HotelExpenseTypes.Select(e => e.SyncId), resolver, ct);

            foreach (var dto in request.Data.HotelExpenses)
            {
                var typeId = await resolver.ResolveHotelExpenseTypeAsync(dto.HotelExpenseTypeSyncId, ct);
                if (typeId is null)
                {
                    AddConflict(response, "HotelExpense", dto.SyncId, "Expense type not found");
                    continue;
                }
                var cashBoxId = await resolver.ResolveHotelCashBoxAsync(dto.HotelCashBoxSyncId, ct);
                accepted += await UpsertHotelExpenseAsync(tenantId, dto, typeId.Value, cashBoxId, response, ct);
            }

            foreach (var dto in request.Data.HotelVouchers)
            {
                var cashBoxId = await resolver.ResolveHotelCashBoxAsync(dto.HotelCashBoxSyncId, ct);
                if (cashBoxId is null)
                {
                    AddConflict(response, "HotelVoucher", dto.SyncId, "Cash box not found");
                    continue;
                }
                var reservationId = await resolver.ResolveHotelReservationAsync(dto.ReservationSyncId, ct);
                var expenseId = await resolver.ResolveHotelExpenseAsync(dto.HotelExpenseSyncId, ct);
                accepted += await UpsertHotelVoucherAsync(tenantId, dto, cashBoxId.Value, reservationId, expenseId, response, ct);
            }

            foreach (var dto in request.Data.HotelRatePlans)
            {
                var roomTypeId = await resolver.ResolveHotelRoomTypeAsync(dto.RoomTypeSyncId, ct);
                if (roomTypeId is null)
                {
                    AddConflict(response, "HotelRatePlan", dto.SyncId, "Room type not found");
                    continue;
                }
                accepted += await UpsertHotelRatePlanAsync(tenantId, dto, roomTypeId.Value, response, ct);
            }
            await FlushAndCacheAsync(_db.HotelRatePlans, tenantId, request.Data.HotelRatePlans.Select(p => p.SyncId), resolver, ct);

            foreach (var dto in request.Data.HotelRatePlanSeasons)
            {
                var planId = await resolver.ResolveHotelRatePlanAsync(dto.RatePlanSyncId, ct);
                if (planId is null)
                {
                    AddConflict(response, "HotelRatePlanSeason", dto.SyncId, "Rate plan not found");
                    continue;
                }
                accepted += await UpsertHotelRatePlanSeasonAsync(tenantId, dto, planId.Value, response, ct);
            }

            foreach (var dto in request.Data.HotelHousekeepingTasks)
            {
                var roomId = await resolver.ResolveHotelRoomAsync(dto.RoomSyncId, ct);
                if (roomId is null)
                {
                    AddConflict(response, "HotelHousekeepingTask", dto.SyncId, "Room not found");
                    continue;
                }
                accepted += await UpsertHotelHousekeepingTaskAsync(tenantId, dto, roomId.Value, response, ct);
            }

            var tenant = await _db.Tenants.FindAsync([tenantId], ct);
            if (tenant is not null)
                tenant.LastSyncAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            response.AcceptedCount = accepted;
            response.RejectedCount = response.Conflicts.Count;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        return response;
    }

    private async Task<SyncPullResponse> PullHotelAsync(int tenantId, SyncPullRequest request, CancellationToken ct)
    {
        var since = request.Since ?? DateTime.MinValue;
        var bundle = new SyncDataBundle
        {
            HotelSettings = await PullEntitiesAsync(_db.HotelSettings, tenantId, since, MapHotelSettings, ct),
            HotelFloors = await PullEntitiesAsync(_db.HotelFloors, tenantId, since, MapHotelFloor, ct),
            HotelRoomTypes = await PullEntitiesAsync(_db.HotelRoomTypes, tenantId, since, MapHotelRoomType, ct),
            HotelGuests = await PullEntitiesAsync(_db.HotelGuests, tenantId, since, MapHotelGuest, ct),
            HotelCashBoxes = await PullEntitiesAsync(_db.HotelCashBoxes, tenantId, since, MapHotelCashBox, ct),
            HotelExpenseTypes = await PullEntitiesAsync(_db.HotelExpenseTypes, tenantId, since, MapHotelExpenseType, ct)
        };

        var floorMap = await _db.HotelFloors.IgnoreQueryFilters().Where(e => e.TenantId == tenantId)
            .ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var roomTypeMap = await _db.HotelRoomTypes.IgnoreQueryFilters().Where(e => e.TenantId == tenantId)
            .ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var roomMap = await _db.HotelRooms.IgnoreQueryFilters().Where(e => e.TenantId == tenantId)
            .ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var guestMap = await _db.HotelGuests.IgnoreQueryFilters().Where(e => e.TenantId == tenantId)
            .ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var reservationMap = await _db.HotelReservations.IgnoreQueryFilters().Where(e => e.TenantId == tenantId)
            .ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var cashBoxMap = await _db.HotelCashBoxes.IgnoreQueryFilters().Where(e => e.TenantId == tenantId)
            .ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var expenseTypeMap = await _db.HotelExpenseTypes.IgnoreQueryFilters().Where(e => e.TenantId == tenantId)
            .ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var expenseMap = await _db.HotelExpenses.IgnoreQueryFilters().Where(e => e.TenantId == tenantId)
            .ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);
        var ratePlanMap = await _db.HotelRatePlans.IgnoreQueryFilters().Where(e => e.TenantId == tenantId)
            .ToDictionaryAsync(e => e.Id, e => e.SyncId, ct);

        var rooms = await _db.HotelRooms.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        bundle.HotelRooms = rooms.Select(r => new HotelRoomSyncDto
        {
            SyncId = r.SyncId, CreatedAt = r.CreatedAt, CreatedBy = r.CreatedBy, UpdatedAt = r.UpdatedAt, UpdatedBy = r.UpdatedBy,
            IsDeleted = r.IsDeleted, DeletedAt = r.DeletedAt, DeletedBy = r.DeletedBy, RowVersion = r.RowVersion,
            RoomNumber = r.RoomNumber, FloorSyncId = floorMap.GetValueOrDefault(r.FloorId),
            RoomTypeSyncId = roomTypeMap.GetValueOrDefault(r.RoomTypeId), Status = r.Status, Notes = r.Notes
        }).ToList();

        var reservations = await _db.HotelReservations.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        bundle.HotelReservations = reservations.Select(r => new HotelReservationSyncDto
        {
            SyncId = r.SyncId, CreatedAt = r.CreatedAt, CreatedBy = r.CreatedBy, UpdatedAt = r.UpdatedAt, UpdatedBy = r.UpdatedBy,
            IsDeleted = r.IsDeleted, DeletedAt = r.DeletedAt, DeletedBy = r.DeletedBy, RowVersion = r.RowVersion,
            ReservationNumber = r.ReservationNumber, GuestSyncId = guestMap.GetValueOrDefault(r.GuestId),
            RoomSyncId = r.RoomId.HasValue ? roomMap.GetValueOrDefault(r.RoomId.Value) : null,
            GuestName = r.GuestName, RoomNumber = r.RoomNumber, CheckInDate = r.CheckInDate, CheckOutDate = r.CheckOutDate,
            ActualCheckIn = r.ActualCheckIn, ActualCheckOut = r.ActualCheckOut, GuestCount = r.GuestCount, Status = r.Status,
            TotalAmount = r.TotalAmount, AmountPaid = r.AmountPaid, RemainingAmount = r.RemainingAmount, Notes = r.Notes
        }).ToList();

        var charges = await _db.HotelReservationCharges.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        bundle.HotelReservationCharges = charges.Select(c => new HotelReservationChargeSyncDto
        {
            SyncId = c.SyncId, CreatedAt = c.CreatedAt, CreatedBy = c.CreatedBy, UpdatedAt = c.UpdatedAt, UpdatedBy = c.UpdatedBy,
            IsDeleted = c.IsDeleted, DeletedAt = c.DeletedAt, DeletedBy = c.DeletedBy, RowVersion = c.RowVersion,
            ReservationSyncId = reservationMap.GetValueOrDefault(c.ReservationId),
            Description = c.Description, Amount = c.Amount, ChargeDate = c.ChargeDate, Notes = c.Notes
        }).ToList();

        var payments = await _db.HotelReservationPayments.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        bundle.HotelReservationPayments = payments.Select(p => new HotelReservationPaymentSyncDto
        {
            SyncId = p.SyncId, CreatedAt = p.CreatedAt, CreatedBy = p.CreatedBy, UpdatedAt = p.UpdatedAt, UpdatedBy = p.UpdatedBy,
            IsDeleted = p.IsDeleted, DeletedAt = p.DeletedAt, DeletedBy = p.DeletedBy, RowVersion = p.RowVersion,
            ReservationSyncId = reservationMap.GetValueOrDefault(p.ReservationId), PaymentDate = p.PaymentDate,
            Amount = p.Amount, PaymentMethod = p.PaymentMethod, Notes = p.Notes,
            HotelCashBoxSyncId = p.HotelCashBoxId.HasValue ? cashBoxMap.GetValueOrDefault(p.HotelCashBoxId.Value) : null
        }).ToList();

        var expenses = await _db.HotelExpenses.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        bundle.HotelExpenses = expenses.Select(e => new HotelExpenseSyncDto
        {
            SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
            IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
            HotelExpenseTypeSyncId = expenseTypeMap.GetValueOrDefault(e.HotelExpenseTypeId), ExpenseDate = e.ExpenseDate,
            Amount = e.Amount, Description = e.Description, Notes = e.Notes,
            HotelCashBoxSyncId = e.HotelCashBoxId.HasValue ? cashBoxMap.GetValueOrDefault(e.HotelCashBoxId.Value) : null
        }).ToList();

        var vouchers = await _db.HotelVouchers.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        bundle.HotelVouchers = vouchers.Select(v => new HotelVoucherSyncDto
        {
            SyncId = v.SyncId, CreatedAt = v.CreatedAt, CreatedBy = v.CreatedBy, UpdatedAt = v.UpdatedAt, UpdatedBy = v.UpdatedBy,
            IsDeleted = v.IsDeleted, DeletedAt = v.DeletedAt, DeletedBy = v.DeletedBy, RowVersion = v.RowVersion,
            VoucherNumber = v.VoucherNumber, VoucherDate = v.VoucherDate, Type = v.Type, Amount = v.Amount,
            HotelCashBoxSyncId = cashBoxMap.GetValueOrDefault(v.HotelCashBoxId),
            ReservationSyncId = v.ReservationId.HasValue ? reservationMap.GetValueOrDefault(v.ReservationId.Value) : null,
            HotelExpenseSyncId = v.HotelExpenseId.HasValue ? expenseMap.GetValueOrDefault(v.HotelExpenseId.Value) : null,
            Description = v.Description, Notes = v.Notes
        }).ToList();

        var ratePlans = await _db.HotelRatePlans.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        bundle.HotelRatePlans = ratePlans.Select(p => new HotelRatePlanSyncDto
        {
            SyncId = p.SyncId, CreatedAt = p.CreatedAt, CreatedBy = p.CreatedBy, UpdatedAt = p.UpdatedAt, UpdatedBy = p.UpdatedBy,
            IsDeleted = p.IsDeleted, DeletedAt = p.DeletedAt, DeletedBy = p.DeletedBy, RowVersion = p.RowVersion,
            Name = p.Name, RoomTypeSyncId = roomTypeMap.GetValueOrDefault(p.RoomTypeId),
            BasePrice = p.BasePrice, IsActive = p.IsActive, Notes = p.Notes
        }).ToList();

        var seasons = await _db.HotelRatePlanSeasons.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        bundle.HotelRatePlanSeasons = seasons.Select(s => new HotelRatePlanSeasonSyncDto
        {
            SyncId = s.SyncId, CreatedAt = s.CreatedAt, CreatedBy = s.CreatedBy, UpdatedAt = s.UpdatedAt, UpdatedBy = s.UpdatedBy,
            IsDeleted = s.IsDeleted, DeletedAt = s.DeletedAt, DeletedBy = s.DeletedBy, RowVersion = s.RowVersion,
            RatePlanSyncId = ratePlanMap.GetValueOrDefault(s.RatePlanId), Name = s.Name,
            StartDate = s.StartDate, EndDate = s.EndDate, PricePerNight = s.PricePerNight
        }).ToList();

        var tasks = await _db.HotelHousekeepingTasks.IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && (e.UpdatedAt ?? e.CreatedAt) >= since).ToListAsync(ct);
        bundle.HotelHousekeepingTasks = tasks.Select(t => new HotelHousekeepingTaskSyncDto
        {
            SyncId = t.SyncId, CreatedAt = t.CreatedAt, CreatedBy = t.CreatedBy, UpdatedAt = t.UpdatedAt, UpdatedBy = t.UpdatedBy,
            IsDeleted = t.IsDeleted, DeletedAt = t.DeletedAt, DeletedBy = t.DeletedBy, RowVersion = t.RowVersion,
            RoomSyncId = roomMap.GetValueOrDefault(t.RoomId), Status = t.Status, AssignedTo = t.AssignedTo,
            StartedAt = t.StartedAt, CompletedAt = t.CompletedAt, Notes = t.Notes
        }).ToList();

        var serverTime = DateTime.UtcNow;
        return new SyncPullResponse
        {
            Data = bundle,
            Cursor = serverTime.Ticks.ToString(),
            ServerTime = serverTime,
            HasMore = false
        };
    }

    private static HotelSettingsSyncDto MapHotelSettings(CloudHotelSettings e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        HotelName = e.HotelName, Address = e.Address, Phone = e.Phone, Email = e.Email,
        CheckInTime = e.CheckInTime, CheckOutTime = e.CheckOutTime, CancellationPolicy = e.CancellationPolicy,
        Currency = e.Currency, IsConfigured = e.IsConfigured
    };

    private static HotelFloorSyncDto MapHotelFloor(CloudHotelFloor e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        Name = e.Name, SortOrder = e.SortOrder
    };

    private static HotelRoomTypeSyncDto MapHotelRoomType(CloudHotelRoomType e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        Name = e.Name, Description = e.Description, Capacity = e.Capacity, BasePrice = e.BasePrice, SortOrder = e.SortOrder
    };

    private static HotelGuestSyncDto MapHotelGuest(CloudHotelGuest e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        FullName = e.FullName, IdNumber = e.IdNumber, Phone = e.Phone, Email = e.Email, Notes = e.Notes
    };

    private static HotelCashBoxSyncDto MapHotelCashBox(CloudHotelCashBox e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        Name = e.Name, IsBank = e.IsBank, OpeningBalance = e.OpeningBalance, CurrentBalance = e.CurrentBalance,
        IsActive = e.IsActive, Notes = e.Notes
    };

    private static HotelExpenseTypeSyncDto MapHotelExpenseType(CloudHotelExpenseType e, Dictionary<int, Guid> _) => new()
    {
        SyncId = e.SyncId, CreatedAt = e.CreatedAt, CreatedBy = e.CreatedBy, UpdatedAt = e.UpdatedAt, UpdatedBy = e.UpdatedBy,
        IsDeleted = e.IsDeleted, DeletedAt = e.DeletedAt, DeletedBy = e.DeletedBy, RowVersion = e.RowVersion,
        Name = e.Name, Notes = e.Notes
    };

    private async Task<int> UpsertHotelSettingsAsync(int tenantId, HotelSettingsSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.HotelSettings, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "HotelSettings", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudHotelSettings { TenantId = tenantId }; _db.HotelSettings.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.HotelName = dto.HotelName; existing.Address = dto.Address; existing.Phone = dto.Phone;
        existing.Email = dto.Email; existing.CheckInTime = dto.CheckInTime; existing.CheckOutTime = dto.CheckOutTime;
        existing.CancellationPolicy = dto.CancellationPolicy; existing.Currency = dto.Currency; existing.IsConfigured = dto.IsConfigured;
        return 1;
    }

    private async Task<int> UpsertHotelFloorAsync(int tenantId, HotelFloorSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.HotelFloors, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "HotelFloor", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudHotelFloor { TenantId = tenantId }; _db.HotelFloors.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.Name = dto.Name; existing.SortOrder = dto.SortOrder;
        return 1;
    }

    private async Task<int> UpsertHotelRoomTypeAsync(int tenantId, HotelRoomTypeSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.HotelRoomTypes, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "HotelRoomType", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudHotelRoomType { TenantId = tenantId }; _db.HotelRoomTypes.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.Name = dto.Name; existing.Description = dto.Description; existing.Capacity = dto.Capacity;
        existing.BasePrice = dto.BasePrice; existing.SortOrder = dto.SortOrder;
        return 1;
    }

    private async Task<int> UpsertHotelRoomAsync(int tenantId, HotelRoomSyncDto dto, int floorId, int roomTypeId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.HotelRooms, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "HotelRoom", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudHotelRoom { TenantId = tenantId }; _db.HotelRooms.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.RoomNumber = dto.RoomNumber; existing.FloorId = floorId; existing.RoomTypeId = roomTypeId;
        existing.Status = dto.Status; existing.Notes = dto.Notes;
        return 1;
    }

    private async Task<int> UpsertHotelGuestAsync(int tenantId, HotelGuestSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.HotelGuests, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "HotelGuest", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudHotelGuest { TenantId = tenantId }; _db.HotelGuests.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.FullName = dto.FullName; existing.IdNumber = dto.IdNumber; existing.Phone = dto.Phone;
        existing.Email = dto.Email; existing.Notes = dto.Notes;
        return 1;
    }

    private async Task<int> UpsertHotelReservationAsync(int tenantId, HotelReservationSyncDto dto, int guestId, int? roomId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.HotelReservations, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "HotelReservation", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudHotelReservation { TenantId = tenantId }; _db.HotelReservations.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.ReservationNumber = dto.ReservationNumber; existing.GuestId = guestId; existing.RoomId = roomId;
        existing.GuestName = dto.GuestName; existing.RoomNumber = dto.RoomNumber;
        existing.CheckInDate = dto.CheckInDate; existing.CheckOutDate = dto.CheckOutDate;
        existing.ActualCheckIn = dto.ActualCheckIn; existing.ActualCheckOut = dto.ActualCheckOut;
        existing.GuestCount = dto.GuestCount; existing.Status = dto.Status;
        existing.TotalAmount = dto.TotalAmount; existing.AmountPaid = dto.AmountPaid; existing.RemainingAmount = dto.RemainingAmount;
        existing.Notes = dto.Notes;
        return 1;
    }

    private async Task<int> UpsertHotelReservationChargeAsync(int tenantId, HotelReservationChargeSyncDto dto, int reservationId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.HotelReservationCharges, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "HotelReservationCharge", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudHotelReservationCharge { TenantId = tenantId }; _db.HotelReservationCharges.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.ReservationId = reservationId; existing.Description = dto.Description;
        existing.Amount = dto.Amount; existing.ChargeDate = dto.ChargeDate; existing.Notes = dto.Notes;
        return 1;
    }

    private async Task<int> UpsertHotelReservationPaymentAsync(int tenantId, HotelReservationPaymentSyncDto dto, int reservationId, int? cashBoxId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.HotelReservationPayments, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "HotelReservationPayment", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudHotelReservationPayment { TenantId = tenantId }; _db.HotelReservationPayments.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.ReservationId = reservationId; existing.PaymentDate = dto.PaymentDate; existing.Amount = dto.Amount;
        existing.PaymentMethod = dto.PaymentMethod; existing.Notes = dto.Notes; existing.HotelCashBoxId = cashBoxId;
        return 1;
    }

    private async Task<int> UpsertHotelCashBoxAsync(int tenantId, HotelCashBoxSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.HotelCashBoxes, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "HotelCashBox", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudHotelCashBox { TenantId = tenantId }; _db.HotelCashBoxes.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.Name = dto.Name; existing.IsBank = dto.IsBank; existing.OpeningBalance = dto.OpeningBalance;
        existing.CurrentBalance = dto.CurrentBalance; existing.IsActive = dto.IsActive; existing.Notes = dto.Notes;
        return 1;
    }

    private async Task<int> UpsertHotelExpenseTypeAsync(int tenantId, HotelExpenseTypeSyncDto dto, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.HotelExpenseTypes, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "HotelExpenseType", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudHotelExpenseType { TenantId = tenantId }; _db.HotelExpenseTypes.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.Name = dto.Name; existing.Notes = dto.Notes;
        return 1;
    }

    private async Task<int> UpsertHotelExpenseAsync(int tenantId, HotelExpenseSyncDto dto, int typeId, int? cashBoxId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.HotelExpenses, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "HotelExpense", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudHotelExpense { TenantId = tenantId }; _db.HotelExpenses.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.HotelExpenseTypeId = typeId; existing.ExpenseDate = dto.ExpenseDate; existing.Amount = dto.Amount;
        existing.HotelCashBoxId = cashBoxId; existing.Description = dto.Description; existing.Notes = dto.Notes;
        return 1;
    }

    private async Task<int> UpsertHotelVoucherAsync(int tenantId, HotelVoucherSyncDto dto, int cashBoxId, int? reservationId, int? expenseId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.HotelVouchers, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "HotelVoucher", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudHotelVoucher { TenantId = tenantId }; _db.HotelVouchers.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.VoucherNumber = dto.VoucherNumber; existing.VoucherDate = dto.VoucherDate; existing.Type = dto.Type;
        existing.Amount = dto.Amount; existing.HotelCashBoxId = cashBoxId; existing.ReservationId = reservationId;
        existing.HotelExpenseId = expenseId; existing.Description = dto.Description; existing.Notes = dto.Notes;
        return 1;
    }

    private async Task<int> UpsertHotelRatePlanAsync(int tenantId, HotelRatePlanSyncDto dto, int roomTypeId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.HotelRatePlans, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "HotelRatePlan", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudHotelRatePlan { TenantId = tenantId }; _db.HotelRatePlans.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.Name = dto.Name; existing.RoomTypeId = roomTypeId; existing.BasePrice = dto.BasePrice;
        existing.IsActive = dto.IsActive; existing.Notes = dto.Notes;
        return 1;
    }

    private async Task<int> UpsertHotelRatePlanSeasonAsync(int tenantId, HotelRatePlanSeasonSyncDto dto, int planId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.HotelRatePlanSeasons, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "HotelRatePlanSeason", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudHotelRatePlanSeason { TenantId = tenantId }; _db.HotelRatePlanSeasons.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.RatePlanId = planId; existing.Name = dto.Name; existing.StartDate = dto.StartDate;
        existing.EndDate = dto.EndDate; existing.PricePerNight = dto.PricePerNight;
        return 1;
    }

    private async Task<int> UpsertHotelHousekeepingTaskAsync(int tenantId, HotelHousekeepingTaskSyncDto dto, int roomId, SyncPushResponse response, CancellationToken ct)
    {
        var existing = await FindBySyncIdAsync(_db.HotelHousekeepingTasks, tenantId, dto.SyncId, ct);
        if (ShouldReject(existing, dto)) { AddConflict(response, "HotelHousekeepingTask", dto.SyncId, "Server version is newer"); return 0; }
        if (existing is null) { existing = new CloudHotelHousekeepingTask { TenantId = tenantId }; _db.HotelHousekeepingTasks.Add(existing); }
        if (!TryApplyAudit(existing, dto, GetEntityTypeName(existing), response)) return 0;
        existing.RoomId = roomId; existing.Status = dto.Status; existing.AssignedTo = dto.AssignedTo;
        existing.StartedAt = dto.StartedAt; existing.CompletedAt = dto.CompletedAt; existing.Notes = dto.Notes;
        return 1;
    }
}
