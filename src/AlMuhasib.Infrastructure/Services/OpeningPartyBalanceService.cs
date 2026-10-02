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
    private readonly IAccountingPeriodLockService _periodLockService;
    private readonly IBranchContext _branchContext;

    public OpeningPartyBalanceService(
        IDbContextFactory<AppDbContext> contextFactory,
        ICurrentUserService currentUserService,
        IAccountingPeriodLockService periodLockService,
        IBranchContext branchContext)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
        _periodLockService = periodLockService;
        _branchContext = branchContext;
    }

    public Task<OpeningPartyBalancePagedResult> GetCustomerOpeningBalancesAsync(OpeningPartyBalanceQuery query)
        => GetOpeningBalancesAsync(query, InvoiceType.Sale, isCustomer: true);

    public Task<OpeningPartyBalancePagedResult> GetSupplierOpeningBalancesAsync(OpeningPartyBalanceQuery query)
        => GetOpeningBalancesAsync(query, InvoiceType.Purchase, isCustomer: false);

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
        var branchId = _branchContext.CurrentBranchId;
        var customersQuery = context.Customers.AsQueryable();
        if (branchId is > 0)
            customersQuery = customersQuery.Where(c => c.BranchId == branchId);
        var customers = await customersQuery.Select(c => new { c.Id, c.Name }).ToListAsync();
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
        var branchId = _branchContext.CurrentBranchId;
        var suppliersQuery = context.Suppliers.AsQueryable();
        if (branchId is > 0)
            suppliersQuery = suppliersQuery.Where(s => s.BranchId == branchId);
        var suppliers = await suppliersQuery.Select(s => new { s.Id, s.Name }).ToListAsync();
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

    public Task UpdateCustomerOpeningBalanceAsync(OpeningPartyBalanceUpdateRequest request)
        => UpdateOpeningBalanceAsync(request, InvoiceType.Sale, "عميل");

    public Task UpdateSupplierOpeningBalanceAsync(OpeningPartyBalanceUpdateRequest request)
        => UpdateOpeningBalanceAsync(request, InvoiceType.Purchase, "مورد");

    public Task DeleteCustomerOpeningBalanceAsync(int invoiceId)
        => DeleteOpeningBalanceAsync(invoiceId, InvoiceType.Sale, "عميل");

    public Task DeleteSupplierOpeningBalanceAsync(int invoiceId)
        => DeleteOpeningBalanceAsync(invoiceId, InvoiceType.Purchase, "مورد");

    private async Task<OpeningPartyBalancePagedResult> GetOpeningBalancesAsync(
        OpeningPartyBalanceQuery query,
        InvoiceType invoiceType,
        bool isCustomer)
    {
        query ??= new OpeningPartyBalanceQuery();
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 500);
        var search = query.Search?.Trim();

        await using var context = await _contextFactory.CreateDbContextAsync();
        var invoices = context.Invoices
            .AsNoTracking()
            .Where(i => i.InvoiceType == invoiceType
                        && i.PaymentMethod == PaymentMethod.Credit
                        && i.Notes != null
                        && i.Notes.StartsWith(OpeningCreditBalanceMarkers.NotesPrefix));

        if (isCustomer)
            invoices = invoices.Where(i => i.CustomerId != null);
        else
            invoices = invoices.Where(i => i.SupplierId != null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            if (isCustomer)
            {
                invoices = invoices.Where(i =>
                    (i.Customer != null && i.Customer.Name.Contains(search))
                    || (i.Customer != null && i.Customer.Phone != null && i.Customer.Phone.Contains(search))
                    || (i.Customer != null && i.Customer.FileNumber != null && i.Customer.FileNumber.Contains(search))
                    || i.InvoiceNumber.Contains(search));
            }
            else
            {
                invoices = invoices.Where(i =>
                    (i.Supplier != null && i.Supplier.Name.Contains(search))
                    || (i.Supplier != null && i.Supplier.Phone != null && i.Supplier.Phone.Contains(search))
                    || i.InvoiceNumber.Contains(search));
            }
        }

        if (query.FromDate is DateTime from)
            invoices = invoices.Where(i => i.Date >= from.Date);
        if (query.ToDate is DateTime to)
            invoices = invoices.Where(i => i.Date <= to.Date);
        if (query.MinAmount is decimal minAmount)
            invoices = invoices.Where(i => i.NetAmount >= minAmount);
        if (query.MaxAmount is decimal maxAmount)
            invoices = invoices.Where(i => i.NetAmount <= maxAmount);
        if (query.UnpaidOnly)
            invoices = invoices.Where(i => !i.IsCreditPaid && i.RemainingAmount > 0);

        var totalCount = await invoices.CountAsync();

        var pageItems = await invoices
            .OrderByDescending(i => i.Date)
            .ThenByDescending(i => i.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new
            {
                i.Id,
                i.InvoiceNumber,
                PartyId = isCustomer ? i.CustomerId!.Value : i.SupplierId!.Value,
                PartyName = isCustomer
                    ? (i.Customer != null ? i.Customer.Name : string.Empty)
                    : (i.Supplier != null ? i.Supplier.Name : string.Empty),
                Phone = isCustomer
                    ? (i.Customer != null ? i.Customer.Phone : null)
                    : (i.Supplier != null ? i.Supplier.Phone : null),
                FileNumber = isCustomer && i.Customer != null ? i.Customer.FileNumber : null,
                Amount = i.NetAmount,
                i.PaidAmount,
                i.RemainingAmount,
                i.Date,
                i.Notes,
                i.IsCreditPaid,
                i.Currency,
                i.FxRate
            })
            .ToListAsync();

        var items = pageItems.Select(i => new OpeningPartyBalanceListItem
        {
            InvoiceId = i.Id,
            InvoiceNumber = i.InvoiceNumber,
            PartyId = i.PartyId,
            PartyName = i.PartyName,
            Phone = i.Phone,
            FileNumber = i.FileNumber,
            Amount = i.Amount,
            PaidAmount = i.PaidAmount,
            RemainingAmount = i.RemainingAmount,
            Date = i.Date,
            Notes = i.Notes,
            UserNotes = OpeningCreditBalanceMarkers.ExtractUserNotes(i.Notes),
            IsFullyPaid = i.IsCreditPaid || i.RemainingAmount <= 0,
            Currency = i.Currency,
            FxRate = i.FxRate
        }).ToList();

        return new OpeningPartyBalancePagedResult
        {
            Items = items,
            TotalCount = totalCount
        };
    }

    private async Task UpdateOpeningBalanceAsync(
        OpeningPartyBalanceUpdateRequest request,
        InvoiceType invoiceType,
        string partyLabel)
    {
        if (request.Amount <= 0)
            throw new InvalidOperationException("المبلغ يجب أن يكون أكبر من صفر");

        await using var context = await _contextFactory.CreateDbContextAsync();
        var (currency, fxRate) = await MultiCurrencyFeatureGate.ApplyForWriteAsync(
            context, request.Currency, request.FxRate, $"تعديل رصيد افتتاحي {partyLabel}");
        var amount = AccountingCurrencyHelper.NormalizeAmount(request.Amount, currency);

        var invoice = await context.Invoices.FirstOrDefaultAsync(i => i.Id == request.InvoiceId)
            ?? throw new InvalidOperationException("الرصيد الافتتاحي غير موجود");

        EnsureOpeningBalanceInvoice(invoice, invoiceType, partyLabel);

        if (invoice.PaidAmount > 0)
            throw new InvalidOperationException("لا يمكن تعديل رصيد تم تسديد جزء منه. احذف التسديدات أولاً أو أنشئ رصيداً جديداً.");

        await _periodLockService.EnsureDateAllowedAsync(invoice.Date);
        await _periodLockService.EnsureDateAllowedAsync(request.Date.Date);

        var username = _currentUserService.Username;
        var oldAmount = invoice.NetAmount;

        invoice.Date = request.Date.Date;
        invoice.TotalAmount = amount;
        invoice.NetAmount = amount;
        invoice.RemainingAmount = amount;
        invoice.PaidAmount = 0;
        invoice.IsCreditPaid = false;
        invoice.Currency = currency;
        invoice.FxRate = fxRate;
        invoice.Notes = OpeningCreditBalanceMarkers.BuildNotes(request.Notes);
        invoice.UpdatedBy = username;
        invoice.UpdatedAt = DateTime.UtcNow;

        if (_currentUserService.UserId.HasValue)
        {
            await context.AuditLogs.AddAsync(new AuditLog
            {
                UserId = _currentUserService.UserId.Value,
                Action = AuditAction.Edit,
                EntityName = nameof(Invoice),
                EntityId = invoice.Id,
                OldValues = $"رصيد افتتاحي {partyLabel} بمبلغ {oldAmount:N0}",
                NewValues = $"تعديل رصيد افتتاحي {partyLabel} بمبلغ {amount:N0} {request.Currency}",
                Timestamp = DateTime.UtcNow,
                CreatedBy = username,
                CreatedAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();
    }

    private async Task DeleteOpeningBalanceAsync(int invoiceId, InvoiceType invoiceType, string partyLabel)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var invoice = await context.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId)
            ?? throw new InvalidOperationException("الرصيد الافتتاحي غير موجود");

        EnsureOpeningBalanceInvoice(invoice, invoiceType, partyLabel);

        if (invoice.PaidAmount > 0)
            throw new InvalidOperationException("لا يمكن حذف رصيد تم تسديد جزء منه. احذف التسديدات المرتبطة أولاً.");

        await _periodLockService.EnsureDateAllowedAsync(invoice.Date);

        var username = _currentUserService.Username;
        invoice.MarkSoftDeleted(username);

        if (_currentUserService.UserId.HasValue)
        {
            await context.AuditLogs.AddAsync(new AuditLog
            {
                UserId = _currentUserService.UserId.Value,
                Action = AuditAction.Delete,
                EntityName = nameof(Invoice),
                EntityId = invoice.Id,
                OldValues = $"حذف رصيد افتتاحي {partyLabel} بمبلغ {invoice.NetAmount:N0}",
                Timestamp = DateTime.UtcNow,
                CreatedBy = username,
                CreatedAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();
    }

    private static void EnsureOpeningBalanceInvoice(Invoice invoice, InvoiceType invoiceType, string partyLabel)
    {
        if (invoice.InvoiceType != invoiceType
            || invoice.PaymentMethod != PaymentMethod.Credit
            || !OpeningCreditBalanceMarkers.IsOpeningCreditBalance(invoice.Notes))
        {
            throw new InvalidOperationException($"الفاتورة المحددة ليست رصيداً افتتاحياً لـ{partyLabel}");
        }
    }

    private async Task<Invoice> CreateCustomerCoreAsync(
        OpeningPartyBalanceRequest request,
        Dictionary<string, int>? nameCache)
    {
        ValidateRequest(request, "عميل");
        await _periodLockService.EnsureDateAllowedAsync(request.Date.Date);

        await using var context = await _contextFactory.CreateDbContextAsync();
        context.BypassBranchFilter = true;
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var username = _currentUserService.Username;
            var customerId = await ResolveCustomerIdAsync(context, request, username, nameCache);
            var warehouse = await ResolveWarehouseForOpeningAsync(context);
            var (currency, fxRate) = await MultiCurrencyFeatureGate.ApplyForWriteAsync(
                context, request.Currency, request.FxRate, "رصيد افتتاحي عميل");
            var amount = AccountingCurrencyHelper.NormalizeAmount(request.Amount, currency);

            // لا تكرار: إن وُجد رصيد افتتاحي آجل بنفس العملة وغير مسدَّد — حدّثه
            var existingOpening = await context.Invoices.IgnoreQueryFilters()
                .FirstOrDefaultAsync(i => !i.IsDeleted
                                          && i.CustomerId == customerId
                                          && i.InvoiceType == InvoiceType.Sale
                                          && i.PaymentMethod == PaymentMethod.Credit
                                          && i.Currency == currency
                                          && i.Notes != null
                                          && i.Notes.StartsWith(OpeningCreditBalanceMarkers.NotesPrefix));
            if (existingOpening is not null)
            {
                if (existingOpening.PaidAmount > 0)
                    return existingOpening;

                existingOpening.TotalAmount = amount;
                existingOpening.NetAmount = amount;
                existingOpening.RemainingAmount = amount;
                existingOpening.FxRate = fxRate;
                existingOpening.Date = request.Date.Date;
                existingOpening.Notes = OpeningCreditBalanceMarkers.BuildNotes(request.Notes);
                existingOpening.IsCreditPaid = false;
                existingOpening.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
                return existingOpening;
            }

            var branchId = _branchContext.CurrentBranchId ?? warehouse.BranchId;
            if (branchId <= 0)
                throw new InvalidOperationException("يجب اختيار فرع قبل إدخال أرصدة العملاء");

            var invoiceNumber = await InvoiceNumberHelper.GenerateNextAsync(context, InvoiceType.Sale, branchId);
            var invoice = new Invoice
            {
                InvoiceNumber = invoiceNumber,
                InvoiceType = InvoiceType.Sale,
                CustomerId = customerId,
                WarehouseId = warehouse.Id,
                BranchId = branchId,
                PaymentMethod = PaymentMethod.Credit,
                Currency = currency,
                FxRate = fxRate,
                TotalAmount = amount,
                DiscountAmount = 0,
                NetAmount = amount,
                RoundingAmount = 0,
                RoundingType = RoundingType.RoundDown,
                PaidAmount = 0,
                RemainingAmount = amount,
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
                    NewValues = $"رصيد افتتاحي آجل للعميل #{customerId} بمبلغ {request.Amount:N0} {currency}",
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
        await _periodLockService.EnsureDateAllowedAsync(request.Date.Date);

        await using var context = await _contextFactory.CreateDbContextAsync();
        context.BypassBranchFilter = true;
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var username = _currentUserService.Username;
            var supplierId = await ResolveSupplierIdAsync(context, request, username, nameCache);
            var warehouse = await ResolveWarehouseForOpeningAsync(context);
            var (currency, fxRate) = await MultiCurrencyFeatureGate.ApplyForWriteAsync(
                context, request.Currency, request.FxRate, "رصيد افتتاحي مورد");
            var amount = AccountingCurrencyHelper.NormalizeAmount(request.Amount, currency);

            var existingOpening = await context.Invoices.IgnoreQueryFilters()
                .FirstOrDefaultAsync(i => !i.IsDeleted
                                          && i.SupplierId == supplierId
                                          && i.InvoiceType == InvoiceType.Purchase
                                          && i.PaymentMethod == PaymentMethod.Credit
                                          && i.Currency == currency
                                          && i.Notes != null
                                          && i.Notes.StartsWith(OpeningCreditBalanceMarkers.NotesPrefix));
            if (existingOpening is not null)
            {
                if (existingOpening.PaidAmount > 0)
                    return existingOpening;

                existingOpening.TotalAmount = amount;
                existingOpening.NetAmount = amount;
                existingOpening.RemainingAmount = amount;
                existingOpening.FxRate = fxRate;
                existingOpening.Date = request.Date.Date;
                existingOpening.Notes = OpeningCreditBalanceMarkers.BuildNotes(request.Notes);
                existingOpening.IsCreditPaid = false;
                existingOpening.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
                return existingOpening;
            }

            var branchId = _branchContext.CurrentBranchId ?? warehouse.BranchId;
            if (branchId <= 0)
                throw new InvalidOperationException("يجب اختيار فرع قبل إدخال أرصدة الموردين");

            var invoiceNumber = await InvoiceNumberHelper.GenerateNextAsync(context, InvoiceType.Purchase, branchId);
            var invoice = new Invoice
            {
                InvoiceNumber = invoiceNumber,
                InvoiceType = InvoiceType.Purchase,
                SupplierId = supplierId,
                WarehouseId = warehouse.Id,
                BranchId = branchId,
                PaymentMethod = PaymentMethod.Credit,
                Currency = currency,
                FxRate = fxRate,
                TotalAmount = amount,
                DiscountAmount = 0,
                NetAmount = amount,
                RoundingAmount = 0,
                RoundingType = RoundingType.RoundDown,
                PaidAmount = 0,
                RemainingAmount = amount,
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
                    NewValues = $"رصيد افتتاحي آجل للمورد #{supplierId} بمبلغ {request.Amount:N0} {currency}",
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

    private async Task<Warehouse> ResolveWarehouseForOpeningAsync(AppDbContext context)
    {
        var currentId = _branchContext.CurrentBranchId;
        var allowed = _branchContext.AllowedBranchIds
            .Where(id => id > 0)
            .Distinct()
            .ToHashSet();
        if (allowed.Count == 0 && currentId is > 0)
            allowed.Add(currentId.Value);

        var query = context.Warehouses.IgnoreQueryFilters()
            .Where(w => !w.IsDeleted);
        if (allowed.Count > 0)
            query = query.Where(w => allowed.Contains(w.BranchId));

        var warehouse = await query
            .OrderBy(w => currentId.HasValue && w.BranchId == currentId.Value ? 0 : 1)
            .ThenBy(w => w.Id)
            .FirstOrDefaultAsync();

        return warehouse
               ?? throw new InvalidOperationException(
                   "يجب إنشاء مخزن واحد على الأقل لفرع مسموح قبل إدخال الأرصدة الافتتاحية");
    }

    private static void ValidateRequest(OpeningPartyBalanceRequest request, string partyLabel)
    {
        if (request.Amount <= 0)
            throw new InvalidOperationException("المبلغ يجب أن يكون أكبر من صفر");
        if (request.PartyId is null && string.IsNullOrWhiteSpace(request.PartyName))
            throw new InvalidOperationException($"يجب اختيار {partyLabel} أو إدخال اسمه");
        AccountingCurrencyRules.EnsureValidFxRate(request.Currency, request.FxRate, $"رصيد افتتاحي {partyLabel}");
        request.Amount = AccountingCurrencyHelper.NormalizeAmount(request.Amount, request.Currency);
    }

    private async Task<int> ResolveCustomerIdAsync(
        AppDbContext context,
        OpeningPartyBalanceRequest request,
        string username,
        Dictionary<string, int>? nameCache)
    {
        var branchId = _branchContext.CurrentBranchId
                       ?? throw new InvalidOperationException("يجب اختيار فرع قبل إدخال أرصدة العملاء");

        if (request.PartyId is int existingId)
        {
            var exists = await context.Customers.IgnoreQueryFilters()
                .AnyAsync(c => c.Id == existingId && !c.IsDeleted && c.BranchId == branchId);
            if (!exists)
                throw new InvalidOperationException("العميل المحدد غير موجود في الفرع الحالي");
            return existingId;
        }

        var name = request.PartyName!.Trim();
        var compact = ArabicNameNormalizer.Compact(name);

        if (nameCache is not null && compact.Length > 0 && nameCache.TryGetValue(compact, out var cachedId))
            return cachedId;

        var exact = await context.Customers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => !c.IsDeleted && c.BranchId == branchId && c.Name == name);
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
            var candidates = await context.Customers.IgnoreQueryFilters()
                .Where(c => !c.IsDeleted && c.BranchId == branchId)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();
            var match = candidates.FirstOrDefault(c => ArabicNameNormalizer.Compact(c.Name) == compact);
            if (match is not null)
                return match.Id;
        }

        var newCustomer = new Customer
        {
            Name = name,
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            FileNumber = string.IsNullOrWhiteSpace(request.FileNumber) ? null : request.FileNumber.Trim(),
            BranchId = branchId,
            CreatedBy = username,
            CreatedAt = DateTime.UtcNow
        };
        await context.Customers.AddAsync(newCustomer);
        await context.SaveChangesAsync();

        if (nameCache is not null && compact.Length > 0)
            nameCache[compact] = newCustomer.Id;

        return newCustomer.Id;
    }

    private async Task<int> ResolveSupplierIdAsync(
        AppDbContext context,
        OpeningPartyBalanceRequest request,
        string username,
        Dictionary<string, int>? nameCache)
    {
        var branchId = _branchContext.CurrentBranchId
                       ?? throw new InvalidOperationException("يجب اختيار فرع قبل إدخال أرصدة الموردين");

        if (request.PartyId is int existingId)
        {
            var exists = await context.Suppliers.IgnoreQueryFilters()
                .AnyAsync(s => s.Id == existingId && !s.IsDeleted && s.BranchId == branchId);
            if (!exists)
                throw new InvalidOperationException("المورد المحدد غير موجود في الفرع الحالي");
            return existingId;
        }

        var name = request.PartyName!.Trim();
        var compact = ArabicNameNormalizer.Compact(name);

        if (nameCache is not null && compact.Length > 0 && nameCache.TryGetValue(compact, out var cachedId))
            return cachedId;

        var exact = await context.Suppliers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => !s.IsDeleted && s.BranchId == branchId && s.Name == name);
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
            var candidates = await context.Suppliers.IgnoreQueryFilters()
                .Where(s => !s.IsDeleted && s.BranchId == branchId)
                .Select(s => new { s.Id, s.Name })
                .ToListAsync();
            var match = candidates.FirstOrDefault(s => ArabicNameNormalizer.Compact(s.Name) == compact);
            if (match is not null)
                return match.Id;
        }

        var newSupplier = new Supplier
        {
            Name = name,
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            BranchId = branchId,
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
