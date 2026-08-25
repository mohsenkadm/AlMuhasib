using System.Globalization;
using System.IO;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using ClosedXML.Excel;

namespace AlMuhasib.Shared.Services.Gold;

public sealed class GoldItemsExcelService : IGoldItemsExcelService
{
    private static readonly string[] Headers =
    [
        "الاسم",
        "الباركود",
        "العيار",
        "الوزن_غرام",
        "أجور_الصياغة",
        "تكلفة_غرام",
        "التصنيف",
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
            "قالب استيراد أصناف الذهب",
            "",
            "1) املأ البيانات في ورقة «البيانات» فقط — لا تغيّر أسماء الأعمدة.",
            "2) الاسم: مطلوب.",
            "3) العيار: 24 أو 22 أو 21 أو 18 (أو أي عيار مسجّل في النظام).",
            "4) الوزن_غرام: رقم أكبر من صفر.",
            "5) الباركود: اختياري — يجب أن يكون فريداً إن وُجد.",
            "6) أجور_الصياغة وتكلفة_غرام: اختياريان (رقم)."
        };
        for (var i = 0; i < lines.Length; i++)
            instructions.Cell(i + 1, 1).Value = lines[i];

        var data = workbook.Worksheets.Add("البيانات");
        data.RightToLeft = true;
        for (var c = 0; c < Headers.Length; c++)
        {
            data.Cell(1, c + 1).Value = Headers[c];
            data.Cell(1, c + 1).Style.Font.Bold = true;
            data.Column(c + 1).Width = 16;
        }

        data.Cell(2, 1).Value = "خاتم ذهب";
        data.Cell(2, 2).Value = "G001";
        data.Cell(2, 3).Value = 21;
        data.Cell(2, 4).Value = 5.250;
        data.Cell(2, 5).Value = 15000;
        data.Cell(2, 6).Value = 0;
        data.Cell(2, 7).Value = "خواتم";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public IReadOnlyList<GoldItemsImportRow> ParseImportFile(string filePath)
    {
        using var workbook = new XLWorkbook(filePath);
        var sheet = workbook.Worksheets.FirstOrDefault(w => w.Name == "البيانات")
            ?? workbook.Worksheets.First();

        var headerRow = sheet.FirstRowUsed()?.RowNumber() ?? 1;
        var colMap = MapColumns(sheet, headerRow);
        if (!colMap.ContainsKey("الاسم"))
            throw new InvalidOperationException("عمود «الاسم» غير موجود في الملف");

        var rows = new List<GoldItemsImportRow>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? headerRow;
        for (var r = headerRow + 1; r <= lastRow; r++)
        {
            var name = GetCell(sheet, r, colMap, "الاسم");
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var karatText = GetCell(sheet, r, colMap, "العيار");
            if (!int.TryParse(karatText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var karat) || karat <= 0)
                throw new InvalidOperationException($"صف {r}: العيار غير صالح");

            var weightText = GetCell(sheet, r, colMap, "الوزن_غرام");
            if (!decimal.TryParse(weightText, NumberStyles.Number, CultureInfo.InvariantCulture, out var weight) || weight <= 0)
                throw new InvalidOperationException($"صف {r}: الوزن غير صالح");

            decimal.TryParse(GetCell(sheet, r, colMap, "أجور_الصياغة"), NumberStyles.Number, CultureInfo.InvariantCulture, out var making);
            decimal.TryParse(GetCell(sheet, r, colMap, "تكلفة_غرام"), NumberStyles.Number, CultureInfo.InvariantCulture, out var cost);

            rows.Add(new GoldItemsImportRow
            {
                RowNumber = r,
                Name = name.Trim(),
                Barcode = GetCell(sheet, r, colMap, "الباركود").Trim(),
                KaratValue = karat,
                WeightGrams = weight,
                MakingCharge = making,
                CostPerGram = cost,
                Category = GetCell(sheet, r, colMap, "التصنيف").Trim(),
                Notes = GetCell(sheet, r, colMap, "ملاحظات").Trim()
            });
        }

        return rows;
    }

    private static Dictionary<string, int> MapColumns(IXLWorksheet sheet, int headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastCol = sheet.LastColumnUsed()?.ColumnNumber() ?? Headers.Length;
        for (var c = 1; c <= lastCol; c++)
        {
            var header = sheet.Cell(headerRow, c).GetString().Trim();
            if (!string.IsNullOrEmpty(header) && !map.ContainsKey(header))
                map[header] = c;
        }
        return map;
    }

    private static string GetCell(IXLWorksheet sheet, int row, Dictionary<string, int> colMap, string header)
    {
        if (!colMap.TryGetValue(header, out var col))
            return string.Empty;
        return sheet.Cell(row, col).GetString().Trim();
    }
}
