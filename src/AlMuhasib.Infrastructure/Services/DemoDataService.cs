using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public sealed class DemoDataService : IDemoDataService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public DemoDataService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<DemoDataSeedResult> TrySeedAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        if (await context.Products.AnyAsync(cancellationToken))
        {
            return new DemoDataSeedResult
            {
                Success = false,
                Message = "لا يمكن إضافة بيانات تجريبية — توجد منتجات مسجّلة بالفعل."
            };
        }

        var warehouse = await context.Warehouses.FirstOrDefaultAsync(cancellationToken);
        if (warehouse is null)
        {
            return new DemoDataSeedResult
            {
                Success = false,
                Message = "أنشئ مخزناً واحداً على الأقل قبل تحميل البيانات التجريبية."
            };
        }

        var category = await context.Categories.FirstOrDefaultAsync(cancellationToken);
        if (category is null)
        {
            category = new Category { Name = "عام" };
            context.Categories.Add(category);
            await context.SaveChangesAsync(cancellationToken);
        }

        var productNames = new[]
        {
            "لابتوب ديل",
            "طابعة HP",
            "ماوس لاسلكي",
            "كيبورد عربي",
            "شاشة 24 بوصة"
        };

        var products = new List<Product>();
        for (var i = 0; i < productNames.Length; i++)
        {
            var p = new Product
            {
                Name = productNames[i],
                Barcode = $"DEMO-{1000 + i}",
                CategoryId = category.Id,
                Description = "منتج تجريبي"
            };
            context.Products.Add(p);
            products.Add(p);
        }

        var customerNames = new[] { "أحمد محمد", "سارة علي", "محل الإلكترونيات" };
        foreach (var name in customerNames)
        {
            context.Customers.Add(new Customer
            {
                Name = name,
                Phone = "07" + Random.Shared.Next(100000000, 999999999).ToString()
            });
        }

        var supplier = await context.Suppliers.FirstOrDefaultAsync(cancellationToken);
        if (supplier is null)
        {
            supplier = new Supplier { Name = "مورد تجريبي", Phone = "07800000000" };
            context.Suppliers.Add(supplier);
        }

        await context.SaveChangesAsync(cancellationToken);

        var quantities = new[] { 2m, 15m, 8m, 3m, 25m };
        for (var i = 0; i < products.Count; i++)
        {
            context.WarehouseStocks.Add(new WarehouseStock
            {
                ProductId = products[i].Id,
                WarehouseId = warehouse.Id,
                Quantity = quantities[i],
                OpeningQuantity = quantities[i],
                UnitCost = 50000 + i * 10000
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        return new DemoDataSeedResult
        {
            Success = true,
            Message = "تم تحميل بيانات تجريبية (منتجات، عملاء، مخزون).",
            ProductsCreated = products.Count,
            CustomersCreated = customerNames.Length
        };
    }
}
