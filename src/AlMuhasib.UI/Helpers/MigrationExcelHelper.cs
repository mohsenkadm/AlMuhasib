using System.Globalization;
using System.IO;
using ClosedXML.Excel;

namespace AlMuhasib.UI.Helpers;

/// <summary>قوالب Excel بسيطة لخطوات معالج النقل التي لا تملك خدمة مخصّصة.</summary>
public static class MigrationExcelHelper
{
    public static byte[] BuildTemplate(params string[] headers)
        => BuildTemplate(headers, sampleRows: null, guide: null);

    public static byte[] BuildTemplate(
        string[] headers,
        string[]? sampleRow1 = null,
        string[]? sampleRow2 = null,
        string? guide = null)
    {
        var samples = new List<string[]>();
        if (sampleRow1 is { Length: > 0 }) samples.Add(sampleRow1);
        if (sampleRow2 is { Length: > 0 }) samples.Add(sampleRow2);
        return BuildTemplate(headers, samples, guide);
    }

    public static byte[] BuildTemplate(
        IReadOnlyList<string> headers,
        IReadOnlyList<string[]>? sampleRows,
        string? guide)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("البيانات");
        sheet.RightToLeft = true;

        for (var i = 0; i < headers.Count; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E3F2FD");
            sheet.Column(i + 1).Width = i == 0 ? 22 : 16;
        }

        if (sampleRows is not null)
        {
            for (var r = 0; r < sampleRows.Count; r++)
            {
                var row = sampleRows[r];
                for (var c = 0; c < headers.Count && c < row.Length; c++)
                    sheet.Cell(r + 2, c + 1).Value = row[c];
            }
        }

        if (!string.IsNullOrWhiteSpace(guide))
        {
            var guideSheet = workbook.Worksheets.Add("تعليمات");
            guideSheet.RightToLeft = true;
            guideSheet.Column(1).Width = 90;
            guideSheet.Cell(1, 1).Value = "تعليمات تعبئة القالب";
            guideSheet.Cell(1, 1).Style.Font.Bold = true;
            guideSheet.Cell(2, 1).Value = guide;
            guideSheet.Cell(3, 1).Value = "املأ ورقة «البيانات» ثم احفظ الملف واستورده من المعالج. احذف صفوف المثال أو عدّلها قبل الاستيراد.";
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static IReadOnlyList<string[]> ReadDataRows(string filePath, int expectedColumns)
    {
        using var workbook = new XLWorkbook(filePath);
        var sheet = workbook.Worksheets.FirstOrDefault(w =>
                        w.Name.Equals("البيانات", StringComparison.OrdinalIgnoreCase))
                    ?? workbook.Worksheet(1);
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        var rows = new List<string[]>();
        for (var r = 2; r <= lastRow; r++)
        {
            var values = new string[expectedColumns];
            var empty = true;
            for (var c = 0; c < expectedColumns; c++)
            {
                values[c] = sheet.Cell(r, c + 1).GetFormattedString()?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(values[c])) empty = false;
            }
            if (!empty) rows.Add(values);
        }
        return rows;
    }

    public static decimal ParseDecimal(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        text = text.Replace(",", "").Trim();
        return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)
            || decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out v)
            ? v
            : 0;
    }

    public static int ParseInt(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        return int.TryParse(text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v)
            || int.TryParse(text.Trim(), NumberStyles.Any, CultureInfo.CurrentCulture, out v)
            ? v
            : 0;
    }

    public static DateTime ParseDate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return DateTime.Today;
        if (DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out var d)
            || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
            return d.Date;
        return DateTime.Today;
    }
}
