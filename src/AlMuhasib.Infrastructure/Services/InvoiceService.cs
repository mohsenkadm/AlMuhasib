using AlMuhasib.Core;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly LoyaltyService _loyaltyService;
    private readonly IAccountingPeriodLockService _periodLockService;

    public InvoiceService(
        IDbContextFactory<AppDbContext> contextFactory,
        ICurrentUserService currentUserService,
        LoyaltyService loyaltyService,
        IAccountingPeriodLockService periodLockService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
        _loyaltyService = loyaltyService;
        _periodLockService = periodLockService;
    }

    public async Task<Invoice> CreateInvoiceAsync(
        Invoice invoice,
        IEnumerable<InvoiceItem> items,
        bool skipStockUpdate = false,
        int loyaltyRedeemPoints = 0,
        bool applyLoyalty = false)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            await _periodLockService.EnsureDateAllowedAsync(invoice.Date);

            var username = _currentUserService.Username;
            invoice.CreatedBy = username;
            invoice.CreatedAt = DateTime.UtcNow;

            var itemsList = items.ToList();
            decimal subtotal = 0m;
            foreach (var item in itemsList)
            {
                if (item.DiscountAmount < 0m)
                    item.DiscountAmount = 0m;

                var gross = item.Quantity * item.UnitPrice;
                var maxDiscount = Math.Abs(gross);
                if (item.DiscountAmount > maxDiscount)
                    item.DiscountAmount = maxDiscount;

                item.TotalPrice = ProductDiscountHelper.CalculateLineTotal(
                    item.Quantity, item.UnitPrice, item.DiscountAmount);
                item.CreatedBy = username;
                item.CreatedAt = DateTime.UtcNow;
                subtotal += item.TotalPrice;
            }

            invoice.TotalAmount = subtotal;
            if (invoice.LoyaltyRedeemDiscountAmount < 0m)
                invoice.LoyaltyRedeemDiscountAmount = 0m;
            if (invoice.DiscountAmount < 0m)
                invoice.DiscountAmount = 0m;

            // دمج/التحقق من خصم الولاء قبل احتساب الصافي والقاصة
            if (applyLoyalty && (loyaltyRedeemPoints > 0 || invoice.LoyaltyRedeemDiscountAmount > 0m))
            {
                await _loyaltyService.PrepareInvoiceRedeemDiscountAsync(
                    context, invoice, loyaltyRedeemPoints, CancellationToken.None);
            }
            else if (!applyLoyalty)
            {
                invoice.LoyaltyPointsEarned = 0;
                invoice.LoyaltyPointsRedeemed = 0;
                invoice.LoyaltyRedeemDiscountAmount = 0m;
            }

            if (invoice.DiscountAmount > Math.Max(0m, subtotal))
                invoice.DiscountAmount = Math.Max(0m, subtotal);
            if (invoice.TransportFeeAmount < 0m)
                invoice.TransportFeeAmount = 0m;
            decimal netAmount = subtotal - invoice.DiscountAmount;

            decimal roundingAmount = CalculateRounding(netAmount, invoice.InvoiceType);
            invoice.RoundingAmount = roundingAmount;
            invoice.RoundingType = invoice.InvoiceType is InvoiceType.Purchase or InvoiceType.PurchaseReturn
                ? RoundingType.RoundUp
                : RoundingType.RoundDown;
            invoice.NetAmount = netAmount + roundingAmount + invoice.TransportFeeAmount;

            // Initialize credit payment tracking (supports down-payment on credit)
            if (invoice.PaymentMethod == PaymentMethod.Credit)
            {
                var downPayment = Math.Clamp(invoice.PaidAmount, 0m, invoice.NetAmount);
                invoice.PaidAmount = downPayment;
                invoice.RemainingAmount = invoice.NetAmount - downPayment;
                invoice.IsCreditPaid = invoice.RemainingAmount <= 0;
            }
            else
            {
                invoice.PaidAmount = invoice.NetAmount;
                invoice.RemainingAmount = 0;
                invoice.IsCreditPaid = true;
            }

            if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
                invoice.InvoiceNumber = await GenerateInvoiceNumberAsync(context, invoice.InvoiceType);

            await context.Invoices.AddAsync(invoice);
            await context.SaveChangesAsync();

            foreach (var item in itemsList)
            {
                item.InvoiceId = invoice.Id;
                await context.InvoiceItems.AddAsync(item);
            }
            await context.SaveChangesAsync();

            if (!skipStockUpdate && invoice.InvoiceType is InvoiceType.Purchase or InvoiceType.PurchaseReturn
                or InvoiceType.Sale or InvoiceType.SaleReturn or InvoiceType.Installment
                or InvoiceType.Damage)
            {
                foreach (var item in itemsList.Where(i => i.ProductId.HasValue))
                {
                    var stock = await context.WarehouseStocks
                        .FirstOrDefaultAsync(s =>
                            s.WarehouseId == invoice.WarehouseId &&
                            s.ProductId == item.ProductId!.Value);

                    if (invoice.InvoiceType is InvoiceType.Purchase or InvoiceType.SaleReturn)
                    {
                        if (stock is not null)
                        {
                            stock.Quantity += item.Quantity;
                            stock.UpdatedBy = username;
                            stock.UpdatedAt = DateTime.UtcNow;
                        }
                        else
                        {
                            await context.WarehouseStocks.AddAsync(new WarehouseStock
                            {
                                WarehouseId = invoice.WarehouseId,
                                ProductId = item.ProductId!.Value,
                                Quantity = item.Quantity,
                                CreatedBy = username,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                    else
                    {
                        // Sale / Installment / PurchaseReturn: decrease stock
                        if (stock is not null)
                        {
                            stock.Quantity -= item.Quantity;
                            stock.UpdatedBy = username;
                            stock.UpdatedAt = DateTime.UtcNow;
                        }
                        else if (invoice.InvoiceType == InvoiceType.PurchaseReturn)
                        {
                            throw new InvalidOperationException(
                                $"لا يوجد رصيد كافٍ لإرجاع المنتج (#{item.ProductId}) من المخزن");
                        }
                    }
                }
                await context.SaveChangesAsync();
            }

            if (invoice.CashBoxId.HasValue &&
                (invoice.PaymentMethod == PaymentMethod.Cash
                 || (invoice.PaymentMethod == PaymentMethod.Credit && invoice.PaidAmount > 0)))
            {
                var cashBox = await context.CashBoxes.FindAsync(invoice.CashBoxId.Value);
                if (cashBox is not null)
                {
                    var cashAmount = invoice.PaymentMethod == PaymentMethod.Credit
                        ? invoice.PaidAmount
                        : invoice.NetAmount;

                    if (invoice.InvoiceType == InvoiceType.Purchase || invoice.InvoiceType == InvoiceType.SaleReturn)
                        cashBox.Balance -= cashAmount; // شراء أو استرداد نقد للعميل
                    else if (invoice.InvoiceType == InvoiceType.PurchaseReturn)
                        cashBox.Balance += cashAmount; // استرداد نقد من المورد
                    else
                        cashBox.Balance += cashAmount;

                    cashBox.UpdatedBy = username;
                    cashBox.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                }
            }

            // سجل كسب/استبدال نقاط الولاء ضمن نفس المعاملة
            if (applyLoyalty)
            {
                await _loyaltyService.ApplyInvoiceLoyaltyAsync(
                    context,
                    invoice,
                    loyaltyRedeemPoints,
                    username,
                    _currentUserService.UserId,
                    CancellationToken.None);
            }

            if (_currentUserService.UserId.HasValue)
            {
                await context.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = _currentUserService.UserId.Value,
                    Action = AuditAction.Add,
                    EntityName = "Invoice",
                    EntityId = invoice.Id,
                    NewValues = $"رقم الفاتورة: {invoice.InvoiceNumber}, المبلغ: {invoice.NetAmount}, النوع: {invoice.InvoiceType}",
                    Timestamp = DateTime.UtcNow,
                    CreatedBy = username,
                    CreatedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            return invoice;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Invoice?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Invoices
            .Include(i => i.Items)
            .Include(i => i.Customer)
            .Include(i => i.Supplier)
            .Include(i => i.Driver)
            .Include(i => i.Warehouse)
            .Include(i => i.CashBox)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<Invoice?> GetByIdWithDetailsAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Invoices
            .Include(i => i.Items)
            .Include(i => i.Customer)
            .Include(i => i.Supplier)
            .Include(i => i.Driver)
            .Include(i => i.Warehouse)
            .Include(i => i.CashBox)
            .Include(i => i.InstallmentPlans)
                .ThenInclude(p => p.Installments)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<(IEnumerable<Invoice> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, InvoiceType? type = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Supplier)
            .Include(i => i.Warehouse)
            .AsQueryable();

        if (type.HasValue) query = query.Where(i => i.InvoiceType == type.Value);
        if (fromDate.HasValue) query = query.Where(i => i.Date >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(i => i.Date <= toDate.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(i => i.Date)
            .ThenByDescending(i => i.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<string> GenerateInvoiceNumberAsync(InvoiceType type)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await GenerateInvoiceNumberAsync(context, type);
    }

    private static Task<string> GenerateInvoiceNumberAsync(AppDbContext context, InvoiceType type)
        => InvoiceNumberHelper.GenerateNextAsync(context, type);

    public decimal CalculateRounding(decimal netAmount, InvoiceType invoiceType)
    {
        const decimal roundingStep = 250m;
        decimal remainder = netAmount % roundingStep;
        if (remainder == 0) return 0m;

        if (invoiceType is InvoiceType.Purchase or InvoiceType.PurchaseReturn)
            return roundingStep - remainder;
        else
            return -remainder;
    }

    public async Task<IReadOnlyList<Invoice>> SearchAsync(
        InvoiceType invoiceType,
        string? searchText,
        bool newestFirst,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.Invoices
            .AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Supplier)
            .Where(i => i.InvoiceType == invoiceType);

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var term = searchText.Trim();
            query = invoiceType switch
            {
                InvoiceType.Purchase => query.Where(i =>
                    EF.Functions.Like(i.InvoiceNumber, $"%{term}%") ||
                    (i.Supplier != null && EF.Functions.Like(i.Supplier.Name, $"%{term}%"))),
                _ => query.Where(i =>
                    EF.Functions.Like(i.InvoiceNumber, $"%{term}%") ||
                    (i.Customer != null && EF.Functions.Like(i.Customer.Name, $"%{term}%")))
            };
        }

        query = newestFirst
            ? query.OrderByDescending(i => i.Date).ThenByDescending(i => i.Id)
            : query.OrderBy(i => i.Date).ThenBy(i => i.Id);

        return await query.Take(limit).ToListAsync(cancellationToken);
    }

    public async Task<Invoice> ReplaceInvoiceAsync(
        int existingId,
        Invoice invoice,
        IEnumerable<InvoiceItem> items,
        bool skipStockUpdate = false)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.Invoices
            .AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Supplier)
            .Include(i => i.Warehouse)
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == existingId);

        var preservedNumber = !string.IsNullOrWhiteSpace(invoice.InvoiceNumber)
            ? invoice.InvoiceNumber
            : existing?.InvoiceNumber ?? string.Empty;

        decimal preservedPaidAmount = 0;
        decimal preservedRemainingAmount = 0;
        bool preservedIsCreditPaid = false;
        var preserveCreditState = existing?.PaymentMethod == PaymentMethod.Credit
                                  && invoice.PaymentMethod == PaymentMethod.Credit;
        if (preserveCreditState && existing is not null)
        {
            preservedPaidAmount = existing.PaidAmount;
            preservedRemainingAmount = existing.RemainingAmount;
            preservedIsCreditPaid = existing.IsCreditPaid;
        }

        var itemsList = items.ToList();
        var oldSnapshot = existing is null ? null : BuildInvoiceRevisionSnapshot(existing, existing.Items);
        var newSnapshotPreview = BuildInvoiceRevisionPreview(invoice, itemsList, preservedNumber, existing);

        await DeleteInvoiceAsync(existingId);

        invoice.InvoiceNumber = preservedNumber;
        invoice.Id = 0;
        var created = await CreateInvoiceAsync(invoice, itemsList, skipStockUpdate);

        if (preserveCreditState)
        {
            await using var updateContext = await _contextFactory.CreateDbContextAsync();
            var updated = await updateContext.Invoices.FirstOrDefaultAsync(i => i.Id == created.Id);
            if (updated is not null)
            {
                updated.PaidAmount = Math.Min(preservedPaidAmount, updated.NetAmount);
                updated.RemainingAmount = Math.Max(0, updated.NetAmount - updated.PaidAmount);
                updated.IsCreditPaid = updated.RemainingAmount <= 0;
                await updateContext.SaveChangesAsync();
                created = updated;
            }
        }

        await WriteInvoiceRevisionAuditAsync(created.Id, preservedNumber, oldSnapshot, newSnapshotPreview, created, existing);
        return created;
    }

    private async Task WriteInvoiceRevisionAuditAsync(
        int newInvoiceId,
        string invoiceNumber,
        Dictionary<string, object?>? oldSnapshot,
        Dictionary<string, object?> newSnapshotPreview,
        Invoice created,
        Invoice? existingBefore)
    {
        if (!_currentUserService.UserId.HasValue)
            return;

        newSnapshotPreview["InvoiceNumber"] = invoiceNumber;
        newSnapshotPreview["NetAmount"] = created.NetAmount;
        newSnapshotPreview["TotalAmount"] = created.TotalAmount;
        newSnapshotPreview["DiscountAmount"] = created.DiscountAmount;
        newSnapshotPreview["ItemsCount"] = created.Items?.Count
            ?? (newSnapshotPreview.TryGetValue("ItemsCount", out var c) ? c : 0);

        await using var context = await _contextFactory.CreateDbContextAsync();
        string? partyName = null;
        if (created.CustomerId.HasValue)
            partyName = await context.Customers.Where(c => c.Id == created.CustomerId).Select(c => c.Name).FirstOrDefaultAsync();
        else if (created.SupplierId.HasValue)
            partyName = await context.Suppliers.Where(s => s.Id == created.SupplierId).Select(s => s.Name).FirstOrDefaultAsync();

        var warehouseName = await context.Warehouses
            .Where(w => w.Id == created.WarehouseId)
            .Select(w => w.Name)
            .FirstOrDefaultAsync();

        newSnapshotPreview["PartyName"] = partyName ?? existingBefore?.Customer?.Name ?? existingBefore?.Supplier?.Name;
        newSnapshotPreview["WarehouseName"] = warehouseName ?? existingBefore?.Warehouse?.Name;
        newSnapshotPreview["Summary"] =
            $"تعديل فاتورة {invoiceNumber} | الطرف: {newSnapshotPreview["PartyName"] ?? "—"} | الصافي قبل: {FormatSnapshotAmount(oldSnapshot)} ← بعد: {created.NetAmount:N0}";

        await context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = _currentUserService.UserId.Value,
            Action = AuditAction.Edit,
            EntityName = "InvoiceRevision",
            EntityId = newInvoiceId,
            OldValues = oldSnapshot is null ? null : System.Text.Json.JsonSerializer.Serialize(oldSnapshot, AuditJsonOptions),
            NewValues = System.Text.Json.JsonSerializer.Serialize(newSnapshotPreview, AuditJsonOptions),
            Timestamp = DateTime.UtcNow,
            CreatedBy = _currentUserService.Username,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }

    private static readonly System.Text.Json.JsonSerializerOptions AuditJsonOptions = new()
    {
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static string FormatSnapshotAmount(Dictionary<string, object?>? snapshot)
    {
        if (snapshot is null || !snapshot.TryGetValue("NetAmount", out var value) || value is null)
            return "—";
        return value is decimal d ? d.ToString("N0") : value.ToString() ?? "—";
    }

    private static Dictionary<string, object?> BuildInvoiceRevisionSnapshot(Invoice invoice, IEnumerable<InvoiceItem> items)
    {
        var itemsList = items.ToList();
        return new Dictionary<string, object?>
        {
            ["InvoiceNumber"] = StripDeleteSuffix(invoice.InvoiceNumber),
            ["InvoiceType"] = invoice.InvoiceType.ToString(),
            ["InvoiceTypeDisplay"] = InvoiceTypeArabic(invoice.InvoiceType),
            ["PartyName"] = invoice.Customer?.Name ?? invoice.Supplier?.Name,
            ["CustomerId"] = invoice.CustomerId,
            ["SupplierId"] = invoice.SupplierId,
            ["WarehouseId"] = invoice.WarehouseId,
            ["WarehouseName"] = invoice.Warehouse?.Name,
            ["PaymentMethod"] = invoice.PaymentMethod.ToString(),
            ["TotalAmount"] = invoice.TotalAmount,
            ["DiscountAmount"] = invoice.DiscountAmount,
            ["NetAmount"] = invoice.NetAmount,
            ["Date"] = invoice.Date,
            ["Notes"] = invoice.Notes,
            ["ItemsCount"] = itemsList.Count,
            ["ItemsSummary"] = string.Join("؛ ", itemsList.Select(i =>
                $"{(string.IsNullOrWhiteSpace(i.ItemName) ? ("#" + i.ProductId) : i.ItemName)} × {i.Quantity:N0} @ {i.UnitPrice:N0}"))
        };
    }

    private static Dictionary<string, object?> BuildInvoiceRevisionPreview(
        Invoice invoice, IReadOnlyList<InvoiceItem> items, string invoiceNumber, Invoice? existing)
    {
        return new Dictionary<string, object?>
        {
            ["InvoiceNumber"] = invoiceNumber,
            ["InvoiceType"] = invoice.InvoiceType.ToString(),
            ["InvoiceTypeDisplay"] = InvoiceTypeArabic(invoice.InvoiceType),
            ["PartyName"] = null,
            ["CustomerId"] = invoice.CustomerId,
            ["SupplierId"] = invoice.SupplierId,
            ["WarehouseId"] = invoice.WarehouseId,
            ["WarehouseName"] = existing?.Warehouse?.Name,
            ["PaymentMethod"] = invoice.PaymentMethod.ToString(),
            ["TotalAmount"] = items.Sum(i => i.Quantity * i.UnitPrice),
            ["DiscountAmount"] = invoice.DiscountAmount,
            ["NetAmount"] = invoice.NetAmount,
            ["Date"] = invoice.Date,
            ["Notes"] = invoice.Notes,
            ["ItemsCount"] = items.Count,
            ["ItemsSummary"] = string.Join("؛ ", items.Select(i =>
                $"{(string.IsNullOrWhiteSpace(i.ItemName) ? ("#" + i.ProductId) : i.ItemName)} × {i.Quantity:N0} @ {i.UnitPrice:N0}"))
        };
    }

    private static string StripDeleteSuffix(string invoiceNumber)
    {
        var idx = invoiceNumber.LastIndexOf("-D", StringComparison.Ordinal);
        if (idx <= 0) return invoiceNumber;
        var suffix = invoiceNumber[(idx + 2)..];
        return int.TryParse(suffix, out _) ? invoiceNumber[..idx] : invoiceNumber;
    }

    private static string InvoiceTypeArabic(InvoiceType type) => type switch
    {
        InvoiceType.Sale => "مبيعات",
        InvoiceType.SaleReturn => "مرتجع مبيعات",
        InvoiceType.Purchase => "مشتريات",
        InvoiceType.Installment => "أقساط",
        InvoiceType.PurchaseReturn => "مرتجع مشتريات",
        InvoiceType.Damage => "تلف",
        _ => type.ToString()
    };

    public async Task DeleteInvoiceAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var invoice = await context.Invoices
            .Include(i => i.Items)
            .Include(i => i.InstallmentPlans)
                .ThenInclude(p => p.Installments)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (invoice is null) return;

        await _periodLockService.EnsureDateAllowedAsync(invoice.Date);

        var username = _currentUserService.Username;
        var originalInvoiceNumber = invoice.InvoiceNumber;

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            if (invoice.CashBoxId.HasValue &&
                (invoice.PaymentMethod == PaymentMethod.Cash
                 || (invoice.PaymentMethod == PaymentMethod.Credit && invoice.PaidAmount > 0)))
            {
                var cashBox = await context.CashBoxes.FindAsync(invoice.CashBoxId.Value);
                if (cashBox is not null)
                {
                    var cashAmount = invoice.PaymentMethod == PaymentMethod.Credit
                        ? invoice.PaidAmount
                        : invoice.NetAmount;

                    if (invoice.InvoiceType == InvoiceType.Purchase || invoice.InvoiceType == InvoiceType.SaleReturn)
                        cashBox.Balance += cashAmount;
                    else if (invoice.InvoiceType == InvoiceType.PurchaseReturn)
                        cashBox.Balance -= cashAmount;
                    else
                        cashBox.Balance -= cashAmount;

                    cashBox.UpdatedBy = username;
                    cashBox.UpdatedAt = DateTime.UtcNow;
                }
            }

            foreach (var item in invoice.Items.Where(i => i.ProductId.HasValue))
            {
                var stock = await context.WarehouseStocks
                    .FirstOrDefaultAsync(s =>
                        s.WarehouseId == invoice.WarehouseId &&
                        s.ProductId == item.ProductId!.Value);

                if (stock is not null)
                {
                    if (invoice.InvoiceType is InvoiceType.Purchase or InvoiceType.SaleReturn)
                        stock.Quantity -= item.Quantity;
                    else if (invoice.InvoiceType == InvoiceType.PurchaseReturn)
                        stock.Quantity += item.Quantity;
                    else
                        stock.Quantity += item.Quantity;

                    stock.UpdatedBy = username;
                    stock.UpdatedAt = DateTime.UtcNow;
                }
            }

            ReleaseInvoiceNumberForSoftDelete(invoice);
            invoice.MarkSoftDeleted(username);

            foreach (var item in invoice.Items)
                item.MarkSoftDeleted(username);

            foreach (var plan in invoice.InstallmentPlans)
            {
                plan.MarkSoftDeleted(username);
                foreach (var installment in plan.Installments)
                    installment.MarkSoftDeleted(username);
            }

            await context.SaveChangesAsync();

            if (_currentUserService.UserId.HasValue)
            {
                await context.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = _currentUserService.UserId.Value,
                    Action = AuditAction.Delete,
                    EntityName = "Invoice",
                    EntityId = invoice.Id,
                    OldValues = $"رقم الفاتورة: {originalInvoiceNumber}, المبلغ: {invoice.NetAmount}, النوع: {invoice.InvoiceType}",
                    Timestamp = DateTime.UtcNow,
                    CreatedBy = username,
                    CreatedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task PayCreditInvoiceAsync(int invoiceId, decimal amount, int cashBoxId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var username = _currentUserService.Username;
            var invoice = await context.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId)
                ?? throw new InvalidOperationException("الفاتورة غير موجودة");

            if (invoice.PaymentMethod != PaymentMethod.Credit)
                throw new InvalidOperationException("هذه الفاتورة ليست آجلة");

            if (invoice.IsCreditPaid)
                throw new InvalidOperationException("تم تسديد هذه الفاتورة بالكامل مسبقاً");

            if (amount <= 0)
                throw new InvalidOperationException("مبلغ الدفع يجب أن يكون أكبر من صفر");

            if (amount > invoice.RemainingAmount)
                throw new InvalidOperationException($"مبلغ الدفع ({amount:N0}) أكبر من المتبقي ({invoice.RemainingAmount:N0})");

            invoice.PaidAmount += amount;
            invoice.RemainingAmount = invoice.NetAmount - invoice.PaidAmount;
            invoice.IsCreditPaid = invoice.RemainingAmount <= 0;
            invoice.CashBoxId = cashBoxId;
            invoice.UpdatedBy = username;
            invoice.UpdatedAt = DateTime.UtcNow;

            // Update CashBox balance
            var cashBox = await context.CashBoxes.FindAsync(cashBoxId);
            if (cashBox is not null)
            {
                if (invoice.InvoiceType == InvoiceType.Purchase)
                    cashBox.Balance -= amount;
                else
                    cashBox.Balance += amount;

                cashBox.UpdatedBy = username;
                cashBox.UpdatedAt = DateTime.UtcNow;
            }

            // إنشاء سند قبض دين للمزامنة وكشف الحساب (معلّم كمطبّق لأن الفاتورة حُدّثت أعلاه)
            if (invoice.CustomerId.HasValue && invoice.InvoiceType != InvoiceType.Purchase)
            {
                var voucherNumber = await GetNextDebtReceiptNumberAsync(context);
                await context.Vouchers.AddAsync(new Voucher
                {
                    VoucherNumber = voucherNumber,
                    VoucherType = VoucherType.DebtReceipt,
                    Amount = amount,
                    CustomerId = invoice.CustomerId,
                    CashBoxId = cashBoxId,
                    Date = DateTime.Today,
                    Notes = CustomerBalanceHelper.MarkDebtReceiptApplied(
                        $"تسديد فاتورة آجلة {invoice.InvoiceNumber}"),
                    CreatedBy = username,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();

            if (_currentUserService.UserId.HasValue)
            {
                await context.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = _currentUserService.UserId.Value,
                    Action = AuditAction.Edit,
                    EntityName = "Invoice",
                    EntityId = invoice.Id,
                    NewValues = $"تسديد فاتورة آجلة: {amount:N0} د.ع, المتبقي: {invoice.RemainingAmount:N0}",
                    Timestamp = DateTime.UtcNow,
                    CreatedBy = username,
                    CreatedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task<string> GetNextDebtReceiptNumberAsync(AppDbContext context)
    {
        var lastVoucher = await context.Vouchers
            .IgnoreQueryFilters()
            .Where(v => v.VoucherType == VoucherType.DebtReceipt)
            .OrderByDescending(v => v.Id)
            .FirstOrDefaultAsync();

        var nextNum = 1;
        if (lastVoucher?.VoucherNumber is { Length: > 3 } number &&
            int.TryParse(number.AsSpan(3), out var parsed))
            nextNum = parsed + 1;

        return $"DRC{nextNum:D6}";
    }

    /// <summary>
    /// Frees the unique invoice number slot when soft-deleting so a replacement invoice can reuse the number.
    /// </summary>
    private static void ReleaseInvoiceNumberForSoftDelete(Invoice invoice)
    {
        const int maxLength = 50;
        var suffix = $"-D{invoice.Id}";
        var number = invoice.InvoiceNumber;

        if (number.EndsWith(suffix, StringComparison.Ordinal))
            return;

        var maxBaseLength = maxLength - suffix.Length;
        if (number.Length > maxBaseLength)
            number = number[..maxBaseLength];

        invoice.InvoiceNumber = number + suffix;
    }
}
