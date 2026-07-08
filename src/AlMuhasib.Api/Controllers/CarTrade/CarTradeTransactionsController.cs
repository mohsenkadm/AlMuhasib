using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers.CarTrade;

[ApiController]
[Route("api/car-trade/transactions")]
[Authorize(Policy = "Tenant")]
public sealed class CarTradeTransactionsController : CarTradeApiControllerBase
{
    public CarTradeTransactionsController(CloudDbContext db, ITenantContext tenantContext) : base(db, tenantContext) { }

    [HttpGet]
    public async Task<ActionResult<List<CarTradeListDto>>> GetTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        [FromQuery] string? tradeType = null,
        [FromQuery] string? status = null,
        [FromQuery] string? paymentMode = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] bool? unpaidOnly = null,
        CancellationToken ct = default)
    {
        if (await EnsureCarTradeTenantAsync(ct) is { } err) return err;

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = Db.CarTradeTransactions.AsNoTracking().Where(t => t.TenantId == TenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(t =>
                t.TransactionNumber.Contains(term) ||
                t.CarName.Contains(term) ||
                t.SellerName.Contains(term) ||
                t.BuyerName.Contains(term) ||
                t.PlateNumber.Contains(term) ||
                t.ChassisNumber.Contains(term) ||
                t.CarType.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(tradeType) && Enum.TryParse<CarTradeType>(tradeType, true, out var tradeTypeEnum))
            query = query.Where(t => t.TradeType == tradeTypeEnum);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CarTradeStatus>(status, true, out var statusEnum))
            query = query.Where(t => t.Status == statusEnum);

        if (!string.IsNullOrWhiteSpace(paymentMode) && Enum.TryParse<CarTradePaymentMode>(paymentMode, true, out var paymentModeEnum))
            query = query.Where(t => t.PaymentMode == paymentModeEnum);

        if (from.HasValue)
            query = query.Where(t => t.TransactionDate >= from.Value.Date);

        if (to.HasValue)
            query = query.Where(t => t.TransactionDate <= to.Value.Date);

        if (unpaidOnly == true)
            query = query.Where(t => t.RemainingAmount > 0);

        var items = await query
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Ok(items.Select(CarTradeMapper.ToListItem).ToList());
    }

    [HttpGet("{syncId:guid}")]
    public async Task<ActionResult<CarTradeDetailDto>> GetTransaction(Guid syncId, CancellationToken ct)
    {
        if (await EnsureCarTradeTenantAsync(ct) is { } err) return err;

        var transaction = await Db.CarTradeTransactions.AsNoTracking()
            .Include(t => t.Payments)
            .FirstOrDefaultAsync(t => t.TenantId == TenantId && t.SyncId == syncId, ct);
        if (transaction is null) return NotFound();

        return Ok(CarTradeMapper.ToDetail(transaction));
    }

    [HttpPost]
    public async Task<ActionResult<object>> CreateTransaction([FromBody] CreateCarTradeTransactionRequest request, CancellationToken ct)
    {
        if (await EnsureCarTradeTenantAsync(ct) is { } err) return err;

        var validationError = CarTradeMapper.ValidateRequest(request);
        if (validationError is not null)
            return BadRequest(validationError);

        var transaction = new CloudCarTradeTransaction
        {
            TenantId = TenantId,
            SyncId = Guid.NewGuid(),
            TransactionNumber = request.TransactionNumber,
            TransactionDate = request.TransactionDate,
            TradeType = request.TradeType,
            CarName = request.CarName,
            CarColor = request.CarColor ?? string.Empty,
            PlateNumber = request.PlateNumber ?? string.Empty,
            ChassisNumber = request.ChassisNumber ?? string.Empty,
            CarType = request.CarType ?? string.Empty,
            SellerName = request.SellerName ?? string.Empty,
            SellerPhone = request.SellerPhone ?? string.Empty,
            BuyerName = request.BuyerName ?? string.Empty,
            BuyerPhone = request.BuyerPhone ?? string.Empty,
            PurchasePrice = request.PurchasePrice,
            SalePrice = request.SalePrice,
            PaymentMode = request.PaymentMode,
            AmountPaid = request.AmountPaid,
            Notes = request.Notes ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name ?? "mobile"
        };

        if (string.IsNullOrWhiteSpace(transaction.TransactionNumber))
            transaction.TransactionNumber = await GenerateTransactionNumberAsync(ct);

        CarTradeMapper.ApplyAmounts(transaction);
        Db.CarTradeTransactions.Add(transaction);
        await Db.SaveChangesAsync(ct);

        return Ok(new { syncId = transaction.SyncId });
    }

    [HttpPut("{syncId:guid}")]
    public async Task<IActionResult> UpdateTransaction(Guid syncId, [FromBody] UpdateCarTradeTransactionRequest request, CancellationToken ct)
    {
        if (await EnsureCarTradeTenantAsync(ct) is { } err) return err;

        var validationError = CarTradeMapper.ValidateRequest(request);
        if (validationError is not null)
            return BadRequest(validationError);

        var transaction = await Db.CarTradeTransactions
            .FirstOrDefaultAsync(t => t.TenantId == TenantId && t.SyncId == syncId, ct);
        if (transaction is null) return NotFound();

        transaction.TransactionDate = request.TransactionDate;
        transaction.TradeType = request.TradeType;
        transaction.CarName = request.CarName;
        transaction.CarColor = request.CarColor ?? string.Empty;
        transaction.PlateNumber = request.PlateNumber ?? string.Empty;
        transaction.ChassisNumber = request.ChassisNumber ?? string.Empty;
        transaction.CarType = request.CarType ?? string.Empty;
        transaction.SellerName = request.SellerName ?? string.Empty;
        transaction.SellerPhone = request.SellerPhone ?? string.Empty;
        transaction.BuyerName = request.BuyerName ?? string.Empty;
        transaction.BuyerPhone = request.BuyerPhone ?? string.Empty;
        transaction.PurchasePrice = request.PurchasePrice;
        transaction.SalePrice = request.SalePrice;
        transaction.PaymentMode = request.PaymentMode;
        transaction.AmountPaid = request.AmountPaid;
        transaction.Notes = request.Notes ?? string.Empty;
        transaction.UpdatedAt = DateTime.UtcNow;
        transaction.UpdatedBy = User.Identity?.Name ?? "mobile";

        CarTradeMapper.ApplyAmounts(transaction);
        await Db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpDelete("{syncId:guid}")]
    public async Task<IActionResult> DeleteTransaction(Guid syncId, CancellationToken ct)
    {
        if (await EnsureCarTradeTenantAsync(ct) is { } err) return err;

        var transaction = await Db.CarTradeTransactions
            .FirstOrDefaultAsync(t => t.TenantId == TenantId && t.SyncId == syncId, ct);
        if (transaction is null) return NotFound();

        transaction.IsDeleted = true;
        transaction.DeletedAt = DateTime.UtcNow;
        transaction.DeletedBy = User.Identity?.Name ?? "mobile";
        transaction.UpdatedAt = DateTime.UtcNow;
        await Db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpPost("{syncId:guid}/payments")]
    public async Task<ActionResult<object>> RecordPayment(Guid syncId, [FromBody] CarTradePaymentRequest request, CancellationToken ct)
    {
        if (await EnsureCarTradeTenantAsync(ct) is { } err) return err;
        if (request.Amount <= 0)
            return BadRequest("Payment amount must be greater than zero.");

        var transaction = await Db.CarTradeTransactions
            .FirstOrDefaultAsync(t => t.TenantId == TenantId && t.SyncId == syncId, ct);
        if (transaction is null) return NotFound();
        if (transaction.Status == CarTradeStatus.Cancelled)
            return BadRequest("Cannot record payment on a cancelled transaction.");

        var isSalePayment = string.Equals(request.PaymentKind, nameof(CarTradePaymentKind.Sale), StringComparison.OrdinalIgnoreCase);
        var remaining = isSalePayment ? transaction.SaleRemainingAmount : transaction.RemainingAmount;
        if (request.Amount > remaining)
            return BadRequest("Payment amount exceeds remaining balance.");

        var remainingBefore = remaining;
        if (isSalePayment)
        {
            transaction.SaleAmountPaid += request.Amount;
            transaction.SaleRemainingAmount = transaction.SalePrice - transaction.SaleAmountPaid;
            if (transaction.SaleRemainingAmount < 0) transaction.SaleRemainingAmount = 0;
            transaction.SalePaymentMode = transaction.SaleRemainingAmount <= 0
                ? CarTradePaymentMode.FullCash
                : CarTradePaymentMode.Partial;
        }
        else
        {
            transaction.AmountPaid += request.Amount;
            transaction.RemainingAmount = transaction.TotalAmount - transaction.AmountPaid;
            if (transaction.RemainingAmount < 0) transaction.RemainingAmount = 0;
            transaction.PaymentMode = transaction.RemainingAmount <= 0
                ? CarTradePaymentMode.FullCash
                : CarTradePaymentMode.Partial;
            CarTradeMapper.UpdateStatus(transaction);
        }

        transaction.UpdatedAt = DateTime.UtcNow;
        transaction.UpdatedBy = User.Identity?.Name ?? "mobile";

        var payment = new CloudCarTradePayment
        {
            TenantId = TenantId,
            SyncId = Guid.NewGuid(),
            TransactionId = transaction.Id,
            PaymentKind = isSalePayment ? CarTradePaymentKind.Sale : CarTradePaymentKind.Purchase,
            PaymentDate = request.PaymentDate,
            Amount = request.Amount,
            Notes = request.Notes ?? string.Empty,
            RemainingBefore = remainingBefore,
            RemainingAfter = isSalePayment ? transaction.SaleRemainingAmount : transaction.RemainingAmount,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name ?? "mobile"
        };
        Db.CarTradePayments.Add(payment);
        await Db.SaveChangesAsync(ct);

        return Ok(new { payment.SyncId });
    }

    [HttpPost("{syncId:guid}/sell")]
    public async Task<IActionResult> SellCar(Guid syncId, [FromBody] CarTradeSellRequest request, CancellationToken ct)
    {
        if (await EnsureCarTradeTenantAsync(ct) is { } err) return err;
        if (string.IsNullOrWhiteSpace(request.BuyerName))
            return BadRequest("Buyer name is required.");
        if (request.SalePrice <= 0)
            return BadRequest("Sale price must be greater than zero.");

        var transaction = await Db.CarTradeTransactions
            .FirstOrDefaultAsync(t => t.TenantId == TenantId && t.SyncId == syncId, ct);
        if (transaction is null) return NotFound();
        if (transaction.IsSold) return BadRequest("Car is already sold.");

        transaction.BuyerName = request.BuyerName.Trim();
        transaction.BuyerPhone = request.BuyerPhone?.Trim() ?? string.Empty;
        transaction.SalePrice = request.SalePrice;
        transaction.SaleDate = request.SaleDate.Date;
        transaction.SalePaymentMode = request.SalePaymentMode;
        transaction.IsSold = true;
        CarTradeMapper.ApplySaleAmounts(transaction, request.SaleAmountPaid);
        transaction.UpdatedAt = DateTime.UtcNow;
        transaction.UpdatedBy = User.Identity?.Name ?? "mobile";
        await Db.SaveChangesAsync(ct);
        return Ok();
    }

    private async Task<string> GenerateTransactionNumberAsync(CancellationToken ct)
    {
        var year = DateTime.Today.Year;
        var prefix = $"TRD-{year}-";
        var numbers = await Db.CarTradeTransactions.IgnoreQueryFilters()
            .Where(t => t.TenantId == TenantId && t.TransactionNumber.StartsWith(prefix))
            .Select(t => t.TransactionNumber)
            .ToListAsync(ct);

        var max = 0;
        foreach (var number in numbers)
        {
            if (number.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(number[prefix.Length..], out var sequence) &&
                sequence > max)
            {
                max = sequence;
            }
        }

        return $"{prefix}{(max + 1):D5}";
    }
}

