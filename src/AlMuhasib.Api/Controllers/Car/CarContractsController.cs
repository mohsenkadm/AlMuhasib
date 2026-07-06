using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers.Car;

[ApiController]
[Route("api/car/contracts")]
[Authorize(Policy = "Tenant")]
public sealed class CarContractsController : CarApiControllerBase
{
    public CarContractsController(CloudDbContext db, ITenantContext tenantContext) : base(db, tenantContext) { }

    [HttpGet]
    public async Task<ActionResult<List<CarContractListDto>>> GetContracts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] bool? hasRemaining = null,
        CancellationToken ct = default)
    {
        if (await EnsureCarTenantAsync(ct) is { } err) return err;

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = Db.CarSaleContracts.AsNoTracking().Where(c => c.TenantId == TenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                c.ContractNumber.Contains(term) ||
                c.SellerName.Contains(term) ||
                c.BuyerName.Contains(term) ||
                c.PlateNumber.Contains(term) ||
                c.ChassisNumber.Contains(term) ||
                c.CarType.Contains(term) ||
                c.CarModel.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CarContractStatus>(status, true, out var statusEnum))
            query = query.Where(c => c.Status == statusEnum);

        if (from.HasValue)
            query = query.Where(c => c.ContractDate >= from.Value.Date);

        if (to.HasValue)
            query = query.Where(c => c.ContractDate <= to.Value.Date);

        if (hasRemaining == true)
            query = query.Where(c => c.RemainingAmount > 0);

        var items = await query
            .OrderByDescending(c => c.ContractDate)
            .ThenByDescending(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Ok(items.Select(CarContractMapper.ToListItem).ToList());
    }

    [HttpGet("{syncId:guid}")]
    public async Task<ActionResult<CarContractDetailDto>> GetContract(Guid syncId, CancellationToken ct)
    {
        if (await EnsureCarTenantAsync(ct) is { } err) return err;

        var contract = await Db.CarSaleContracts.AsNoTracking()
            .Include(c => c.Payments)
            .FirstOrDefaultAsync(c => c.TenantId == TenantId && c.SyncId == syncId, ct);
        if (contract is null) return NotFound();

        return Ok(CarContractMapper.ToDetail(contract));
    }

    [HttpPost]
    public async Task<ActionResult<object>> CreateContract([FromBody] CreateCarContractRequest request, CancellationToken ct)
    {
        if (await EnsureCarTenantAsync(ct) is { } err) return err;

        var contract = new CloudCarSaleContract
        {
            TenantId = TenantId,
            SyncId = Guid.NewGuid(),
            ContractNumber = request.ContractNumber,
            ContractDate = request.ContractDate,
            SellerName = request.SellerName,
            SellerPhone = request.SellerPhone ?? string.Empty,
            BuyerName = request.BuyerName,
            BuyerPhone = request.BuyerPhone ?? string.Empty,
            PlateNumber = request.PlateNumber,
            CarType = request.CarType,
            CarModel = request.CarModel ?? string.Empty,
            CarColor = request.CarColor ?? string.Empty,
            ChassisNumber = request.ChassisNumber ?? string.Empty,
            CarPrice = request.CarPrice,
            AmountReceived = request.AmountReceived,
            Notes = request.Notes ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name ?? "mobile"
        };

        if (string.IsNullOrWhiteSpace(contract.ContractNumber))
            contract.ContractNumber = await GenerateContractNumberAsync(ct);

        CarContractMapper.ApplyAmounts(contract);
        Db.CarSaleContracts.Add(contract);
        await Db.SaveChangesAsync(ct);

        return Ok(new { syncId = contract.SyncId });
    }

    [HttpPut("{syncId:guid}")]
    public async Task<IActionResult> UpdateContract(Guid syncId, [FromBody] UpdateCarContractRequest request, CancellationToken ct)
    {
        if (await EnsureCarTenantAsync(ct) is { } err) return err;

        var contract = await Db.CarSaleContracts
            .FirstOrDefaultAsync(c => c.TenantId == TenantId && c.SyncId == syncId, ct);
        if (contract is null) return NotFound();

        contract.ContractDate = request.ContractDate;
        contract.SellerName = request.SellerName;
        contract.SellerPhone = request.SellerPhone ?? string.Empty;
        contract.BuyerName = request.BuyerName;
        contract.BuyerPhone = request.BuyerPhone ?? string.Empty;
        contract.PlateNumber = request.PlateNumber;
        contract.CarType = request.CarType;
        contract.CarModel = request.CarModel ?? string.Empty;
        contract.CarColor = request.CarColor ?? string.Empty;
        contract.ChassisNumber = request.ChassisNumber ?? string.Empty;
        contract.CarPrice = request.CarPrice;
        contract.AmountReceived = request.AmountReceived;
        contract.Notes = request.Notes ?? string.Empty;
        contract.UpdatedAt = DateTime.UtcNow;
        contract.UpdatedBy = User.Identity?.Name ?? "mobile";

        CarContractMapper.ApplyAmounts(contract);
        await Db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpDelete("{syncId:guid}")]
    public async Task<IActionResult> DeleteContract(Guid syncId, CancellationToken ct)
    {
        if (await EnsureCarTenantAsync(ct) is { } err) return err;

        var contract = await Db.CarSaleContracts
            .FirstOrDefaultAsync(c => c.TenantId == TenantId && c.SyncId == syncId, ct);
        if (contract is null) return NotFound();

        contract.IsDeleted = true;
        contract.DeletedAt = DateTime.UtcNow;
        contract.DeletedBy = User.Identity?.Name ?? "mobile";
        contract.UpdatedAt = DateTime.UtcNow;
        await Db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpPost("{syncId:guid}/payments")]
    public async Task<ActionResult<object>> RecordPayment(Guid syncId, [FromBody] CarPaymentRequest request, CancellationToken ct)
    {
        if (await EnsureCarTenantAsync(ct) is { } err) return err;
        if (request.Amount <= 0)
            return BadRequest("Payment amount must be greater than zero.");

        var contract = await Db.CarSaleContracts
            .FirstOrDefaultAsync(c => c.TenantId == TenantId && c.SyncId == syncId, ct);
        if (contract is null) return NotFound();
        if (contract.Status == CarContractStatus.Cancelled)
            return BadRequest("Cannot record payment on a cancelled contract.");
        if (request.Amount > contract.RemainingAmount)
            return BadRequest("Payment amount exceeds remaining balance.");

        var remainingBefore = contract.RemainingAmount;
        contract.AmountReceived += request.Amount;
        contract.RemainingAmount = contract.CarPrice - contract.AmountReceived;
        if (contract.RemainingAmount < 0) contract.RemainingAmount = 0;
        CarContractMapper.UpdateStatus(contract);
        contract.UpdatedAt = DateTime.UtcNow;
        contract.UpdatedBy = User.Identity?.Name ?? "mobile";

        var payment = new CloudCarContractPayment
        {
            TenantId = TenantId,
            SyncId = Guid.NewGuid(),
            ContractId = contract.Id,
            PaymentDate = request.PaymentDate,
            Amount = request.Amount,
            Notes = request.Notes ?? string.Empty,
            RemainingBefore = remainingBefore,
            RemainingAfter = contract.RemainingAmount,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name ?? "mobile"
        };
        Db.CarContractPayments.Add(payment);
        await Db.SaveChangesAsync(ct);

        return Ok(new { payment.SyncId });
    }

    private async Task<string> GenerateContractNumberAsync(CancellationToken ct)
    {
        var year = DateTime.Today.Year;
        var prefix = $"CAR-{year}-";
        var last = await Db.CarSaleContracts.IgnoreQueryFilters()
            .Where(c => c.TenantId == TenantId && c.ContractNumber.StartsWith(prefix))
            .OrderByDescending(c => c.ContractNumber)
            .Select(c => c.ContractNumber)
            .FirstOrDefaultAsync(ct);

        var next = 1;
        if (!string.IsNullOrEmpty(last))
        {
            var suffix = last[prefix.Length..];
            if (int.TryParse(suffix, out var n))
                next = n + 1;
        }

        return $"{prefix}{next:D4}";
    }
}

