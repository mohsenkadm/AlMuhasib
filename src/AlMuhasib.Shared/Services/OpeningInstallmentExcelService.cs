using System.Globalization;
using System.IO;
using System.Text;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models;
using ClosedXML.Excel;

namespace AlMuhasib.Shared.Services;

public class OpeningInstallmentExcelService : IOpeningInstallmentExcelService
{
    private static readonly string[] Headers =
    [
        "اسم_الزبون",
        "رقم_الملف",
        "المبلغ_الكلي",
        "عدد_الاقساط",
        "عدد_الاقساط_المسددة",
        "تاريخ_اول_قسط",
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
            "قالب استيراد أرصدة الأقساط الافتتاحية",
            "",
            "1) املأ البيانات في ورقة «البيانات» فقط — لا تغيّر أسماء الأعمدة.",
            "2) اسم_الزبون: مطلوب. إذا لم يكن موجوداً في النظام سيُنشأ تلقائياً.",
            "3) رقم_الملف: اختياري.",
            "4) المبلغ_الكلي: رقم أكبر من صفر.",
            "5) عدد_الاقساط: عدد صحيح أكبر من صفر (يُقبل 12 أو 12.0).",
            "6) عدد_الاقساط_المسددة: من 0 إلى عدد_الاقساط — تُسجّل كرصيد سابق ولا تدخل الأرباح. الخلية الفارغة = 0.",
            "7) تاريخ_اول_قسط: بصيغة yyyy/MM/dd مثل 2024/01/15.",
            "8) ملاحظات: اختياري.",
            "",
            "ملاحظة: الأقساط المسددة عند الاستيراد لا تؤثر على رصيد القاصة.",
            "التسديدات المستقبلية تتم من شاشة «الأقساط» وتُحسب ضمن حركات النظام.",
            "أسماء الزبائن المتكررة في الملف تُدمج في حساب عميل واحد مع فواتير منفصلة لكل صف."
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
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1565C0");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        AddSampleRow(data, 2, "أحمد محمد", "F-1001", 1200000, 12, 4, new DateTime(2024, 6, 1), "مثال — رصيد سابق");
        AddSampleRow(data, 3, "سارة علي", "", 600000, 6, 2, new DateTime(2025, 1, 1), "مثال — زبونة جديدة");

        data.Column(1).Width = 22;
        data.Column(2).Width = 14;
        data.Column(3).Width = 14;
        data.Column(4).Width = 14;
        data.Column(5).Width = 18;
        data.Column(6).Width = 16;
        data.Column(7).Width = 28;

        // المبلغ: عشري؛ أعداد الأقساط: صحيحة فقط
        data.Range(2, 3, 500, 3).CreateDataValidation().Decimal.Between(0.01, 999999999999);
        data.Range(2, 4, 500, 4).CreateDataValidation().WholeNumber.Between(1, 9999);
        data.Range(2, 5, 500, 5).CreateDataValidation().WholeNumber.Between(0, 9999);
        data.Range(2, 6, 500, 6).CreateDataValidation().Date.Between(new DateTime(2000, 1, 1), new DateTime(2100, 12, 31));

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public IReadOnlyList<OpeningInstallmentImportRow> ParseImportFile(string filePath)
    {
        using var workbook = new XLWorkbook(filePath);
        var sheet = workbook.Worksheets.FirstOrDefault(w =>
            w.Name.Equals("البيانات", StringComparison.OrdinalIgnoreCase))
            ?? workbook.Worksheet(1);

        var rows = new List<OpeningInstallmentImportRow>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var rowNum = 2; rowNum <= lastRow; rowNum++)
        {
            var customerName = sheet.Cell(rowNum, 1).GetString().Trim();
            if (string.IsNullOrWhiteSpace(customerName))
                continue;

            var importRow = new OpeningInstallmentImportRow
            {
                RowNumber = rowNum,
                CustomerName = customerName,
                FileNumber = NullIfEmpty(sheet.Cell(rowNum, 2).GetString()),
                Notes = NullIfEmpty(sheet.Cell(rowNum, 7).GetString())
            };

            if (!TryParseDecimal(sheet.Cell(rowNum, 3), out var total) || total <= 0)
                importRow.Errors.Add("المبلغ_الكلي غير صالح");
            else
                importRow.TotalAmount = total;

            if (!TryParseInt(sheet.Cell(rowNum, 4), out var count) || count <= 0)
                importRow.Errors.Add("عدد_الاقساط غير صالح");
            else
                importRow.NumberOfInstallments = count;

            var paidCell = sheet.Cell(rowNum, 5);
            // المسدد = 0 أو فارغ أو غير مقروء كصفر → مقبول دائماً ويُستورد (عقد جديد)
            importRow.PaidInstallmentsCount = ResolvePaidInstallmentsCount(paidCell, importRow);

            if (!TryParseDate(sheet.Cell(rowNum, 6), out var startDate))
                importRow.Errors.Add("تاريخ_اول_قسط غير صالح");
            else
                importRow.StartDate = startDate;

            if (importRow.NumberOfInstallments > 0 && importRow.PaidInstallmentsCount > importRow.NumberOfInstallments)
                importRow.Errors.Add("عدد الأقساط المسددة أكبر من إجمالي الأقساط");

            rows.Add(importRow);
        }

        return rows;
    }

