using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers.RealEstate;

[ApiController]
[Route("api/real-estate/contracts")]
[Authorize(Policy = "Tenant")]
public sealed class RealEstateContractsController : RealEstateApiControllerBase
{
    public RealEstateContractsController(CloudDbContext db, ITenantContext tenantContext) : base(db, tenantContext) { }

    [HttpGet]
    public async Task<ActionResult<List<RealEstateContractListDto>>> GetContracts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? contractType = null,
        [FromQuery] string? propertyType = null,
        [FromQuery] string? paymentMode = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] bool? hasRemaining = null,
        [FromQuery] bool? creditOnly = null,
        CancellationToken ct = default)
    {
        if (await EnsureRealEstateTenantAsync(ct) is { } err) return err;

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = Db.RealEstateContracts.AsNoTracking().Where(c => c.TenantId == TenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                c.ContractNumber.Contains(term) ||
                c.SellerName.Contains(term) ||
                c.BuyerName.Contains(term) ||
                c.PropertyLocation.Contains(term) ||
                c.PropertyAddress.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<RealEstateContractStatus>(status, true, out var statusEnum))
            query = query.Where(c => c.Status == statusEnum);

        if (!string.IsNullOrWhiteSpace(contractType) && Enum.TryParse<RealEstateContractType>(contractType, true, out var typeEnum))
            query = query.Where(c => c.ContractType == typeEnum);

        if (!string.IsNullOrWhiteSpace(propertyType) && Enum.TryParse<RealEstatePropertyType>(propertyType, true, out var propEnum))
            query = query.Where(c => c.PropertyType == propEnum);

        if (!string.IsNullOrWhiteSpace(paymentMode) && Enum.TryParse<RealEstatePaymentMode>(paymentMode, true, out var modeEnum))
            query = query.Where(c => c.PaymentMode == modeEnum);

        if (from.HasValue)
            query = query.Where(c => c.ContractDate >= from.Value.Date);

        if (to.HasValue)
            query = query.Where(c => c.ContractDate <= to.Value.Date);

        if (hasRemaining == true)
            query = query.Where(c => c.RemainingAmount > 0);

        if (creditOnly == true)
            query = query.Where(c => c.PaymentMode == RealEstatePaymentMode.Credit);

        var items = await query
            .OrderByDescending(c => c.ContractDate)
            .ThenByDescending(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Ok(items.Select(RealEstateContractMapper.ToListItem).ToList());
    }

    [HttpGet("{syncId:guid}")]
    public async Task<ActionResult<RealEstateContractDetailDto>> GetContract(Guid syncId, CancellationToken ct)
    {
        if (await EnsureRealEstateTenantAsync(ct) is { } err) return err;

        var contract = await Db.RealEstateContracts.AsNoTracking()
            .Include(c => c.Payments)
            .Include(c => c.Clauses)
            .FirstOrDefaultAsync(c => c.TenantId == TenantId && c.SyncId == syncId, ct);
        if (contract is null) return NotFound();

        return Ok(RealEstateContractMapper.ToDetail(contract));
    }

    [HttpPost]
    public async Task<ActionResult<object>> CreateContract([FromBody] CreateRealEstateContractRequest request, CancellationToken ct)
    {
        if (await EnsureRealEstateTenantAsync(ct) is { } err) return err;

        var contract = new CloudRealEstateContract
        {
            TenantId = TenantId,
            SyncId = Guid.NewGuid(),
            ContractNumber = request.ContractNumber,
            ContractDate = request.ContractDate,
            ContractType = request.ContractType,
            PropertyType = request.PropertyType,
            PropertyLocation = request.PropertyLocation ?? string.Empty,
            PropertyAddress = request.PropertyAddress ?? string.Empty,
            PropertyAreaSqm = request.PropertyAreaSqm,
            PropertyDescription = request.PropertyDescription ?? string.Empty,
            SellerName = request.SellerName,
            SellerAddress = request.SellerAddress ?? string.Empty,
            SellerIdNumber = request.SellerIdNumber ?? string.Empty,
            SellerIdDate = request.SellerIdDate,
            SellerPhone = request.SellerPhone ?? string.Empty,
            BuyerName = request.BuyerName,
            BuyerAddress = request.BuyerAddress ?? string.Empty,
            BuyerIdNumber = request.BuyerIdNumber ?? string.Empty,
            BuyerIdDate = request.BuyerIdDate,
            BuyerPhone = request.BuyerPhone ?? string.Empty,
            TotalPrice = request.TotalPrice,
            DownPayment = request.DownPayment,
            AmountPaid = request.AmountPaid,
            PaymentMode = request.PaymentMode,
            DebtorParty = request.DebtorParty,
            DueDate = request.DueDate,
            WitnessOneName = request.WitnessOneName ?? string.Empty,
            WitnessTwoName = request.WitnessTwoName ?? string.Empty,
            Notes = request.Notes ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name ?? "mobile"
        };

        if (string.IsNullOrWhiteSpace(contract.ContractNumber))
            contract.ContractNumber = await GenerateContractNumberAsync(ct);

        RealEstateContractMapper.ApplyAmounts(contract);
        Db.RealEstateContracts.Add(contract);
        await Db.SaveChangesAsync(ct);

        return Ok(new { syncId = contract.SyncId });
    }

    [HttpPut("{syncId:guid}")]
    public async Task<IActionResult> UpdateContract(Guid syncId, [FromBody] UpdateRealEstateContractRequest request, CancellationToken ct)
    {
        if (await EnsureRealEstateTenantAsync(ct) is { } err) return err;

        var contract = await Db.RealEstateContracts
            .FirstOrDefaultAsync(c => c.TenantId == TenantId && c.SyncId == syncId, ct);
        if (contract is null) return NotFound();

        contract.ContractDate = request.ContractDate;
        contract.ContractType = request.ContractType;
        contract.PropertyType = request.PropertyType;
        contract.PropertyLocation = request.PropertyLocation ?? string.Empty;
        contract.PropertyAddress = request.PropertyAddress ?? string.Empty;
        contract.PropertyAreaSqm = request.PropertyAreaSqm;
        contract.PropertyDescription = request.PropertyDescription ?? string.Empty;
        contract.SellerName = request.SellerName;
        contract.SellerAddress = request.SellerAddress ?? string.Empty;
        contract.SellerIdNumber = request.SellerIdNumber ?? string.Empty;
        contract.SellerIdDate = request.SellerIdDate;
        contract.SellerPhone = request.SellerPhone ?? string.Empty;
        contract.BuyerName = request.BuyerName;
        contract.BuyerAddress = request.BuyerAddress ?? string.Empty;
        contract.BuyerIdNumber = request.BuyerIdNumber ?? string.Empty;
        contract.BuyerIdDate = request.BuyerIdDate;
        contract.BuyerPhone = request.BuyerPhone ?? string.Empty;
        contract.TotalPrice = request.TotalPrice;
        contract.DownPayment = request.DownPayment;
        contract.AmountPaid = request.AmountPaid;
        contract.PaymentMode = request.PaymentMode;
        contract.DebtorParty = request.DebtorParty;
        contract.DueDate = request.DueDate;
        contract.WitnessOneName = request.WitnessOneName ?? string.Empty;
        contract.WitnessTwoName = request.WitnessTwoName ?? string.Empty;
        contract.Notes = request.Notes ?? string.Empty;
        contract.UpdatedAt = DateTime.UtcNow;
        contract.UpdatedBy = User.Identity?.Name ?? "mobile";

        RealEstateContractMapper.ApplyAmounts(contract);
        await Db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpDelete("{syncId:guid}")]
    public async Task<IActionResult> DeleteContract(Guid syncId, CancellationToken ct)
    {
        if (await EnsureRealEstateTenantAsync(ct) is { } err) return err;

        var contract = await Db.RealEstateContracts
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
    public async Task<ActionResult<object>> RecordPayment(Guid syncId, [FromBody] RealEstatePaymentRequest request, CancellationToken ct)
    {
        if (await EnsureRealEstateTenantAsync(ct) is { } err) return err;
        if (request.Amount <= 0)
            return BadRequest("Payment amount must be greater than zero.");

        var contract = await Db.RealEstateContracts
            .FirstOrDefaultAsync(c => c.TenantId == TenantId && c.SyncId == syncId, ct);
        if (contract is null) return NotFound();
        if (contract.Status == RealEstateContractStatus.Cancelled)
            return BadRequest("Cannot record payment on a cancelled contract.");
        if (request.Amount > contract.RemainingAmount)
            return BadRequest("Payment amount exceeds remaining balance.");

        var remainingBefore = contract.RemainingAmount;
        contract.AmountPaid += request.Amount;
        contract.RemainingAmount = Math.Max(0, contract.TotalPrice - contract.AmountPaid);
        RealEstateContractMapper.UpdateStatus(contract);
        contract.UpdatedAt = DateTime.UtcNow;
        contract.UpdatedBy = User.Identity?.Name ?? "mobile";

        var payment = new CloudRealEstateContractPayment
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
        Db.RealEstateContractPayments.Add(payment);
        await Db.SaveChangesAsync(ct);

        return Ok(new { payment.SyncId });
    }

    private async Task<string> GenerateContractNumberAsync(CancellationToken ct)
    {
        var year = DateTime.Today.Year;
        var prefix = $"RE-{year}-";
        var last = await Db.RealEstateContracts.IgnoreQueryFilters()
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

public class CreateRealEstateContractRequest
{
    public string ContractNumber { get; set; } = string.Empty;
    public DateTime ContractDate { get; set; } = DateTime.Today;
    public RealEstateContractType ContractType { get; set; } = RealEstateContractType.Sale;
    public RealEstatePropertyType PropertyType { get; set; } = RealEstatePropertyType.House;
    public string? PropertyLocation { get; set; }
    public string? PropertyAddress { get; set; }
    public decimal PropertyAreaSqm { get; set; }
    public string? PropertyDescription { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public string? SellerAddress { get; set; }
    public string? SellerIdNumber { get; set; }
    public DateTime? SellerIdDate { get; set; }
    public string? SellerPhone { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string? BuyerAddress { get; set; }
    public string? BuyerIdNumber { get; set; }
    public DateTime? BuyerIdDate { get; set; }
    public string? BuyerPhone { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal DownPayment { get; set; }
    public decimal AmountPaid { get; set; }
    public RealEstatePaymentMode PaymentMode { get; set; } = RealEstatePaymentMode.Cash;
    public RealEstateDebtorParty DebtorParty { get; set; } = RealEstateDebtorParty.None;
    public DateTime? DueDate { get; set; }
    public string? WitnessOneName { get; set; }
    public string? WitnessTwoName { get; set; }
    public string? Notes { get; set; }
}

public sealed class UpdateRealEstateContractRequest : CreateRealEstateContractRequest;

public sealed class RealEstatePaymentRequest
{
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public string? Notes { get; set; }
}

public class RealEstateContractListDto
{
    public Guid SyncId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public DateTime ContractDate { get; set; }
    public string ContractType { get; set; } = string.Empty;
    public string PropertyType { get; set; } = string.Empty;
    public string PropertyLocation { get; set; } = string.Empty;
    public decimal PropertyAreaSqm { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public string BuyerName { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RemainingAmount { get; set; }
    public string PaymentMode { get; set; } = string.Empty;
    public string DebtorParty { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class RealEstateContractDetailDto : RealEstateContractListDto
{
    public string PropertyAddress { get; set; } = string.Empty;
    public string PropertyDescription { get; set; } = string.Empty;
    public string SellerPhone { get; set; } = string.Empty;
    public string SellerAddress { get; set; } = string.Empty;
    public string SellerIdNumber { get; set; } = string.Empty;
    public string BuyerPhone { get; set; } = string.Empty;
    public string BuyerAddress { get; set; } = string.Empty;
    public string BuyerIdNumber { get; set; } = string.Empty;
    public decimal DownPayment { get; set; }
    public string TotalPriceInWords { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public List<RealEstatePaymentDto> Payments { get; set; } = [];
    public List<RealEstateClauseDto> Clauses { get; set; } = [];
}

public sealed class RealEstatePaymentDto
{
    public Guid SyncId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class RealEstateClauseDto
{
    public Guid SyncId { get; set; }
    public int SortOrder { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

internal static class RealEstateContractMapper
{
    public static RealEstateContractListDto ToListItem(CloudRealEstateContract c) => new()
    {
        SyncId = c.SyncId,
        ContractNumber = c.ContractNumber,
        ContractDate = c.ContractDate,
        ContractType = c.ContractType.ToString(),
        PropertyType = c.PropertyType.ToString(),
        PropertyLocation = c.PropertyLocation,
        PropertyAreaSqm = c.PropertyAreaSqm,
        SellerName = c.SellerName,
        BuyerName = c.BuyerName,
        TotalPrice = c.TotalPrice,
        AmountPaid = c.AmountPaid,
        RemainingAmount = c.RemainingAmount,
        PaymentMode = c.PaymentMode.ToString(),
        DebtorParty = c.DebtorParty.ToString(),
        DueDate = c.DueDate,
        Status = c.Status.ToString()
    };

    public static RealEstateContractDetailDto ToDetail(CloudRealEstateContract c)
    {
        var list = ToListItem(c);
        return new RealEstateContractDetailDto
        {
            SyncId = list.SyncId,
            ContractNumber = list.ContractNumber,
            ContractDate = list.ContractDate,
            ContractType = list.ContractType,
            PropertyType = list.PropertyType,
            PropertyLocation = list.PropertyLocation,
            PropertyAreaSqm = list.PropertyAreaSqm,
            SellerName = list.SellerName,
            BuyerName = list.BuyerName,
            TotalPrice = list.TotalPrice,
            AmountPaid = list.AmountPaid,
            RemainingAmount = list.RemainingAmount,
            PaymentMode = list.PaymentMode,
            DebtorParty = list.DebtorParty,
            DueDate = list.DueDate,
            Status = list.Status,
            PropertyAddress = c.PropertyAddress,
            PropertyDescription = c.PropertyDescription,
            SellerPhone = c.SellerPhone,
            SellerAddress = c.SellerAddress,
            SellerIdNumber = c.SellerIdNumber,
            BuyerPhone = c.BuyerPhone,
            BuyerAddress = c.BuyerAddress,
            BuyerIdNumber = c.BuyerIdNumber,
            DownPayment = c.DownPayment,
            TotalPriceInWords = c.TotalPriceInWords,
            Notes = c.Notes,
            Payments = c.Payments
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new RealEstatePaymentDto
                {
                    SyncId = p.SyncId,
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate,
                    Notes = p.Notes
                })
                .ToList(),
            Clauses = c.Clauses
                .Where(cl => !cl.IsDeleted)
                .OrderBy(cl => cl.SortOrder)
                .Select(cl => new RealEstateClauseDto
                {
                    SyncId = cl.SyncId,
                    SortOrder = cl.SortOrder,
                    Title = cl.Title,
                    Body = cl.Body
                })
                .ToList()
        };
    }

    public static void ApplyAmounts(CloudRealEstateContract contract)
    {
        if (contract.PaymentMode == RealEstatePaymentMode.Cash)
        {
            contract.DebtorParty = RealEstateDebtorParty.None;
            if (contract.AmountPaid <= 0 && contract.DownPayment > 0)
                contract.AmountPaid = contract.DownPayment;
            if (contract.AmountPaid <= 0)
                contract.AmountPaid = contract.TotalPrice;
        }
        else
        {
            if (contract.DebtorParty == RealEstateDebtorParty.None)
                contract.DebtorParty = RealEstateDebtorParty.Buyer;
            if (contract.AmountPaid <= 0 && contract.DownPayment > 0)
                contract.AmountPaid = contract.DownPayment;
        }

        contract.RemainingAmount = Math.Max(0, contract.TotalPrice - contract.AmountPaid);
        contract.TotalPriceInWords = ArabicAmountToWords.Convert(contract.TotalPrice, "دينار", "فلس");
        UpdateStatus(contract);
    }

    public static void UpdateStatus(CloudRealEstateContract contract)
    {
        if (contract.Status == RealEstateContractStatus.Cancelled)
            return;

        contract.Status = contract.RemainingAmount <= 0
            ? RealEstateContractStatus.Completed
            : RealEstateContractStatus.Active;
    }
}
