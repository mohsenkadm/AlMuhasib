using System.IO;
using System.Text.RegularExpressions;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Models.Import;
using AlMuhasib.UI.Models;
using ClosedXML.Excel;

namespace AlMuhasib.UI.Helpers;

/// <summary>
/// قالب واستيراد منتجات معالج النقل —
/// أعمدة ثابتة + تكلفة د.ع/$ + كمية لكل مخزن + سعر لكل نوع تسعير (+ دولار عند تعدد العملات) + ظهور الكاشير/الفروع.
/// </summary>
public static class MigrationProductExcelHelper
{
    public const string ShowInCashierHeader = "يظهر_في_شاشة_الكاشير";
    public const string ShowInCashierHeaderSpaced = "يظهر في شاشة الكاشير";

    public static byte[] BuildTemplate(
        IReadOnlyList<string> pricingTypeNames,
        IReadOnlyList<string> warehouseNames,
        bool enableMultiCurrency,
        IReadOnlyList<(int Id, string Name)>? branches = null,
        bool includeBranchColumns = false)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("البيانات");
        sheet.RightToLeft = true;

        var headers = new List<string>
        {
            "اسم_المنتج",
            "الباركود",
            "الصنف",
            ShowInCashierHeader,
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

        if (includeBranchColumns && branches is { Count: > 1 })
        {
            foreach (var (_, name) in branches)
                headers.Add(ProductImportSchema.BuildBranchHeader(name));
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
        sheet.Cell(2, 4).Value = "نعم"; // يظهر_في_شاشة_الكاشير
        sheet.Cell(2, 5).Value = 1000; // تكلفة_دينار
        var col = 6;
        if (enableMultiCurrency)
        {
            sheet.Cell(2, col).Value = 0;
            col++;
        }

        var firstQtyCol = col;
        if (warehouseNames.Count == 0)
        {
            sheet.Cell(2, col).Value = 10;
            col++;
        }
        else
        {
            foreach (var _ in warehouseNames)
            {
                sheet.Cell(2, col).Value = col == firstQtyCol ? 10 : 0;
                col++;
            }
        }

        for (var i = 0; i < pricingTypeNames.Count; i++)
        {
            sheet.Cell(2, col++).Value = 1500 + i * 100;
            if (enableMultiCurrency)
                sheet.Cell(2, col++).Value = 0;
        }

        if (includeBranchColumns && branches is { Count: > 1 })
        {
            var first = true;
            foreach (var _ in branches)
            {
                sheet.Cell(2, col++).Value = first ? "نعم" : "لا";
                first = false;
            }
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
            "4) يظهر_في_شاشة_الكاشير: نعم/1 للإظهار، لا/0 للإخفاء (افتراضي: نعم). قيمة واحدة على المنتج لكل الفروع.",
            "5) تكلفة_دينار (و تكلفة_دولار عند تعدد العملات) لتكلفة الوحدة الافتتاحية.",
            "6) أعمدة كمية_* تمثل الكمية الافتتاحية لكل مخزن.",
            "7) أعمدة سعر_بيع_*_دينار لأسعار البيع بالدينار لكل نوع تسعير."
        };
        if (enableMultiCurrency)
            lines.Add("8) أعمدة سعر_بيع_*_دولار لأسعار البيع بالدولار — مستقلة عن الدينار.");
        if (includeBranchColumns && branches is { Count: > 1 })
            lines.Add("9) أعمدة ظهور_فرع_* = نعم/1 لإظهار المنتج في الفرع، لا/0 أو فارغ لإخفائه. إن تُركت كلها فارغة يُربط بالفرع الرئيسي.");
        lines.Add("بعد الاستيراد راجع الجدول ثم اضغط «حفظ الخطوة» لتحديث المنتجات والأسعار.");
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
        decimal initialUsdToIqd,
        IReadOnlyList<(int Id, string Name, bool IsMain)>? branches = null)
    {
        using var workbook = new XLWorkbook(filePath);
        var sheet = workbook.Worksheets.FirstOrDefault(w =>
            w.Name.Equals("البيانات", StringComparison.OrdinalIgnoreCase))
            ?? workbook.Worksheet(1);

        var headerMap = ReadHeaderMap(sheet);
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        var rows = new List<MigrationNamedBalanceRow>();
        _ = initialUsdToIqd;
        branches ??= [];

        for (var r = 2; r <= lastRow; r++)
        {
            var name = GetCell(sheet, r, headerMap, "اسم_المنتج");
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var row = new MigrationNamedBalanceRow
            {
                Name = name.Trim(),
                Barcode = AlMuhasib.Core.Helpers.ProductBarcodeHelper.Normalize(
                    GetCell(sheet, r, headerMap, "الباركود")),
                CategoryName = string.IsNullOrWhiteSpace(GetCell(sheet, r, headerMap, "الصنف"))
                    ? "عام"
                    : GetCell(sheet, r, headerMap, "الصنف").Trim(),
                IsValid = true
            };

            var showInCashierCell = GetShowInCashierCell(sheet, r, headerMap);
            row.ShowInCashier = string.IsNullOrWhiteSpace(showInCashierCell)
                || ProductImportSchema.IsBranchVisibleCell(showInCashierCell);

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

            if (branches.Count > 1)
            {
                foreach (var (id, branchName, isMain) in branches)
                {
                    var header = ProductImportSchema.BuildBranchHeader(branchName);
                    var cell = GetCell(sheet, r, headerMap, header);
                    var visible = headerMap.ContainsKey(header)
                        ? ProductImportSchema.IsBranchVisibleCell(cell)
                        : isMain;
                    row.BranchVisibility.Add(new MigrationBranchVisibilityCell
                    {
                        BranchId = id,
                        BranchName = branchName,
                        IsVisible = visible
                    });
                }

                // إن وُجدت أعمدة فروع وكلها لا → الفرع الرئيسي
                if (branches.Any(b => headerMap.ContainsKey(ProductImportSchema.BuildBranchHeader(b.Name)))
                    && row.BranchVisibility.All(b => !b.IsVisible))
                {
                    var main = row.BranchVisibility.FirstOrDefault(b =>
                        branches.Any(x => x.Id == b.BranchId && x.IsMain));
                    if (main is not null)
                        main.IsVisible = true;
                    else if (row.BranchVisibility.Count > 0)
                        row.BranchVisibility[0].IsVisible = true;
                }
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

    private static string GetShowInCashierCell(
        IXLWorksheet sheet, int row, Dictionary<string, int> map)
    {
        var direct = GetCell(sheet, row, map, ShowInCashierHeader);
        if (!string.IsNullOrWhiteSpace(direct))
            return direct;
        direct = GetCell(sheet, row, map, ShowInCashierHeaderSpaced);
        if (!string.IsNullOrWhiteSpace(direct))
            return direct;

        var match = map.FirstOrDefault(kv =>
            kv.Key.Contains("كاشير", StringComparison.OrdinalIgnoreCase)
            || kv.Key.Contains("cashier", StringComparison.OrdinalIgnoreCase));
        return match.Key is null
            ? string.Empty
            : sheet.Cell(row, match.Value).GetFormattedString()?.Trim() ?? string.Empty;
    }

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
