using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core.Enums.Gold;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Cloud.Infrastructure.Services.Gold;

public sealed class CloudGoldSaleHelper
{
    private readonly CloudDbContext _db;

    public CloudGoldSaleHelper(CloudDbContext db)
    {
        _db = db;
    }

    public async Task<CloudGoldInvoice> CreateSaleAsync(
        int tenantId,
        CloudGoldCreateSaleRequest request,
        string createdBy,
        CancellationToken ct = default)
    {
        if (request.Lines is null || request.Lines.Count == 0)
            throw new InvalidOperationException("At least one invoice line is required.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var settings = await _db.GoldSettings
                .FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);
            var mithqalGrams = settings?.MithqalGrams > 0 ? settings.MithqalGrams : 5m;

            var fx = request.FxRate > 0
                ? request.FxRate
                : (await _db.GoldFxRates
                    .Where(r => r.TenantId == tenantId)
                    .OrderByDescending(r => r.RateDate)
                    .ThenByDescending(r => r.Id)
                    .Select(r => (decimal?)r.UsdToIqd)
                    .FirstOrDefaultAsync(ct)) ?? 1m;

            CloudGoldCustomer? customer = null;
            if (request.CustomerId.HasValue)
            {
                customer = await _db.GoldCustomers
                    .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == request.CustomerId.Value, ct)
                    ?? throw new InvalidOperationException("Customer not found.");
            }
            else if (request.PaymentMethod == GoldPaymentMethod.Credit)
            {
                throw new InvalidOperationException("Credit sales require a customer.");
            }

            var warehouseId = await ResolveWarehouseIdAsync(tenantId, request.WarehouseId, ct);

            if (request.SupplierId.HasValue)
            {
                var supplierExists = await _db.GoldSuppliers
                    .AnyAsync(s => s.TenantId == tenantId && s.Id == request.SupplierId.Value, ct);
                if (!supplierExists)
                    throw new InvalidOperationException("Supplier not found.");
            }

            var invoice = new CloudGoldInvoice
            {
                TenantId = tenantId,
                SyncId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                InvoiceNumber = await NextInvoiceNumberAsync(tenantId, ct),
                InvoiceDate = (request.InvoiceDate == default ? DateTime.Today : request.InvoiceDate).Date,
                InvoiceType = GoldInvoiceType.Sale,
                PaymentMethod = request.PaymentMethod,
                CustomerId = request.CustomerId,
                SupplierId = request.SupplierId,
                WarehouseId = warehouseId,
                PricingCurrency = request.PricingCurrency,
                PaymentCurrency = request.PaymentCurrency,
                FxRate = fx,
                DiscountAmount = Math.Max(0, request.DiscountAmount),
                Notes = request.Notes ?? string.Empty,
                WeightFromScale = request.WeightFromScale,
                CashBoxId = request.CashBoxId
            };

            decimal totalGold = 0, totalMaking = 0, totalWeight = 0;

            foreach (var lineReq in request.Lines)
            {
                if (lineReq.WeightGrams <= 0)
                    throw new InvalidOperationException("Line weight must be greater than zero.");
                if (lineReq.MithqalPrice <= 0)
                    throw new InvalidOperationException("Mithqal price must be greater than zero.");

                var pricePerGram = Round(lineReq.MithqalPrice / mithqalGrams, 6);
                var goldValue = Round(lineReq.WeightGrams * pricePerGram);
                var making = Math.Max(0, lineReq.MakingCharge);
                var lineTotal = Round(goldValue + making);

                if (lineReq.ItemId.HasValue)
                {
                    var item = await _db.GoldItems
                        .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.Id == lineReq.ItemId.Value, ct)
                        ?? throw new InvalidOperationException($"Item {lineReq.ItemId} not found.");

                    if (item.Status != GoldItemStatus.InStock)
                        throw new InvalidOperationException($"Item «{item.Name}» is not available for sale.");

                    if (item.TrackAsPiece)
                        item.Status = GoldItemStatus.Sold;
                }

                await AdjustStockAsync(tenantId, warehouseId, lineReq.KaratValue, -lineReq.WeightGrams, createdBy, ct);

                invoice.Lines.Add(new CloudGoldInvoiceLine
                {
                    TenantId = tenantId,
                    SyncId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy,
                    ItemId = lineReq.ItemId,
                    KaratValue = lineReq.KaratValue,
                    WeightGrams = lineReq.WeightGrams,
                    MithqalPrice = lineReq.MithqalPrice,
                    PricePerGram = pricePerGram,
                    GoldValue = goldValue,
                    MakingCharge = making,
                    LineTotal = lineTotal,
                    Description = string.IsNullOrWhiteSpace(lineReq.Description)
                        ? $"عيار {lineReq.KaratValue}"
                        : lineReq.Description,
                    WeightFromScale = lineReq.WeightFromScale,
                    LineDirection = GoldInvoiceLineDirection.Out
                });

