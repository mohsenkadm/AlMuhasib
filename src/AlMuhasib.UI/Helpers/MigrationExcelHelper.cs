using System.Globalization;
using System.IO;
using ClosedXML.Excel;

namespace AlMuhasib.UI.Helpers;

/// <summary>قوالب Excel بسيطة لخطوات معالج النقل التي لا تملك خدمة مخصّصة.</summary>
public static class MigrationExcelHelper
{
    public static byte[] BuildTemplate(params string[] headers)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("البيانات");
        sheet.RightToLeft = true;
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            sheet.Column(i + 1).Width = 18;
        }
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static IReadOnlyList<string[]> ReadDataRows(string filePath, int expectedColumns)
    {
        using var workbook = new XLWorkbook(filePath);
        var sheet = workbook.Worksheets.First();
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
