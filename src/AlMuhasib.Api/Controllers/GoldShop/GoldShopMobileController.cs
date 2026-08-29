using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Cloud.Infrastructure.Services.Gold;
using AlMuhasib.Core.Enums.Gold;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers.GoldShop;

[ApiController]
[Route("api/gold-shop")]
[Authorize(Policy = "Tenant")]
public sealed class GoldShopMobileController : GoldShopApiControllerBase
{
    private readonly CloudGoldSaleHelper _saleHelper;
    private readonly CloudGoldOpsHelper _opsHelper;

    public GoldShopMobileController(
        ITenantContext tenantContext,
        CloudDbContext db,
        CloudGoldSaleHelper saleHelper,
        CloudGoldOpsHelper opsHelper) : base(db, tenantContext)
    {
        _saleHelper = saleHelper;
        _opsHelper = opsHelper;
    }

    // Also exposed under /mobile/* for Flutter clients.
    [HttpGet("dashboard")]
    [HttpGet("mobile/dashboard")]
    public async Task<ActionResult<GoldShopDashboardDto>> GetDashboard(CancellationToken ct)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;

        var today = DateTime.Today;
        var invoices = await Db.GoldInvoices.AsNoTracking()
            .Include(i => i.Customer)
            .Where(i => i.TenantId == TenantId && i.Status != GoldInvoiceStatus.Cancelled)
            .ToListAsync(ct);

        var todaySales = invoices.Where(i => i.InvoiceType == GoldInvoiceType.Sale && i.InvoiceDate.Date == today).ToList();
        var todayPurchases = invoices.Where(i => i.InvoiceType == GoldInvoiceType.Purchase && i.InvoiceDate.Date == today).ToList();
        var todayReturns = invoices.Where(i => i.InvoiceType == GoldInvoiceType.SaleReturn && i.InvoiceDate.Date == today).ToList();
        var todayExchanges = invoices.Where(i => i.InvoiceType == GoldInvoiceType.Exchange && i.InvoiceDate.Date == today).ToList();
        var credit = invoices.Where(i => i.RemainingAmount > 0 && i.InvoiceType == GoldInvoiceType.Sale).ToList();

