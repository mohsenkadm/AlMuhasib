using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers.Hotel;

[ApiController]
[Route("api/hotel/restaurant")]
[Authorize(Policy = "Tenant")]
public sealed class HotelRestaurantController : HotelApiControllerBase
{
    private const string KitchenPurchaseExpenseType = "مشتريات مطعم";

    public HotelRestaurantController(CloudDbContext db, ITenantContext tenantContext) : base(db, tenantContext) { }

    [HttpGet("menu")]
    public async Task<ActionResult<RestaurantMenuDto>> GetMenu(CancellationToken ct)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;

        var categories = await Db.RestaurantMenuCategories.AsNoTracking()
            .Where(c => c.TenantId == TenantId && !c.IsDeleted && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(ct);

        var items = await Db.RestaurantMenuItems.AsNoTracking()
            .Where(m => m.TenantId == TenantId && !m.IsDeleted && m.IsActive)
            .OrderBy(m => m.SortOrder)
            .ToListAsync(ct);

        return Ok(new RestaurantMenuDto
        {
            Categories = categories.Select(c => new RestaurantCategoryDto
            {
                SyncId = c.SyncId,
                Name = c.Name,
                ColorHex = c.ColorHex,
                SortOrder = c.SortOrder
            }).ToList(),
            Items = items.Select(m => new RestaurantMenuItemDto
            {
                SyncId = m.SyncId,
                CategorySyncId = categories.FirstOrDefault(c => c.Id == m.RestaurantMenuCategoryId)?.SyncId ?? Guid.Empty,
                Name = m.Name,
                SalePrice = m.SalePrice,
                Barcode = m.Barcode
            }).ToList()
        });
    }

    [HttpGet("tables")]
    public async Task<ActionResult<IReadOnlyList<RestaurantTableDto>>> GetTables(CancellationToken ct)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;

        var tables = await Db.RestaurantTables.AsNoTracking()
            .Where(t => t.TenantId == TenantId && !t.IsDeleted && t.IsActive)
            .OrderBy(t => t.SortOrder)
            .ToListAsync(ct);

