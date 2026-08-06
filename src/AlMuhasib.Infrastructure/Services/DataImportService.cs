using System.Globalization;
using System.Text.Json;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Import;
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

    public Task<DataImportPreview> PreviewProductsAsync(string filePath, ProductImportOptions? options = null)
    {
        var headers = ProductImportSchema.BuildHeaders(options).ToArray();
        return PreviewAsync(filePath, headers);
    }

    public async Task<DataImportResult> ImportProductsAsync(string filePath, ProductImportOptions? options = null)
    {
        options ??= new ProductImportOptions();
        await using var context = await _contextFactory.CreateDbContextAsync();
        var result = new DataImportResult();
        var defaultCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "عام")
            ?? await context.Categories.FirstOrDefaultAsync()
            ?? context.Categories.Add(new Category { Name = "عام" }).Entity;
        await context.SaveChangesAsync();

        PricingType? defaultPricingType = null;
        if (options.IncludePricingFields)
        {
            defaultPricingType = await context.PricingTypes
                .OrderByDescending(t => t.IsDefault)
                .FirstOrDefaultAsync();
        }

        using var wb = new XLWorkbook(filePath);
        var ws = wb.Worksheet(1);
        var headerMap = BuildHeaderMap(ws);

        foreach (var row in ws.RowsUsed().Skip(1))
        {
            try
            {
                var name = GetCell(row, headerMap, ProductImportSchema.Name).Trim();
                if (string.IsNullOrEmpty(name))
                {
                    result.SkippedCount++;
                    continue;
                }

                if (await context.Products.AnyAsync(p => p.Name == name))
                {
                    result.SkippedCount++;
                    continue;
                }

                var barcode = GetCell(row, headerMap, ProductImportSchema.Barcode).Trim();
                var catName = GetCell(row, headerMap, ProductImportSchema.Category).Trim();
                var category = string.IsNullOrEmpty(catName)
                    ? defaultCategory
                    : await context.Categories.FirstOrDefaultAsync(c => c.Name == catName)
                      ?? context.Categories.Add(new Category { Name = catName }).Entity;

                var product = new Product
                {
                    Name = name,
                    Barcode = string.IsNullOrEmpty(barcode) ? null : barcode,
                    Description = NullIfEmpty(GetCell(row, headerMap, ProductImportSchema.Description)),
                    CategoryId = category.Id
                };

                if (options.IncludePharmacyFields)
                {
                    product.ScientificName = NullIfEmpty(GetCell(row, headerMap, ProductImportSchema.ScientificName));
                    product.UsageInstructions = NullIfEmpty(GetCell(row, headerMap, ProductImportSchema.UsageInstructions));
                }

                if (options.IncludeWeightFields)
                {
                    product.Weight = ParseDecimal(GetCell(row, headerMap, ProductImportSchema.Weight));
                    product.WeightUnit = NullIfEmpty(GetCell(row, headerMap, ProductImportSchema.WeightUnit));
                }

                if (options.IncludeDiscountFields)
                {
                    product.DiscountType = ParseDiscountType(GetCell(row, headerMap, ProductImportSchema.DiscountType));
                    product.DiscountValue = ParseDecimal(GetCell(row, headerMap, ProductImportSchema.DiscountValue));
                    product.DiscountExpiresAt = ParseDate(GetCell(row, headerMap, ProductImportSchema.DiscountExpiresAt));
                }

                if (options.CustomFields.Count > 0)
                {
                    var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var field in options.CustomFields)
                    {
                        var header = string.IsNullOrWhiteSpace(field.Header) ? $"حقل {field.Slot}" : field.Header.Trim();
                        var value = GetCell(row, headerMap, header).Trim();
                        if (!string.IsNullOrEmpty(value))
                            dict[field.SlotKey] = value;
                    }

                    if (dict.Count > 0)
                        product.CustomFieldsJson = JsonSerializer.Serialize(dict);
                }

                context.Products.Add(product);
                await context.SaveChangesAsync();

                if (options.IncludePricingFields && defaultPricingType is not null)
                {
                    var sale = ParseDecimal(GetCell(row, headerMap, ProductImportSchema.SalePrice));
                    var purchase = ParseDecimal(GetCell(row, headerMap, ProductImportSchema.PurchasePrice));
                    if (sale > 0 || purchase > 0)
                    {
                        context.ProductPrices.Add(new ProductPrice
                        {
                            ProductId = product.Id,
                            PricingTypeId = defaultPricingType.Id,
                            SalePrice = sale,
                            PurchasePrice = purchase
                        });
                        await context.SaveChangesAsync();
                    }
                }

                result.ImportedCount++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"صف {row.RowNumber()}: {ex.Message}");
            }
        }

        return result;
    }

    public void SaveCustomerTemplate(string filePath) =>
        SaveTemplate(filePath, ["الاسم", "الهاتف", "العنوان"], [["مثال عميل", "07701234567", "بغداد"]]);

    public void SaveSupplierTemplate(string filePath) =>
        SaveTemplate(filePath, ["الاسم", "الهاتف", "العنوان"], [["مثال مورد", "07709876543", "بغداد"]]);

    public void SaveProductTemplate(string filePath, ProductImportOptions? options = null)
    {
        var headers = ProductImportSchema.BuildHeaders(options).ToArray();
        var sample = ProductImportSchema.BuildSampleRow(options);
        SaveTemplate(filePath, headers, [sample]);
    }

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
        var headerRow = ws.Row(1);
        var lastCol = Math.Max(expectedHeaders.Length, headerRow.LastCellUsed()?.Address.ColumnNumber ?? expectedHeaders.Length);
        var preview = new DataImportPreview
        {
            RowCount = Math.Max(0, ws.RowsUsed().Count() - 1),
            Headers = expectedHeaders.ToList()
        };
        foreach (var row in ws.RowsUsed().Skip(1).Take(5))
            preview.SampleRows.Add(string.Join(" | ", row.Cells(1, lastCol).Select(c => c.GetString())));
        return Task.FromResult(preview);
    }

    private static Dictionary<string, int> BuildHeaderMap(IXLWorksheet ws)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var headerRow = ws.Row(1);
        var lastCol = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
        for (var c = 1; c <= lastCol; c++)
        {
            var header = headerRow.Cell(c).GetString().Trim();
            if (!string.IsNullOrEmpty(header) && !map.ContainsKey(header))
                map[header] = c;
        }

        // توافق القوالب القديمة (الاسم، الباركود، التصنيف) بدون صف رأس مطابق تماماً
        if (map.Count == 0)
        {
            map[ProductImportSchema.Name] = 1;
            map[ProductImportSchema.Barcode] = 2;
            map[ProductImportSchema.Category] = 3;
        }

        return map;
    }

    private static string GetCell(IXLRow row, Dictionary<string, int> headerMap, string header) =>
        headerMap.TryGetValue(header, out var col) ? row.Cell(col).GetString() : string.Empty;

    private static string? NullIfEmpty(string value)
    {
        var trimmed = value.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static decimal ParseDecimal(string value)
    {
        value = value.Trim().Replace(",", "");
        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)
            || decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out d)
            ? d
            : 0m;
    }

    private static DateTime? ParseDate(string value)
    {
        value = value.Trim();
        if (string.IsNullOrEmpty(value)) return null;
        if (DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var dt)
            || DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dt))
            return dt.ToUniversalTime();
        return null;
    }

    private static DiscountType ParseDiscountType(string value)
    {
        value = value.Trim();
        if (string.IsNullOrEmpty(value) || value is "بدون" or "لا" or "none" or "0")
            return DiscountType.None;
        if (value.Contains("نسب", StringComparison.OrdinalIgnoreCase)
            || value.Contains('%')
            || value.Equals("percentage", StringComparison.OrdinalIgnoreCase)
            || value == "1")
            return DiscountType.Percentage;
        if (value.Contains("ثابت", StringComparison.OrdinalIgnoreCase)
            || value.Contains("قيمة", StringComparison.OrdinalIgnoreCase)
            || value.Equals("fixed", StringComparison.OrdinalIgnoreCase)
            || value == "2")
            return DiscountType.FixedAmount;
        return DiscountType.None;
    }
}
