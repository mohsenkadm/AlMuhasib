using System.IO;
using System.Text.RegularExpressions;
using AlMuhasib.Core.Enums;
using AlMuhasib.UI.Models;
using ClosedXML.Excel;

namespace AlMuhasib.UI.Helpers;

/// <summary>
/// قالب واستيراد منتجات معالج النقل —
/// أعمدة ثابتة + تكلفة د.ع/$ + كمية لكل مخزن + سعر لكل نوع تسعير (+ دولار عند تعدد العملات).
/// </summary>
public static class MigrationProductExcelHelper
{
    public static byte[] BuildTemplate(
        IReadOnlyList<string> pricingTypeNames,
        IReadOnlyList<string> warehouseNames,
        bool enableMultiCurrency)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("البيانات");
        sheet.RightToLeft = true;

        var headers = new List<string>
        {
            "اسم_المنتج",
            "الباركود",
            "الصنف",
            "تكلفة_دينار"
        };
        if (enableMultiCurrency)
            headers.Add("تكلفة_دولار");

        foreach (var wh in warehouseNames.Where(n => !string.IsNullOrWhiteSpace(n)))
            headers.Add($"كمية_{SanitizeHeader(wh)}");

        // توافق: إن لم توجد مخازن بعد، عمود كمية عام
        if (warehouseNames.Count == 0)
            headers.Add("الكمية");

        foreach (var typeName in pricingTypeNames.Where(n => !string.IsNullOrWhiteSpace(n)))
        {
            headers.Add($"سعر_بيع_{SanitizeHeader(typeName)}_دينار");
            if (enableMultiCurrency)
                headers.Add($"سعر_بيع_{SanitizeHeader(typeName)}_دولار");
        }

        for (var i = 0; i < headers.Count; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E3F2FD");
            sheet.Column(i + 1).Width = i == 0 ? 28 : 16;
        }

        sheet.Cell(2, 1).Value = "منتج تجريبي";
        sheet.Cell(2, 3).Value = "عام";
        sheet.Cell(2, 4).Value = 1000;
        var col = 5;
        if (enableMultiCurrency)
        {
            sheet.Cell(2, col).Value = 0;
            col++;
        }

        if (warehouseNames.Count == 0)
        {
            sheet.Cell(2, col).Value = 10;
            col++;
        }
        else
        {
            foreach (var _ in warehouseNames)
            {
                sheet.Cell(2, col).Value = col == (enableMultiCurrency ? 6 : 5) ? 10 : 0;
                col++;
            }
        }

        for (var i = 0; i < pricingTypeNames.Count; i++)
        {
            sheet.Cell(2, col++).Value = 1500 + i * 100;
            if (enableMultiCurrency)
                sheet.Cell(2, col++).Value = 0;
        }

