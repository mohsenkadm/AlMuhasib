using System.Globalization;
using System.Text;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models;
using ClosedXML.Excel;

namespace AlMuhasib.Shared.Services;

public class PlatformDeductionExcelService : IPlatformDeductionExcelService
{
    private static readonly string[] NameHeaders =
    [
        "أسم الزبون", "اسم الزبون", "اسم العميل", "أسم العميل", "الزبون", "العميل"
    ];

    private static readonly string[] DeductedAmountHeaders =
    [
        "المبلغ المستقطع", "مبلغ المستقطع", "المبلغ المستقطع "
    ];

    private static readonly string[] RequestedAmountHeaders =
    [
        "مبلغ الأستقطاع", "مبلغ الاستقطاع", "مبلغ الأستقطاع "
    ];

    public IReadOnlyList<PlatformDeductionImportRow> ParseImportFile(string filePath)
    {
        using var workbook = new XLWorkbook(filePath);
        var sheet = workbook.Worksheet(1);
        var used = sheet.RangeUsed();
        if (used is null)
            return [];

        var lastRow = used.LastRow().RowNumber();
        var lastCol = used.LastColumn().ColumnNumber();
        if (lastRow < 2)
            return [];

        var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var col = 1; col <= lastCol; col++)
        {
            var header = SafeCellText(sheet.Cell(1, col));
            if (header.Length > 0 && !headerMap.ContainsKey(header))
                headerMap[header] = col;
        }

        var nameCol = FindColumn(headerMap, NameHeaders) ?? 3;
        var deductedCol = FindColumn(headerMap, DeductedAmountHeaders) ?? 7;
        var requestedCol = FindColumn(headerMap, RequestedAmountHeaders) ?? 6;
        var invoiceCol = FindColumn(headerMap, ["معرف الفاتورة"]);
        var deductionIdCol = FindColumn(headerMap, ["معرف الأستقطاع", "معرف الاستقطاع"]);
        var motherCol = FindColumn(headerMap, ["أسم أم الزبون", "اسم أم الزبون"]);
        var govCol = FindColumn(headerMap, ["الرقم الحكومي"]);
        var deductionDateCol = FindColumn(headerMap, ["تاريخ الأستقطاع", "تاريخ الاستقطاع"]);
        var dueDateCol = FindColumn(headerMap, ["تاريخ الأستحقاق", "تاريخ الاستحقاق"]);
        var statusCol = FindColumn(headerMap, ["حالة الأستقطاع", "حالة الاستقطاع"]);
        var categoryCol = FindColumn(headerMap, ["صنف الزبون"]);

        var rows = new List<PlatformDeductionImportRow>(Math.Max(0, lastRow - 1));
        for (var rowNum = 2; rowNum <= lastRow; rowNum++)
        {
            try
            {
                var customerName = SafeCellText(sheet.Cell(rowNum, nameCol));
                if (string.IsNullOrWhiteSpace(customerName))
                    continue;

                var importRow = new PlatformDeductionImportRow
                {
                    RowNumber = rowNum,
                    CustomerName = customerName,
                    PlatformInvoiceId = GetOptionalString(sheet, rowNum, invoiceCol),
                    DeductionId = GetOptionalString(sheet, rowNum, deductionIdCol),
                    MotherName = GetOptionalString(sheet, rowNum, motherCol),
                    GovernmentNumber = GetOptionalString(sheet, rowNum, govCol),
                    DeductionStatus = GetOptionalString(sheet, rowNum, statusCol),
                    CustomerCategory = GetOptionalString(sheet, rowNum, categoryCol)
                };

                if (TryParseDecimal(sheet.Cell(rowNum, deductedCol), out var deducted) && deducted > 0)
                    importRow.DeductedAmount = deducted;
                else
                    importRow.Errors.Add("المبلغ المستقطع غير صالح");

                if (TryParseDecimal(sheet.Cell(rowNum, requestedCol), out var requested) && requested >= 0)
                    importRow.RequestedAmount = requested;

                if (deductionDateCol is int dCol && TryParseDate(sheet.Cell(rowNum, dCol), out var dDate))
                    importRow.DeductionDate = dDate;
                if (dueDateCol is int uCol && TryParseDate(sheet.Cell(rowNum, uCol), out var uDate))
                    importRow.DueDate = uDate;

                rows.Add(importRow);
            }
            catch
            {
                // تجاهل صف تالف واستمرار بقية الملف
            }
        }

        return rows;
    }

    private static string SafeCellText(IXLCell cell)
    {
        try
        {
            return cell.GetFormattedString().Trim();
        }
        catch
        {
            try { return cell.GetString().Trim(); }
            catch { return string.Empty; }
        }
    }

    private static int? FindColumn(Dictionary<string, int> map, IEnumerable<string> candidates)
    {
        foreach (var c in candidates)
        {
            if (map.TryGetValue(c.Trim(), out var col))
                return col;
        }

        // تطابق جزئي مرن لعناوين المنصة
        foreach (var kv in map)
        {
            foreach (var c in candidates)
            {
                if (kv.Key.Contains(c.Trim(), StringComparison.OrdinalIgnoreCase)
                    || c.Trim().Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
                    return kv.Value;
            }
        }

        return null;
    }

    private static string? GetOptionalString(IXLWorksheet sheet, int row, int? col)
    {
        if (col is null) return null;
        var text = SafeCellText(sheet.Cell(row, col.Value));
        return string.IsNullOrWhiteSpace(text) ? null : text;
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
            return false;

        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value)
               || decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value);
    }

    private static bool TryParseDate(IXLCell cell, out DateTime value)
    {
        value = default;
        if (cell.TryGetValue(out DateTime dt))
        {
            value = dt.Date;
            return true;
        }

        var text = cell.GetString().Trim();
        return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out value)
               || DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out value);
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
