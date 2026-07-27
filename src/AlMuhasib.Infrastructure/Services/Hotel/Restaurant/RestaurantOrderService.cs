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
        return await GenerateOrderNumberInternalAsync(db, ct);
    }

    public async Task<RestaurantOrder> CreateOrderAsync(RestaurantOrderType orderType, int? tableId, int? reservationId, int? roomId, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);

        if (orderType == RestaurantOrderType.DineIn)
        {
            if (!tableId.HasValue)
                throw new InvalidOperationException("اختر طاولة للصالة");

            var table = await db.RestaurantTables.FirstOrDefaultAsync(t => t.Id == tableId.Value, ct)
                ?? throw new InvalidOperationException("الطاولة غير موجودة");

            var openOnTable = await db.RestaurantOrders
                .FirstOrDefaultAsync(o => o.RestaurantTableId == tableId.Value
                    && (o.Status == RestaurantOrderStatus.Open || o.Status == RestaurantOrderStatus.Draft), ct);
            if (openOnTable is not null)
                throw new InvalidOperationException("الطاولة لديها طلب مفتوح — قم باستئنافه");

            if (table.Status == RestaurantTableStatus.Occupied)
                throw new InvalidOperationException("الطاولة مشغولة");

            table.Status = RestaurantTableStatus.Occupied;
        }

        if (orderType == RestaurantOrderType.RoomService && !reservationId.HasValue)
            throw new InvalidOperationException("اختر غرفة لخدمة الغرف");

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
            OrderNumber = await GenerateOrderNumberInternalAsync(db, ct),
            OrderType = orderType,
            Status = RestaurantOrderStatus.Open,
            RestaurantTableId = orderType == RestaurantOrderType.DineIn ? tableId : null,
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

    public async Task<RestaurantOrder?> GetOpenOrderByTableAsync(int tableId, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.RestaurantOrders
            .Include(o => o.Lines)
            .Include(o => o.Payments)
            .Include(o => o.Table)
            .Include(o => o.Room)
            .Include(o => o.Guest)
            .Where(o => o.RestaurantTableId == tableId
                && (o.Status == RestaurantOrderStatus.Open || o.Status == RestaurantOrderStatus.Draft))
            .OrderByDescending(o => o.OrderDate)
            .FirstOrDefaultAsync(ct);
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

    public async Task<RestaurantPaymentResult> CompleteAndPayAsync(int orderId, IReadOnlyList<RestaurantPaymentRequest> payments, bool overrideStock = false, CancellationToken ct = default)
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

        decimal tenderedAmount = 0;
        decimal changeDue = 0;

        if (order.OrderType == RestaurantOrderType.RoomService)
        {
            if (!order.ReservationId.HasValue)
                throw new InvalidOperationException("يجب ربط الطلب بحجز نشط");

            if (payments.Any(p => p.PaymentMethod != RestaurantPaymentMethod.RoomCharge))
                throw new InvalidOperationException("خدمة الغرف تُقيَّد على الغرفة فقط");

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

            await db.RestaurantOrderPayments.AddAsync(new RestaurantOrderPayment
            {
                RestaurantOrderId = order.Id,
                Amount = order.TotalAmount,
                PaymentMethod = RestaurantPaymentMethod.RoomCharge
            }, ct);

            tenderedAmount = order.TotalAmount;
        }
        else
        {
            if (payments.Count == 0)
                throw new InvalidOperationException("أدخل طريقة الدفع");

            var hasCash = payments.Any(p => p.PaymentMethod == RestaurantPaymentMethod.Cash);
            var hasCard = payments.Any(p => p.PaymentMethod == RestaurantPaymentMethod.Card);
            if (hasCash && hasCard)
                throw new InvalidOperationException("ادفع بطريقة واحدة أو استخدم الدفع المختلط لاحقاً");

            foreach (var payment in payments)
            {
                if (payment.PaymentMethod == RestaurantPaymentMethod.RoomCharge)
                    throw new InvalidOperationException("لا يمكن القيد على الغرفة إلا لخدمة الغرف");

                if (payment.PaymentMethod == RestaurantPaymentMethod.Cash)
                {
                    if (payment.Amount + 0.01m < order.TotalAmount)
                        throw new InvalidOperationException("المبلغ المستلم أقل من إجمالي الطلب");

                    tenderedAmount = payment.Amount;
                    changeDue = Math.Max(0, payment.Amount - order.TotalAmount);
                    var recorded = order.TotalAmount;

                    await db.RestaurantOrderPayments.AddAsync(new RestaurantOrderPayment
                    {
                        RestaurantOrderId = order.Id,
                        Amount = recorded,
                        PaymentMethod = RestaurantPaymentMethod.Cash,
                        HotelCashBoxId = payment.HotelCashBoxId
                    }, ct);

                    if (payment.HotelCashBoxId.HasValue && recorded > 0)
                    {
                        var cashBox = await db.HotelCashBoxes.FirstOrDefaultAsync(c => c.Id == payment.HotelCashBoxId.Value, ct)
                            ?? throw new InvalidOperationException("الصندوق غير موجود");
                        cashBox.CurrentBalance += recorded;

                        var voucherNumber = await GetNextVoucherNumberAsync(db, HotelVoucherType.Receipt, ct);
                        await db.HotelVouchers.AddAsync(new HotelVoucher
                        {
                            VoucherNumber = voucherNumber,
                            Type = HotelVoucherType.Receipt,
                            VoucherDate = DateTime.Today,
                            Amount = recorded,
                            HotelCashBoxId = payment.HotelCashBoxId.Value,
                            ReservationId = order.ReservationId,
                            Description = $"مطعم - طلب {order.OrderNumber}",
                            Notes = changeDue > 0 ? $"باقي للعميل: {changeDue:N0}" : order.Notes
                        }, ct);
                    }
                }
                else if (payment.PaymentMethod == RestaurantPaymentMethod.Card)
                {
                    if (Math.Abs(payment.Amount - order.TotalAmount) > 0.01m)
                        throw new InvalidOperationException("الدفع بالبطاقة يجب أن يطابق إجمالي الطلب");

                    tenderedAmount = payment.Amount;

                    await db.RestaurantOrderPayments.AddAsync(new RestaurantOrderPayment
                    {
                        RestaurantOrderId = order.Id,
                        Amount = order.TotalAmount,
                        PaymentMethod = RestaurantPaymentMethod.Card,
                        HotelCashBoxId = payment.HotelCashBoxId
                    }, ct);

                    if (payment.HotelCashBoxId.HasValue && order.TotalAmount > 0)
                    {
                        var cashBox = await db.HotelCashBoxes.FirstOrDefaultAsync(c => c.Id == payment.HotelCashBoxId.Value, ct)
                            ?? throw new InvalidOperationException("الصندوق غير موجود");
                        cashBox.CurrentBalance += order.TotalAmount;

                        var voucherNumber = await GetNextVoucherNumberAsync(db, HotelVoucherType.Receipt, ct);
                        await db.HotelVouchers.AddAsync(new HotelVoucher
                        {
                            VoucherNumber = voucherNumber,
                            Type = HotelVoucherType.Receipt,
                            VoucherDate = DateTime.Today,
                            Amount = order.TotalAmount,
                            HotelCashBoxId = payment.HotelCashBoxId.Value,
                            ReservationId = order.ReservationId,
                            Description = $"مطعم (بطاقة) - طلب {order.OrderNumber}",
                            Notes = order.Notes
                        }, ct);
                    }
                }
                else
                {
                    if (Math.Abs(payment.Amount - order.TotalAmount) > 0.01m)
                        throw new InvalidOperationException("مجموع الدفعات لا يطابق إجمالي الطلب");

                    tenderedAmount = payment.Amount;
                    await db.RestaurantOrderPayments.AddAsync(new RestaurantOrderPayment
                    {
                        RestaurantOrderId = order.Id,
                        Amount = order.TotalAmount,
                        PaymentMethod = payment.PaymentMethod,
                        HotelCashBoxId = payment.HotelCashBoxId
                    }, ct);
                }
            }

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

        return new RestaurantPaymentResult
        {
            Order = order,
            TenderedAmount = tenderedAmount,
            ChangeDue = changeDue
        };
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

    private static async Task<string> GenerateOrderNumberInternalAsync(HotelDbContext db, CancellationToken ct)
    {
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
