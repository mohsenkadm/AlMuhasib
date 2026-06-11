using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class DataImportService : IDataImportService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public DataImportService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public Task<DataImportPreview> PreviewCustomersAsync(string filePath) =>
        PreviewAsync(filePath, ["الاسم", "الهاتف", "العنوان"]);

    public async Task<DataImportResult> ImportCustomersAsync(string filePath)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var result = new DataImportResult();
        using var wb = new XLWorkbook(filePath);
        var ws = wb.Worksheet(1);
        foreach (var row in ws.RowsUsed().Skip(1))
        {
            var name = row.Cell(1).GetString().Trim();
            if (string.IsNullOrEmpty(name)) { result.SkippedCount++; continue; }
            if (await context.Customers.AnyAsync(c => c.Name == name))
            {
                result.SkippedCount++;
                continue;
            }
            context.Customers.Add(new Customer
            {
                Name = name,
                Phone = row.Cell(2).GetString().Trim(),
                Address = row.Cell(3).GetString().Trim()
            });
            result.ImportedCount++;
        }
        await context.SaveChangesAsync();
        return result;
    }

    public Task<DataImportPreview> PreviewSuppliersAsync(string filePath) =>
        PreviewAsync(filePath, ["الاسم", "الهاتف", "العنوان"]);

    public async Task<DataImportResult> ImportSuppliersAsync(string filePath)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var result = new DataImportResult();
        using var wb = new XLWorkbook(filePath);
        var ws = wb.Worksheet(1);
        foreach (var row in ws.RowsUsed().Skip(1))
        {
            var name = row.Cell(1).GetString().Trim();
            if (string.IsNullOrEmpty(name)) { result.SkippedCount++; continue; }
            if (await context.Suppliers.AnyAsync(s => s.Name == name))
            {
                result.SkippedCount++;
                continue;
            }
            context.Suppliers.Add(new Supplier
            {
                Name = name,
                Phone = row.Cell(2).GetString().Trim(),
                Address = row.Cell(3).GetString().Trim()
            });
            result.ImportedCount++;
        }
        await context.SaveChangesAsync();
        return result;
    }

    public Task<DataImportPreview> PreviewProductsAsync(string filePath) =>
        PreviewAsync(filePath, ["الاسم", "الباركود", "التصنيف"]);

    public async Task<DataImportResult> ImportProductsAsync(string filePath)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var result = new DataImportResult();
        var defaultCategory = await context.Categories.FirstOrDefaultAsync()
            ?? context.Categories.Add(new Category { Name = "عام" }).Entity;
        await context.SaveChangesAsync();

        using var wb = new XLWorkbook(filePath);
        var ws = wb.Worksheet(1);
        foreach (var row in ws.RowsUsed().Skip(1))
        {
            var name = row.Cell(1).GetString().Trim();
            if (string.IsNullOrEmpty(name)) { result.SkippedCount++; continue; }
            var barcode = row.Cell(2).GetString().Trim();
            var catName = row.Cell(3).GetString().Trim();
            var category = string.IsNullOrEmpty(catName)
                ? defaultCategory
                : await context.Categories.FirstOrDefaultAsync(c => c.Name == catName)
                  ?? context.Categories.Add(new Category { Name = catName }).Entity;

            context.Products.Add(new Product
            {
                Name = name,
                Barcode = string.IsNullOrEmpty(barcode) ? null : barcode,
                CategoryId = category.Id
            });
            result.ImportedCount++;
        }
        await context.SaveChangesAsync();
        return result;
    }

    public void SaveCustomerTemplate(string filePath) => SaveTemplate(filePath, ["الاسم", "الهاتف", "العنوان"], [["مثال عميل", "07701234567", "بغداد"]]);
    public void SaveSupplierTemplate(string filePath) => SaveTemplate(filePath, ["الاسم", "الهاتف", "العنوان"], [["مثال مورد", "07709876543", "بغداد"]]);
    public void SaveProductTemplate(string filePath) => SaveTemplate(filePath, ["الاسم", "الباركود", "التصنيف"], [["منتج 1", "123456", "عام"]]);

    private static void SaveTemplate(string filePath, string[] headers, IList<object[]> sample)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("استيراد");
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];
        for (var r = 0; r < sample.Count; r++)
            for (var c = 0; c < sample[r].Length; c++)
                ws.Cell(r + 2, c + 1).Value = sample[r][c]?.ToString() ?? "";
        wb.SaveAs(filePath);
    }

    private static Task<DataImportPreview> PreviewAsync(string filePath, string[] expectedHeaders)
    {
        using var wb = new XLWorkbook(filePath);
        var ws = wb.Worksheet(1);
        var preview = new DataImportPreview
        {
            RowCount = ws.RowsUsed().Count() - 1,
            Headers = expectedHeaders.ToList()
        };
        foreach (var row in ws.RowsUsed().Skip(1).Take(5))
            preview.SampleRows.Add(string.Join(" | ", row.Cells(1, expectedHeaders.Length).Select(c => c.GetString())));
        return Task.FromResult(preview);
    }
}
