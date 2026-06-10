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

    public InvoiceService(IDbContextFactory<AppDbContext> contextFactory, ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
    }

    public async Task<Invoice> CreateInvoiceAsync(Invoice invoice, IEnumerable<InvoiceItem> items)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var username = _currentUserService.Username;
            invoice.CreatedBy = username;
            invoice.CreatedAt = DateTime.UtcNow;

            var itemsList = items.ToList();
            decimal subtotal = 0m;
            foreach (var item in itemsList)
            {
                item.TotalPrice = item.Quantity * item.UnitPrice;
                item.CreatedBy = username;
                item.CreatedAt = DateTime.UtcNow;
                subtotal += item.TotalPrice;
            }

            invoice.TotalAmount = subtotal;
            invoice.DiscountAmount = 0m;
            decimal netAmount = subtotal - invoice.DiscountAmount;

            decimal roundingAmount = CalculateRounding(netAmount, invoice.InvoiceType);
            invoice.RoundingAmount = roundingAmount;
            invoice.RoundingType = invoice.InvoiceType == InvoiceType.Purchase
                ? RoundingType.RoundUp
                : RoundingType.RoundDown;
            invoice.NetAmount = netAmount + roundingAmount;

            // Initialize credit payment tracking
            if (invoice.PaymentMethod == PaymentMethod.Credit)
            {
                invoice.PaidAmount = 0;
                invoice.RemainingAmount = invoice.NetAmount;
                invoice.IsCreditPaid = false;
            }
            else
            {
                invoice.PaidAmount = invoice.NetAmount;
                invoice.RemainingAmount = 0;
                invoice.IsCreditPaid = true;
            }

            invoice.InvoiceNumber = await GenerateInvoiceNumberAsync(context, invoice.InvoiceType);

            await context.Invoices.AddAsync(invoice);
            await context.SaveChangesAsync();

            foreach (var item in itemsList)
            {
                item.InvoiceId = invoice.Id;
                await context.InvoiceItems.AddAsync(item);
            }
            await context.SaveChangesAsync();

            if (invoice.InvoiceType == InvoiceType.Purchase || invoice.InvoiceType == InvoiceType.Sale || invoice.InvoiceType == InvoiceType.Installment)
            {
                foreach (var item in itemsList.Where(i => i.ProductId.HasValue))
                {
                    var stock = await context.WarehouseStocks
                        .FirstOrDefaultAsync(s =>
                            s.WarehouseId == invoice.WarehouseId &&
                            s.ProductId == item.ProductId!.Value);

                    if (invoice.InvoiceType == InvoiceType.Purchase)
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
                        if (stock is not null)
                        {
                            stock.Quantity -= item.Quantity;
                            stock.UpdatedBy = username;
                            stock.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }
                await context.SaveChangesAsync();
            }

            if (invoice.PaymentMethod == PaymentMethod.Cash && invoice.CashBoxId.HasValue)
            {
                var cashBox = await context.CashBoxes.FindAsync(invoice.CashBoxId.Value);
                if (cashBox is not null)
                {
                    if (invoice.InvoiceType == InvoiceType.Purchase)
                        cashBox.Balance -= invoice.NetAmount;
                    else
                        cashBox.Balance += invoice.NetAmount;

                    cashBox.UpdatedBy = username;
                    cashBox.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                }
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

        if (invoiceType == InvoiceType.Purchase)
            return roundingStep - remainder;
        else
            return -remainder;
    }

    public async Task DeleteInvoiceAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var invoice = await context.Invoices
            .Include(i => i.Items)
            .Include(i => i.InstallmentPlans)
                .ThenInclude(p => p.Installments)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (invoice is null) return;

        var username = _currentUserService.Username;

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            if (invoice.PaymentMethod == PaymentMethod.Cash && invoice.CashBoxId.HasValue)
            {
                var cashBox = await context.CashBoxes.FindAsync(invoice.CashBoxId.Value);
                if (cashBox is not null)
                {
                    if (invoice.InvoiceType == InvoiceType.Purchase)
                        cashBox.Balance += invoice.NetAmount;
                    else
                        cashBox.Balance -= invoice.NetAmount;

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
                    if (invoice.InvoiceType == InvoiceType.Purchase)
                        stock.Quantity -= item.Quantity;
                    else
                        stock.Quantity += item.Quantity;

                    stock.UpdatedBy = username;
                    stock.UpdatedAt = DateTime.UtcNow;
                }
            }

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
                    OldValues = $"رقم الفاتورة: {invoice.InvoiceNumber}, المبلغ: {invoice.NetAmount}, النوع: {invoice.InvoiceType}",
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
}
