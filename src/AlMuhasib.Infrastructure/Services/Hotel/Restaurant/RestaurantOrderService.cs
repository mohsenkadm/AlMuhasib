using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Core.Entities.Hotel.Restaurant;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Core.Models.Hotel;
using AlMuhasib.Infrastructure.Data.Hotel;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Hotel.Restaurant;

public sealed class RestaurantOrderService : IRestaurantOrderService
{
    private readonly IDbContextFactory<HotelDbContext> _contextFactory;

    public RestaurantOrderService(IDbContextFactory<HotelDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<string> GenerateOrderNumberAsync(CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var prefix = $"RST-{DateTime.Today:yyyyMMdd}";
        var last = await db.RestaurantOrders.IgnoreQueryFilters()
            .Where(o => o.OrderNumber.StartsWith(prefix))
            .OrderByDescending(o => o.Id)
            .Select(o => o.OrderNumber)
            .FirstOrDefaultAsync(ct);

        var next = 1;
        if (last is not null)
        {
            var parts = last.Split('-');
            if (parts.Length >= 3 && int.TryParse(parts[^1], out var n))
                next = n + 1;
        }

        return $"{prefix}-{next:D4}";
    }

    public async Task<RestaurantOrder> CreateOrderAsync(RestaurantOrderType orderType, int? tableId, int? reservationId, int? roomId, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);

        if (orderType == RestaurantOrderType.DineIn && tableId.HasValue)
        {
            var table = await db.RestaurantTables.FirstOrDefaultAsync(t => t.Id == tableId.Value, ct)
                ?? throw new InvalidOperationException("الطاولة غير موجودة");
            if (table.Status == RestaurantTableStatus.Occupied)
                throw new InvalidOperationException("الطاولة مشغولة");
            table.Status = RestaurantTableStatus.Occupied;
        }

        int? guestId = null;
        if (reservationId.HasValue)
        {
            var reservation = await db.Reservations.Include(r => r.Guest).FirstOrDefaultAsync(r => r.Id == reservationId.Value, ct)
                ?? throw new InvalidOperationException("الحجز غير موجود");
            guestId = reservation.GuestId;
            roomId ??= reservation.RoomId;
        }

        var order = new RestaurantOrder
        {
            OrderNumber = await GenerateOrderNumberAsync(ct),
            OrderType = orderType,
            Status = RestaurantOrderStatus.Open,
            RestaurantTableId = tableId,
            ReservationId = reservationId,
            RoomId = roomId,
            GuestId = guestId,
            OrderDate = DateTime.Now
        };

        await db.RestaurantOrders.AddAsync(order, ct);
        await db.SaveChangesAsync(ct);
        return order;
    }

