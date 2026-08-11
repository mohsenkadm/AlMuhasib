using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class OpeningPartyBalanceService : IOpeningPartyBalanceService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ICurrentUserService _currentUserService;

    public OpeningPartyBalanceService(
        IDbContextFactory<AppDbContext> contextFactory,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
    }

    public Task<Invoice> CreateCustomerOpeningBalanceAsync(OpeningPartyBalanceRequest request)
        => CreateCustomerCoreAsync(request, nameCache: null);

    public async Task<OpeningPartyBalanceBatchResult> CreateCustomerOpeningBalancesBatchAsync(
        IReadOnlyList<OpeningPartyBalanceRequest> requests)
    {
        var result = new OpeningPartyBalanceBatchResult();
        if (requests is null || requests.Count == 0)
        {
            result.Errors.Add("لا توجد بيانات للاستيراد");
            return result;
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        var customers = await context.Customers.Select(c => new { c.Id, c.Name }).ToListAsync();
        var nameCache = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var c in customers)
        {
            var key = ArabicNameNormalizer.Compact(c.Name);
            if (key.Length > 0 && !nameCache.ContainsKey(key))
                nameCache[key] = c.Id;
        }

        for (var index = 0; index < requests.Count; index++)
        {
            var request = requests[index];
            try
            {
                ValidateRequest(request, "عميل");
                await CreateCustomerCoreAsync(request, nameCache);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                var label = request.PartyName ?? request.PartyId?.ToString() ?? $"سطر {index + 1}";
                result.Errors.Add($"{label}: {ex.Message}");
            }
        }

        return result;
    }

    public Task<Invoice> CreateSupplierOpeningBalanceAsync(OpeningPartyBalanceRequest request)
        => CreateSupplierCoreAsync(request, nameCache: null);

    public async Task<OpeningPartyBalanceBatchResult> CreateSupplierOpeningBalancesBatchAsync(
        IReadOnlyList<OpeningPartyBalanceRequest> requests)
    {
        var result = new OpeningPartyBalanceBatchResult();
        if (requests is null || requests.Count == 0)
        {
            result.Errors.Add("لا توجد بيانات للاستيراد");
            return result;
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        var suppliers = await context.Suppliers.Select(s => new { s.Id, s.Name }).ToListAsync();
        var nameCache = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var s in suppliers)
        {
            var key = ArabicNameNormalizer.Compact(s.Name);
            if (key.Length > 0 && !nameCache.ContainsKey(key))
                nameCache[key] = s.Id;
        }

        for (var index = 0; index < requests.Count; index++)
        {
            var request = requests[index];
            try
            {
                ValidateRequest(request, "مورد");
                await CreateSupplierCoreAsync(request, nameCache);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                var label = request.PartyName ?? request.PartyId?.ToString() ?? $"سطر {index + 1}";
                result.Errors.Add($"{label}: {ex.Message}");
            }
        }

        return result;
    }

    private async Task<Invoice> CreateCustomerCoreAsync(
        OpeningPartyBalanceRequest request,
        Dictionary<string, int>? nameCache)
    {
        ValidateRequest(request, "عميل");

        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var username = _currentUserService.Username;
            var customerId = await ResolveCustomerIdAsync(context, request, username, nameCache);
            var warehouse = await context.Warehouses.OrderBy(w => w.Id).FirstOrDefaultAsync()
                ?? throw new InvalidOperationException("يجب إنشاء مخزن واحد على الأقل قبل إدخال الأرصدة الافتتاحية");

            var invoiceNumber = await InvoiceNumberHelper.GenerateNextAsync(context, InvoiceType.Sale);
            var invoice = new Invoice
            {
                InvoiceNumber = invoiceNumber,
                InvoiceType = InvoiceType.Sale,
                CustomerId = customerId,
                WarehouseId = warehouse.Id,
                PaymentMethod = PaymentMethod.Credit,
                TotalAmount = request.Amount,
                DiscountAmount = 0,
                NetAmount = request.Amount,
                RoundingAmount = 0,
                RoundingType = RoundingType.RoundDown,
                PaidAmount = 0,
                RemainingAmount = request.Amount,
                IsCreditPaid = false,
                Date = request.Date.Date,
                Notes = OpeningCreditBalanceMarkers.BuildNotes(request.Notes),
                CreatedBy = username,
                CreatedAt = DateTime.UtcNow
            };
            await context.Invoices.AddAsync(invoice);
            await context.SaveChangesAsync();

            if (_currentUserService.UserId.HasValue)
            {
                await context.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = _currentUserService.UserId.Value,
                    Action = AuditAction.Add,
                    EntityName = nameof(Invoice),
                    EntityId = invoice.Id,
                    NewValues = $"رصيد افتتاحي آجل للعميل #{customerId} بمبلغ {request.Amount:N0}",
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

    private async Task<Invoice> CreateSupplierCoreAsync(
        OpeningPartyBalanceRequest request,
        Dictionary<string, int>? nameCache)
    {
        ValidateRequest(request, "مورد");

        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var username = _currentUserService.Username;
            var supplierId = await ResolveSupplierIdAsync(context, request, username, nameCache);
            var warehouse = await context.Warehouses.OrderBy(w => w.Id).FirstOrDefaultAsync()
                ?? throw new InvalidOperationException("يجب إنشاء مخزن واحد على الأقل قبل إدخال الأرصدة الافتتاحية");

            var invoiceNumber = await InvoiceNumberHelper.GenerateNextAsync(context, InvoiceType.Purchase);
            var invoice = new Invoice
            {
                InvoiceNumber = invoiceNumber,
                InvoiceType = InvoiceType.Purchase,
                SupplierId = supplierId,
                WarehouseId = warehouse.Id,
                PaymentMethod = PaymentMethod.Credit,
                TotalAmount = request.Amount,
                DiscountAmount = 0,
                NetAmount = request.Amount,
                RoundingAmount = 0,
                RoundingType = RoundingType.RoundDown,
                PaidAmount = 0,
                RemainingAmount = request.Amount,
                IsCreditPaid = false,
                Date = request.Date.Date,
                Notes = OpeningCreditBalanceMarkers.BuildNotes(request.Notes),
                CreatedBy = username,
                CreatedAt = DateTime.UtcNow
            };
            await context.Invoices.AddAsync(invoice);
            await context.SaveChangesAsync();

            if (_currentUserService.UserId.HasValue)
            {
                await context.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = _currentUserService.UserId.Value,
                    Action = AuditAction.Add,
                    EntityName = nameof(Invoice),
                    EntityId = invoice.Id,
                    NewValues = $"رصيد افتتاحي آجل للمورد #{supplierId} بمبلغ {request.Amount:N0}",
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

    private static void ValidateRequest(OpeningPartyBalanceRequest request, string partyLabel)
    {
        if (request.Amount <= 0)
            throw new InvalidOperationException("المبلغ يجب أن يكون أكبر من صفر");
        if (request.PartyId is null && string.IsNullOrWhiteSpace(request.PartyName))
            throw new InvalidOperationException($"يجب اختيار {partyLabel} أو إدخال اسمه");
    }

    private static async Task<int> ResolveCustomerIdAsync(
        AppDbContext context,
        OpeningPartyBalanceRequest request,
        string username,
        Dictionary<string, int>? nameCache)
    {
        if (request.PartyId is int existingId)
        {
            var exists = await context.Customers.AnyAsync(c => c.Id == existingId);
            if (!exists)
                throw new InvalidOperationException("العميل المحدد غير موجود");
            return existingId;
        }

        var name = request.PartyName!.Trim();
        var compact = ArabicNameNormalizer.Compact(name);

        if (nameCache is not null && compact.Length > 0 && nameCache.TryGetValue(compact, out var cachedId))
            return cachedId;

        var exact = await context.Customers.FirstOrDefaultAsync(c => c.Name == name);
        if (exact is not null)
        {
            if (nameCache is not null)
            {
                var exactKey = ArabicNameNormalizer.Compact(exact.Name);
                if (exactKey.Length > 0)
                    nameCache[exactKey] = exact.Id;
            }
            return exact.Id;
        }

        if (compact.Length > 0 && nameCache is null)
        {
            var candidates = await context.Customers.Select(c => new { c.Id, c.Name }).ToListAsync();
            var match = candidates.FirstOrDefault(c => ArabicNameNormalizer.Compact(c.Name) == compact);
            if (match is not null)
                return match.Id;
        }

        var newCustomer = new Customer
        {
            Name = name,
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            FileNumber = string.IsNullOrWhiteSpace(request.FileNumber) ? null : request.FileNumber.Trim(),
            CreatedBy = username,
            CreatedAt = DateTime.UtcNow
        };
        await context.Customers.AddAsync(newCustomer);
        await context.SaveChangesAsync();

        if (nameCache is not null && compact.Length > 0)
            nameCache[compact] = newCustomer.Id;

        return newCustomer.Id;
    }

    private static async Task<int> ResolveSupplierIdAsync(
        AppDbContext context,
        OpeningPartyBalanceRequest request,
        string username,
        Dictionary<string, int>? nameCache)
    {
        if (request.PartyId is int existingId)
        {
            var exists = await context.Suppliers.AnyAsync(s => s.Id == existingId);
            if (!exists)
                throw new InvalidOperationException("المورد المحدد غير موجود");
            return existingId;
        }

        var name = request.PartyName!.Trim();
        var compact = ArabicNameNormalizer.Compact(name);

        if (nameCache is not null && compact.Length > 0 && nameCache.TryGetValue(compact, out var cachedId))
            return cachedId;

        var exact = await context.Suppliers.FirstOrDefaultAsync(s => s.Name == name);
        if (exact is not null)
        {
            if (nameCache is not null)
            {
                var exactKey = ArabicNameNormalizer.Compact(exact.Name);
                if (exactKey.Length > 0)
                    nameCache[exactKey] = exact.Id;
            }
            return exact.Id;
        }

        if (compact.Length > 0 && nameCache is null)
        {
            var candidates = await context.Suppliers.Select(s => new { s.Id, s.Name }).ToListAsync();
            var match = candidates.FirstOrDefault(s => ArabicNameNormalizer.Compact(s.Name) == compact);
            if (match is not null)
                return match.Id;
        }

        var newSupplier = new Supplier
        {
            Name = name,
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            CreatedBy = username,
            CreatedAt = DateTime.UtcNow
        };
        await context.Suppliers.AddAsync(newSupplier);
        await context.SaveChangesAsync();

        if (nameCache is not null && compact.Length > 0)
            nameCache[compact] = newSupplier.Id;

        return newSupplier.Id;
    }
}