        return Ok(tables.Select(t => new RestaurantTableDto
        {
            SyncId = t.SyncId,
            TableNumber = t.TableNumber,
            Capacity = t.Capacity,
            Status = t.Status.ToString()
        }).ToList());
    }

    [HttpGet("rooms/active")]
    public async Task<ActionResult<IReadOnlyList<ActiveRoomDto>>> GetActiveRooms(CancellationToken ct)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;

        var reservations = await Db.HotelReservations.AsNoTracking()
            .Where(r => r.TenantId == TenantId && !r.IsDeleted && r.Status == ReservationStatus.CheckedIn && r.RoomId != null)
            .Include(r => r.Guest)
            .Include(r => r.Room)
            .ToListAsync(ct);

        return Ok(reservations.Select(r => new ActiveRoomDto
        {
            RoomSyncId = r.Room!.SyncId,
            RoomNumber = r.Room.RoomNumber,
            ReservationSyncId = r.SyncId,
            GuestName = r.Guest.FullName,
            GuestSyncId = r.Guest.SyncId
        }).ToList());
    }

    [HttpGet("orders/open")]
    public async Task<ActionResult<IReadOnlyList<RestaurantOrderDto>>> GetOpenOrders(CancellationToken ct)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;

        var orders = await Db.RestaurantOrders.AsNoTracking()
            .Where(o => o.TenantId == TenantId && !o.IsDeleted
                && (o.Status == RestaurantOrderStatus.Open || o.Status == RestaurantOrderStatus.Draft))
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(ct);

        var tableIds = orders.Where(o => o.RestaurantTableId.HasValue).Select(o => o.RestaurantTableId!.Value).Distinct().ToList();
        var tables = await Db.RestaurantTables.AsNoTracking()
            .Where(t => t.TenantId == TenantId && tableIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, ct);

        return Ok(orders.Select(o => MapOrder(o, o.RestaurantTableId is int tid && tables.TryGetValue(tid, out var t) ? t.SyncId : null, o.RestaurantTableId is int tid2 && tables.TryGetValue(tid2, out var t2) ? t2.TableNumber : null)).ToList());
    }

    [HttpPost("orders")]
    public async Task<ActionResult<RestaurantOrderDto>> CreateOrder([FromBody] CreateRestaurantOrderRequest request, CancellationToken ct)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;

        if (request.OrderType == RestaurantOrderType.DineIn && !request.TableSyncId.HasValue)
            return BadRequest("اختر طاولة للصالة");

        if (request.OrderType == RestaurantOrderType.RoomService && !request.ReservationSyncId.HasValue)
            return BadRequest("اختر غرفة لخدمة الغرف");

        CloudRestaurantTable? table = null;
        if (request.TableSyncId.HasValue)
        {
            table = await Db.RestaurantTables.FirstOrDefaultAsync(t => t.TenantId == TenantId && t.SyncId == request.TableSyncId.Value, ct);
            if (table is null) return BadRequest("الطاولة غير موجودة");

            var openOnTable = await Db.RestaurantOrders
                .FirstOrDefaultAsync(o => o.TenantId == TenantId && o.RestaurantTableId == table.Id
                    && (o.Status == RestaurantOrderStatus.Open || o.Status == RestaurantOrderStatus.Draft), ct);
            if (openOnTable is not null)
                return Ok(MapOrder(openOnTable, table.SyncId, table.TableNumber));

            if (table.Status == RestaurantTableStatus.Occupied)
                return BadRequest("الطاولة مشغولة");

            table.Status = RestaurantTableStatus.Occupied;
        }

        var order = new CloudRestaurantOrder
        {
            TenantId = TenantId,
            OrderNumber = await GenerateOrderNumberAsync(ct),
            OrderType = request.OrderType,
            Status = RestaurantOrderStatus.Open,
            KitchenStatus = RestaurantKitchenStatus.Pending,
            OrderDate = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name ?? "Mobile",
            RestaurantTableId = table?.Id
        };

        if (request.ReservationSyncId.HasValue)
        {
            var reservation = await Db.HotelReservations.FirstOrDefaultAsync(r => r.TenantId == TenantId && r.SyncId == request.ReservationSyncId.Value, ct);
            if (reservation is null) return BadRequest("الحجز غير موجود");
            order.ReservationId = reservation.Id;
            order.RoomId = reservation.RoomId;
            order.GuestId = reservation.GuestId;
        }

        Db.RestaurantOrders.Add(order);
        await Db.SaveChangesAsync(ct);

        return Ok(MapOrder(order, table?.SyncId, table?.TableNumber));
    }

    [HttpPost("orders/{syncId:guid}/lines")]
    public async Task<ActionResult<RestaurantOrderDto>> AddLine(Guid syncId, [FromBody] AddOrderLineRequest request, CancellationToken ct)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;

        var order = await Db.RestaurantOrders
            .FirstOrDefaultAsync(o => o.TenantId == TenantId && o.SyncId == syncId, ct);
        if (order is null) return NotFound();

        if (order.Status is not (RestaurantOrderStatus.Open or RestaurantOrderStatus.Draft))
            return BadRequest("لا يمكن تعديل طلب مغلق");

        var item = await Db.RestaurantMenuItems.AsNoTracking()
            .FirstOrDefaultAsync(m => m.TenantId == TenantId && m.SyncId == request.MenuItemSyncId, ct);
        if (item is null) return BadRequest("Menu item not found");

        if (request.Quantity <= 0)
            return BadRequest("الكمية يجب أن تكون أكبر من صفر");

        var existing = await Db.RestaurantOrderLines
            .FirstOrDefaultAsync(l => l.RestaurantOrderId == order.Id && !l.IsDeleted && l.RestaurantMenuItemId == item.Id, ct);

        if (existing is not null)
        {
            existing.Quantity += request.Quantity;
            existing.LineTotal = existing.Quantity * existing.UnitPrice - existing.DiscountAmount;
        }
        else
        {
            Db.RestaurantOrderLines.Add(new CloudRestaurantOrderLine
            {
                TenantId = TenantId,
                RestaurantOrderId = order.Id,
                RestaurantMenuItemId = item.Id,
                ItemName = item.Name,
                Quantity = request.Quantity,
                UnitPrice = item.SalePrice,
                LineTotal = request.Quantity * item.SalePrice,
                CreatedBy = User.Identity?.Name ?? "Mobile"
            });
        }

        await RecalculateOrderAsync(order.Id, ct);
        await Db.SaveChangesAsync(ct);

        order = await Db.RestaurantOrders.FirstAsync(o => o.Id == order.Id, ct);
        return Ok(MapOrder(order));
    }

    [HttpPost("orders/{syncId:guid}/pay")]
    public async Task<ActionResult<RestaurantPayResultDto>> PayOrder(Guid syncId, [FromBody] PayRestaurantOrderRequest request, CancellationToken ct)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;

        await using var tx = await Db.Database.BeginTransactionAsync(ct);

        var order = await Db.RestaurantOrders.FirstOrDefaultAsync(o => o.TenantId == TenantId && o.SyncId == syncId, ct);
        if (order is null) return NotFound();

        if (order.Status is RestaurantOrderStatus.Paid or RestaurantOrderStatus.PostedToRoom or RestaurantOrderStatus.Cancelled)
            return BadRequest("الطلب مغلق بالفعل");

        var lines = await Db.RestaurantOrderLines
            .Where(l => l.RestaurantOrderId == order.Id && !l.IsDeleted)
            .ToListAsync(ct);
        if (lines.Count == 0)
            return BadRequest("الطلب فارغ");

        await RecalculateOrderAsync(order.Id, ct);
        order = await Db.RestaurantOrders.FirstAsync(o => o.Id == order.Id, ct);

        decimal cogs;
        try
        {
            cogs = await DeductStockAndComputeCogsAsync(order, lines, request.OverrideStock, ct);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        order.CogsAmount = cogs;
        order.GrossProfit = order.TotalAmount - cogs;

        decimal tendered = request.Amount;
        decimal changeDue = 0;
        var createdBy = User.Identity?.Name ?? "Mobile";

        if (order.OrderType == RestaurantOrderType.RoomService)
        {
            if (!order.ReservationId.HasValue)
                return BadRequest("يجب ربط الطلب بحجز نشط");

            Db.HotelReservationCharges.Add(new CloudHotelReservationCharge
            {
                TenantId = TenantId,
                ReservationId = order.ReservationId.Value,
                Description = $"مطعم - طلب {order.OrderNumber}",
                Amount = order.TotalAmount,
                ChargeDate = DateTime.UtcNow,
                CreatedBy = createdBy
            });

            Db.RestaurantOrderPayments.Add(new CloudRestaurantOrderPayment
            {
                TenantId = TenantId,
                RestaurantOrderId = order.Id,
                Amount = order.TotalAmount,
                PaymentMethod = RestaurantPaymentMethod.RoomCharge,
                CreatedBy = createdBy
            });

            order.Status = RestaurantOrderStatus.PostedToRoom;
            tendered = order.TotalAmount;
        }
        else
        {
            var method = request.PaymentMethod;
            if (method == RestaurantPaymentMethod.Cash)
            {
                if (request.Amount + 0.01m < order.TotalAmount)
                    return BadRequest("المبلغ المستلم أقل من إجمالي الطلب");
                changeDue = Math.Max(0, request.Amount - order.TotalAmount);
            }
            else if (Math.Abs(request.Amount - order.TotalAmount) > 0.01m && request.Amount > 0)
            {
                // Normalize non-cash to exact total if client sent cart total noise
                if (method != RestaurantPaymentMethod.Cash)
                    tendered = order.TotalAmount;
            }

            var recorded = order.TotalAmount;
            int? cashBoxId = null;

            if (request.CashBoxSyncId.HasValue)
            {
                var cashBox = await Db.HotelCashBoxes.FirstOrDefaultAsync(c => c.TenantId == TenantId && c.SyncId == request.CashBoxSyncId.Value, ct);
                if (cashBox is null) return BadRequest("الصندوق غير موجود");
                cashBoxId = cashBox.Id;
                cashBox.CurrentBalance += recorded;

                Db.HotelVouchers.Add(new CloudHotelVoucher
                {
                    TenantId = TenantId,
                    VoucherNumber = await GetNextVoucherNumberAsync(ct),
                    Type = HotelVoucherType.Receipt,
                    VoucherDate = DateTime.UtcNow.Date,
                    Amount = recorded,
                    HotelCashBoxId = cashBox.Id,
                    ReservationId = order.ReservationId,
                    Description = $"مطعم - طلب {order.OrderNumber}",
                    Notes = changeDue > 0 ? $"باقي للعميل: {changeDue:N0}" : order.Notes,
                    CreatedBy = createdBy
                });
            }

            Db.RestaurantOrderPayments.Add(new CloudRestaurantOrderPayment
            {
                TenantId = TenantId,
                RestaurantOrderId = order.Id,
                Amount = recorded,
                PaymentMethod = method == RestaurantPaymentMethod.RoomCharge ? RestaurantPaymentMethod.Cash : method,
                HotelCashBoxId = cashBoxId,
                CreatedBy = createdBy
            });

            order.Status = RestaurantOrderStatus.Paid;
        }

        order.PaidAt = DateTime.UtcNow;
        order.KitchenStatus = RestaurantKitchenStatus.Served;

        if (order.RestaurantTableId.HasValue)
        {
            var table = await Db.RestaurantTables.FirstOrDefaultAsync(t => t.Id == order.RestaurantTableId.Value, ct);
            if (table is not null) table.Status = RestaurantTableStatus.Available;
        }

        await Db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return Ok(new RestaurantPayResultDto
        {
            Order = MapOrder(order),
            TenderedAmount = tendered,
            ChangeDue = changeDue
        });
    }

    [HttpGet("reports/summary")]
    public async Task<ActionResult<RestaurantProfitSummaryDto>> GetSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;

        var (start, end) = ResolveRange(from, to);
        var orders = await LoadClosedOrdersAsync(start, end, ct);

        var revenue = orders.Sum(o => o.TotalAmount);
        var cogs = orders.Sum(o => o.CogsAmount);
        var profit = revenue - cogs;
        var roomOrders = orders.Where(o => o.OrderType == RestaurantOrderType.RoomService).ToList();

        return Ok(new RestaurantProfitSummaryDto
        {
            Revenue = revenue,
            Cogs = cogs,
            GrossProfit = profit,
            MarginPercent = revenue > 0 ? Math.Round(profit / revenue * 100, 1) : 0,
            OrderCount = orders.Count,
            AverageOrderValue = orders.Count > 0 ? Math.Round(revenue / orders.Count, 0) : 0,
            DiscountTotal = orders.Sum(o => o.DiscountAmount),
            RoomServiceRevenue = roomOrders.Sum(o => o.TotalAmount),
            RoomServiceOrderCount = roomOrders.Count
        });
    }

    [HttpGet("reports/channels")]
    public async Task<ActionResult<IReadOnlyList<RestaurantChannelSalesDto>>> GetChannelSales([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;
        var (start, end) = ResolveRange(from, to);
        var orders = await LoadClosedOrdersAsync(start, end, ct);

        return Ok(orders.GroupBy(o => o.OrderType)
            .Select(g => new RestaurantChannelSalesDto
            {
                OrderType = g.Key,
                Label = OrderTypeLabel(g.Key),
                Revenue = g.Sum(o => o.TotalAmount),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.Revenue)
            .ToList());
    }

    [HttpGet("reports/top-items")]
    public async Task<ActionResult<IReadOnlyList<RestaurantTopItemDto>>> GetTopItems([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int limit = 10, CancellationToken ct = default)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;
        var (start, end) = ResolveRange(from, to);
        var orderIds = (await LoadClosedOrdersAsync(start, end, ct)).Select(o => o.Id).ToList();

        var items = await Db.RestaurantOrderLines.AsNoTracking()
            .Where(l => orderIds.Contains(l.RestaurantOrderId) && !l.IsDeleted)
            .GroupBy(l => l.ItemName)
            .Select(g => new RestaurantTopItemDto
            {
                ItemName = g.Key,
                QuantitySold = g.Sum(l => l.Quantity),
                Revenue = g.Sum(l => l.LineTotal)
            })
            .OrderByDescending(x => x.Revenue)
            .Take(Math.Clamp(limit, 1, 50))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("reports/overview")]
    public async Task<ActionResult<RestaurantFinancialOverviewDto>> GetOverview([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;
        var (start, end) = ResolveRange(from, to);
        var orders = await LoadClosedOrdersAsync(start, end, ct);
        var revenue = orders.Sum(o => o.TotalAmount);
        var cogs = orders.Sum(o => o.CogsAmount);
        var profit = revenue - cogs;

        var kitchenPurchases = await Db.HotelExpenses.AsNoTracking()
            .Where(e => e.TenantId == TenantId && !e.IsDeleted
                && e.ExpenseDate >= start && e.ExpenseDate < end
                && e.ExpenseType.Name == KitchenPurchaseExpenseType)
            .SumAsync(e => (decimal?)e.Amount, ct) ?? 0;

        return Ok(new RestaurantFinancialOverviewDto
        {
            RestaurantRevenue = revenue,
            RestaurantCogs = cogs,
            RestaurantGrossProfit = profit,
            KitchenPurchases = kitchenPurchases,
            NetOperating = profit - kitchenPurchases
        });
    }

    [HttpGet("inventory/alerts")]
    public async Task<ActionResult<IReadOnlyList<RestaurantStockAlertDto>>> GetStockAlerts(CancellationToken ct)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;

        var ingredients = await Db.RestaurantIngredients.AsNoTracking()
            .Where(i => i.TenantId == TenantId && !i.IsDeleted && i.IsActive)
            .ToListAsync(ct);

        var stocks = await Db.RestaurantIngredientStocks.AsNoTracking()
            .Where(s => s.TenantId == TenantId && !s.IsDeleted)
            .ToListAsync(ct);

        return Ok(ingredients
            .Select(i =>
            {
                var stock = stocks.FirstOrDefault(s => s.RestaurantIngredientId == i.Id);
                return new { i, Qty = stock?.Quantity ?? 0 };
            })
            .Where(x => x.Qty <= x.i.MinQuantity)
            .Select(x => new RestaurantStockAlertDto
            {
                SyncId = x.i.SyncId,
                Name = x.i.Name,
                Quantity = x.Qty,
                MinQuantity = x.i.MinQuantity,
                Unit = x.i.Unit
            }).ToList());
    }

    [HttpGet("kitchen/orders")]
    public async Task<ActionResult<IReadOnlyList<RestaurantOrderDto>>> GetKitchenOrders(CancellationToken ct)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;

        var orders = await Db.RestaurantOrders.AsNoTracking()
            .Where(o => o.TenantId == TenantId && !o.IsDeleted
                && o.Status == RestaurantOrderStatus.Open
                && o.KitchenStatus != RestaurantKitchenStatus.Served)
            .OrderBy(o => o.OrderDate)
            .ToListAsync(ct);

        return Ok(orders.Select(o => MapOrder(o)).ToList());
    }

    [HttpPatch("orders/{syncId:guid}/kitchen-status")]
    public async Task<ActionResult> UpdateKitchenStatus(Guid syncId, [FromBody] UpdateKitchenStatusRequest request, CancellationToken ct)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;

        var order = await Db.RestaurantOrders.FirstOrDefaultAsync(o => o.TenantId == TenantId && o.SyncId == syncId, ct);
        if (order is null) return NotFound();
        order.KitchenStatus = request.Status;
        await Db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<decimal> DeductStockAndComputeCogsAsync(
        CloudRestaurantOrder order,
        List<CloudRestaurantOrderLine> lines,
        bool overrideStock,
        CancellationToken ct)
    {
        decimal totalCogs = 0;
        var createdBy = User.Identity?.Name ?? "Mobile";

        foreach (var line in lines)
        {
            var menuItem = await Db.RestaurantMenuItems.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == line.RestaurantMenuItemId, ct);
            if (menuItem?.RecipeId is null)
                continue;

            var recipeLines = await Db.RestaurantRecipeLines
                .Include(rl => rl.Ingredient)
                .Where(rl => rl.RestaurantRecipeId == menuItem.RecipeId.Value && !rl.IsDeleted)
                .ToListAsync(ct);

            decimal lineCogs = 0;
            foreach (var recipeLine in recipeLines)
            {
                var needed = recipeLine.Quantity * line.Quantity;
                var stock = await Db.RestaurantIngredientStocks
                    .FirstOrDefaultAsync(s => s.TenantId == TenantId && s.RestaurantIngredientId == recipeLine.RestaurantIngredientId, ct);

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
                    Db.RestaurantStockMovements.Add(new CloudRestaurantStockMovement
                    {
                        TenantId = TenantId,
                        RestaurantIngredientId = recipeLine.RestaurantIngredientId,
                        MovementType = RestaurantStockMovementType.Sale,
                        Quantity = needed,
                        UnitCost = recipeLine.Ingredient.AverageCost,
                        RestaurantOrderId = order.Id,
                        MovementDate = DateTime.UtcNow,
                        Notes = $"طلب {order.OrderNumber} - {line.ItemName}",
                        CreatedBy = createdBy
                    });
                }

                lineCogs += needed * recipeLine.Ingredient.AverageCost;
            }

            line.CogsAmount = lineCogs;
            totalCogs += lineCogs;
        }

        return totalCogs;
    }

    private async Task RecalculateOrderAsync(int orderId, CancellationToken ct)
    {
        var lines = await Db.RestaurantOrderLines.Where(l => l.RestaurantOrderId == orderId && !l.IsDeleted).ToListAsync(ct);
        var order = await Db.RestaurantOrders.FirstAsync(o => o.Id == orderId, ct);
        order.SubTotal = lines.Sum(l => l.LineTotal + l.DiscountAmount);
        order.TotalAmount = order.SubTotal - order.DiscountAmount;
        order.GrossProfit = order.TotalAmount - order.CogsAmount;
    }

    private async Task<string> GenerateOrderNumberAsync(CancellationToken ct)
    {
        var prefix = $"RST-{DateTime.UtcNow:yyyyMMdd}";
        var last = await Db.RestaurantOrders.IgnoreQueryFilters()
            .Where(o => o.TenantId == TenantId && o.OrderNumber.StartsWith(prefix))
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

    private async Task<string> GetNextVoucherNumberAsync(CancellationToken ct)
    {
        const string prefix = "HRC-";
        var last = await Db.HotelVouchers.IgnoreQueryFilters()
            .Where(v => v.TenantId == TenantId && v.Type == HotelVoucherType.Receipt && v.VoucherNumber.StartsWith(prefix))
            .OrderByDescending(v => v.Id)
            .Select(v => v.VoucherNumber)
            .FirstOrDefaultAsync(ct);

        var next = 1;
        if (last is not null)
        {
            var parts = last.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[1], out var n))
                next = n + 1;
        }

        return $"{prefix}{next:D4}";
    }

    private async Task<List<CloudRestaurantOrder>> LoadClosedOrdersAsync(DateTime start, DateTime end, CancellationToken ct)
    {
        return await Db.RestaurantOrders.AsNoTracking()
            .Where(o => o.TenantId == TenantId && !o.IsDeleted
                && o.OrderDate >= start && o.OrderDate < end
                && (o.Status == RestaurantOrderStatus.Paid || o.Status == RestaurantOrderStatus.PostedToRoom))
            .ToListAsync(ct);
    }

    private static (DateTime start, DateTime end) ResolveRange(DateTime? from, DateTime? to)
    {
        var start = from?.Date ?? DateTime.Today.AddDays(-30);
        var end = (to?.Date ?? DateTime.Today).AddDays(1);
        return (start, end);
    }

    private static string OrderTypeLabel(RestaurantOrderType type) => type switch
    {
        RestaurantOrderType.DineIn => "صالة",
        RestaurantOrderType.Takeaway => "سفري",
        RestaurantOrderType.RoomService => "خدمة غرف",
        _ => type.ToString()
    };

    private static RestaurantOrderDto MapOrder(CloudRestaurantOrder o, Guid? tableSyncId = null, string? tableNumber = null) => new()
    {
        SyncId = o.SyncId,
        OrderNumber = o.OrderNumber,
        OrderType = o.OrderType,
        Status = o.Status,
        KitchenStatus = o.KitchenStatus,
        TotalAmount = o.TotalAmount,
        OrderDate = o.OrderDate,
        TableSyncId = tableSyncId,
        TableNumber = tableNumber
    };
}

