using System.Globalization;
using System.IO;
using System.Text;
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
        "المبلغ",
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
            "4) المبلغ: رقم أكبر من صفر — المبلغ الذي في ذمة العميل (آجل).",
            "5) التاريخ: بصيغة yyyy/MM/dd مثل 2024/01/15. الخلية الفارغة = تاريخ اليوم.",
            "6) ملاحظات: اختياري.",
            "",
            "ملاحظة: يُنشأ رصيد آجل على ذمة العميل دون التأثير على القاصة أو المخزون."
        };
        for (var i = 0; i < lines.Length; i++)
            instructions.Cell(i + 1, 1).Value = lines[i];
        instructions.Cell(1, 1).Style.Font.Bold = true;
        instructions.Cell(1, 1).Style.Font.FontSize = 14;

        var data = workbook.Worksheets.Add("البيانات");
        data.RightToLeft = true;
        WriteHeaders(data);
        AddSampleRow(data, 2, "أحمد محمد", "07701234567", "F-1001", 500000, new DateTime(2024, 6, 1), "مثال — رصيد سابق");
        AddSampleRow(data, 3, "سارة علي", "", "", 250000, DateTime.Today, "مثال — عميلة جديدة");

        data.Column(1).Width = 22;
        data.Column(2).Width = 16;
        data.Column(3).Width = 14;
        data.Column(4).Width = 14;
        data.Column(5).Width = 14;
        data.Column(6).Width = 28;

        data.Range(2, 4, 500, 4).CreateDataValidation().Decimal.Between(0.01, 999999999999);
        data.Range(2, 5, 500, 5).CreateDataValidation().Date.Between(new DateTime(2000, 1, 1), new DateTime(2100, 12, 31));

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public IReadOnlyList<OpeningPartyBalanceImportRow> ParseImportFile(string filePath)
        => OpeningPartyExcelParseHelper.Parse(filePath, "اسم_العميل");

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
        string? file, decimal amount, DateTime date, string? notes)
    {
        sheet.Cell(row, 1).Value = name;
        sheet.Cell(row, 2).Value = phone ?? string.Empty;
        sheet.Cell(row, 3).Value = file ?? string.Empty;
        sheet.Cell(row, 4).Value = amount;
        sheet.Cell(row, 5).Value = date;
        sheet.Cell(row, 5).Style.DateFormat.Format = "yyyy/MM/dd";
        sheet.Cell(row, 6).Value = notes ?? string.Empty;
        sheet.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#E1F5FE");
    }
}

