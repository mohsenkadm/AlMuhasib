using AlMuhasib.Core.Constants;
using AlMuhasib.Core.Entities.Customs;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Enums.Customs;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces.Services.Customs;
using AlMuhasib.Core.Models.Customs;
using AlMuhasib.Infrastructure.Data.Customs;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Customs;

public sealed class CustomsReceiptService : ICustomsReceiptService
{
    private readonly IDbContextFactory<CustomsDbContext> _contextFactory;
    private readonly CustomsPermissionGuard _permissions;

    public CustomsReceiptService(
        IDbContextFactory<CustomsDbContext> contextFactory,
        CustomsPermissionGuard permissions)
    {
        _contextFactory = contextFactory;
        _permissions = permissions;
    }

    public async Task<(IReadOnlyList<CustomsReceiptListItem> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? customerId = null,
        AccountingCurrency? currency = null,
        string? sortBy = null,
        bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        _permissions.EnsureView(CustomsScreenNames.Receipts);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.CustomsReceipts.AsNoTracking().AsQueryable();

        if (customerId.HasValue)
            query = query.Where(r => r.CustomerId == customerId.Value);

        if (currency.HasValue)
            query = query.Where(r => r.Currency == currency.Value);

        if (fromDate.HasValue)
        {
            var from = fromDate.Value.Date;
            query = query.Where(r => r.ReceiptDate >= from);
        }

        if (toDate.HasValue)
        {
            var toExclusive = toDate.Value.Date.AddDays(1);
            query = query.Where(r => r.ReceiptDate < toExclusive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(r =>
                r.ReceiptNumber.Contains(term) ||
                r.Customer.Name.Contains(term) ||
                r.ReceiverName.Contains(term) ||
                r.Notes.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var rows = await ApplyReceiptSort(query, sortBy, sortDescending)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new CustomsReceiptListItem
            {
                Id = r.Id,
                ReceiptNumber = r.ReceiptNumber,
                ReceiptDate = r.ReceiptDate,
                CustomerId = r.CustomerId,
                CustomerName = r.Customer.Name,
                Amount = r.Amount,
                Currency = r.Currency,
                PaymentMethod = r.PaymentMethod,
                ReceiverName = r.ReceiverName,
                AllocationCount = r.Allocations.Count
            })
            .ToListAsync(cancellationToken);

        foreach (var item in rows)
        {
            item.CurrencyLabel = item.Currency == AccountingCurrency.USD ? "$" : "د.ع";
            item.PaymentMethodLabel = CustomsPaymentMethodDisplay.ToArabic(item.PaymentMethod);
        }

        return (rows, totalCount);
    }

    /// <summary>M-04: allowlisted receipt list sort. Default remains ReceiptDate DESC, Id DESC.</summary>
    private static IOrderedQueryable<CustomsReceipt> ApplyReceiptSort(
        IQueryable<CustomsReceipt> query,
        string? sortBy,
        bool sortDescending)
    {
        var key = string.IsNullOrWhiteSpace(sortBy) ? "ReceiptDate" : sortBy.Trim();
        return key switch
        {
            "ReceiptDate" => CustomsListSort.ThenByDir(
                CustomsListSort.By(query, sortDescending, r => r.ReceiptDate),
                sortDescending,
                r => r.Id),
            "Id" => CustomsListSort.By(query, sortDescending, r => r.Id),
            "ReceiptNumber" => CustomsListSort.By(query, sortDescending, r => r.ReceiptNumber),
            "Amount" => CustomsListSort.By(query, sortDescending, r => r.Amount),
            _ => throw new InvalidOperationException($"عمود الفرز غير مسموح: {key}")
        };
    }

    public async Task<CustomsReceiptDetail?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _permissions.EnsureView(CustomsScreenNames.Receipts);
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var receipt = await db.CustomsReceipts.AsNoTracking()
            .Include(r => r.Customer)
            .Include(r => r.Allocations).ThenInclude(a => a.Invoice)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (receipt is null)
            return null;

        var attachments = await db.CustomsAttachments.AsNoTracking()
            .Where(a => a.OwnerType == CustomsAttachmentOwnerType.Receipt && a.OwnerId == id)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        return ToDetail(receipt, attachments);
    }

    public async Task<string> PreviewNextReceiptNumberAsync(CancellationToken cancellationToken = default)
    {
        _permissions.EnsureView(CustomsScreenNames.Receipts);
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await CustomsReceiptNumberHelper.GetNextAsync(db, cancellationToken);
    }

    public async Task<IReadOnlyList<CustomsOpenInvoiceItem>> GetOpenInvoicesAsync(
        int customerId,
        AccountingCurrency currency,
        int? excludeReceiptId = null,
        CancellationToken cancellationToken = default)
    {
        _permissions.EnsureView(CustomsScreenNames.Receipts);
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var invoices = await db.CustomsInvoices.AsNoTracking()
            .Where(i => i.CustomerId == customerId
                        && i.Status == CustomsInvoiceStatus.Approved
                        && i.Currency == currency)
            .OrderBy(i => i.InvoiceDate)
            .ThenBy(i => i.Id)
            .ToListAsync(cancellationToken);

        Dictionary<int, decimal>? priorAllocations = null;
        if (excludeReceiptId is int rid)
        {
            priorAllocations = await db.CustomsReceiptAllocations.AsNoTracking()
                .Where(a => a.ReceiptId == rid)
                .GroupBy(a => a.InvoiceId)
                .ToDictionaryAsync(g => g.Key, g => g.Sum(x => x.Amount), cancellationToken);
        }

        var result = new List<CustomsOpenInvoiceItem>();
        foreach (var inv in invoices)
        {
            var prior = priorAllocations is not null && priorAllocations.TryGetValue(inv.Id, out var p) ? p : 0m;
            var available = inv.RemainingAmount + prior;
            if (available <= 0 && prior <= 0)
                continue;

            result.Add(new CustomsOpenInvoiceItem
            {
                InvoiceId = inv.Id,
                InvoiceNumber = inv.InvoiceNumber,
                InvoiceDate = inv.InvoiceDate,
                TotalAmount = inv.TotalAmount,
                PaidAmount = inv.PaidAmount,
                RemainingAmount = available,
                Currency = inv.Currency,
                CurrencyLabel = inv.Currency == AccountingCurrency.USD ? "$" : "د.ع"
            });
        }

        return result;
    }

    public async Task<CustomsReceiptDetail> CreateAsync(
        CustomsReceiptSaveRequest request,
        CancellationToken cancellationToken = default)
    {
        _permissions.EnsureAdd(CustomsScreenNames.Receipts);
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await ValidateRequestAsync(db, request, excludeReceiptId: null, cancellationToken);

            var fxRate = CustomsFinancialRules.NormalizeFxRate(
                request.Currency,
                await ResolveFxRateAsync(db, request.Currency, request.ReceiptDate, request.FxRate, cancellationToken),
                "الوصل");
            var user = request.UserName ?? "System";

            var receipt = new CustomsReceipt
            {
                CustomerId = request.CustomerId,
                ReceiptNumber = await CustomsReceiptNumberHelper.GetNextAsync(db, cancellationToken),
                ReceiptDate = request.ReceiptDate.Date,
                Amount = request.Amount,
                Currency = request.Currency,
                FxRate = fxRate,
                PaymentMethod = request.PaymentMethod,
                ReceiverName = request.ReceiverName?.Trim() ?? string.Empty,
                Notes = request.Notes?.Trim() ?? string.Empty,
                CreatedBy = user
            };

            foreach (var line in request.Allocations.Where(a => a.Amount > 0))
            {
                receipt.Allocations.Add(new CustomsReceiptAllocation
                {
                    InvoiceId = line.InvoiceId,
                    Amount = line.Amount,
                    CreatedBy = user
                });
            }

            await db.CustomsReceipts.AddAsync(receipt, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            // Rule 5: Paid/Remaining from allocation sum (independent receipt record), not a bare invoice field.
            var invoiceIds = receipt.Allocations.Select(a => a.InvoiceId).Distinct().ToList();
            await CustomsSettlementHelper.RecomputeInvoiceSettlementAsync(db, invoiceIds, cancellationToken);
            await CustomsSettlementHelper.RecomputeCustomerBalanceAsync(db, request.CustomerId, cancellationToken);

            await CustomsAuditWriter.WriteAsync(
                db,
                AuditAction.Add,
                "CustomsReceipt",
                receipt.Id,
                oldValues: null,
                newValues: new
                {
                    receipt.ReceiptNumber,
                    receipt.CustomerId,
                    receipt.Amount,
                    Currency = receipt.Currency.ToString(),
                    Allocations = receipt.Allocations
                        .Where(a => !a.IsDeleted)
                        .Select(a => new { a.InvoiceId, a.Amount })
                        .ToList()
                },
                cancellationToken);

            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return (await GetByIdAsync(receipt.Id, cancellationToken))!;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<CustomsReceiptDetail> UpdateAsync(
        int id,
        CustomsReceiptSaveRequest request,
        CancellationToken cancellationToken = default)
    {
        _permissions.EnsureEdit(CustomsScreenNames.Receipts);
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var receipt = await db.CustomsReceipts
                .Include(r => r.Allocations)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
                ?? throw new InvalidOperationException("الوصل غير موجود");

            await ValidateRequestAsync(db, request, excludeReceiptId: id, cancellationToken);

            var user = request.UserName ?? "System";
            var fxRate = CustomsFinancialRules.NormalizeFxRate(
                request.Currency,
                await ResolveFxRateAsync(db, request.Currency, request.ReceiptDate, request.FxRate, cancellationToken),
                "الوصل");

            var oldSnapshot = new
            {
                receipt.CustomerId,
                receipt.Amount,
                Currency = receipt.Currency.ToString(),
                Allocations = receipt.Allocations
                    .Where(a => !a.IsDeleted)
                    .Select(a => new { a.InvoiceId, a.Amount })
                    .ToList()
            };

            var previousCustomerId = receipt.CustomerId;
            var affectedInvoiceIds = receipt.Allocations
                .Where(a => !a.IsDeleted)
                .Select(a => a.InvoiceId)
                .Concat(request.Allocations.Where(a => a.Amount > 0).Select(a => a.InvoiceId))
                .Distinct()
                .ToList();

            receipt.CustomerId = request.CustomerId;
            receipt.ReceiptDate = request.ReceiptDate.Date;
            receipt.Amount = request.Amount;
            receipt.Currency = request.Currency;
            receipt.FxRate = fxRate;
            receipt.PaymentMethod = request.PaymentMethod;
            receipt.ReceiverName = request.ReceiverName?.Trim() ?? string.Empty;
            receipt.Notes = request.Notes?.Trim() ?? string.Empty;
            receipt.UpdatedBy = user;
            receipt.UpdatedAt = DateTime.UtcNow;

            UpsertAllocations(receipt, request.Allocations, user);

            await db.SaveChangesAsync(cancellationToken);

            var active = receipt.Allocations.Where(a => !a.IsDeleted).ToList();
            await CustomsSettlementHelper.RecomputeInvoiceSettlementAsync(db, affectedInvoiceIds, cancellationToken);
            await CustomsSettlementHelper.RecomputeCustomerBalanceAsync(db, request.CustomerId, cancellationToken);
            if (previousCustomerId != request.CustomerId)
                await CustomsSettlementHelper.RecomputeCustomerBalanceAsync(db, previousCustomerId, cancellationToken);

            await CustomsAuditWriter.WriteAsync(
                db,
                AuditAction.Edit,
                "CustomsReceipt",
                receipt.Id,
                oldValues: oldSnapshot,
                newValues: new
                {
                    receipt.CustomerId,
                    receipt.Amount,
                    Currency = receipt.Currency.ToString(),
                    Allocations = active.Select(a => new { a.InvoiceId, a.Amount }).ToList()
                },
                cancellationToken);

            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return (await GetByIdAsync(receipt.Id, cancellationToken))!;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Cancels a receipt by soft-deleting the row and allocations, then recomputing Paid/Remaining from remaining allocations.
    /// Audit trail is retained (soft-delete + CustomsReceipt.Cancel log) — no hard-delete of financial trail.
    /// </summary>
    public async Task CancelAsync(int id, string userName, CancellationToken cancellationToken = default)
    {
        _permissions.EnsureDelete(CustomsScreenNames.Receipts);
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var receipt = await db.CustomsReceipts
                .Include(r => r.Allocations)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
                ?? throw new InvalidOperationException("الوصل غير موجود");

            var active = receipt.Allocations.Where(a => !a.IsDeleted).ToList();
            var oldSnapshot = new
            {
                receipt.CustomerId,
                receipt.Amount,
                Currency = receipt.Currency.ToString(),
                Allocations = active.Select(a => new { a.InvoiceId, a.Amount }).ToList()
            };

            var invoiceIds = active.Select(a => a.InvoiceId).Distinct().ToList();
            foreach (var a in active)
                a.MarkSoftDeleted(userName);

            var customerId = receipt.CustomerId;
            receipt.MarkSoftDeleted(userName);
            await db.SaveChangesAsync(cancellationToken);

            await CustomsSettlementHelper.RecomputeInvoiceSettlementAsync(db, invoiceIds, cancellationToken);
            await CustomsSettlementHelper.RecomputeCustomerBalanceAsync(db, customerId, cancellationToken);

            await CustomsAuditWriter.WriteAsync(
                db,
                AuditAction.Delete,
                "CustomsReceipt.Cancel",
                receipt.Id,
                oldValues: oldSnapshot,
                newValues: new { Reversed = true, AllocationsCleared = active.Count },
                cancellationToken);

            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static void UpsertAllocations(
        CustomsReceipt receipt,
        IReadOnlyList<CustomsReceiptAllocationDraft> drafts,
        string user)
    {
        var existingById = receipt.Allocations
            .Where(a => !a.IsDeleted)
            .ToDictionary(a => a.Id);

        var keptIds = new HashSet<int>();
        foreach (var draft in drafts.Where(d => d.Amount > 0))
        {
            if (draft.Id is int id && existingById.TryGetValue(id, out var existing))
            {
                existing.InvoiceId = draft.InvoiceId;
                existing.Amount = draft.Amount;
                existing.UpdatedBy = user;
                existing.UpdatedAt = DateTime.UtcNow;
                keptIds.Add(id);
            }
            else
            {
                receipt.Allocations.Add(new CustomsReceiptAllocation
                {
                    InvoiceId = draft.InvoiceId,
                    Amount = draft.Amount,
                    CreatedBy = user
                });
            }
        }

        foreach (var old in receipt.Allocations.Where(a => !a.IsDeleted && a.Id > 0 && !keptIds.Contains(a.Id)).ToList())
            old.MarkSoftDeleted(user);
    }

    private static async Task ValidateRequestAsync(
        CustomsDbContext db,
        CustomsReceiptSaveRequest request,
        int? excludeReceiptId,
        CancellationToken cancellationToken)
    {
        if (request.CustomerId <= 0)
            throw new InvalidOperationException("اختر العميل");

        if (request.Amount <= 0)
            throw new InvalidOperationException("مبلغ الوصل يجب أن يكون أكبر من صفر");

        var allocations = request.Allocations.Where(a => a.Amount > 0).ToList();
        if (allocations.Count == 0)
            throw new InvalidOperationException("وزّع المبلغ على فاتورة واحدة على الأقل");

        if (allocations.Any(a => a.Amount < 0))
            throw new InvalidOperationException("مبلغ التوزيع لا يمكن أن يكون سالباً");

        var allocSum = allocations.Sum(a => a.Amount);
        if (allocSum != request.Amount)
            throw new InvalidOperationException($"مجموع التوزيع ({allocSum:N0}) يجب أن يساوي مبلغ الوصل ({request.Amount:N0})");

        var customerExists = await db.CustomsCustomers.AnyAsync(
            c => c.Id == request.CustomerId && c.IsActive, cancellationToken);
        if (!customerExists)
            throw new InvalidOperationException("العميل غير موجود أو غير نشط");

        // C-02: remaining must be checked against the sum per InvoiceId, not each line alone.
        var byInvoice = allocations.GroupBy(a => a.InvoiceId).ToList();
        var invoiceIds = byInvoice.Select(g => g.Key).ToList();
        var invoices = await db.CustomsInvoices
            .Where(i => invoiceIds.Contains(i.Id))
            .ToListAsync(cancellationToken);

        if (invoices.Count != invoiceIds.Count)
            throw new InvalidOperationException("إحدى الفواتير الموزَّعة غير موجودة");

        Dictionary<int, decimal>? priorByInvoice = null;
        if (excludeReceiptId is int rid)
        {
            // Load then group in-memory — InMemory provider cannot translate GroupBy+ToDictionary.
            var priorRows = await db.CustomsReceiptAllocations
                .Where(a => a.ReceiptId == rid && !a.IsDeleted)
                .Select(a => new { a.InvoiceId, a.Amount })
                .ToListAsync(cancellationToken);
            priorByInvoice = priorRows
                .GroupBy(a => a.InvoiceId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
        }

        foreach (var group in byInvoice)
        {
            var inv = invoices.First(i => i.Id == group.Key);
            if (inv.CustomerId != request.CustomerId)
                throw new InvalidOperationException($"الفاتورة {inv.InvoiceNumber} لا تخص هذا العميل");

            if (inv.Status != CustomsInvoiceStatus.Approved)
                throw new InvalidOperationException($"الفاتورة {inv.InvoiceNumber} غير معتمدة");

            CustomsFinancialRules.EnsureReceiptMatchesInvoiceCurrency(request.Currency, inv.Currency);

            var sum = group.Sum(x => x.Amount);
            var prior = priorByInvoice is not null && priorByInvoice.TryGetValue(inv.Id, out var p) ? p : 0m;
            var available = inv.RemainingAmount + prior;
            if (sum > available)
                throw new InvalidOperationException(
                    $"التوزيع على {inv.InvoiceNumber} ({sum:N0}) يتجاوز المتبقي ({available:N0})");
        }
    }

    private static async Task<decimal> ResolveFxRateAsync(
        CustomsDbContext db,
        AccountingCurrency currency,
        DateTime receiptDate,
        decimal? requested,
        CancellationToken cancellationToken)
    {
        if (currency == AccountingCurrency.IQD)
            return 1m;

        if (requested is > 0)
            return requested.Value;

        var date = receiptDate.Date;
        var rate = await db.CustomsFxRates.AsNoTracking()
            .Where(r => r.RateDate <= date)
            .OrderByDescending(r => r.RateDate)
            .Select(r => (decimal?)r.UsdToIqd)
            .FirstOrDefaultAsync(cancellationToken);

        if (rate is null or <= 0)
            throw new InvalidOperationException("لا يوجد سعر صرف محفوظ لتاريخ الوصل — أدخل FxRate يدوياً");

        return rate.Value;
    }

    private static CustomsReceiptDetail ToDetail(CustomsReceipt r, List<CustomsAttachment> attachments) =>
        new()
        {
            Id = r.Id,
            ReceiptNumber = r.ReceiptNumber,
            ReceiptDate = r.ReceiptDate,
            CustomerId = r.CustomerId,
            CustomerName = r.Customer?.Name ?? string.Empty,
            CustomerPhone = r.Customer?.Phone,
            Amount = r.Amount,
            Currency = r.Currency,
            CurrencyLabel = r.Currency == AccountingCurrency.USD ? "$" : "د.ع",
            FxRate = r.FxRate,
            PaymentMethod = r.PaymentMethod,
            PaymentMethodLabel = CustomsPaymentMethodDisplay.ToArabic(r.PaymentMethod),
            ReceiverName = r.ReceiverName,
            Notes = r.Notes,
            Allocations = r.Allocations.Select(a => new CustomsReceiptAllocationDraft
            {
                Id = a.Id,
                InvoiceId = a.InvoiceId,
                InvoiceNumber = a.Invoice?.InvoiceNumber ?? string.Empty,
                Amount = a.Amount
            }).ToList(),
            Attachments = attachments.Select(a => new CustomsAttachmentItem
            {
                Id = a.Id,
                FileName = a.FileName,
                FilePath = a.FilePath,
                Description = a.Description,
                CreatedAt = a.CreatedAt
            }).ToList()
        };
}
