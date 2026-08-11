using System.Globalization;
using System.IO;
using System.Text;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models;
using ClosedXML.Excel;

namespace AlMuhasib.Shared.Services;

public class OpeningStockExcelService : IOpeningStockExcelService
{
    private static readonly string[] BaseHeaders =
    [
        "اسم_المنتج",
        "الباركود",
        "الكمية",
        "سعر_الوحدة"
    ];

    private const string ProductSalePriceHeader = "سعر_المنتج";

    public byte[] GenerateTemplate(bool includeProductSalePrice)
    {
        using var workbook = new XLWorkbook();

        var instructions = workbook.Worksheets.Add("تعليمات");
        instructions.RightToLeft = true;
        instructions.Column(1).Width = 90;
        var lines = new List<string>
        {
            "قالب استيراد الأرصدة الافتتاحية للمنتجات",
            "",
            "1) املأ البيانات في ورقة «البيانات» فقط — لا تغيّر أسماء الأعمدة.",
            "2) اسم_المنتج أو الباركود: مطلوب أحدهما على الأقل لمطابقة المنتج الموجود.",
            "3) الكمية: رقم أكبر من صفر.",
            "4) سعر_الوحدة: كلفة الوحدة للرصيد الافتتاحي (رقم أكبر من صفر).",
        };
        if (includeProductSalePrice)
        {
            lines.Add("5) سعر_المنتج: سعر البيع — يُحفظ تلقائياً على نوع التسعير «سعر مفرد».");
            lines.Add("");
            lines.Add("ملاحظة: ميزة تسعير المنتجات مفعّلة — عمود سعر_المنتج موجود في القالب.");
        }
        else
        {
            lines.Add("");
            lines.Add("ملاحظة: الاستيراد يطبّق على المخزن المحدد في شاشة الأرصدة الافتتاحية.");
        }

        for (var i = 0; i < lines.Count; i++)
            instructions.Cell(i + 1, 1).Value = lines[i];
        instructions.Cell(1, 1).Style.Font.Bold = true;
        instructions.Cell(1, 1).Style.Font.FontSize = 14;

        var data = workbook.Worksheets.Add("البيانات");
        data.RightToLeft = true;

        var headers = includeProductSalePrice
            ? BaseHeaders.Concat([ProductSalePriceHeader]).ToArray()
            : BaseHeaders;

        for (var col = 0; col < headers.Length; col++)
        {
            var cell = data.Cell(1, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2E7D32");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        data.Cell(2, 1).Value = "منتج مثال";
        data.Cell(2, 2).Value = "123456";
        data.Cell(2, 3).Value = 10;
        data.Cell(2, 4).Value = 5000;
        if (includeProductSalePrice)
            data.Cell(2, 5).Value = 7500;
        data.Row(2).Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F5E9");

        data.Column(1).Width = 24;
        data.Column(2).Width = 16;
        data.Column(3).Width = 12;
        data.Column(4).Width = 14;
        if (includeProductSalePrice)
            data.Column(5).Width = 14;

        data.Range(2, 3, 500, 3).CreateDataValidation().Decimal.Between(0.01, 999999999999);
        data.Range(2, 4, 500, 4).CreateDataValidation().Decimal.Between(0.01, 999999999999);
        if (includeProductSalePrice)
            data.Range(2, 5, 500, 5).CreateDataValidation().Decimal.Between(0, 999999999999);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public IReadOnlyList<OpeningStockImportRow> ParseImportFile(string filePath, bool includeProductSalePrice)
    {
        using var workbook = new XLWorkbook(filePath);
        var sheet = workbook.Worksheets.FirstOrDefault(w =>
            w.Name.Equals("البيانات", StringComparison.OrdinalIgnoreCase))
            ?? workbook.Worksheet(1);

        var headerMap = BuildHeaderMap(sheet);
        var rows = new List<OpeningStockImportRow>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        for (var rowNum = 2; rowNum <= lastRow; rowNum++)
        {
            var productName = GetCellString(sheet, rowNum, headerMap, "اسم_المنتج", 1);
            var barcode = GetCellString(sheet, rowNum, headerMap, "الباركود", 2);
            if (string.IsNullOrWhiteSpace(productName) && string.IsNullOrWhiteSpace(barcode))
                continue;

            var importRow = new OpeningStockImportRow
            {
                RowNumber = rowNum,
                ProductName = productName,
                Barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode
            };

            if (!TryParseDecimal(sheet.Cell(rowNum, ResolveColumn(headerMap, "الكمية", 3)), out var qty) || qty <= 0)
                importRow.Errors.Add("الكمية غير صالحة");
            else
                importRow.Quantity = qty;

            if (!TryParseDecimal(sheet.Cell(rowNum, ResolveColumn(headerMap, "سعر_الوحدة", 4)), out var unitCost) || unitCost <= 0)
                importRow.Errors.Add("سعر_الوحدة غير صالح");
            else
                importRow.UnitCost = unitCost;

            if (includeProductSalePrice)
            {
                var priceCol = ResolveColumn(headerMap, ProductSalePriceHeader, 5);
                var priceCell = sheet.Cell(rowNum, priceCol);
                if (!IsBlankCell(priceCell))
                {
                    if (!TryParseDecimal(priceCell, out var salePrice) || salePrice < 0)
                        importRow.Errors.Add("سعر_المنتج غير صالح");
                    else
                        importRow.ProductSalePrice = salePrice;
                }
            }

            rows.Add(importRow);
        }

        return rows;
    }

    private static Dictionary<string, int> BuildHeaderMap(IXLWorksheet sheet)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastCol = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        for (var col = 1; col <= lastCol; col++)
        {
            var name = sheet.Cell(1, col).GetString().Trim();
            if (!string.IsNullOrWhiteSpace(name) && !map.ContainsKey(name))
                map[name] = col;
        }
        return map;
    }

    private static int ResolveColumn(Dictionary<string, int> headerMap, string header, int fallback)
        => headerMap.TryGetValue(header, out var col) ? col : fallback;

    private static string GetCellString(IXLWorksheet sheet, int row, Dictionary<string, int> headerMap, string header, int fallback)
        => sheet.Cell(row, ResolveColumn(headerMap, header, fallback)).GetString().Trim();

    private static bool IsBlankCell(IXLCell cell)
    {
        if (cell.IsEmpty())
            return true;
        if (cell.DataType == XLDataType.Number)
            return false;
        return string.IsNullOrWhiteSpace(cell.GetString())
               && string.IsNullOrWhiteSpace(cell.GetFormattedString());
    }

    private static bool TryParseDecimal(IXLCell cell, out decimal value)
    {
        value = 0;
        if (cell.TryGetValue(out double d) && !double.IsNaN(d) && !double.IsInfinity(d))
        {
            value = (decimal)d;
            return true;
        }

        var text = NormalizeNumericText(cell.GetString());
        if (string.IsNullOrWhiteSpace(text))
            text = NormalizeNumericText(cell.GetFormattedString());
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value)
               || decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
               || decimal.TryParse(text, NumberStyles.Number, CultureInfo.GetCultureInfo("en-US"), out value)
               || decimal.TryParse(text, NumberStyles.Number, CultureInfo.GetCultureInfo("ar-IQ"), out value);
    }

    private static string NormalizeNumericText(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var sb = new StringBuilder(raw.Length);
        foreach (var ch in raw.Trim())
        {
            if (ch is ' ' or '\u00A0' or '٬')
                continue;

            sb.Append(ch switch
            {
                '٠' => '0', '١' => '1', '٢' => '2', '٣' => '3', '٤' => '4',
                '٥' => '5', '٦' => '6', '٧' => '7', '٨' => '8', '٩' => '9',
                '۰' => '0', '۱' => '1', '۲' => '2', '۳' => '3', '۴' => '4',
                '۵' => '5', '۶' => '6', '۷' => '7', '۸' => '8', '۹' => '9',
                '،' => ',',
                _ => ch
            });
        }

        var text = sb.ToString();
        var hasDot = text.Contains('.');
        var hasComma = text.Contains(',');

        if (hasDot && hasComma)
        {
            if (text.LastIndexOf(',') > text.LastIndexOf('.'))
                return text.Replace(".", "", StringComparison.Ordinal).Replace(',', '.');
            return text.Replace(",", "", StringComparison.Ordinal);
        }

        if (hasComma)
        {
            var parts = text.Split(',');
            if (parts.Length == 2 && parts[1].Length is > 0 and <= 3)
                return parts[0] + "." + parts[1];
            return text.Replace(",", "", StringComparison.Ordinal);
        }

        return text;
    }
}