public class OpeningSupplierBalanceExcelService : IOpeningSupplierBalanceExcelService
{
    private static readonly string[] Headers =
    [
        "اسم_المورد",
        "الهاتف",
        "المبلغ",
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
            "4) المبلغ: رقم أكبر من صفر — المبلغ المستحق للمورد (آجل).",
            "5) التاريخ: بصيغة yyyy/MM/dd مثل 2024/01/15. الخلية الفارغة = تاريخ اليوم.",
            "6) ملاحظات: اختياري.",
            "",
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
        data.Cell(2, 4).Value = new DateTime(2024, 6, 1);
        data.Cell(2, 4).Style.DateFormat.Format = "yyyy/MM/dd";
        data.Cell(2, 5).Value = "مثال — رصيد سابق";
        data.Row(2).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF3E0");

        data.Column(1).Width = 22;
        data.Column(2).Width = 16;
        data.Column(3).Width = 14;
        data.Column(4).Width = 14;
        data.Column(5).Width = 28;

        data.Range(2, 3, 500, 3).CreateDataValidation().Decimal.Between(0.01, 999999999999);
        data.Range(2, 4, 500, 4).CreateDataValidation().Date.Between(new DateTime(2000, 1, 1), new DateTime(2100, 12, 31));

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public IReadOnlyList<OpeningPartyBalanceImportRow> ParseImportFile(string filePath)
        => OpeningPartyExcelParseHelper.ParseSupplier(filePath);
}

internal static class OpeningPartyExcelParseHelper
{
    public static IReadOnlyList<OpeningPartyBalanceImportRow> Parse(string filePath, string _)
    {
        using var workbook = new XLWorkbook(filePath);
        var sheet = workbook.Worksheets.FirstOrDefault(w =>
            w.Name.Equals("البيانات", StringComparison.OrdinalIgnoreCase))
            ?? workbook.Worksheet(1);

        var rows = new List<OpeningPartyBalanceImportRow>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var rowNum = 2; rowNum <= lastRow; rowNum++)
        {
            var partyName = sheet.Cell(rowNum, 1).GetString().Trim();
            if (string.IsNullOrWhiteSpace(partyName))
                continue;

            var importRow = new OpeningPartyBalanceImportRow
            {
                RowNumber = rowNum,
                PartyName = partyName,
                Phone = NullIfEmpty(sheet.Cell(rowNum, 2).GetString()),
                FileNumber = NullIfEmpty(sheet.Cell(rowNum, 3).GetString()),
                Notes = NullIfEmpty(sheet.Cell(rowNum, 6).GetString())
            };

            if (!TryParseDecimal(sheet.Cell(rowNum, 4), out var amount) || amount <= 0)
                importRow.Errors.Add("المبلغ غير صالح");
            else
                importRow.Amount = amount;

            var dateCell = sheet.Cell(rowNum, 5);
            if (IsBlankCell(dateCell))
                importRow.Date = DateTime.Today;
            else if (!TryParseDate(dateCell, out var date))
                importRow.Errors.Add("التاريخ غير صالح");
            else
                importRow.Date = date;

            rows.Add(importRow);
        }

        return rows;
    }

    public static IReadOnlyList<OpeningPartyBalanceImportRow> ParseSupplier(string filePath)
    {
        using var workbook = new XLWorkbook(filePath);
        var sheet = workbook.Worksheets.FirstOrDefault(w =>
            w.Name.Equals("البيانات", StringComparison.OrdinalIgnoreCase))
            ?? workbook.Worksheet(1);

        var rows = new List<OpeningPartyBalanceImportRow>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var rowNum = 2; rowNum <= lastRow; rowNum++)
        {
            var partyName = sheet.Cell(rowNum, 1).GetString().Trim();
            if (string.IsNullOrWhiteSpace(partyName))
                continue;

            var importRow = new OpeningPartyBalanceImportRow
            {
                RowNumber = rowNum,
                PartyName = partyName,
                Phone = NullIfEmpty(sheet.Cell(rowNum, 2).GetString()),
                Notes = NullIfEmpty(sheet.Cell(rowNum, 5).GetString())
            };

            if (!TryParseDecimal(sheet.Cell(rowNum, 3), out var amount) || amount <= 0)
                importRow.Errors.Add("المبلغ غير صالح");
            else
                importRow.Amount = amount;

            var dateCell = sheet.Cell(rowNum, 4);
            if (IsBlankCell(dateCell))
                importRow.Date = DateTime.Today;
            else if (!TryParseDate(dateCell, out var date))
                importRow.Errors.Add("التاريخ غير صالح");
            else
                importRow.Date = date;

            rows.Add(importRow);
        }

        return rows;
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

    private static bool TryParseDate(IXLCell cell, out DateTime value)
    {
        value = default;
        if (cell.TryGetValue(out DateTime dt))
        {
            value = dt.Date;
            return true;
        }

        if (cell.TryGetValue(out double serial) && !double.IsNaN(serial) && serial > 20000)
        {
            try
            {
                value = DateTime.FromOADate(serial).Date;
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
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var cultures = new[]
        {
            CultureInfo.InvariantCulture,
            CultureInfo.GetCultureInfo("en-US"),
            CultureInfo.CurrentCulture,
            CultureInfo.GetCultureInfo("ar-IQ")
        };
        foreach (var culture in cultures)
        {
            if (DateTime.TryParse(text, culture, DateTimeStyles.AllowWhiteSpaces, out value))
            {
                value = value.Date;
                return true;
            }
        }

        return false;
    }
}