        var guide = workbook.Worksheets.Add("تعليمات");
        guide.RightToLeft = true;
        guide.Column(1).Width = 90;
        var lines = new List<string>
        {
            "قالب استيراد المنتجات — معالج النقل",
            "",
            "1) املأ ورقة «البيانات» ثم احفظ الملف واستورده من المعالج.",
            "2) اسم_المنتج مطلوب. باقي الحقول اختيارية.",
            "3) الصنف: إن تُرك فارغاً يُستخدم «عام».",
            "4) تكلفة_دينار (و تكلفة_دولار عند تعدد العملات) لتكلفة الوحدة الافتتاحية.",
            "5) أعمدة كمية_* تمثل الكمية الافتتاحية لكل مخزن.",
            "6) أعمدة سعر_بيع_*_دينار لأسعار البيع بالدينار لكل نوع تسعير."
        };
        if (enableMultiCurrency)
            lines.Add("7) أعمدة سعر_بيع_*_دولار لأسعار البيع بالدولار — مستقلة عن الدينار.");
        lines.Add("8) بعد الاستيراد راجع الجدول ثم اضغط «حفظ الخطوة» لتحديث المنتجات والأسعار.");
        for (var i = 0; i < lines.Count; i++)
            guide.Cell(i + 1, 1).Value = lines[i];
        guide.Cell(1, 1).Style.Font.Bold = true;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static IReadOnlyList<MigrationNamedBalanceRow> Parse(
        string filePath,
        IReadOnlyList<string> pricingTypeNames,
        IReadOnlyList<string> warehouseNames,
        bool enableMultiCurrency,
        decimal initialUsdToIqd)
    {
        using var workbook = new XLWorkbook(filePath);
        var sheet = workbook.Worksheets.FirstOrDefault(w =>
            w.Name.Equals("البيانات", StringComparison.OrdinalIgnoreCase))
            ?? workbook.Worksheet(1);

        var headerMap = ReadHeaderMap(sheet);
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        var rows = new List<MigrationNamedBalanceRow>();
        _ = initialUsdToIqd;

        for (var r = 2; r <= lastRow; r++)
        {
            var name = GetCell(sheet, r, headerMap, "اسم_المنتج");
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var row = new MigrationNamedBalanceRow
            {
                Name = name.Trim(),
                Barcode = NullIfEmpty(GetCell(sheet, r, headerMap, "الباركود")),
                CategoryName = string.IsNullOrWhiteSpace(GetCell(sheet, r, headerMap, "الصنف"))
                    ? "عام"
                    : GetCell(sheet, r, headerMap, "الصنف").Trim(),
                IsValid = true
            };

            // تكلفة: النموذج الجديد أو القديم
            var costIqd = MigrationExcelHelper.ParseDecimal(GetCell(sheet, r, headerMap, "تكلفة_دينار"));
            if (costIqd <= 0)
                costIqd = MigrationExcelHelper.ParseDecimal(GetCell(sheet, r, headerMap, "تكلفة_الوحدة"));
            row.UnitCost = costIqd;
            row.UnitCostUsd = MigrationExcelHelper.ParseDecimal(GetCell(sheet, r, headerMap, "تكلفة_دولار"));

            var legacyCurrency = ParseCurrency(GetCell(sheet, r, headerMap, "عملة_التكلفة"));
            var legacyFx = MigrationExcelHelper.ParseDecimal(GetCell(sheet, r, headerMap, "سعر_الصرف"));
            if (row.UnitCostUsd <= 0 && legacyCurrency == AccountingCurrency.USD && costIqd > 0)
            {
                row.UnitCostUsd = costIqd;
                row.UnitCost = 0;
                row.CostCurrency = AccountingCurrency.USD;
                row.FxRate = legacyFx > 0 ? legacyFx : 0m;
                if (row.FxRate <= 0 && !enableMultiCurrency)
                {
                    row.IsValid = false;
                    row.ErrorText = "سعر الصرف مطلوب عند العملة دولار";
                }
            }
            else
            {
                row.CostCurrency = AccountingCurrency.IQD;
                row.FxRate = 1m;
            }

            // كميات المخازن
            foreach (var wh in warehouseNames.Where(n => !string.IsNullOrWhiteSpace(n)))
            {
                var qty = MigrationExcelHelper.ParseDecimal(GetCell(sheet, r, headerMap, $"كمية_{SanitizeHeader(wh)}"));
                row.WarehouseQtys.Add(new MigrationWarehouseQtyCell
                {
                    WarehouseName = wh,
                    Quantity = qty
                });
            }

            var legacyQty = MigrationExcelHelper.ParseDecimal(GetCell(sheet, r, headerMap, "الكمية"));
            var legacyWh = NullIfEmpty(GetCell(sheet, r, headerMap, "المخزن"));
            if (legacyQty > 0)
            {
                row.Quantity = legacyQty;
                row.WarehouseName = legacyWh;
                if (row.WarehouseQtys.Count > 0)
                {
                    var target = legacyWh is not null
                        ? row.WarehouseQtys.FirstOrDefault(q =>
                            q.WarehouseName.Contains(legacyWh, StringComparison.OrdinalIgnoreCase)
                            || legacyWh.Contains(q.WarehouseName.Split('(')[0].Trim(), StringComparison.OrdinalIgnoreCase))
                        : row.WarehouseQtys[0];
                    if (target is not null && target.Quantity <= 0)
                        target.Quantity = legacyQty;
                }
            }

            foreach (var typeName in pricingTypeNames.Where(n => !string.IsNullOrWhiteSpace(n)))
            {
                var saleKey = $"سعر_بيع_{SanitizeHeader(typeName)}_دينار";
                var saleUsdKey = $"سعر_بيع_{SanitizeHeader(typeName)}_دولار";
                var legacySaleKey = $"سعر_{SanitizeHeader(typeName)}";
                var legacySaleUsdKey = $"سعر_{SanitizeHeader(typeName)}_دولار";
                var sale = FindPrice(sheet, r, headerMap, saleKey, typeName, usd: false);
                if (sale <= 0)
                    sale = FindPrice(sheet, r, headerMap, legacySaleKey, typeName, usd: false);
                var saleUsd = enableMultiCurrency
                    ? FindPrice(sheet, r, headerMap, saleUsdKey, typeName, usd: true)
                    : 0m;
                if (saleUsd <= 0 && enableMultiCurrency)
                    saleUsd = FindPrice(sheet, r, headerMap, legacySaleUsdKey, typeName, usd: true);
                if (sale <= 0 && saleUsd <= 0) continue;
                row.ProductPrices.Add(new MigrationProductPriceCell
                {
                    PricingTypeName = typeName,
                    SalePrice = sale,
                    SalePriceUsd = saleUsd
                });
            }

            row.RebuildPricesSummary();
            rows.Add(row);
        }

        return rows;
    }

