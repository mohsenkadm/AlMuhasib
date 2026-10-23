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

public sealed class CustomsInvoiceService : ICustomsInvoiceService
{
    private readonly IDbContextFactory<CustomsDbContext> _contextFactory;
    private readonly CustomsPermissionGuard _permissions;

    public CustomsInvoiceService(
        IDbContextFactory<CustomsDbContext> contextFactory,
        CustomsPermissionGuard permissions)
    {
        _contextFactory = contextFactory;
        _permissions = permissions;
    }

    public async Task<(IReadOnlyList<CustomsInvoiceListItem> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        CustomsInvoiceStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? customerId = null,
        string? sortBy = null,
        bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        _permissions.EnsureView(CustomsScreenNames.Invoices);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.CustomsInvoices.AsNoTracking().AsQueryable();

        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);

        if (customerId.HasValue)
            query = query.Where(i => i.CustomerId == customerId.Value);

        if (fromDate.HasValue)
        {
            var from = fromDate.Value.Date;
            query = query.Where(i => i.InvoiceDate >= from);
        }

        if (toDate.HasValue)
        {
            var toExclusive = toDate.Value.Date.AddDays(1);
            query = query.Where(i => i.InvoiceDate < toExclusive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(i =>
                i.InvoiceNumber.Contains(term) ||
                i.Customer.Name.Contains(term) ||
                i.Customer.Phone.Contains(term) ||
                i.Description.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var ordered = ApplyInvoiceSort(query, sortBy, sortDescending);
        var rows = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new CustomsInvoiceListItem
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                CustomerId = i.CustomerId,
                CustomerName = i.Customer.Name,
                ServiceType = i.ServiceType,
                Currency = i.Currency,
                TotalAmount = i.TotalAmount,
                PaidAmount = i.PaidAmount,
                RemainingAmount = i.RemainingAmount,
                Status = i.Status,
                VehicleCount = i.Vehicles.Count
            })
            .ToListAsync(cancellationToken);

        foreach (var item in rows)
        {
            item.ServiceTypeLabel = CustomsServiceTypeDisplay.ToArabic(item.ServiceType);
            item.CurrencyLabel = item.Currency == AccountingCurrency.USD ? "$" : "د.ع";
            item.StatusLabel = CustomsInvoiceStatusDisplay.ToArabic(item.Status);
        }

        return (rows, totalCount);
    }