public sealed class RestaurantMenuDto
{
    public List<RestaurantCategoryDto> Categories { get; set; } = [];
    public List<RestaurantMenuItemDto> Items { get; set; } = [];
}

public sealed class RestaurantCategoryDto
{
    public Guid SyncId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#00897B";
    public int SortOrder { get; set; }
}

public sealed class RestaurantMenuItemDto
{
    public Guid SyncId { get; set; }
    public Guid CategorySyncId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public string? Barcode { get; set; }
}

public sealed class RestaurantTableDto
{
    public Guid SyncId { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class ActiveRoomDto
{
    public Guid RoomSyncId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public Guid ReservationSyncId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public Guid GuestSyncId { get; set; }
}

public sealed class RestaurantOrderDto
{
    public Guid SyncId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public RestaurantOrderType OrderType { get; set; }
    public RestaurantOrderStatus Status { get; set; }
    public RestaurantKitchenStatus KitchenStatus { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
    public Guid? TableSyncId { get; set; }
    public string? TableNumber { get; set; }
}

public sealed class RestaurantPayResultDto
{
    public RestaurantOrderDto Order { get; set; } = null!;
    public decimal TenderedAmount { get; set; }
    public decimal ChangeDue { get; set; }
}

public sealed class CreateRestaurantOrderRequest
{
    public RestaurantOrderType OrderType { get; set; }
    public Guid? TableSyncId { get; set; }
    public Guid? ReservationSyncId { get; set; }
}

public sealed class AddOrderLineRequest
{
    public Guid MenuItemSyncId { get; set; }
    public decimal Quantity { get; set; } = 1;
}

public sealed class PayRestaurantOrderRequest
{
    public decimal Amount { get; set; }
    public RestaurantPaymentMethod PaymentMethod { get; set; } = RestaurantPaymentMethod.Cash;
    public Guid? CashBoxSyncId { get; set; }
    public bool OverrideStock { get; set; }
}

public sealed class RestaurantProfitSummaryDto
{
    public decimal Revenue { get; set; }
    public decimal Cogs { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal MarginPercent { get; set; }
    public int OrderCount { get; set; }
    public decimal AverageOrderValue { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal RoomServiceRevenue { get; set; }
    public int RoomServiceOrderCount { get; set; }
}

public sealed class RestaurantChannelSalesDto
{
    public RestaurantOrderType OrderType { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}

public sealed class RestaurantTopItemDto
{
    public string ItemName { get; set; } = string.Empty;
    public decimal QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

public sealed class RestaurantFinancialOverviewDto
{
    public decimal RestaurantRevenue { get; set; }
    public decimal RestaurantCogs { get; set; }
    public decimal RestaurantGrossProfit { get; set; }
    public decimal KitchenPurchases { get; set; }
    public decimal NetOperating { get; set; }
}

public sealed class RestaurantStockAlertDto
{
    public Guid SyncId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal MinQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
}

public sealed class UpdateKitchenStatusRequest
{
    public RestaurantKitchenStatus Status { get; set; }
}