public class CreateCarTradeTransactionRequest
{
    public string TransactionNumber { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; } = DateTime.Today;
    public CarTradeType TradeType { get; set; } = CarTradeType.Buy;
    public string CarName { get; set; } = string.Empty;
    public string? CarColor { get; set; }
    public string? PlateNumber { get; set; }
    public string? ChassisNumber { get; set; }
    public string? CarType { get; set; }
    public string? SellerName { get; set; }
    public string? SellerPhone { get; set; }
    public string? BuyerName { get; set; }
    public string? BuyerPhone { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public CarTradePaymentMode PaymentMode { get; set; } = CarTradePaymentMode.FullCash;
    public decimal AmountPaid { get; set; }
    public string? Notes { get; set; }
}

public sealed class UpdateCarTradeTransactionRequest : CreateCarTradeTransactionRequest;

public sealed class CarTradePaymentRequest
{
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public string? Notes { get; set; }
    public string? PaymentKind { get; set; }
}

public sealed class CarTradeSellRequest
{
    public string BuyerName { get; set; } = string.Empty;
    public string? BuyerPhone { get; set; }
    public decimal SalePrice { get; set; }
    public CarTradePaymentMode SalePaymentMode { get; set; } = CarTradePaymentMode.FullCash;
    public decimal SaleAmountPaid { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.Today;
    public string? Notes { get; set; }
}

public class CarTradeListDto
{
    public Guid SyncId { get; set; }
    public string TransactionNumber { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string TradeType { get; set; } = string.Empty;
    public string CarName { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;
    public string CarType { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RemainingAmount { get; set; }
    public bool IsSold { get; set; }
    public decimal SalePrice { get; set; }
    public decimal SaleAmountPaid { get; set; }
    public decimal SaleRemainingAmount { get; set; }
    public string PaymentMode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class CarTradeDetailDto : CarTradeListDto
{
    public string CarColor { get; set; } = string.Empty;
    public string ChassisNumber { get; set; } = string.Empty;
    public string SellerName { get; set; } = string.Empty;
    public string SellerPhone { get; set; } = string.Empty;
    public string BuyerName { get; set; } = string.Empty;
    public string BuyerPhone { get; set; } = string.Empty;
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<CarTradePaymentDto> Payments { get; set; } = [];
}

public sealed class CarTradePaymentDto
{
    public Guid SyncId { get; set; }
    public string PaymentKind { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string Notes { get; set; } = string.Empty;
}

internal static class CarTradeMapper
{
    public static CarTradeListDto ToListItem(CloudCarTradeTransaction t) => new()
    {
        SyncId = t.SyncId,
        TransactionNumber = t.TransactionNumber,
        TransactionDate = t.TransactionDate,
        TradeType = t.TradeType.ToString(),
        CarName = t.CarName,
        PlateNumber = t.PlateNumber,
        CarType = t.CarType,
        TotalAmount = t.TotalAmount,
        AmountPaid = t.AmountPaid,
        RemainingAmount = t.RemainingAmount,
        IsSold = t.IsSold,
        SalePrice = t.SalePrice,
        SaleAmountPaid = t.SaleAmountPaid,
        SaleRemainingAmount = t.SaleRemainingAmount,
        PaymentMode = t.PaymentMode.ToString(),
        Status = t.Status.ToString()
    };

    public static CarTradeDetailDto ToDetail(CloudCarTradeTransaction t)
    {
        var list = ToListItem(t);
        return new CarTradeDetailDto
        {
            SyncId = list.SyncId,
            TransactionNumber = list.TransactionNumber,
            TransactionDate = list.TransactionDate,
            TradeType = list.TradeType,
            CarName = list.CarName,
            PlateNumber = list.PlateNumber,
            CarType = list.CarType,
            TotalAmount = list.TotalAmount,
            AmountPaid = list.AmountPaid,
            RemainingAmount = list.RemainingAmount,
            PaymentMode = list.PaymentMode,
            Status = list.Status,
            CarColor = t.CarColor,
            ChassisNumber = t.ChassisNumber,
            SellerName = t.SellerName,
            SellerPhone = t.SellerPhone,
            BuyerName = t.BuyerName,
            BuyerPhone = t.BuyerPhone,
            PurchasePrice = t.PurchasePrice,
            SalePrice = t.SalePrice,
            Notes = t.Notes,
            Payments = t.Payments
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new CarTradePaymentDto
                {
                    SyncId = p.SyncId,
                    PaymentKind = p.PaymentKind.ToString(),
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate,
                    Notes = p.Notes
                })
                .ToList()
        };
    }

    public static string? ValidateRequest(CreateCarTradeTransactionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CarName))
            return "Car name is required.";

        if (request.TradeType == CarTradeType.Buy)
        {
            if (string.IsNullOrWhiteSpace(request.SellerName))
                return "Seller name is required for buy transactions.";
            if (request.PurchasePrice <= 0)
                return "Purchase price must be greater than zero.";
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.BuyerName))
                return "Buyer name is required for sell transactions.";
            if (request.SalePrice <= 0)
                return "Sale price must be greater than zero.";
        }

        return null;
    }

