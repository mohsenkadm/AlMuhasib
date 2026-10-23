using AlMuhasib.Core.Entities.Customs;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Enums.Customs;
using AlMuhasib.Core.Interfaces.Services.Customs;
using AlMuhasib.Core.Models.Customs;
using AlMuhasib.Infrastructure.Data.Customs;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Customs;

public sealed class CustomsCustomerService : ICustomsCustomerService
{
    private readonly IDbContextFactory<CustomsDbContext> _contextFactory;

    public CustomsCustomerService(IDbContextFactory<CustomsDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<(IReadOnlyList<CustomsCustomerListItem> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        bool? activeOnly = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.CustomsCustomers.AsNoTracking().AsQueryable();

        if (activeOnly == true)
            query = query.Where(c => c.IsActive);
        else if (activeOnly == false)
            query = query.Where(c => !c.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                c.Name.Contains(term) ||
                c.Phone.Contains(term) ||
                c.CompanyName.Contains(term) ||
                c.Address.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var customers = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var customerIds = customers.Select(c => c.Id).ToList();
        var invoiceStats = customerIds.Count == 0
            ? new Dictionary<int, (int OpenCount, DateTime? LastDate)>()
            : await db.CustomsInvoices.AsNoTracking()
                .Where(i => customerIds.Contains(i.CustomerId)
                            && i.Status != CustomsInvoiceStatus.Cancelled)
                .GroupBy(i => i.CustomerId)
                .Select(g => new
                {
                    CustomerId = g.Key,
                    OpenCount = g.Count(x => x.RemainingAmount > 0),
                    LastDate = g.Max(x => (DateTime?)x.InvoiceDate)
                })
                .ToDictionaryAsync(
                    x => x.CustomerId,
                    x => (x.OpenCount, x.LastDate),
                    cancellationToken);

        var items = customers.Select(c =>
        {
            invoiceStats.TryGetValue(c.Id, out var stats);
            return new CustomsCustomerListItem
            {
                Id = c.Id,
                CustomerType = c.CustomerType,
                CustomerTypeLabel = CustomsCustomerTypeDisplay.ToArabic(c.CustomerType),
                Name = c.Name,
                CompanyName = c.CompanyName,
                Phone = c.Phone,
                Address = c.Address,
                Notes = c.Notes,
                IsActive = c.IsActive,
                BalanceIqd = c.BalanceIqd,
                BalanceUsd = c.BalanceUsd,
                OpenInvoiceCount = stats.OpenCount,
                LastTransactionDate = stats.LastDate
            };
        }).ToList();

        return (items, totalCount);
    }

    public async Task<CustomsCustomer?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.CustomsCustomers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<CustomsCustomer> CreateAsync(CustomsCustomer customer, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customer.Name))
            throw new InvalidOperationException("اسم العميل مطلوب");

        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        customer.Name = customer.Name.Trim();
        customer.CompanyName = customer.CompanyName?.Trim() ?? string.Empty;
        customer.Phone = customer.Phone?.Trim() ?? string.Empty;
        customer.Address = customer.Address?.Trim() ?? string.Empty;
        customer.Notes = customer.Notes?.Trim() ?? string.Empty;
        if (customer.CustomerType != CustomsCustomerType.Company)
            customer.CompanyName = string.Empty;

        await db.CustomsCustomers.AddAsync(customer, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return customer;
    }

    public async Task<CustomsCustomer> UpdateAsync(CustomsCustomer customer, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customer.Name))
            throw new InvalidOperationException("اسم العميل مطلوب");

        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await db.CustomsCustomers.FirstOrDefaultAsync(c => c.Id == customer.Id, cancellationToken)
            ?? throw new InvalidOperationException("العميل غير موجود");

        existing.CustomerType = customer.CustomerType;
        existing.Name = customer.Name.Trim();
        existing.CompanyName = customer.CustomerType == CustomsCustomerType.Company
            ? (customer.CompanyName?.Trim() ?? string.Empty)
            : string.Empty;
        existing.Phone = customer.Phone?.Trim() ?? string.Empty;
        existing.Address = customer.Address?.Trim() ?? string.Empty;
        existing.Notes = customer.Notes?.Trim() ?? string.Empty;
        existing.IsActive = customer.IsActive;
        existing.UpdatedBy = customer.UpdatedBy;
        existing.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task SoftDeleteAsync(int id, string deletedBy, CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var customer = await db.CustomsCustomers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("العميل غير موجود");

        var hasOpenBalance = customer.BalanceIqd > 0 || customer.BalanceUsd > 0;
        if (hasOpenBalance)
            throw new InvalidOperationException("لا يمكن حذف عميل عليه رصيد");

        var hasInvoices = await db.CustomsInvoices.AnyAsync(
            i => i.CustomerId == id && i.Status != CustomsInvoiceStatus.Cancelled,
            cancellationToken);
        if (hasInvoices)
            throw new InvalidOperationException("لا يمكن حذف عميل لديه فواتير — ألغِ الفواتير أولاً أو أوقف العميل");

        customer.MarkSoftDeleted(deletedBy);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CustomsCustomerFile?> GetCustomerFileAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var customer = await db.CustomsCustomers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);
        if (customer is null)
            return null;