    private static void AddSampleRow(IXLWorksheet sheet, int row, string customer, string? file,
        decimal total, int count, int paid, DateTime start, string? notes)
    {
        sheet.Cell(row, 1).Value = customer;
        sheet.Cell(row, 2).Value = file ?? string.Empty;
        sheet.Cell(row, 3).Value = total;
        sheet.Cell(row, 4).Value = count;
        sheet.Cell(row, 5).Value = paid;
        sheet.Cell(row, 6).Value = start;
        sheet.Cell(row, 6).Style.DateFormat.Format = "yyyy/MM/dd";
        sheet.Cell(row, 7).Value = notes ?? string.Empty;
        sheet.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#E3F2FD");
    }

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// يقرأ عدد الأقساط المسددة. 0 / فارغ = عقد جديد ويُقبل للاستيراد.
    /// القيم السالبة فقط تُرفض.
    /// </summary>
    private static int ResolvePaidInstallmentsCount(IXLCell paidCell, OpeningInstallmentImportRow importRow)
    {
        if (IsBlankCell(paidCell))
            return 0;

        if (TryParseInt(paidCell, out var paid))
        {
            if (paid < 0)
            {
                importRow.Errors.Add("عدد_الاقساط_المسددة غير صالح");
                return 0;
            }

            return paid;
        }

        // نص مثل "0" أو "٠" أو "0.0" — إن تعذر التحليل نعتبره 0 ولا نمنع الاستيراد
        var raw = NormalizeNumericText(paidCell.GetString());
        if (string.IsNullOrWhiteSpace(raw))
            raw = NormalizeNumericText(paidCell.GetFormattedString());

        if (string.IsNullOrWhiteSpace(raw)
            || raw is "0" or "0.0" or "0.00"
            || (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) && d == 0)
            || (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.CurrentCulture, out d) && d == 0))
        {
            return 0;
        }

        importRow.Errors.Add("عدد_الاقساط_المسددة غير صالح");
        return 0;
    }

    private static bool IsBlankCell(IXLCell cell)
    {
        if (cell.IsEmpty())
            return true;

        // خلية رقمية (بما فيها 0) ليست فارغة
        if (cell.DataType == XLDataType.Number)
            return false;

        var text = cell.GetString().Trim();
        if (string.IsNullOrEmpty(text))
        {
            var formatted = cell.GetFormattedString()?.Trim();
            return string.IsNullOrEmpty(formatted);
        }

        return false;
    }

    private static bool TryParseInt(IXLCell cell, out int value)
    {
        value = 0;
        if (cell.TryGetValue(out int i))
        {
            value = i;
            return true;
        }

        if (cell.TryGetValue(out double d) && !double.IsNaN(d) && !double.IsInfinity(d))
            return TryConvertToInt(d, out value);

        // GetFormattedString يغطي خلايا منسّقة أو قيم عائمة شبه صحيحة
        var formatted = cell.GetFormattedString()?.Trim();
        if (!string.IsNullOrWhiteSpace(formatted)
            && TryParseDecimalFromText(formatted, out var fromFmt)
            && TryConvertToInt((double)fromFmt, out value))
            return true;

        if (!TryParseDecimal(cell, out var dec))
            return false;

        return TryConvertToInt((double)dec, out value);
    }

    private static bool TryConvertToInt(double d, out int value)
    {
        value = 0;
        if (double.IsNaN(d) || double.IsInfinity(d))
            return false;

        // تسامح أوسع لأخطاء الفاصلة العائمة من Excel مثل 11.000000000000002 أو 6.999999999999999
        var rounded = Math.Round(d, MidpointRounding.AwayFromZero);
        if (Math.Abs(d - rounded) > 0.01)
            return false;
        if (rounded is < int.MinValue or > int.MaxValue)
            return false;

        value = (int)rounded;
        return true;
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

        return TryParseDecimalFromText(text, out value);
    }

    private static bool TryParseDecimalFromText(string text, out decimal value)
    {
        text = NormalizeNumericText(text);
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
            // آخر فاصل هو العشري: 1.200,50 أو 1,200.50
            if (text.LastIndexOf(',') > text.LastIndexOf('.'))
                return text.Replace(".", "", StringComparison.Ordinal).Replace(',', '.');
            return text.Replace(",", "", StringComparison.Ordinal);
        }

        if (hasComma)
        {
            var parts = text.Split(',');
            // فاصلة عشرية شائعة: 12,0 أو 115833,33
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

        // صيغ شائعة من تقارير المنصة: 09/17/2025 17:54:15 PM
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

        var spaceIdx = text.IndexOf(' ');
        if (spaceIdx > 0)
        {
            var dateOnly = text[..spaceIdx];
            foreach (var culture in cultures)
            {
                if (DateTime.TryParse(dateOnly, culture, DateTimeStyles.None, out value))
                {
                    value = value.Date;
                    return true;
                }
            }
        }

        return false;
    }
}