        var cashBoxes = await Db.GoldCashBoxes.AsNoTracking()
            .Where(c => c.TenantId == TenantId && c.IsActive).ToListAsync(ct);
        var stocks = await Db.GoldStockBalances.AsNoTracking()
            .Where(s => s.TenantId == TenantId).ToListAsync(ct);
        var settings = await Db.GoldSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == TenantId, ct);
        var lowThreshold = settings?.LowStockAlertGrams ?? 10m;
        var suppliers = await Db.GoldSuppliers.AsNoTracking()
            .Where(s => s.TenantId == TenantId && (s.CreditBalanceIqd > 0 || s.CreditBalanceUsd > 0))
            .ToListAsync(ct);
        var todayExpenses = await Db.GoldExpenses.AsNoTracking()
            .Where(e => e.TenantId == TenantId && e.ExpenseDate.Date == today)
            .ToListAsync(ct);

        var latestFx = await Db.GoldFxRates.AsNoTracking()
            .Where(r => r.TenantId == TenantId)
            .OrderByDescending(r => r.RateDate)
            .ThenByDescending(r => r.Id)
            .FirstOrDefaultAsync(ct);

        var latestPrices = await GetLatestPricesAsync(ct);
        var pricesUpdatedToday = latestPrices.Any(p => p.PriceDate.Date == today);

        var notifications = await Db.GoldNotifications.AsNoTracking()
            .Where(n => n.TenantId == TenantId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Take(10)
            .ToListAsync(ct);

        return Ok(new GoldShopDashboardDto
        {
            TodaySalesIqd = todaySales.Sum(i => i.TotalAmountIqd),
            TodaySalesUsd = todaySales.Sum(i => i.TotalAmountUsd),
            TodayPurchasesIqd = todayPurchases.Sum(i => i.TotalAmountIqd),
            TodayPurchasesUsd = todayPurchases.Sum(i => i.TotalAmountUsd),
            CashBalanceIqd = cashBoxes.Where(c => c.Currency == GoldCurrency.IQD).Sum(c => c.Balance),
            CashBalanceUsd = cashBoxes.Where(c => c.Currency == GoldCurrency.USD).Sum(c => c.Balance),
            TotalStockGrams = stocks.Sum(s => s.GramsOnHand),
            TotalStockValueIqd = stocks.Sum(s => s.GramsOnHand * s.AverageCostPerGram),
            OpenCreditCount = credit.Count,
            OpenCreditIqd = credit.Sum(i => i.RemainingAmount > 0 && i.PaymentCurrency == GoldCurrency.IQD
                ? i.RemainingAmount
                : (i.FxRate > 0 ? i.RemainingAmount * i.FxRate : 0)),
            OpenCreditUsd = credit.Sum(i => i.PaymentCurrency == GoldCurrency.USD
                ? i.RemainingAmount
                : (i.FxRate > 0 ? i.RemainingAmount / i.FxRate : 0)),
            OverdueCreditCount = credit.Count(i =>
                (today - i.InvoiceDate.Date).TotalDays >= (settings?.OverdueDaysThreshold ?? 30)),
            LowStockKaratCount = stocks.Count(s => s.GramsOnHand <= lowThreshold),
            PricesUpdatedToday = pricesUpdatedToday,
            LatestUsdToIqd = latestFx?.UsdToIqd,
            MithqalGrams = settings?.MithqalGrams > 0 ? settings.MithqalGrams : 5m,
            StockByKarat = stocks
                .GroupBy(s => s.KaratValue)
                .OrderByDescending(g => g.Key)
                .Select(g => new GoldStockRowDto
                {
                    KaratValue = g.Key,
                    KaratName = $"{g.Key}K",
                    GramsOnHand = g.Sum(s => s.GramsOnHand),
                    AverageCostPerGram = g.Sum(s => s.GramsOnHand) > 0
                        ? g.Sum(s => s.GramsOnHand * s.AverageCostPerGram) / g.Sum(s => s.GramsOnHand)
                        : 0,
                    StockValue = g.Sum(s => s.GramsOnHand * s.AverageCostPerGram),
                    IsLowStock = g.Sum(s => s.GramsOnHand) <= lowThreshold
                })
                .ToList(),
            LatestPrices = latestPrices,
            RecentInvoices = invoices
                .OrderByDescending(i => i.InvoiceDate)
                .ThenByDescending(i => i.Id)
                .Take(10)
                .Select(GoldShopInvoiceMapper.ToListItem)
                .ToList(),
            RecentReturns = todayReturns.Concat(
                    invoices.Where(i => i.InvoiceType == GoldInvoiceType.SaleReturn && i.InvoiceDate.Date != today))
                .OrderByDescending(i => i.InvoiceDate).Take(5)
                .Select(GoldShopInvoiceMapper.ToListItem).ToList(),
            RecentExchanges = todayExchanges.Concat(
                    invoices.Where(i => i.InvoiceType == GoldInvoiceType.Exchange && i.InvoiceDate.Date != today))
                .OrderByDescending(i => i.InvoiceDate).Take(5)
                .Select(GoldShopInvoiceMapper.ToListItem).ToList(),
            TodayReturnCount = todayReturns.Count,
            TodayReturnIqd = todayReturns.Sum(i => i.TotalAmountIqd),
            TodayExchangeCount = todayExchanges.Count,
            TodayExchangeCashDiffIqd = todayExchanges.Sum(i =>
                i.PaymentCurrency == GoldCurrency.IQD
                    ? i.ExchangeCashDifference
                    : (i.FxRate > 0 ? i.ExchangeCashDifference * i.FxRate : i.ExchangeCashDifference)),
            SupplierCreditCount = suppliers.Count,
            SupplierCreditIqd = suppliers.Sum(s => s.CreditBalanceIqd),
            SupplierCreditUsd = suppliers.Sum(s => s.CreditBalanceUsd),
            TodayExpensesIqd = todayExpenses.Where(e => e.Currency == GoldCurrency.IQD).Sum(e => e.Amount),
            TodayExpensesUsd = todayExpenses.Where(e => e.Currency == GoldCurrency.USD).Sum(e => e.Amount),
            HasExpenseToday = todayExpenses.Count > 0,
            Alerts = notifications.Select(n => new GoldAlertDto
            {
                Id = n.Id,
                SyncId = n.SyncId,
                Type = n.Type.ToString(),
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                RelatedEntity = n.RelatedEntity
            }).ToList()
        });
    }

    [HttpGet("prices")]
    [HttpGet("prices/latest")]
    [HttpGet("mobile/prices")]
    public async Task<ActionResult<List<GoldPriceDto>>> GetLatestPrices(CancellationToken ct)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;
        return Ok(await GetLatestPricesAsync(ct));
    }

    [HttpGet("warehouses")]
    [HttpGet("mobile/warehouses")]
    public async Task<ActionResult<List<GoldWarehouseDto>>> GetWarehouses(CancellationToken ct)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;

        var items = await Db.GoldWarehouses.AsNoTracking()
            .Where(w => w.TenantId == TenantId && w.IsActive)
            .OrderByDescending(w => w.IsDefault)
            .ThenBy(w => w.Name)
            .ToListAsync(ct);

        return Ok(items.Select(w => new GoldWarehouseDto
        {
            Id = w.Id,
            SyncId = w.SyncId,
            Name = w.Name,
            IsDefault = w.IsDefault,
            IsActive = w.IsActive,
            Notes = w.Notes
        }).ToList());
    }

    [HttpGet("suppliers")]
    [HttpGet("mobile/suppliers")]
    public async Task<ActionResult<List<GoldSupplierDto>>> GetSuppliers(
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;

        var query = Db.GoldSuppliers.AsNoTracking()
            .Where(s => s.TenantId == TenantId && s.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s => s.Name.Contains(term) || s.Phone.Contains(term));
        }

        var items = await query.OrderBy(s => s.Name).Take(200).ToListAsync(ct);
        return Ok(items.Select(s => new GoldSupplierDto
        {
            Id = s.Id,
            SyncId = s.SyncId,
            Name = s.Name,
            Phone = s.Phone,
            Address = s.Address,
            CreditBalanceIqd = s.CreditBalanceIqd,
            CreditBalanceUsd = s.CreditBalanceUsd,
            IsActive = s.IsActive
        }).ToList());
    }

    // Keep under /mobile/* only — GoldShopInvoicesController owns GET api/gold-shop/invoices.
    [HttpGet("mobile/invoices")]
    public async Task<ActionResult<List<GoldInvoiceListDto>>> GetInvoices(
        [FromQuery] string? search = null,
        [FromQuery] int? status = null,
        [FromQuery] int? invoiceType = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = Db.GoldInvoices.AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Supplier)
            .Where(i => i.TenantId == TenantId);

        if (invoiceType.HasValue && Enum.IsDefined(typeof(GoldInvoiceType), invoiceType.Value))
            query = query.Where(i => i.InvoiceType == (GoldInvoiceType)invoiceType.Value);
        if (status.HasValue && Enum.IsDefined(typeof(GoldInvoiceStatus), status.Value))
            query = query.Where(i => i.Status == (GoldInvoiceStatus)status.Value);
        if (from.HasValue)
            query = query.Where(i => i.InvoiceDate >= from.Value.Date);
        if (to.HasValue)
            query = query.Where(i => i.InvoiceDate <= to.Value.Date);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(i =>
                i.InvoiceNumber.Contains(term) ||
                (i.Customer != null && i.Customer.Name.Contains(term)) ||
                (i.Supplier != null && i.Supplier.Name.Contains(term)));
        }

        var items = await query
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Ok(items.Select(GoldShopInvoiceMapper.ToListItem).ToList());
    }

    [HttpGet("mobile/invoices/{id:int}")]
    public async Task<ActionResult<GoldInvoiceDetailDto>> GetInvoiceById(int id, CancellationToken ct)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;

        var invoice = await Db.GoldInvoices.AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.TenantId == TenantId && i.Id == id, ct);

        if (invoice is null)
            return NotFound();

        return Ok(GoldShopInvoiceMapper.ToDetail(invoice));
    }

    [HttpPost("mobile/invoices/sale")]
    public async Task<ActionResult<GoldInvoiceDetailDto>> CreateSale(
        [FromBody] GoldCreateSaleRequestDto request,
        CancellationToken ct)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;
        if (request.Lines is null || request.Lines.Count == 0)
            return BadRequest("At least one invoice line is required.");

        try
        {
            var username = User.Identity?.Name ?? User.FindFirst("sub")?.Value ?? "mobile";
            var paymentMethod = ParseEnum(request.PaymentMethod, GoldPaymentMethod.Cash);
            var pricingCurrency = ParseEnum(request.PricingCurrency, GoldCurrency.USD);
            var paymentCurrency = ParseEnum(request.PaymentCurrency, GoldCurrency.IQD);

            var created = await _saleHelper.CreateSaleAsync(
                TenantId,
                new CloudGoldCreateSaleRequest
                {
                    InvoiceDate = request.InvoiceDate == default ? DateTime.Today : request.InvoiceDate,
                    PaymentMethod = paymentMethod,
                    CustomerId = request.CustomerId,
                    SupplierId = request.SupplierId,
                    WarehouseId = request.WarehouseId,
                    PricingCurrency = pricingCurrency,
                    PaymentCurrency = paymentCurrency,
                    FxRate = request.FxRate,
                    DiscountAmount = request.DiscountAmount,
                    PaidAmount = request.PaidAmount,
                    CashBoxId = request.CashBoxId,
                    Notes = request.Notes ?? string.Empty,
                    WeightFromScale = request.WeightFromScale,
                    Lines = request.Lines.Select(l => new CloudGoldCreateSaleLineRequest
                    {
                        ItemId = l.ItemId,
                        KaratValue = l.KaratValue,
                        WeightGrams = l.WeightGrams,
                        MithqalPrice = l.MithqalPrice,
                        MakingCharge = l.MakingCharge,
                        Description = l.Description ?? string.Empty,
                        WeightFromScale = l.WeightFromScale
                    }).ToList()
                },
                username,
                ct);

            return Ok(GoldShopInvoiceMapper.ToDetail(created));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("mobile/invoices/purchase")]
    public async Task<ActionResult<GoldInvoiceDetailDto>> CreatePurchase(
        [FromBody] GoldCreateSaleRequestDto request, CancellationToken ct)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;
        try
        {
            var username = User.Identity?.Name ?? "mobile";
            var created = await _opsHelper.CreatePurchaseAsync(TenantId, MapSaleRequest(request), username, ct);
            return Ok(GoldShopInvoiceMapper.ToDetail(created));
        }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("mobile/invoices/sale-return")]
    public async Task<ActionResult<GoldInvoiceDetailDto>> CreateSaleReturn(
        [FromBody] GoldCreateSaleReturnRequestDto request, CancellationToken ct)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;
        try
        {
            var username = User.Identity?.Name ?? "mobile";
            var created = await _opsHelper.CreateSaleReturnAsync(
                TenantId, MapSaleRequest(request), request.RelatedInvoiceId, username, ct);
            return Ok(GoldShopInvoiceMapper.ToDetail(created));
        }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("mobile/invoices/exchange")]
    public async Task<ActionResult<GoldInvoiceDetailDto>> CreateExchange(
        [FromBody] GoldCreateExchangeRequestDto request, CancellationToken ct)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;
        try
        {
            var username = User.Identity?.Name ?? "mobile";
            var created = await _opsHelper.CreateExchangeAsync(TenantId, new CloudGoldCreateExchangeRequest
            {
                InvoiceDate = request.InvoiceDate == default ? DateTime.Today : request.InvoiceDate,
                PaymentMethod = ParseEnum(request.PaymentMethod, GoldPaymentMethod.Cash),
                CustomerId = request.CustomerId,
                WarehouseId = request.WarehouseId,
                PricingCurrency = ParseEnum(request.PricingCurrency, GoldCurrency.IQD),
                PaymentCurrency = ParseEnum(request.PaymentCurrency, GoldCurrency.IQD),
                FxRate = request.FxRate,
                ExchangeCashDifference = request.ExchangeCashDifference,
                PaidAmount = request.PaidAmount,
                CashBoxId = request.CashBoxId,
                Notes = request.Notes ?? string.Empty,
                WeightFromScale = request.WeightFromScale,
                InLines = (request.InLines ?? []).Select(MapLine).ToList(),
                OutLines = (request.OutLines ?? []).Select(MapLine).ToList()
            }, username, ct);
            return Ok(GoldShopInvoiceMapper.ToDetail(created));
        }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("mobile/invoices/collection")]
    public async Task<ActionResult<object>> Collect(
        [FromBody] GoldCollectionRequestDto request, CancellationToken ct)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;
        try
        {
            var username = User.Identity?.Name ?? "mobile";
            var payment = await _opsHelper.CollectAsync(TenantId, new CloudGoldCollectionRequest
            {
                InvoiceId = request.InvoiceId,
                Amount = request.Amount,
                Currency = ParseEnum(request.Currency, GoldCurrency.IQD),
                CashBoxId = request.CashBoxId,
                PaymentDate = request.PaymentDate == default ? DateTime.Today : request.PaymentDate,
                Notes = request.Notes ?? string.Empty
            }, username, ct);
            return Ok(new { payment.Id, payment.SyncId, payment.Amount, Currency = payment.Currency.ToString() });
        }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    private CloudGoldCreateSaleRequest MapSaleRequest(GoldCreateSaleRequestDto request) =>
        MapSaleRequestCore(
            request.InvoiceDate, request.PaymentMethod, request.CustomerId, request.SupplierId,
            request.WarehouseId, request.PricingCurrency, request.PaymentCurrency, request.FxRate,
            request.DiscountAmount, request.PaidAmount, request.CashBoxId, request.Notes,
            request.WeightFromScale, request.Lines);

    private CloudGoldCreateSaleRequest MapSaleRequest(GoldCreateSaleReturnRequestDto request) =>
        MapSaleRequestCore(
            request.InvoiceDate, request.PaymentMethod, request.CustomerId, request.SupplierId,
            request.WarehouseId, request.PricingCurrency, request.PaymentCurrency, request.FxRate,
            request.DiscountAmount, request.PaidAmount, request.CashBoxId, request.Notes,
            request.WeightFromScale, request.Lines);

    private CloudGoldCreateSaleRequest MapSaleRequestCore(
        DateTime invoiceDate, string paymentMethod, int? customerId, int? supplierId,
        int? warehouseId, string pricingCurrency, string paymentCurrency, decimal fxRate,
        decimal discountAmount, decimal paidAmount, int? cashBoxId, string? notes,
        bool weightFromScale, List<GoldCreateSaleLineDto>? lines) => new()
    {
        InvoiceDate = invoiceDate == default ? DateTime.Today : invoiceDate,
        PaymentMethod = ParseEnum(paymentMethod, GoldPaymentMethod.Cash),
        CustomerId = customerId,
        SupplierId = supplierId,
        WarehouseId = warehouseId,
        PricingCurrency = ParseEnum(pricingCurrency, GoldCurrency.IQD),
        PaymentCurrency = ParseEnum(paymentCurrency, GoldCurrency.IQD),
        FxRate = fxRate,
        DiscountAmount = discountAmount,
        PaidAmount = paidAmount,
        CashBoxId = cashBoxId,
        Notes = notes ?? string.Empty,
        WeightFromScale = weightFromScale,
        Lines = (lines ?? []).Select(MapLine).ToList()
    };

    private static CloudGoldCreateSaleLineRequest MapLine(GoldCreateSaleLineDto l) => new()
    {
        ItemId = l.ItemId,
        KaratValue = l.KaratValue,
        WeightGrams = l.WeightGrams,
        MithqalPrice = l.MithqalPrice,
        MakingCharge = l.MakingCharge,
        Description = l.Description ?? string.Empty,
        WeightFromScale = l.WeightFromScale
    };

    [HttpGet("customers")]
    [HttpGet("mobile/customers")]
    public async Task<ActionResult<List<GoldCustomerDto>>> GetCustomers(
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;

        var query = Db.GoldCustomers.AsNoTracking().Where(c => c.TenantId == TenantId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c => c.Name.Contains(term) || c.Phone.Contains(term));
        }

        var items = await query.OrderBy(c => c.Name).Take(200).ToListAsync(ct);
        return Ok(items.Select(c => new GoldCustomerDto
        {
            Id = c.Id,
            SyncId = c.SyncId,
            Name = c.Name,
            Phone = c.Phone,
            Address = c.Address,
            CreditBalanceIqd = c.CreditBalanceIqd,
            CreditBalanceUsd = c.CreditBalanceUsd,
            IsActive = c.IsActive
        }).ToList());
    }

    [HttpGet("notifications")]
    [HttpGet("mobile/notifications")]
    public async Task<ActionResult<List<GoldAlertDto>>> GetNotifications(
        [FromQuery] bool unreadOnly = false,
        CancellationToken ct = default)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;

        var query = Db.GoldNotifications.AsNoTracking().Where(n => n.TenantId == TenantId);
        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        var items = await query.OrderByDescending(n => n.CreatedAt).Take(100).ToListAsync(ct);
        return Ok(items.Select(n => new GoldAlertDto
        {
            Id = n.Id,
            SyncId = n.SyncId,
            Type = n.Type.ToString(),
            Title = n.Title,
            Message = n.Message,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt,
            RelatedEntity = n.RelatedEntity
        }).ToList());
    }

    private async Task<List<GoldPriceDto>> GetLatestPricesAsync(CancellationToken ct)
    {
        var prices = await Db.GoldMithqalPrices.AsNoTracking()
            .Where(p => p.TenantId == TenantId)
            .OrderByDescending(p => p.PriceDate)
            .ThenByDescending(p => p.Id)
            .ToListAsync(ct);

        return prices
            .GroupBy(p => p.KaratValue)
            .Select(g => g.First())
            .OrderByDescending(p => p.KaratValue)
            .Select(p => new GoldPriceDto
            {
                Id = p.Id,
                SyncId = p.SyncId,
                PriceDate = p.PriceDate,
                KaratValue = p.KaratValue,
                KaratName = $"{p.KaratValue}K",
                PricePerMithqal = p.PricePerMithqal,
                Currency = p.Currency.ToString(),
                FxRateUsed = p.FxRateUsed,
                PricePerGram = p.PricePerMithqal / 5m
            })
            .ToList();
    }

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;
        if (Enum.TryParse<TEnum>(value, true, out var parsed))
            return parsed;
        if (int.TryParse(value, out var asInt) && Enum.IsDefined(typeof(TEnum), asInt))
            return (TEnum)Enum.ToObject(typeof(TEnum), asInt);
        return fallback;
    }
}