    public async Task<CustomsInvoiceDetail?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _permissions.EnsureView(CustomsScreenNames.Invoices);
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var invoice = await db.CustomsInvoices.AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Vehicles)
            .Include(i => i.Expenses).ThenInclude(e => e.ExpenseType)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (invoice is null)
            return null;

        var attachments = await db.CustomsAttachments.AsNoTracking()
            .Where(a => a.OwnerType == CustomsAttachmentOwnerType.Invoice && a.OwnerId == id)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        var detail = ToDetail(invoice, attachments);
        if (!_permissions.CanViewExpenses)
            detail.Expenses = [];
        return detail;
    }

    public async Task<string> PreviewNextInvoiceNumberAsync(CancellationToken cancellationToken = default)
    {
        _permissions.EnsureView(CustomsScreenNames.Invoices);
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await CustomsInvoiceNumberHelper.GetNextAsync(db, cancellationToken);
    }

    public async Task<CustomsInvoiceDetail> CreateAsync(
        CustomsInvoiceSaveRequest request,
        CancellationToken cancellationToken = default)
    {
        _permissions.EnsureAdd(CustomsScreenNames.Invoices);
        var canManageExpenses = _permissions.CanViewExpenses;
        if (!canManageExpenses && request.Expenses.Count > 0)
            _permissions.EnsureView(CustomsScreenNames.Expenses);
        ValidateRequest(request);

        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureCustomerExistsAsync(db, request.CustomerId, cancellationToken);

        var fxRate = CustomsFinancialRules.NormalizeFxRate(
            request.Currency,
            await ResolveFxRateAsync(db, request.Currency, request.InvoiceDate, request.FxRate, cancellationToken),
            "الفاتورة");
        var vehiclesSum = request.Vehicles.Sum(v => v.VehiclePrice);
        var total = Math.Max(0, vehiclesSum - request.DiscountAmount);

        var invoice = new CustomsInvoice
        {
            CustomerId = request.CustomerId,
            InvoiceNumber = await CustomsInvoiceNumberHelper.GetNextAsync(db, cancellationToken),
            InvoiceDate = request.InvoiceDate.Date,
            ServiceType = request.ServiceType,
            Currency = request.Currency,
            FxRate = fxRate,
            Description = request.Description?.Trim() ?? string.Empty,
            DiscountAmount = request.DiscountAmount,
            TotalAmount = total,
            PaidAmount = 0,
            RemainingAmount = CustomsFinancialRules.ComputeRemaining(total, 0),
            Status = CustomsInvoiceStatus.Draft,
            InternalNotes = request.InternalNotes?.Trim() ?? string.Empty,
            CustomerNotes = request.CustomerNotes?.Trim() ?? string.Empty,
            CreatedBy = request.UserName
        };

        foreach (var v in request.Vehicles)
            invoice.Vehicles.Add(CreateVehicle(v, request.Currency));

        if (canManageExpenses)
        {
            foreach (var e in request.Expenses)
                invoice.Expenses.Add(await CreateExpenseAsync(db, e, cancellationToken));
        }

        await db.CustomsInvoices.AddAsync(invoice, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await CustomsAuditWriter.WriteAsync(
            db,
            AuditAction.Add,
            "CustomsInvoice",
            invoice.Id,
            oldValues: null,
            newValues: new
            {
                invoice.InvoiceNumber,
                invoice.CustomerId,
                invoice.TotalAmount,
                Currency = invoice.Currency.ToString(),
                Status = invoice.Status.ToString(),
                VehicleCount = invoice.Vehicles.Count,
                ExpenseCount = invoice.Expenses.Count
            },
            cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(invoice.Id, cancellationToken))!;
    }

    public async Task<CustomsInvoiceDetail> UpdateAsync(
        int id,
        CustomsInvoiceSaveRequest request,
        CancellationToken cancellationToken = default)
    {
        _permissions.EnsureEdit(CustomsScreenNames.Invoices);
        var canManageExpenses = _permissions.CanViewExpenses;
        ValidateRequest(request);

        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var invoice = await db.CustomsInvoices
            .Include(i => i.Vehicles)
            .Include(i => i.Expenses)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("الفاتورة غير موجودة");

        if (invoice.Status == CustomsInvoiceStatus.Cancelled)
            throw new InvalidOperationException("لا يمكن تعديل فاتورة ملغاة");

        await EnsureCustomerExistsAsync(db, request.CustomerId, cancellationToken);

        var previousCustomerId = invoice.CustomerId;
        var hasAllocations = await db.CustomsReceiptAllocations.AsNoTracking()
            .AnyAsync(a => a.InvoiceId == invoice.Id, cancellationToken);
        if ((invoice.PaidAmount > 0 || hasAllocations) && request.Currency != invoice.Currency)
            throw new InvalidOperationException("لا يمكن تغيير عملة فاتورة عليها مقبوضات/تخصيصات");

        var fxRate = await ResolveFxRateAsync(db, request.Currency, request.InvoiceDate, request.FxRate, cancellationToken);
        fxRate = CustomsFinancialRules.NormalizeFxRate(request.Currency, fxRate, "الفاتورة");
        var vehiclesSum = request.Vehicles.Sum(v => v.VehiclePrice);
        var total = Math.Max(0, vehiclesSum - request.DiscountAmount);

        if (invoice.Status == CustomsInvoiceStatus.Approved && total < invoice.PaidAmount)
            throw new InvalidOperationException("الإجمالي الجديد أقل من المدفوع — لا يمكن الحفظ");

        var oldSnapshot = new
        {
            invoice.CustomerId,
            invoice.TotalAmount,
            invoice.DiscountAmount,
            invoice.PaidAmount,
            invoice.RemainingAmount,
            Currency = invoice.Currency.ToString(),
            Status = invoice.Status.ToString(),
            VehicleCount = invoice.Vehicles.Count(v => !v.IsDeleted),
            ExpenseCount = invoice.Expenses.Count(e => !e.IsDeleted)
        };

        invoice.CustomerId = request.CustomerId;
        invoice.InvoiceDate = request.InvoiceDate.Date;
        invoice.ServiceType = request.ServiceType;
        invoice.Currency = request.Currency;
        invoice.FxRate = fxRate;
        invoice.Description = request.Description?.Trim() ?? string.Empty;
        invoice.DiscountAmount = request.DiscountAmount;
        invoice.TotalAmount = total;
        invoice.InternalNotes = request.InternalNotes?.Trim() ?? string.Empty;
        invoice.CustomerNotes = request.CustomerNotes?.Trim() ?? string.Empty;
        invoice.UpdatedBy = request.UserName;
        invoice.UpdatedAt = DateTime.UtcNow;

        var user = request.UserName ?? "System";
        UpsertVehicles(invoice, request.Vehicles, request.Currency, user);
        // Without expenses permission, leave existing expense rows untouched (UI hides that section).
        if (canManageExpenses)
            await UpsertExpensesAsync(db, invoice, request.Expenses, user, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        // Rule 5: Remaining = Total − allocated receipts (recompute Paid from allocations).
        await CustomsSettlementHelper.RecomputeInvoiceSettlementAsync(db, [invoice.Id], cancellationToken);
        if (invoice.Status == CustomsInvoiceStatus.Approved)
        {
            await CustomsSettlementHelper.RecomputeCustomerBalanceAsync(db, invoice.CustomerId, cancellationToken);
            if (previousCustomerId != invoice.CustomerId)
                await CustomsSettlementHelper.RecomputeCustomerBalanceAsync(db, previousCustomerId, cancellationToken);
        }

        await CustomsAuditWriter.WriteAsync(
            db,
            AuditAction.Edit,
            "CustomsInvoice",
            invoice.Id,
            oldValues: oldSnapshot,
            newValues: new
            {
                invoice.CustomerId,
                invoice.TotalAmount,
                invoice.DiscountAmount,
                invoice.PaidAmount,
                invoice.RemainingAmount,
                Currency = invoice.Currency.ToString(),
                Status = invoice.Status.ToString(),
                VehicleCount = request.Vehicles.Count,
                ExpenseCount = request.Expenses.Count
            },
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(invoice.Id, cancellationToken))!;
    }

    public async Task ApproveAsync(int id, string userName, CancellationToken cancellationToken = default)
    {
        _permissions.EnsureEdit(CustomsScreenNames.Invoices);
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var invoice = await db.CustomsInvoices
            .Include(i => i.Vehicles)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("الفاتورة غير موجودة");

        if (invoice.Status != CustomsInvoiceStatus.Draft)
            throw new InvalidOperationException("يمكن اعتماد المسودات فقط");

        if (invoice.CustomerId <= 0)
            throw new InvalidOperationException("العميل مطلوب للاعتماد");

        if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
            throw new InvalidOperationException("رقم الفاتورة مطلوب");

        if (invoice.Vehicles.Count == 0)
            throw new InvalidOperationException("أضف سيارة واحدة على الأقل قبل الاعتماد");

        var previous = invoice.Status;
        invoice.Status = CustomsInvoiceStatus.Approved;
        invoice.UpdatedBy = userName;
        invoice.UpdatedAt = DateTime.UtcNow;

        await CustomsAuditWriter.WriteAsync(
            db,
            AuditAction.Edit,
            "CustomsInvoice.Approve",
            invoice.Id,
            oldValues: new { Status = previous.ToString() },
            newValues: new { Status = CustomsInvoiceStatus.Approved.ToString() },
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        // Rule 8: customer balance is outstanding on approved invoices — not profit.
        await CustomsSettlementHelper.RecomputeCustomerBalanceAsync(db, invoice.CustomerId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelAsync(
        int id,
        string userName,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        _permissions.EnsureEdit(CustomsScreenNames.Invoices);
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var invoice = await db.CustomsInvoices.FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("الفاتورة غير موجودة");

        if (invoice.Status == CustomsInvoiceStatus.Cancelled)
            throw new InvalidOperationException("الفاتورة ملغاة مسبقاً");

        if (invoice.PaidAmount > 0)
            throw new InvalidOperationException("لا يمكن إلغاء فاتورة عليها مبالغ مدفوعة");

        // H-02: reject even when PaidAmount is stale/zero but active allocations still exist.
        var hasActiveAllocations = await db.CustomsReceiptAllocations.AsNoTracking()
            .AnyAsync(a => a.InvoiceId == invoice.Id, cancellationToken);
        if (hasActiveAllocations)
            throw new InvalidOperationException("لا يمكن إلغاء فاتورة عليها توزيعات قبض نشطة — ألغِ الوصولات أولاً");

        // Rule 9: Cancel keeps the row (Status = Cancelled) and audit trail; never hard-delete approved/cancelled invoices.
        var previousStatus = invoice.Status;
        var remaining = invoice.RemainingAmount;
        var customerId = invoice.CustomerId;
        invoice.Status = CustomsInvoiceStatus.Cancelled;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            var note = reason.Trim();
            invoice.InternalNotes = string.IsNullOrWhiteSpace(invoice.InternalNotes)
                ? $"إلغاء: {note}"
                : $"{invoice.InternalNotes}\nإلغاء: {note}";
        }

        invoice.UpdatedBy = userName;
        invoice.UpdatedAt = DateTime.UtcNow;

        await CustomsAuditWriter.WriteAsync(
            db,
            AuditAction.Edit,
            "CustomsInvoice.Cancel",
            invoice.Id,
            oldValues: new
            {
                Status = previousStatus.ToString(),
                RemainingAmount = remaining,
                invoice.PaidAmount
            },
            newValues: new
            {
                Status = CustomsInvoiceStatus.Cancelled.ToString(),
                RemainingAmount = remaining,
                Reason = reason?.Trim()
            },
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        await CustomsSettlementHelper.RecomputeCustomerBalanceAsync(db, customerId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Soft-delete drafts only. Approved/cancelled invoices must use Cancel (row retained) — no hard-delete of financial trail.
    /// </summary>
    public async Task SoftDeleteAsync(int id, string deletedBy, CancellationToken cancellationToken = default)
    {
        _permissions.EnsureDelete(CustomsScreenNames.Invoices);
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var invoice = await db.CustomsInvoices
            .Include(i => i.Vehicles)
            .Include(i => i.Expenses)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("الفاتورة غير موجودة");

        if (invoice.Status != CustomsInvoiceStatus.Draft)
            throw new InvalidOperationException("يمكن حذف المسودات فقط — استخدم الإلغاء للفواتير المعتمدة");

        if (invoice.PaidAmount > 0)
            throw new InvalidOperationException("لا يمكن حذف فاتورة عليها مبالغ مدفوعة");

        var hasActiveAllocations = await db.CustomsReceiptAllocations.AsNoTracking()
            .AnyAsync(a => a.InvoiceId == invoice.Id, cancellationToken);
        if (hasActiveAllocations)
            throw new InvalidOperationException("لا يمكن حذف فاتورة عليها توزيعات قبض نشطة — ألغِ الوصولات أولاً");

        // M-01: cascade soft-delete of Invoice/Vehicle/Expense attachments (same lifecycle as CustomsAttachmentService).
        var vehicleIds = invoice.Vehicles.Select(v => v.Id).ToList();
        var expenseIds = invoice.Expenses.Select(e => e.Id).ToList();
        var attachments = await db.CustomsAttachments
            .Where(a =>
                (a.OwnerType == CustomsAttachmentOwnerType.Invoice && a.OwnerId == invoice.Id)
                || (a.OwnerType == CustomsAttachmentOwnerType.Vehicle && vehicleIds.Contains(a.OwnerId))
                || (a.OwnerType == CustomsAttachmentOwnerType.Expense && expenseIds.Contains(a.OwnerId)))
            .ToListAsync(cancellationToken);

        var attachmentPaths = new List<string>(attachments.Count);
        foreach (var attachment in attachments)
        {
            attachmentPaths.Add(attachment.FilePath);
            attachment.MarkSoftDeleted(deletedBy);
        }

        foreach (var v in invoice.Vehicles)
            v.MarkSoftDeleted(deletedBy);
        foreach (var e in invoice.Expenses)
            e.MarkSoftDeleted(deletedBy);

        invoice.MarkSoftDeleted(deletedBy);
        await db.SaveChangesAsync(cancellationToken);

        // Best-effort disk cleanup after metadata soft-delete (ownership paths are unique per owner).
        foreach (var path in attachmentPaths)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // Metadata already soft-deleted; ignore disk failures.
            }
        }
    }

    /// <summary>M-04: allowlisted invoice list sort. Default remains InvoiceDate DESC, Id DESC.</summary>
    private static IOrderedQueryable<CustomsInvoice> ApplyInvoiceSort(
        IQueryable<CustomsInvoice> query,
        string? sortBy,
        bool sortDescending)
    {
        var key = string.IsNullOrWhiteSpace(sortBy) ? "InvoiceDate" : sortBy.Trim();
        return key switch
        {
            "InvoiceDate" => CustomsListSort.ThenByDir(
                CustomsListSort.By(query, sortDescending, i => i.InvoiceDate),
                sortDescending,
                i => i.Id),
            "Id" => CustomsListSort.By(query, sortDescending, i => i.Id),
            "InvoiceNumber" => CustomsListSort.By(query, sortDescending, i => i.InvoiceNumber),
            "RemainingAmount" => CustomsListSort.By(query, sortDescending, i => i.RemainingAmount),
            "TotalAmount" => CustomsListSort.By(query, sortDescending, i => i.TotalAmount),
            "PaidAmount" => CustomsListSort.By(query, sortDescending, i => i.PaidAmount),
            "Status" => CustomsListSort.By(query, sortDescending, i => i.Status),
            _ => throw new InvalidOperationException($"عمود الفرز غير مسموح: {key}")
        };
    }

    private static void ValidateRequest(CustomsInvoiceSaveRequest request)
    {
        if (request.CustomerId <= 0)
            throw new InvalidOperationException("اختر العميل");

        if (request.DiscountAmount < 0)
            throw new InvalidOperationException("الخصم لا يمكن أن يكون سالباً");

        var vehiclesSum = request.Vehicles.Sum(v => v.VehiclePrice);
        if (request.DiscountAmount > vehiclesSum)
            throw new InvalidOperationException("الخصم لا يمكن أن يتجاوز مجموع أسعار السيارات");

        foreach (var v in request.Vehicles)
        {
            if (v.VehiclePrice < 0)
                throw new InvalidOperationException("سعر السيارة لا يمكن أن يكون سالباً");
        }

        foreach (var e in request.Expenses)
        {
            if (e.ExpenseTypeId <= 0)
                throw new InvalidOperationException("نوع المصروف مطلوب");
            if (e.Amount < 0)
                throw new InvalidOperationException("مبلغ المصروف لا يمكن أن يكون سالباً");
        }
    }

    private static async Task EnsureCustomerExistsAsync(
        CustomsDbContext db,
        int customerId,
        CancellationToken cancellationToken)
    {
        var exists = await db.CustomsCustomers.AnyAsync(c => c.Id == customerId && c.IsActive, cancellationToken);
        if (!exists)
            throw new InvalidOperationException("العميل غير موجود أو غير نشط");
    }

    private static async Task<decimal> ResolveFxRateAsync(
        CustomsDbContext db,
        AccountingCurrency currency,
        DateTime invoiceDate,
        decimal? requested,
        CancellationToken cancellationToken)
    {
        if (currency == AccountingCurrency.IQD)
            return 1m;

        if (requested is > 0)
            return requested.Value;

        var date = invoiceDate.Date;
        var rate = await db.CustomsFxRates.AsNoTracking()
            .Where(r => r.RateDate <= date)
            .OrderByDescending(r => r.RateDate)
            .Select(r => (decimal?)r.UsdToIqd)
            .FirstOrDefaultAsync(cancellationToken);

        if (rate is null or <= 0)
            throw new InvalidOperationException("لا يوجد سعر صرف محفوظ لتاريخ الفاتورة — أدخل FxRate يدوياً");

        return rate.Value;
    }

    private static void UpsertVehicles(
        CustomsInvoice invoice,
        IReadOnlyList<CustomsInvoiceVehicleDraft> drafts,
        AccountingCurrency currency,
        string user)
    {
        var existingById = invoice.Vehicles
            .Where(v => !v.IsDeleted)
            .ToDictionary(v => v.Id);

        var keptIds = new HashSet<int>();
        foreach (var draft in drafts)
        {
            if (draft.Id is int id && existingById.TryGetValue(id, out var existing))
            {
                ApplyVehicleFields(existing, draft, currency);
                existing.UpdatedBy = user;
                existing.UpdatedAt = DateTime.UtcNow;
                keptIds.Add(id);
            }
            else
            {
                invoice.Vehicles.Add(CreateVehicle(draft, currency));
            }
        }

        foreach (var old in invoice.Vehicles.Where(v => !v.IsDeleted && v.Id > 0 && !keptIds.Contains(v.Id)).ToList())
            old.MarkSoftDeleted(user);
    }

    private static CustomsVehicle CreateVehicle(CustomsInvoiceVehicleDraft draft, AccountingCurrency currency)
    {
        var vehicle = new CustomsVehicle { Currency = currency };
        ApplyVehicleFields(vehicle, draft, currency);
        return vehicle;
    }

    private static void ApplyVehicleFields(
        CustomsVehicle vehicle,
        CustomsInvoiceVehicleDraft draft,
        AccountingCurrency currency)
    {
        // Rule 1: vehicle price shares the invoice Currency (forced here — no independent vehicle currency).
        vehicle.SerialNumber = draft.SerialNumber?.Trim() ?? string.Empty;
        vehicle.IraqiPlate = draft.IraqiPlate?.Trim() ?? string.Empty;
        vehicle.IraqiDriver = draft.IraqiDriver?.Trim() ?? string.Empty;
        vehicle.SyrianPlate = draft.SyrianPlate?.Trim() ?? string.Empty;
        vehicle.SyrianDriver = draft.SyrianDriver?.Trim() ?? string.Empty;
        vehicle.CargoType = draft.CargoType?.Trim() ?? string.Empty;
        vehicle.LoadingDate = draft.LoadingDate;
        vehicle.ExitDate = draft.ExitDate;
        vehicle.WarehouseArrivalDate = draft.WarehouseArrivalDate;
        vehicle.UnloadingDate = draft.UnloadingDate;
        vehicle.VehiclePrice = draft.VehiclePrice;
        vehicle.Currency = currency;
        vehicle.Status = draft.Status;
        vehicle.Notes = draft.Notes?.Trim() ?? string.Empty;
    }

    private static async Task UpsertExpensesAsync(
        CustomsDbContext db,
        CustomsInvoice invoice,
        IReadOnlyList<CustomsInvoiceExpenseDraft> drafts,
        string user,
        CancellationToken cancellationToken)
    {
        var existingById = invoice.Expenses
            .Where(e => !e.IsDeleted)
            .ToDictionary(e => e.Id);

        var keptIds = new HashSet<int>();
        foreach (var draft in drafts)
        {
            await EnsureExpenseTypeExistsAsync(db, draft.ExpenseTypeId, cancellationToken);

            if (draft.Id is int id && existingById.TryGetValue(id, out var existing))
            {
                ApplyExpenseFields(existing, draft);
                existing.UpdatedBy = user;
                existing.UpdatedAt = DateTime.UtcNow;
                keptIds.Add(id);
            }
            else
            {
                invoice.Expenses.Add(CreateExpense(draft));
            }
        }

        foreach (var old in invoice.Expenses.Where(e => !e.IsDeleted && e.Id > 0 && !keptIds.Contains(e.Id)).ToList())
            old.MarkSoftDeleted(user);
    }

    private static async Task<CustomsExpense> CreateExpenseAsync(
        CustomsDbContext db,
        CustomsInvoiceExpenseDraft draft,
        CancellationToken cancellationToken)
    {
        await EnsureExpenseTypeExistsAsync(db, draft.ExpenseTypeId, cancellationToken);
        return CreateExpense(draft);
    }

    private static async Task EnsureExpenseTypeExistsAsync(
        CustomsDbContext db,
        int expenseTypeId,
        CancellationToken cancellationToken)
    {
        var typeExists = await db.CustomsExpenseTypes.AnyAsync(t => t.Id == expenseTypeId, cancellationToken);
        if (!typeExists)
            throw new InvalidOperationException("نوع المصروف غير موجود");
    }

    private static CustomsExpense CreateExpense(CustomsInvoiceExpenseDraft draft)
    {
        var expense = new CustomsExpense();
        ApplyExpenseFields(expense, draft);
        return expense;
    }

    private static void ApplyExpenseFields(CustomsExpense expense, CustomsInvoiceExpenseDraft draft)
    {
        var currency = draft.Currency;
        // Rule 1: every amount has Currency + valid Fx (no silent USD FxRate=1).
        var fx = CustomsFinancialRules.NormalizeFxRate(currency, draft.FxRate, "المصروف");

        expense.ExpenseTypeId = draft.ExpenseTypeId;
        expense.Description = draft.Description?.Trim() ?? string.Empty;
        expense.Amount = draft.Amount;
        expense.Currency = currency;
        expense.FxRate = fx;
        expense.ExpenseDate = draft.ExpenseDate.Date;
        expense.Recipient = draft.Recipient?.Trim() ?? string.Empty;
    }

    private static CustomsInvoiceDetail ToDetail(CustomsInvoice i, List<CustomsAttachment> attachments)
    {
        var vehiclesSum = i.Vehicles.Sum(v => v.VehiclePrice);
        return new CustomsInvoiceDetail
        {
            Id = i.Id,
            InvoiceNumber = i.InvoiceNumber,
            InvoiceDate = i.InvoiceDate,
            CustomerId = i.CustomerId,
            CustomerName = i.Customer?.Name ?? string.Empty,
            ServiceType = i.ServiceType,
            ServiceTypeLabel = CustomsServiceTypeDisplay.ToArabic(i.ServiceType),
            Currency = i.Currency,
            CurrencyLabel = i.Currency == AccountingCurrency.USD ? "$" : "د.ع",
            FxRate = i.FxRate,
            Description = i.Description,
            DiscountAmount = i.DiscountAmount,
            VehiclesSum = vehiclesSum,
            TotalAmount = i.TotalAmount,
            PaidAmount = i.PaidAmount,
            RemainingAmount = i.RemainingAmount,
            Status = i.Status,
            StatusLabel = CustomsInvoiceStatusDisplay.ToArabic(i.Status),
            InternalNotes = i.InternalNotes,
            CustomerNotes = i.CustomerNotes,
            Vehicles = i.Vehicles.Select(v => new CustomsInvoiceVehicleDraft
            {
                Id = v.Id,
                SerialNumber = v.SerialNumber,
                IraqiPlate = v.IraqiPlate,
                IraqiDriver = v.IraqiDriver,
                SyrianPlate = v.SyrianPlate,
                SyrianDriver = v.SyrianDriver,
                CargoType = v.CargoType,
                LoadingDate = v.LoadingDate,
                ExitDate = v.ExitDate,
                WarehouseArrivalDate = v.WarehouseArrivalDate,
                UnloadingDate = v.UnloadingDate,
                VehiclePrice = v.VehiclePrice,
                Status = v.Status,
                Notes = v.Notes
            }).ToList(),
            Expenses = i.Expenses.Select(e => new CustomsInvoiceExpenseDraft
            {
                Id = e.Id,
                ExpenseTypeId = e.ExpenseTypeId,
                ExpenseTypeName = e.ExpenseType?.Name ?? string.Empty,
                Description = e.Description,
                Amount = e.Amount,
                Currency = e.Currency,
                FxRate = e.FxRate,
                ExpenseDate = e.ExpenseDate,
                Recipient = e.Recipient
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
}
