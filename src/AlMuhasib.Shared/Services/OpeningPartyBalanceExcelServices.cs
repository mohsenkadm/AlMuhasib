using System.Globalization;
using System.IO;
using System.Text;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models;
using ClosedXML.Excel;

namespace AlMuhasib.Shared.Services;

public class OpeningCustomerBalanceExcelService : IOpeningCustomerBalanceExcelService
{
    private static readonly string[] Headers =
    [
        "اسم_العميل",
        "الهاتف",
        "رقم_الملف",
        "الرصيد_دينار",
        "الرصيد_دولار",
        "التاريخ",
        "ملاحظات"
    ];

    public byte[] GenerateTemplate()
    {
        using var workbook = new XLWorkbook();

        var instructions = workbook.Worksheets.Add("تعليمات");
        instructions.RightToLeft = true;
        instructions.Column(1).Width = 90;
        var lines = new[]
        {
            "قالب استيراد أرصدة العملاء الافتتاحية (آجل)",
            "",
            "1) املأ البيانات في ورقة «البيانات» فقط — لا تغيّر أسماء الأعمدة.",
            "2) اسم_العميل: مطلوب. إذا لم يكن موجوداً في النظام سيُنشأ تلقائياً.",
            "3) الهاتف ورقم_الملف: اختياريان.",
            "4) الرصيد_دينار والرصيد_دولار: اختياريان — يمكن ملء أحدهما أو كليهما.",
            "5) عند الرصيد_دولار يُستخدم سعر الصرف الافتتاحي من معالج النقل / إعدادات النظام.",
            "6) التاريخ: بصيغة yyyy/MM/dd مثل 2024/01/15. الخلية الفارغة = تاريخ اليوم.",
            "7) ملاحظات: اختياري.",
            "",
            "توافق: القوالب القديمة (المبلغ + العملة + سعر_الصرف) ما زالت تُقرأ إن وُجدت.",
            "ملاحظة: يُنشأ رصيد آجل على ذمة العميل دون التأثير على القاصة أو المخزون."
        };
        for (var i = 0; i < lines.Length; i++)
            instructions.Cell(i + 1, 1).Value = lines[i];
        instructions.Cell(1, 1).Style.Font.Bold = true;
        instructions.Cell(1, 1).Style.Font.FontSize = 14;

        var data = workbook.Worksheets.Add("البيانات");
        data.RightToLeft = true;
        WriteHeaders(data);
        AddSampleRow(data, 2, "أحمد محمد", "07701234567", "F-1001", 500000, 0, new DateTime(2024, 6, 1), "مثال — رصيد دينار");
        AddSampleRow(data, 3, "سارة علي", "", "", 0, 250, DateTime.Today, "مثال — رصيد دولار");

        data.Column(1).Width = 22;
        data.Column(2).Width = 16;
        data.Column(3).Width = 14;
        data.Column(4).Width = 14;
        data.Column(5).Width = 14;
        data.Column(6).Width = 14;
        data.Column(7).Width = 28;

        data.Range(2, 4, 500, 5).CreateDataValidation().Decimal.Between(0, 999999999999);
        data.Range(2, 6, 500, 6).CreateDataValidation().Date.Between(new DateTime(2000, 1, 1), new DateTime(2100, 12, 31));

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public IReadOnlyList<OpeningPartyBalanceImportRow> ParseImportFile(string filePath)
        => OpeningPartyExcelParseHelper.ParseCustomer(filePath);

    private static void WriteHeaders(IXLWorksheet data)
    {
        for (var col = 0; col < Headers.Length; col++)
        {
            var cell = data.Cell(1, col + 1);
            cell.Value = Headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0277BD");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
    }

    private static void AddSampleRow(IXLWorksheet sheet, int row, string name, string? phone,
        string? file, decimal amountIqd, decimal amountUsd, DateTime date, string? notes)
    {
        sheet.Cell(row, 1).Value = name;
        sheet.Cell(row, 2).Value = phone ?? string.Empty;
        sheet.Cell(row, 3).Value = file ?? string.Empty;
        sheet.Cell(row, 4).Value = amountIqd;
        sheet.Cell(row, 5).Value = amountUsd;
        sheet.Cell(row, 6).Value = date;
        sheet.Cell(row, 6).Style.DateFormat.Format = "yyyy/MM/dd";
        sheet.Cell(row, 7).Value = notes ?? string.Empty;
        sheet.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#E1F5FE");
    }
}

public class OpeningSupplierBalanceExcelService : IOpeningSupplierBalanceExcelService
{
    private static readonly string[] Headers =
    [
        "اسم_المورد",
        "الهاتف",
        "الرصيد_دينار",
        "الرصيد_دولار",
        "التاريخ",
        "ملاحظات"
    ];

    public byte[] GenerateTemplate()
    {
        using var workbook = new XLWorkbook();

        var instructions = workbook.Worksheets.Add("تعليمات");
        instructions.RightToLeft = true;
        instructions.Column(1).Width = 90;
        var lines = new[]
        {
            "قالب استيراد أرصدة الموردين الافتتاحية (آجل)",
            "",
            "1) املأ البيانات في ورقة «البيانات» فقط — لا تغيّر أسماء الأعمدة.",
            "2) اسم_المورد: مطلوب. إذا لم يكن موجوداً في النظام سيُنشأ تلقائياً.",
            "3) الهاتف: اختياري.",
            "4) الرصيد_دينار والرصيد_دولار: اختياريان — يمكن ملء أحدهما أو كليهما.",
            "5) عند الرصيد_دولار يُستخدم سعر الصرف الافتتاحي من معالج النقل / إعدادات النظام.",
            "6) التاريخ: بصيغة yyyy/MM/dd مثل 2024/01/15. الخلية الفارغة = تاريخ اليوم.",
            "7) ملاحظات: اختياري.",
            "",
            "توافق: القوالب القديمة (المبلغ + العملة + سعر_الصرف) ما زالت تُقرأ إن وُجدت.",
            "ملاحظة: يُنشأ رصيد آجل على ذمة المورد دون التأثير على القاصة أو المخزون."
        };
        for (var i = 0; i < lines.Length; i++)
            instructions.Cell(i + 1, 1).Value = lines[i];
        instructions.Cell(1, 1).Style.Font.Bold = true;
        instructions.Cell(1, 1).Style.Font.FontSize = 14;

        var data = workbook.Worksheets.Add("البيانات");
        data.RightToLeft = true;
        for (var col = 0; col < Headers.Length; col++)
        {
            var cell = data.Cell(1, col + 1);
            cell.Value = Headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EF6C00");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        data.Cell(2, 1).Value = "مورد مثال";
        data.Cell(2, 2).Value = "07709876543";
        data.Cell(2, 3).Value = 750000;
        data.Cell(2, 4).Value = 0;
        data.Cell(2, 5).Value = new DateTime(2024, 6, 1);
        data.Cell(2, 5).Style.DateFormat.Format = "yyyy/MM/dd";
        data.Cell(2, 6).Value = "مثال — رصيد سابق";
        data.Row(2).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF3E0");

        data.Column(1).Width = 22;
        data.Column(2).Width = 16;
        data.Column(3).Width = 14;
        data.Column(4).Width = 14;
        data.Column(5).Width = 14;
        data.Column(6).Width = 28;

        data.Range(2, 3, 500, 4).CreateDataValidation().Decimal.Between(0, 999999999999);
        data.Range(2, 5, 500, 5).CreateDataValidation().Date.Between(new DateTime(2000, 1, 1), new DateTime(2100, 12, 31));

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public IReadOnlyList<OpeningPartyBalanceImportRow> ParseImportFile(string filePath)
        => OpeningPartyExcelParseHelper.ParseSupplier(filePath);
}

internal static class OpeningPartyExcelParseHelper
{
    public static IReadOnlyList<OpeningPartyBalanceImportRow> ParseCustomer(string filePath)
        => Parse(filePath, hasFileNumber: true);

    public static IReadOnlyList<OpeningPartyBalanceImportRow> Parse(string filePath, string _)
        => ParseCustomer(filePath);

    public static IReadOnlyList<OpeningPartyBalanceImportRow> ParseSupplier(string filePath)
        => Parse(filePath, hasFileNumber: false);

    private static IReadOnlyList<OpeningPartyBalanceImportRow> Parse(string filePath, bool hasFileNumber)
    {
        using var workbook = new XLWorkbook(filePath);
        var sheet = workbook.Worksheets.FirstOrDefault(w =>
            w.Name.Equals("البيانات", StringComparison.OrdinalIgnoreCase))
            ?? workbook.Worksheet(1);

        var headerMap = ReadHeaderMap(sheet);
        var dualMode = headerMap.ContainsKey("الرصيد_دينار") || headerMap.ContainsKey("الرصيد_دولار");
        var rows = new List<OpeningPartyBalanceImportRow>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        for (var rowNum = 2; rowNum <= lastRow; rowNum++)
        {
            var partyName = GetCell(sheet, rowNum, headerMap, hasFileNumber ? "اسم_العميل" : "اسم_المورد", 1);
            if (string.IsNullOrWhiteSpace(partyName))
                continue;

            var importRow = new OpeningPartyBalanceImportRow
            {
                RowNumber = rowNum,
                PartyName = partyName.Trim(),
                Phone = NullIfEmpty(GetCell(sheet, rowNum, headerMap, "الهاتف", 2)),
                FileNumber = hasFileNumber
                    ? NullIfEmpty(GetCell(sheet, rowNum, headerMap, "رقم_الملف", 3))
                    : null,
                Notes = NullIfEmpty(GetCell(sheet, rowNum, headerMap, "ملاحظات",
                    dualMode ? (hasFileNumber ? 7 : 6) : (hasFileNumber ? 6 : 5)))
            };

            var dateCol = dualMode ? (hasFileNumber ? 6 : 5) : (hasFileNumber ? 5 : 4);
            var dateCell = headerMap.TryGetValue("التاريخ", out var dateHeaderCol)
                ? sheet.Cell(rowNum, dateHeaderCol)
                : sheet.Cell(rowNum, dateCol);
            if (IsBlankCell(dateCell))
                importRow.Date = DateTime.Today;
            else if (!TryParseDate(dateCell, out var date))
                importRow.Errors.Add("التاريخ غير صالح");
            else
                importRow.Date = date;

            if (dualMode)
            {
                var iqdText = GetCell(sheet, rowNum, headerMap, "الرصيد_دينار", hasFileNumber ? 4 : 3);
                var usdText = GetCell(sheet, rowNum, headerMap, "الرصيد_دولار", hasFileNumber ? 5 : 4);
                var hasIqd = TryParseDecimalText(iqdText, out var iqd) && iqd > 0;
                var hasUsd = TryParseDecimalText(usdText, out var usd) && usd > 0;
                if (hasIqd) importRow.Amount = iqd;
                if (hasUsd) importRow.AmountUsd = usd;
                // صفر/صفر مسموح — يُنشأ الطرف فقط
            }
            else
            {
                var amountCol = hasFileNumber ? 4 : 3;
                var amountCell = headerMap.TryGetValue("المبلغ", out var amountHeader)
                    ? sheet.Cell(rowNum, amountHeader)
                    : sheet.Cell(rowNum, amountCol);
                if (!TryParseDecimal(amountCell, out var amount) || amount <= 0)
                    importRow.Errors.Add("المبلغ غير صالح");
                else
                    importRow.Amount = amount;

                var currencyCol = hasFileNumber ? 7 : 6;
                var fxCol = hasFileNumber ? 8 : 7;
                var currencyCell = headerMap.TryGetValue("العملة", out var cCol)
                    ? sheet.Cell(rowNum, cCol)
                    : sheet.Cell(rowNum, currencyCol);
                var fxCell = headerMap.TryGetValue("سعر_الصرف", out var fCol)
                    ? sheet.Cell(rowNum, fCol)
                    : sheet.Cell(rowNum, fxCol);
                ApplyCurrencyAndFx(importRow, currencyCell, fxCell);
            }

            rows.Add(importRow);
        }

        return rows;
    }

    private static Dictionary<string, int> ReadHeaderMap(IXLWorksheet sheet)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastCol = sheet.LastColumnUsed()?.ColumnNumber() ?? 1;
        for (var c = 1; c <= lastCol; c++)
        {
            var header = sheet.Cell(1, c).GetString().Trim();
            if (!string.IsNullOrWhiteSpace(header) && !map.ContainsKey(header))
                map[header] = c;
        }
        return map;
    }

    private static string GetCell(IXLWorksheet sheet, int row, Dictionary<string, int> map, string key, int fallbackCol)
    {
        if (map.TryGetValue(key, out var col))
            return sheet.Cell(row, col).GetFormattedString().Trim();
        return sheet.Cell(row, fallbackCol).GetFormattedString().Trim();
    }

    private static void ApplyCurrencyAndFx(OpeningPartyBalanceImportRow importRow, IXLCell currencyCell, IXLCell fxCell)
    {
        var raw = currencyCell.GetString().Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            importRow.Currency = AccountingCurrency.IQD;
            importRow.FxRate = 1m;
            return;
        }

        if (raw.Equals("USD", StringComparison.OrdinalIgnoreCase)
            || raw.Equals("$", StringComparison.Ordinal)
            || raw.Contains("دولار", StringComparison.OrdinalIgnoreCase))
        {
            importRow.Currency = AccountingCurrency.USD;
        }
        else if (raw.Equals("IQD", StringComparison.OrdinalIgnoreCase)
                 || raw.Contains("دينار", StringComparison.OrdinalIgnoreCase)
                 || raw.Equals("د.ع", StringComparison.OrdinalIgnoreCase))
        {
            importRow.Currency = AccountingCurrency.IQD;
            importRow.FxRate = 1m;
            return;
        }
        else
        {
            importRow.Errors.Add("العملة غير صالحة (IQD أو USD)");
            return;
        }

        if (!TryParseDecimal(fxCell, out var fx) || fx <= 0)
            importRow.Errors.Add("سعر الصرف مطلوب وصالح عند العملة دولار");
        else
            importRow.FxRate = fx;
    }

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsBlankCell(IXLCell cell)
    {
        if (cell.IsEmpty())
            return true;
        if (cell.DataType == XLDataType.Number || cell.DataType == XLDataType.DateTime)
            return false;
        return string.IsNullOrWhiteSpace(cell.GetString())
               && string.IsNullOrWhiteSpace(cell.GetFormattedString());
    }

    private static bool TryParseDecimalText(string? text, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var normalized = NormalizeNumericText(text);
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value)
               || decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.CurrentCulture, out value);
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

    private static bool TryParseDate(IXLCell cell, out DateTime date)
    {
        date = default;
        if (cell.TryGetValue(out DateTime dt))
        {
            date = dt.Date;
            return true;
        }

        if (cell.TryGetValue(out double oa) && oa > 0)
        {
            try
            {
                date = DateTime.FromOADate(oa).Date;
                return true;
            }
            catch
            {
                // ignore
            }
        }

        var text = cell.GetString().Trim();
        if (string.IsNullOrWhiteSpace(text))
            text = cell.GetFormattedString().Trim();

        return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
               || DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out date)
               || DateTime.TryParseExact(text, ["yyyy/MM/dd", "yyyy-MM-dd", "dd/MM/yyyy"],
                   CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }
}