public sealed class GoldShopDashboardDto
{
    public decimal TodaySalesIqd { get; set; }
    public decimal TodaySalesUsd { get; set; }
    public decimal TodayPurchasesIqd { get; set; }
    public decimal TodayPurchasesUsd { get; set; }
    public decimal CashBalanceIqd { get; set; }
    public decimal CashBalanceUsd { get; set; }
    public decimal TotalStockGrams { get; set; }
    public decimal TotalStockValueIqd { get; set; }
    public int OpenCreditCount { get; set; }
    public decimal OpenCreditIqd { get; set; }
    public decimal OpenCreditUsd { get; set; }
    public int OverdueCreditCount { get; set; }
    public int LowStockKaratCount { get; set; }
    public bool PricesUpdatedToday { get; set; }
    public decimal? LatestUsdToIqd { get; set; }
    public decimal MithqalGrams { get; set; } = 5m;
    public List<GoldStockRowDto> StockByKarat { get; set; } = [];
    public List<GoldPriceDto> LatestPrices { get; set; } = [];
    public List<GoldInvoiceListDto> RecentInvoices { get; set; } = [];
    public List<GoldInvoiceListDto> RecentReturns { get; set; } = [];
    public List<GoldInvoiceListDto> RecentExchanges { get; set; } = [];
    public int TodayReturnCount { get; set; }
    public decimal TodayReturnIqd { get; set; }
    public int TodayExchangeCount { get; set; }
    public decimal TodayExchangeCashDiffIqd { get; set; }
    public int SupplierCreditCount { get; set; }
    public decimal SupplierCreditIqd { get; set; }
    public decimal SupplierCreditUsd { get; set; }
    public decimal TodayExpensesIqd { get; set; }
    public decimal TodayExpensesUsd { get; set; }
    public bool HasExpenseToday { get; set; }
    public List<GoldAlertDto> Alerts { get; set; } = [];
}

