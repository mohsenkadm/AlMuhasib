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

    [HttpPost("orders")]
    public async Task<ActionResult<RestaurantOrderDto>> CreateOrder([FromBody] CreateRestaurantOrderRequest request, CancellationToken ct)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;

        var order = new CloudRestaurantOrder
        {
            TenantId = TenantId,
            OrderNumber = await GenerateOrderNumberAsync(ct),
            OrderType = request.OrderType,
            Status = RestaurantOrderStatus.Open,
            KitchenStatus = RestaurantKitchenStatus.Pending,
            OrderDate = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name ?? "Mobile"
        };

        if (request.TableSyncId.HasValue)
        {
            var table = await Db.RestaurantTables.FirstOrDefaultAsync(t => t.TenantId == TenantId && t.SyncId == request.TableSyncId.Value, ct);
            if (table is not null)
            {
                order.RestaurantTableId = table.Id;
                table.Status = RestaurantTableStatus.Occupied;
            }
        }

        if (request.ReservationSyncId.HasValue)
        {
            var reservation = await Db.HotelReservations.FirstOrDefaultAsync(r => r.TenantId == TenantId && r.SyncId == request.ReservationSyncId.Value, ct);
            if (reservation is not null)
            {
                order.ReservationId = reservation.Id;
                order.RoomId = reservation.RoomId;
                order.GuestId = reservation.GuestId;
            }
        }

        Db.RestaurantOrders.Add(order);
        await Db.SaveChangesAsync(ct);

        return Ok(MapOrder(order));
    }

    [HttpPost("orders/{syncId:guid}/lines")]
    public async Task<ActionResult<RestaurantOrderDto>> AddLine(Guid syncId, [FromBody] AddOrderLineRequest request, CancellationToken ct)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;

        var order = await Db.RestaurantOrders
            .FirstOrDefaultAsync(o => o.TenantId == TenantId && o.SyncId == syncId, ct);
        if (order is null) return NotFound();

        var item = await Db.RestaurantMenuItems.AsNoTracking()
            .FirstOrDefaultAsync(m => m.TenantId == TenantId && m.SyncId == request.MenuItemSyncId, ct);
        if (item is null) return BadRequest("Menu item not found");

        var line = new CloudRestaurantOrderLine
        {
            TenantId = TenantId,
            RestaurantOrderId = order.Id,
            RestaurantMenuItemId = item.Id,
            ItemName = item.Name,
            Quantity = request.Quantity,
            UnitPrice = item.SalePrice,
            LineTotal = request.Quantity * item.SalePrice,
            CreatedBy = User.Identity?.Name ?? "Mobile"
        };
        Db.RestaurantOrderLines.Add(line);
        await RecalculateOrderAsync(order.Id, ct);
        await Db.SaveChangesAsync(ct);

        order = await Db.RestaurantOrders.FirstAsync(o => o.Id == order.Id, ct);
        return Ok(MapOrder(order));
    }

    [HttpPost("orders/{syncId:guid}/pay")]
    public async Task<ActionResult<RestaurantOrderDto>> PayOrder(Guid syncId, [FromBody] PayRestaurantOrderRequest request, CancellationToken ct)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;

        var order = await Db.RestaurantOrders.FirstOrDefaultAsync(o => o.TenantId == TenantId && o.SyncId == syncId, ct);
        if (order is null) return NotFound();

        order.Status = order.OrderType == RestaurantOrderType.RoomService
            ? RestaurantOrderStatus.PostedToRoom
            : RestaurantOrderStatus.Paid;
        order.PaidAt = DateTime.UtcNow;
        order.KitchenStatus = RestaurantKitchenStatus.Served;

        if (request.Amount > 0 && request.CashBoxSyncId.HasValue && order.OrderType != RestaurantOrderType.RoomService)
        {
            var cashBox = await Db.HotelCashBoxes.FirstOrDefaultAsync(c => c.TenantId == TenantId && c.SyncId == request.CashBoxSyncId.Value, ct);
            if (cashBox is not null)
            {
                cashBox.CurrentBalance += request.Amount;
                Db.RestaurantOrderPayments.Add(new CloudRestaurantOrderPayment
                {
                    TenantId = TenantId,
                    RestaurantOrderId = order.Id,
                    Amount = request.Amount,
                    PaymentMethod = request.PaymentMethod,
                    HotelCashBoxId = cashBox.Id,
                    CreatedBy = User.Identity?.Name ?? "Mobile"
                });
            }
        }

        if (order.OrderType == RestaurantOrderType.RoomService && order.ReservationId.HasValue)
        {
            Db.HotelReservationCharges.Add(new CloudHotelReservationCharge
            {
                TenantId = TenantId,
                ReservationId = order.ReservationId.Value,
                Description = $"مطعم - طلب {order.OrderNumber}",
                Amount = order.TotalAmount,
                ChargeDate = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name ?? "Mobile"
            });
        }

        if (order.RestaurantTableId.HasValue)
        {
            var table = await Db.RestaurantTables.FirstOrDefaultAsync(t => t.Id == order.RestaurantTableId.Value, ct);
            if (table is not null) table.Status = RestaurantTableStatus.Available;
        }

        await Db.SaveChangesAsync(ct);
        return Ok(MapOrder(order));
    }

    [HttpGet("reports/summary")]
    public async Task<ActionResult<RestaurantProfitSummaryDto>> GetSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        if (await EnsureHotelTenantAsync(ct) is { } err) return err;

        var start = from?.Date ?? DateTime.Today.AddDays(-30);
        var end = (to?.Date ?? DateTime.Today).AddDays(1);

        var orders = await Db.RestaurantOrders.AsNoTracking()
            .Where(o => o.TenantId == TenantId && !o.IsDeleted
                && o.OrderDate >= start && o.OrderDate < end
                && (o.Status == RestaurantOrderStatus.Paid || o.Status == RestaurantOrderStatus.PostedToRoom))
            .ToListAsync(ct);

        var revenue = orders.Sum(o => o.TotalAmount);
        var cogs = orders.Sum(o => o.CogsAmount);
        var profit = revenue - cogs;

        return Ok(new RestaurantProfitSummaryDto
        {
            Revenue = revenue,
            Cogs = cogs,
            GrossProfit = profit,
            MarginPercent = revenue > 0 ? Math.Round(profit / revenue * 100, 1) : 0,
            OrderCount = orders.Count
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

        return Ok(orders.Select(MapOrder).ToList());
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

    private async Task RecalculateOrderAsync(int orderId, CancellationToken ct)
    {
        var lines = await Db.RestaurantOrderLines.Where(l => l.RestaurantOrderId == orderId && !l.IsDeleted).ToListAsync(ct);
        var order = await Db.RestaurantOrders.FirstAsync(o => o.Id == orderId, ct);
        order.SubTotal = lines.Sum(l => l.LineTotal);
        order.TotalAmount = order.SubTotal - order.DiscountAmount;
        order.GrossProfit = order.TotalAmount - order.CogsAmount;
    }

    private async Task<string> GenerateOrderNumberAsync(CancellationToken ct)
    {
        var prefix = $"RST-{DateTime.UtcNow:yyyyMMdd}";
        var count = await Db.RestaurantOrders.IgnoreQueryFilters()
            .CountAsync(o => o.TenantId == TenantId && o.OrderNumber.StartsWith(prefix), ct);
        return $"{prefix}-{(count + 1):D4}";
    }

    private static RestaurantOrderDto MapOrder(CloudRestaurantOrder o) => new()
    {
        SyncId = o.SyncId,
        OrderNumber = o.OrderNumber,
        OrderType = o.OrderType,
        Status = o.Status,
        KitchenStatus = o.KitchenStatus,
        TotalAmount = o.TotalAmount,
        OrderDate = o.OrderDate
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
}

public sealed class RestaurantProfitSummaryDto
{
    public decimal Revenue { get; set; }
    public decimal Cogs { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal MarginPercent { get; set; }
    public int OrderCount { get; set; }
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
