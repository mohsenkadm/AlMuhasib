using System.Globalization;
using System.IO;
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
            "4) المبلغ_الكلي: رقم أكبر من صفر (بدون فواصل آلاف).",
            "5) عدد_الاقساط: عدد صحيح أكبر من صفر.",
            "6) عدد_الاقساط_المسددة: من 0 إلى عدد_الاقساط — تُسجّل كرصيد سابق ولا تدخل الأرباح.",
            "7) تاريخ_اول_قسط: بصيغة yyyy/MM/dd مثل 2024/01/15.",
            "8) ملاحظات: اختياري.",
            "",
            "ملاحظة: الأقساط المسددة عند الاستيراد لا تؤثر على رصيد القاصة.",
            "التسديدات المستقبلية تتم من شاشة «الأقساط» وتُحسب ضمن حركات النظام."
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

        data.Range(2, 3, 500, 5).SetDataValidation().WholeNumber.Between(1, 999999999);
        data.Range(2, 5, 500, 5).SetDataValidation().WholeNumber.Between(0, 9999);
        data.Range(2, 6, 500, 6).SetDataValidation().Date.Between(new DateTime(2000, 1, 1), new DateTime(2100, 12, 31));

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

            if (!TryParseInt(sheet.Cell(rowNum, 5), out var paid) || paid < 0)
                importRow.Errors.Add("عدد_الاقsاط_المسdدة غير صالح");
            else
                importRow.PaidInstallmentsCount = paid;

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

    private static bool TryParseDecimal(IXLCell cell, out decimal value)
    {
        value = 0;
        if (cell.TryGetValue(out double d))
        {
            value = (decimal)d;
            return true;
        }

        var text = cell.GetString().Trim().Replace(",", "");
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value)
               || decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value);
    }

    private static bool TryParseInt(IXLCell cell, out int value)
    {
        value = 0;
        if (cell.TryGetValue(out int i))
            return true;
        if (cell.TryGetValue(out double d))
        {
            value = (int)d;
            return true;
        }

        var text = cell.GetString().Trim();
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
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
}