public sealed class GoldStockRowDto
{
    public int KaratValue { get; set; }
    public string KaratName { get; set; } = string.Empty;
    public decimal GramsOnHand { get; set; }
    public decimal AverageCostPerGram { get; set; }
    public decimal StockValue { get; set; }
    public bool IsLowStock { get; set; }
    public int? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
}

public sealed class GoldPriceDto
{
    public int Id { get; set; }
    public Guid SyncId { get; set; }
    public DateTime PriceDate { get; set; }
    public int KaratValue { get; set; }
    public string KaratName { get; set; } = string.Empty;
    public decimal PricePerMithqal { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal? FxRateUsed { get; set; }
    public decimal? PricePerGram { get; set; }
}

public sealed class GoldCustomerDto
{
    public int Id { get; set; }
    public Guid SyncId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal CreditBalanceIqd { get; set; }
    public decimal CreditBalanceUsd { get; set; }
    public bool IsActive { get; set; }
}

public sealed class GoldSupplierDto
{
    public int Id { get; set; }
    public Guid SyncId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal CreditBalanceIqd { get; set; }
    public decimal CreditBalanceUsd { get; set; }
    public bool IsActive { get; set; }
}

public sealed class GoldWarehouseDto
{
    public int Id { get; set; }
    public Guid SyncId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class GoldAlertDto
{
    public int Id { get; set; }
    public Guid SyncId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? RelatedEntity { get; set; }
}

public sealed class GoldCreateSaleRequestDto
{
    public DateTime InvoiceDate { get; set; } = DateTime.Today;
    public string PaymentMethod { get; set; } = "Cash";
    public int? CustomerId { get; set; }
    public int? SupplierId { get; set; }
    public int? WarehouseId { get; set; }
    public string PricingCurrency { get; set; } = "USD";
    public string PaymentCurrency { get; set; } = "IQD";
    public decimal FxRate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public int? CashBoxId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool WeightFromScale { get; set; }
    public List<GoldCreateSaleLineDto> Lines { get; set; } = [];
}

public sealed class GoldCreateSaleLineDto
{
    public int? ItemId { get; set; }
    public int KaratValue { get; set; }
    public decimal WeightGrams { get; set; }
    public decimal MithqalPrice { get; set; }
    public decimal MakingCharge { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool WeightFromScale { get; set; }
}

public sealed class GoldCreateSaleReturnRequestDto
{
    public DateTime InvoiceDate { get; set; } = DateTime.Today;
    public string PaymentMethod { get; set; } = "Cash";
    public int? CustomerId { get; set; }
    public int? SupplierId { get; set; }
    public int? WarehouseId { get; set; }
    public string PricingCurrency { get; set; } = "IQD";
    public string PaymentCurrency { get; set; } = "IQD";
    public decimal FxRate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public int? CashBoxId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool WeightFromScale { get; set; }
    public int? RelatedInvoiceId { get; set; }
    public List<GoldCreateSaleLineDto>? Lines { get; set; }
}

public sealed class GoldCreateExchangeRequestDto
{
    public DateTime InvoiceDate { get; set; } = DateTime.Today;
    public string PaymentMethod { get; set; } = "Cash";
    public int? CustomerId { get; set; }
    public int? WarehouseId { get; set; }
    public string PricingCurrency { get; set; } = "IQD";
    public string PaymentCurrency { get; set; } = "IQD";
    public decimal FxRate { get; set; }
    public decimal ExchangeCashDifference { get; set; }
    public decimal PaidAmount { get; set; }
    public int? CashBoxId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool WeightFromScale { get; set; }
    public List<GoldCreateSaleLineDto>? InLines { get; set; }
    public List<GoldCreateSaleLineDto>? OutLines { get; set; }
}

public sealed class GoldCollectionRequestDto
{
    public int InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "IQD";
    public int? CashBoxId { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public string? Notes { get; set; }
}