        var invoices = await db.CustomsInvoices.AsNoTracking()
            .Where(i => i.CustomerId == customerId)
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Id)
            .ToListAsync(cancellationToken);

        var invoiceIds = invoices.Select(i => i.Id).ToList();
        var vehicles = invoiceIds.Count == 0
            ? []
            : await db.CustomsVehicles.AsNoTracking()
                .Include(v => v.Invoice)
                .Where(v => invoiceIds.Contains(v.InvoiceId))
                .OrderByDescending(v => v.Id)
                .ToListAsync(cancellationToken);

        var receipts = await db.CustomsReceipts.AsNoTracking()
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.ReceiptDate)
            .ThenByDescending(r => r.Id)
            .ToListAsync(cancellationToken);

        var attachments = await db.CustomsAttachments.AsNoTracking()
            .Where(a => a.OwnerType == CustomsAttachmentOwnerType.Customer && a.OwnerId == customerId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        var activeInvoices = invoices.Where(i => i.Status != CustomsInvoiceStatus.Cancelled).ToList();
        var approvedInvoices = invoices.Where(i => i.Status == CustomsInvoiceStatus.Approved).ToList();

        var debitIqd = approvedInvoices.Where(i => i.Currency == AccountingCurrency.IQD).Sum(i => i.TotalAmount);
        var debitUsd = approvedInvoices.Where(i => i.Currency == AccountingCurrency.USD).Sum(i => i.TotalAmount);
        var creditIqd = receipts.Where(r => r.Currency == AccountingCurrency.IQD).Sum(r => r.Amount);
        var creditUsd = receipts.Where(r => r.Currency == AccountingCurrency.USD).Sum(r => r.Amount);

        return new CustomsCustomerFile
        {
            Id = customer.Id,
            CustomerType = customer.CustomerType,
            CustomerTypeLabel = CustomsCustomerTypeDisplay.ToArabic(customer.CustomerType),
            Name = customer.Name,
            CompanyName = customer.CompanyName,
            Phone = customer.Phone,
            Address = customer.Address,
            Notes = customer.Notes,
            IsActive = customer.IsActive,
            BalanceIqd = customer.BalanceIqd,
            BalanceUsd = customer.BalanceUsd,
            DebitIqd = debitIqd,
            DebitUsd = debitUsd,
            CreditIqd = creditIqd,
            CreditUsd = creditUsd,
            InvoiceCount = activeInvoices.Count,
            UnpaidInvoiceCount = activeInvoices.Count(i => i.RemainingAmount > 0),
            VehicleCount = vehicles.Count,
            ReceiptCount = receipts.Count,
            Invoices = invoices.Select(i => new CustomsCustomerInvoiceRow
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                ServiceTypeLabel = CustomsServiceTypeDisplay.ToArabic(i.ServiceType),
                Currency = i.Currency,
                CurrencyLabel = i.Currency == AccountingCurrency.USD ? "$" : "د.ع",
                TotalAmount = i.TotalAmount,
                PaidAmount = i.PaidAmount,
                RemainingAmount = i.RemainingAmount,
                Status = i.Status,
                StatusLabel = CustomsInvoiceStatusDisplay.ToArabic(i.Status)
            }).ToList(),
            Vehicles = vehicles.Select(v => new CustomsCustomerVehicleRow
            {
                Id = v.Id,
                InvoiceNumber = v.Invoice.InvoiceNumber,
                SerialNumber = v.SerialNumber,
                IraqiPlate = v.IraqiPlate,
                SyrianPlate = v.SyrianPlate,
                IraqiDriver = v.IraqiDriver,
                SyrianDriver = v.SyrianDriver,
                CargoType = v.CargoType,
                StatusLabel = CustomsVehicleStatusDisplay.ToArabic(v.Status),
                VehiclePrice = v.VehiclePrice,
                CurrencyLabel = v.Currency == AccountingCurrency.USD ? "$" : "د.ع"
            }).ToList(),
            Receipts = receipts.Select(r => new CustomsCustomerReceiptRow
            {
                Id = r.Id,
                ReceiptNumber = r.ReceiptNumber,
                ReceiptDate = r.ReceiptDate,
                Amount = r.Amount,
                Currency = r.Currency,
                CurrencyLabel = r.Currency == AccountingCurrency.USD ? "$" : "د.ع",
                PaymentMethodLabel = CustomsPaymentMethodDisplay.ToArabic(r.PaymentMethod),
                ReceiverName = r.ReceiverName,
                Notes = r.Notes
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