                totalGold += goldValue;
                totalMaking += making;
                totalWeight += lineReq.WeightGrams;
            }

            invoice.TotalGoldValue = Round(totalGold);
            invoice.TotalMakingCharge = Round(totalMaking);
            invoice.TotalWeightGrams = Round(totalWeight);
            invoice.TotalAmount = Round(invoice.TotalGoldValue + invoice.TotalMakingCharge - invoice.DiscountAmount);
            if (invoice.TotalAmount < 0)
                invoice.TotalAmount = 0;

            ApplyDualTotals(invoice);

            var paidInPricing = ConvertAmount(
                Math.Max(0, request.PaidAmount),
                request.PaymentCurrency,
                request.PricingCurrency,
                fx);

            if (request.PaymentMethod == GoldPaymentMethod.Cash && paidInPricing <= 0)
                paidInPricing = invoice.TotalAmount;

            if (paidInPricing > invoice.TotalAmount)
                paidInPricing = invoice.TotalAmount;

            invoice.PaidAmount = Round(paidInPricing);
            invoice.RemainingAmount = Round(invoice.TotalAmount - invoice.PaidAmount);
            invoice.Status = ResolveStatus(invoice.TotalAmount, invoice.PaidAmount, request.PaymentMethod);

            if (invoice.PaidAmount > 0)
            {
                var paidInPaymentCurrency = ConvertAmount(
                    invoice.PaidAmount,
                    request.PricingCurrency,
                    request.PaymentCurrency,
                    fx);

                var cashBox = await ResolveCashBoxAsync(tenantId, request.CashBoxId, request.PaymentCurrency, createdBy, ct);
                invoice.CashBoxId = cashBox.Id;
                cashBox.Balance = Round(cashBox.Balance + paidInPaymentCurrency);
                cashBox.UpdatedAt = DateTime.UtcNow;
                cashBox.UpdatedBy = createdBy;

                invoice.Payments.Add(new CloudGoldPayment
                {
                    TenantId = tenantId,
                    SyncId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy,
                    PaymentDate = invoice.InvoiceDate,
                    Amount = Round(paidInPaymentCurrency),
                    Currency = request.PaymentCurrency,
                    FxRate = fx,
                    CashBoxId = cashBox.Id,
                    Notes = "دفعة عند البيع"
                });
            }

            if (invoice.RemainingAmount > 0)
            {
                if (customer is null)
                    throw new InvalidOperationException("Remaining balance requires a customer.");

                var credit = ConvertAmount(
                    invoice.RemainingAmount,
                    request.PricingCurrency,
                    request.PaymentCurrency,
                    fx);
                AdjustCredit(customer, request.PaymentCurrency, credit);
                customer.UpdatedAt = DateTime.UtcNow;
                customer.UpdatedBy = createdBy;
            }

            _db.GoldInvoices.Add(invoice);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return await _db.GoldInvoices
                .AsNoTracking()
                .Include(i => i.Customer)
                .Include(i => i.Lines)
                .Include(i => i.Payments)
                .FirstAsync(i => i.Id == invoice.Id, ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<string> NextInvoiceNumberAsync(int tenantId, CancellationToken ct)
    {
        var prefix = $"GS-{DateTime.Today:yyyyMM}-";
        var last = await _db.GoldInvoices
            .Where(i => i.TenantId == tenantId && i.InvoiceType == GoldInvoiceType.Sale && i.InvoiceNumber.StartsWith(prefix))
            .OrderByDescending(i => i.InvoiceNumber)
            .Select(i => i.InvoiceNumber)
            .FirstOrDefaultAsync(ct);

        var next = 1;
        if (!string.IsNullOrEmpty(last) && last.Length > prefix.Length
            && int.TryParse(last[prefix.Length..], out var n))
            next = n + 1;

        return $"{prefix}{next:D4}";
    }

    private async Task<int> ResolveWarehouseIdAsync(int tenantId, int? warehouseId, CancellationToken ct)
    {
        if (warehouseId.HasValue)
        {
            var exists = await _db.GoldWarehouses
                .AnyAsync(w => w.TenantId == tenantId && w.Id == warehouseId.Value && w.IsActive, ct);
            if (!exists)
                throw new InvalidOperationException("Warehouse not found.");
            return warehouseId.Value;
        }

        var def = await _db.GoldWarehouses
            .Where(w => w.TenantId == tenantId && w.IsActive)
            .OrderByDescending(w => w.IsDefault)
            .ThenBy(w => w.Id)
            .FirstOrDefaultAsync(ct);

        if (def is not null)
            return def.Id;

        var created = new CloudGoldWarehouse
        {
            TenantId = tenantId,
            SyncId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system",
            Name = "المخزن الرئيسي",
            IsDefault = true,
            IsActive = true
        };
        _db.GoldWarehouses.Add(created);
        await _db.SaveChangesAsync(ct);
        return created.Id;
    }

    private async Task AdjustStockAsync(
        int tenantId, int warehouseId, int karatValue, decimal gramsDelta, string username, CancellationToken ct)
    {
        var balance = await _db.GoldStockBalances
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.WarehouseId == warehouseId && s.KaratValue == karatValue, ct);

        if (balance is null)
        {
            balance = new CloudGoldStockBalance
            {
                TenantId = tenantId,
                SyncId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = username,
                WarehouseId = warehouseId,
                KaratValue = karatValue,
                GramsOnHand = 0,
                AverageCostPerGram = 0
            };
            _db.GoldStockBalances.Add(balance);
        }

        balance.GramsOnHand = Round(balance.GramsOnHand + gramsDelta, 4);
        if (balance.GramsOnHand < 0)
            balance.GramsOnHand = 0;
        balance.UpdatedAt = DateTime.UtcNow;
        balance.UpdatedBy = username;
    }

    private async Task<CloudGoldCashBox> ResolveCashBoxAsync(
        int tenantId, int? cashBoxId, GoldCurrency currency, string username, CancellationToken ct)
    {
        if (cashBoxId.HasValue)
        {
            return await _db.GoldCashBoxes
                .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == cashBoxId.Value, ct)
                ?? throw new InvalidOperationException("Cash box not found.");
        }

        var box = await _db.GoldCashBoxes
            .Where(c => c.TenantId == tenantId && c.IsActive && c.Currency == currency)
            .OrderByDescending(c => c.IsDefault)
            .ThenBy(c => c.Id)
            .FirstOrDefaultAsync(ct);

        if (box is not null)
            return box;

        box = new CloudGoldCashBox
        {
            TenantId = tenantId,
            SyncId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = username,
            Name = currency == GoldCurrency.USD ? "قاصة دولار" : "قاصة دينار",
            Currency = currency,
            IsDefault = true,
            IsActive = true,
            Balance = 0
        };
        _db.GoldCashBoxes.Add(box);
        await _db.SaveChangesAsync(ct);
        return box;
    }

    private static void AdjustCredit(CloudGoldCustomer customer, GoldCurrency currency, decimal delta)
    {
        if (currency == GoldCurrency.USD)
            customer.CreditBalanceUsd = Round(customer.CreditBalanceUsd + delta);
        else
            customer.CreditBalanceIqd = Round(customer.CreditBalanceIqd + delta);
    }

    private static void ApplyDualTotals(CloudGoldInvoice invoice)
    {
        var fx = invoice.FxRate <= 0 ? 1m : invoice.FxRate;
        if (invoice.PricingCurrency == GoldCurrency.USD)
        {
            invoice.TotalAmountUsd = invoice.TotalAmount;
            invoice.TotalAmountIqd = Round(invoice.TotalAmount * fx);
        }
        else
        {
            invoice.TotalAmountIqd = invoice.TotalAmount;
            invoice.TotalAmountUsd = Round(invoice.TotalAmount / fx);
        }
    }

    private static decimal ConvertAmount(decimal amount, GoldCurrency from, GoldCurrency to, decimal fxRate)
    {
        if (from == to) return amount;
        var fx = fxRate <= 0 ? 1m : fxRate;
        return from == GoldCurrency.USD ? amount * fx : amount / fx;
    }

    private static GoldInvoiceStatus ResolveStatus(decimal totalAmount, decimal paidAmount, GoldPaymentMethod method)
    {
        if (paidAmount <= 0)
            return method == GoldPaymentMethod.Credit ? GoldInvoiceStatus.Open : GoldInvoiceStatus.Completed;
        if (paidAmount + 0.0001m >= totalAmount)
            return GoldInvoiceStatus.Completed;
        return GoldInvoiceStatus.PartiallyPaid;
    }

    private static decimal Round(decimal value, int decimals = 4) =>
        Math.Round(value, decimals, MidpointRounding.AwayFromZero);
}

public sealed class CloudGoldCreateSaleRequest
{
    public DateTime InvoiceDate { get; set; } = DateTime.Today;
    public GoldPaymentMethod PaymentMethod { get; set; } = GoldPaymentMethod.Cash;
    public int? CustomerId { get; set; }
    public int? SupplierId { get; set; }
    public int? WarehouseId { get; set; }
    public GoldCurrency PricingCurrency { get; set; } = GoldCurrency.USD;
    public GoldCurrency PaymentCurrency { get; set; } = GoldCurrency.IQD;
    public decimal FxRate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public int? CashBoxId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool WeightFromScale { get; set; }
    public List<CloudGoldCreateSaleLineRequest> Lines { get; set; } = [];
}

public sealed class CloudGoldCreateSaleLineRequest
{
    public int? ItemId { get; set; }
    public int KaratValue { get; set; }
    public decimal WeightGrams { get; set; }
    public decimal MithqalPrice { get; set; }
    public decimal MakingCharge { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool WeightFromScale { get; set; }
}