    public static void ApplyAmounts(CloudCarTradeTransaction transaction)
    {
        transaction.TotalAmount = transaction.PurchasePrice;

        if (transaction.PaymentMode == CarTradePaymentMode.FullCash)
            transaction.AmountPaid = transaction.PurchasePrice;

        if (transaction.AmountPaid > transaction.PurchasePrice)
            transaction.AmountPaid = transaction.PurchasePrice;

        transaction.RemainingAmount = transaction.PurchasePrice - transaction.AmountPaid;
        if (transaction.RemainingAmount < 0)
            transaction.RemainingAmount = 0;

        UpdateStatus(transaction);
    }

    public static void ApplySaleAmounts(CloudCarTradeTransaction transaction, decimal saleAmountPaid)
    {
        if (transaction.SalePaymentMode == CarTradePaymentMode.FullCash)
            transaction.SaleAmountPaid = transaction.SalePrice;
        else
            transaction.SaleAmountPaid = saleAmountPaid;

        if (transaction.SaleAmountPaid > transaction.SalePrice)
            transaction.SaleAmountPaid = transaction.SalePrice;

        transaction.SaleRemainingAmount = transaction.SalePrice - transaction.SaleAmountPaid;
        if (transaction.SaleRemainingAmount < 0)
            transaction.SaleRemainingAmount = 0;
    }

    public static void UpdateStatus(CloudCarTradeTransaction transaction)
    {
        if (transaction.Status == CarTradeStatus.Cancelled)
            return;

        transaction.Status = transaction.RemainingAmount <= 0
            ? CarTradeStatus.Completed
            : CarTradeStatus.Active;
    }
}