public class CreateCarContractRequest
{
    public string ContractNumber { get; set; } = string.Empty;
    public DateTime ContractDate { get; set; } = DateTime.Today;
    public string SellerName { get; set; } = string.Empty;
    public string? SellerPhone { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string? BuyerPhone { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string CarType { get; set; } = string.Empty;
    public string? CarModel { get; set; }
    public string? CarColor { get; set; }
    public string? ChassisNumber { get; set; }
    public decimal CarPrice { get; set; }
    public decimal AmountReceived { get; set; }
    public string? Notes { get; set; }
}

public sealed class UpdateCarContractRequest : CreateCarContractRequest;

public sealed class CarPaymentRequest
{
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public string? Notes { get; set; }
}

public class CarContractListDto
{
    public Guid SyncId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public DateTime ContractDate { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public string BuyerName { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;
    public string CarType { get; set; } = string.Empty;
    public decimal CarPrice { get; set; }
    public decimal AmountReceived { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class CarContractDetailDto : CarContractListDto
{
    public string SellerPhone { get; set; } = string.Empty;
    public string BuyerPhone { get; set; } = string.Empty;
    public string CarModel { get; set; } = string.Empty;
    public string CarColor { get; set; } = string.Empty;
    public string ChassisNumber { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public List<CarPaymentDto> Payments { get; set; } = [];
}

public sealed class CarPaymentDto
{
    public Guid SyncId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string Notes { get; set; } = string.Empty;
}

internal static class CarContractMapper
{
    public static CarContractListDto ToListItem(CloudCarSaleContract c) => new()
    {
        SyncId = c.SyncId,
        ContractNumber = c.ContractNumber,
        ContractDate = c.ContractDate,
        SellerName = c.SellerName,
        BuyerName = c.BuyerName,
        PlateNumber = c.PlateNumber,
        CarType = c.CarType,
        CarPrice = c.CarPrice,
        AmountReceived = c.AmountReceived,
        RemainingAmount = c.RemainingAmount,
        Status = c.Status.ToString()
    };

    public static CarContractDetailDto ToDetail(CloudCarSaleContract c)
    {
        var list = ToListItem(c);
        return new CarContractDetailDto
        {
            SyncId = list.SyncId,
            ContractNumber = list.ContractNumber,
            ContractDate = list.ContractDate,
            SellerName = list.SellerName,
            BuyerName = list.BuyerName,
            PlateNumber = list.PlateNumber,
            CarType = list.CarType,
            CarPrice = list.CarPrice,
            AmountReceived = list.AmountReceived,
            RemainingAmount = list.RemainingAmount,
            Status = list.Status,
            SellerPhone = c.SellerPhone,
            BuyerPhone = c.BuyerPhone,
            CarModel = c.CarModel,
            CarColor = c.CarColor,
            ChassisNumber = c.ChassisNumber,
            Notes = c.Notes,
            Payments = c.Payments
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new CarPaymentDto
                {
                    SyncId = p.SyncId,
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate,
                    Notes = p.Notes
                })
                .ToList()
        };
    }

    public static void ApplyAmounts(CloudCarSaleContract contract)
    {
        contract.RemainingAmount = contract.CarPrice - contract.AmountReceived;
        if (contract.RemainingAmount < 0)
            contract.RemainingAmount = 0;
        UpdateStatus(contract);
    }

    public static void UpdateStatus(CloudCarSaleContract contract)
    {
        if (contract.Status == CarContractStatus.Cancelled)
            return;

        contract.Status = contract.RemainingAmount <= 0
            ? CarContractStatus.Completed
            : CarContractStatus.Active;
    }
}