    public async Task<RestaurantOrder?> GetOrderByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.RestaurantOrders
            .Include(o => o.Lines)
            .Include(o => o.Payments)
            .Include(o => o.Table)
            .Include(o => o.Room)
            .Include(o => o.Guest)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task<IReadOnlyList<RestaurantOrder>> GetOpenOrdersAsync(CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.RestaurantOrders
            .Include(o => o.Lines)
            .Include(o => o.Table)
            .Where(o => o.Status == RestaurantOrderStatus.Open || o.Status == RestaurantOrderStatus.Draft)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<RestaurantOrder>> GetKitchenOrdersAsync(CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.RestaurantOrders
            .Include(o => o.Lines)
            .Include(o => o.Table)
            .Where(o => o.Status == RestaurantOrderStatus.Open
                && o.KitchenStatus != RestaurantKitchenStatus.Served)
            .OrderBy(o => o.OrderDate)
            .ToListAsync(ct);
    }

    public async Task AddLineAsync(int orderId, int menuItemId, decimal quantity, CancellationToken ct = default)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("الكمية يجب أن تكون أكبر من صفر");

        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var order = await db.RestaurantOrders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new InvalidOperationException("الطلب غير موجود");

        if (order.Status != RestaurantOrderStatus.Open && order.Status != RestaurantOrderStatus.Draft)
            throw new InvalidOperationException("لا يمكن تعديل طلب مغلق");

        var menuItem = await db.RestaurantMenuItems.FirstOrDefaultAsync(m => m.Id == menuItemId, ct)
            ?? throw new InvalidOperationException("الصنف غير موجود");

        var existing = order.Lines.FirstOrDefault(l => l.RestaurantMenuItemId == menuItemId);
        if (existing is not null)
        {
            existing.Quantity += quantity;
            existing.LineTotal = existing.Quantity * existing.UnitPrice - existing.DiscountAmount;
        }
        else
        {
            await db.RestaurantOrderLines.AddAsync(new RestaurantOrderLine
            {
                RestaurantOrderId = orderId,
                RestaurantMenuItemId = menuItemId,
                ItemName = menuItem.Name,
                Quantity = quantity,
                UnitPrice = menuItem.SalePrice,
                LineTotal = quantity * menuItem.SalePrice
            }, ct);
        }

        RecalculateOrderTotals(order);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateLineQuantityAsync(int lineId, decimal quantity, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var line = await db.RestaurantOrderLines.Include(l => l.Order).ThenInclude(o => o.Lines)
            .FirstOrDefaultAsync(l => l.Id == lineId, ct)
            ?? throw new InvalidOperationException("السطر غير موجود");

        if (quantity <= 0)
        {
            db.RestaurantOrderLines.Remove(line);
        }
        else
        {
            line.Quantity = quantity;
            line.LineTotal = quantity * line.UnitPrice - line.DiscountAmount;
        }

        RecalculateOrderTotals(line.Order);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveLineAsync(int lineId, CancellationToken ct = default)
    {
        await UpdateLineQuantityAsync(lineId, 0, ct);
    }

    public async Task SetOrderDiscountAsync(int orderId, decimal discountAmount, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var order = await db.RestaurantOrders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new InvalidOperationException("الطلب غير موجود");
        order.DiscountAmount = discountAmount;
        RecalculateOrderTotals(order);
        await db.SaveChangesAsync(ct);
    }

    public async Task<RestaurantOrder> CompleteAndPayAsync(int orderId, IReadOnlyList<RestaurantPaymentRequest> payments, bool overrideStock = false, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var order = await db.RestaurantOrders
            .Include(o => o.Lines)
            .Include(o => o.Table)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new InvalidOperationException("الطلب غير موجود");

        if (order.Status is RestaurantOrderStatus.Paid or RestaurantOrderStatus.PostedToRoom or RestaurantOrderStatus.Cancelled)
            throw new InvalidOperationException("الطلب مغلق بالفعل");

        if (order.Lines.Count == 0)
            throw new InvalidOperationException("الطلب فارغ");

        RecalculateOrderTotals(order);
        var cogs = await CalculateAndDeductStockAsync(db, order, overrideStock, ct);
        order.CogsAmount = cogs;
        order.GrossProfit = order.TotalAmount - cogs;

        var paymentTotal = payments.Sum(p => p.Amount);
        if (order.OrderType != RestaurantOrderType.RoomService && Math.Abs(paymentTotal - order.TotalAmount) > 0.01m)
            throw new InvalidOperationException("مجموع الدفعات لا يطابق إجمالي الطلب");

        foreach (var payment in payments)
        {
            await db.RestaurantOrderPayments.AddAsync(new RestaurantOrderPayment
            {
                RestaurantOrderId = order.Id,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                HotelCashBoxId = payment.HotelCashBoxId
            }, ct);

            if (payment.PaymentMethod is RestaurantPaymentMethod.Cash or RestaurantPaymentMethod.Card
                && payment.HotelCashBoxId.HasValue && payment.Amount > 0)
            {
                var cashBox = await db.HotelCashBoxes.FirstOrDefaultAsync(c => c.Id == payment.HotelCashBoxId.Value, ct)
                    ?? throw new InvalidOperationException("الصندوق غير موجود");
                cashBox.CurrentBalance += payment.Amount;

                var voucherNumber = await GetNextVoucherNumberAsync(db, HotelVoucherType.Receipt, ct);
                await db.HotelVouchers.AddAsync(new HotelVoucher
                {
                    VoucherNumber = voucherNumber,
                    Type = HotelVoucherType.Receipt,
                    VoucherDate = DateTime.Today,
                    Amount = payment.Amount,
                    HotelCashBoxId = payment.HotelCashBoxId.Value,
                    ReservationId = order.ReservationId,
                    Description = $"مطعم - طلب {order.OrderNumber}",
                    Notes = order.Notes
                }, ct);
            }
        }

        if (order.OrderType == RestaurantOrderType.RoomService)
        {
            if (!order.ReservationId.HasValue)
                throw new InvalidOperationException("يجب ربط الطلب بحجز نشط");

            var charge = new ReservationCharge
            {
                ReservationId = order.ReservationId.Value,
                Description = $"مطعم - طلب {order.OrderNumber}",
                Amount = order.TotalAmount,
                ChargeDate = DateTime.Today,
                Notes = order.Notes
            };
            await db.ReservationCharges.AddAsync(charge, ct);
            await db.SaveChangesAsync(ct);
            order.ReservationChargeId = charge.Id;
            order.Status = RestaurantOrderStatus.PostedToRoom;
        }
        else
        {
            order.Status = RestaurantOrderStatus.Paid;
        }

        order.PaidAt = DateTime.Now;
        order.KitchenStatus = RestaurantKitchenStatus.Served;

        if (order.RestaurantTableId.HasValue)
        {
            var table = await db.RestaurantTables.FirstOrDefaultAsync(t => t.Id == order.RestaurantTableId.Value, ct);
            if (table is not null)
                table.Status = RestaurantTableStatus.Available;
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return order;
    }

    public async Task CancelOrderAsync(int orderId, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var order = await db.RestaurantOrders.FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new InvalidOperationException("الطلب غير موجود");

        order.Status = RestaurantOrderStatus.Cancelled;
        if (order.RestaurantTableId.HasValue)
        {
            var table = await db.RestaurantTables.FirstOrDefaultAsync(t => t.Id == order.RestaurantTableId.Value, ct);
            if (table is not null)
                table.Status = RestaurantTableStatus.Available;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateKitchenStatusAsync(int orderId, RestaurantKitchenStatus status, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var order = await db.RestaurantOrders.FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new InvalidOperationException("الطلب غير موجود");
        order.KitchenStatus = status;
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ActiveRoomForService>> GetActiveRoomsForServiceAsync(CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.Reservations
            .Include(r => r.Room)
            .Include(r => r.Guest)
            .Where(r => r.Status == ReservationStatus.CheckedIn && r.RoomId != null)
            .Select(r => new ActiveRoomForService
            {
                RoomId = r.RoomId!.Value,
                RoomNumber = r.Room!.RoomNumber,
                ReservationId = r.Id,
                GuestName = r.Guest.FullName,
                GuestId = r.GuestId
            })
            .OrderBy(r => r.RoomNumber)
            .ToListAsync(ct);
    }

    private static void RecalculateOrderTotals(RestaurantOrder order)
    {
        order.SubTotal = order.Lines.Sum(l => l.LineTotal + l.DiscountAmount);
        order.TotalAmount = order.SubTotal - order.DiscountAmount;
    }

    private static async Task<decimal> CalculateAndDeductStockAsync(HotelDbContext db, RestaurantOrder order, bool overrideStock, CancellationToken ct)
    {
        decimal totalCogs = 0;

        foreach (var line in order.Lines)
        {
            var menuItem = await db.RestaurantMenuItems.FirstOrDefaultAsync(m => m.Id == line.RestaurantMenuItemId, ct);
            if (menuItem?.RecipeId is null)
                continue;

            var recipe = await db.RestaurantRecipes
                .Include(r => r.Lines).ThenInclude(rl => rl.Ingredient).ThenInclude(i => i.Stock)
                .FirstOrDefaultAsync(r => r.Id == menuItem.RecipeId.Value, ct);

            if (recipe is null)
                continue;

            decimal lineCogs = 0;
            foreach (var recipeLine in recipe.Lines)
            {
                var needed = recipeLine.Quantity * line.Quantity;
                var stock = await db.RestaurantIngredientStocks
                    .FirstOrDefaultAsync(s => s.RestaurantIngredientId == recipeLine.RestaurantIngredientId, ct);

                if (stock is null || stock.Quantity < needed)
                {
                    if (!overrideStock)
                    {
                        var ingName = recipeLine.Ingredient?.Name ?? "مكون";
                        throw new InvalidOperationException($"رصيد {ingName} غير كافٍ");
                    }
                }

                if (stock is not null)
                {
                    stock.Quantity -= needed;
                    await db.RestaurantStockMovements.AddAsync(new RestaurantStockMovement
                    {
                        RestaurantIngredientId = recipeLine.RestaurantIngredientId,
                        MovementType = RestaurantStockMovementType.Sale,
                        Quantity = needed,
                        UnitCost = recipeLine.Ingredient.AverageCost,
                        RestaurantOrderId = order.Id,
                        MovementDate = DateTime.Now,
                        Notes = $"طلب {order.OrderNumber} - {line.ItemName}"
                    }, ct);
                }

                lineCogs += needed * recipeLine.Ingredient.AverageCost;
            }

            line.CogsAmount = lineCogs;
            totalCogs += lineCogs;
        }

        return totalCogs;
    }

    private static async Task<string> GetNextVoucherNumberAsync(HotelDbContext db, HotelVoucherType type, CancellationToken ct)
    {
        var prefix = type == HotelVoucherType.Receipt ? "HRC" : "HPY";
        var lastVoucher = await db.HotelVouchers.IgnoreQueryFilters()
            .Where(v => v.Type == type && v.VoucherNumber.StartsWith(prefix + "-"))
            .OrderByDescending(v => v.Id)
            .Select(v => v.VoucherNumber)
            .FirstOrDefaultAsync(ct);

        var nextNum = 1;
        if (lastVoucher is not null)
        {
            var parts = lastVoucher.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[1], out var lastNum))
                nextNum = lastNum + 1;
        }

        return $"{prefix}-{nextNum:D4}";
    }
}