    private static decimal FindPrice(
        IXLWorksheet sheet, int row, Dictionary<string, int> headerMap,
        string exactKey, string typeName, bool usd)
    {
        if (headerMap.TryGetValue(exactKey, out var col))
            return MigrationExcelHelper.ParseDecimal(sheet.Cell(row, col).GetFormattedString());

        var suffix = usd ? "_دولار" : string.Empty;
        var match = headerMap.FirstOrDefault(kv =>
            kv.Key.StartsWith("سعر_", StringComparison.OrdinalIgnoreCase)
            && kv.Key.Contains(SanitizeHeader(typeName), StringComparison.OrdinalIgnoreCase)
            && (usd
                ? kv.Key.Contains("دولار", StringComparison.OrdinalIgnoreCase)
                : !kv.Key.Contains("دولار", StringComparison.OrdinalIgnoreCase)));
        if (match.Key is null) return 0;
        return MigrationExcelHelper.ParseDecimal(sheet.Cell(row, match.Value).GetFormattedString());
    }

    private static Dictionary<string, int> ReadHeaderMap(IXLWorksheet sheet)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastCol = sheet.LastColumnUsed()?.ColumnNumber() ?? 1;
        for (var c = 1; c <= lastCol; c++)
        {
            var h = sheet.Cell(1, c).GetString()?.Trim() ?? string.Empty;
            if (h.Length > 0 && !map.ContainsKey(h))
                map[h] = c;
        }
        return map;
    }

    private static string GetCell(IXLWorksheet sheet, int row, Dictionary<string, int> map, string header)
        => map.TryGetValue(header, out var col)
            ? sheet.Cell(row, col).GetFormattedString()?.Trim() ?? string.Empty
            : string.Empty;

    private static string? NullIfEmpty(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static AccountingCurrency ParseCurrency(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return AccountingCurrency.IQD;
        text = text.Trim().ToUpperInvariant();
        if (text is "USD" or "دولار" or "$") return AccountingCurrency.USD;
        return AccountingCurrency.IQD;
    }

    private static string SanitizeHeader(string name)
    {
        var cleaned = Regex.Replace(name.Trim(), @"\s+", "_");
        return cleaned;
    }
}
