using System.IO;
using System.Text.Json;

namespace AlMuhasib.UI.Services;

/// <summary>User print preferences persisted under AppData.</summary>
public static class PrintPreferences
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AlMuhasib",
        "print-settings.json");

    public static string? PreferredPrinter { get; set; }
    public static string PaperSize { get; set; } = "A4";
    /// <summary>POS cashier receipt paper: A4, 80mm, 58mm, or 50mm.</summary>
    public static string PosReceiptPaperSize { get; set; } = "80mm";
    /// <summary>Gold invoice paper: A4, 80mm, 58mm, or 50mm.</summary>
    public static string GoldReceiptPaperSize { get; set; } = "A4";
    /// <summary>A4 invoice layout template: Classic, Compact, or Modern.</summary>
    public static string A4InvoiceTemplate { get; set; } = A4InvoiceTemplates.Classic;
    public static bool ShowPrintPreview { get; set; } = true;

    public static void Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return;

            var json = File.ReadAllText(SettingsPath);
            var data = JsonSerializer.Deserialize<PrintSettingsData>(json);
            if (data is null)
                return;

            PreferredPrinter = data.PreferredPrinter;
            PaperSize = string.IsNullOrWhiteSpace(data.PaperSize) ? "A4" : data.PaperSize;
            PosReceiptPaperSize = string.IsNullOrWhiteSpace(data.PosReceiptPaperSize)
                ? "80mm"
                : data.PosReceiptPaperSize;
            GoldReceiptPaperSize = string.IsNullOrWhiteSpace(data.GoldReceiptPaperSize)
                ? "A4"
                : data.GoldReceiptPaperSize;
            A4InvoiceTemplate = A4InvoiceTemplates.Normalize(data.A4InvoiceTemplate);
            ShowPrintPreview = data.ShowPrintPreview;
        }
        catch
        {
            // ignore corrupt settings
        }
    }

    public static void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(dir);

            var data = new PrintSettingsData
            {
                PreferredPrinter = PreferredPrinter,
                PaperSize = PaperSize,
                PosReceiptPaperSize = PosReceiptPaperSize,
                GoldReceiptPaperSize = GoldReceiptPaperSize,
                A4InvoiceTemplate = A4InvoiceTemplates.Normalize(A4InvoiceTemplate),
                ShowPrintPreview = ShowPrintPreview
            };

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // non-critical
        }
    }

    private sealed class PrintSettingsData
    {
        public string? PreferredPrinter { get; set; }
        public string PaperSize { get; set; } = "A4";
        public string PosReceiptPaperSize { get; set; } = "80mm";
        public string GoldReceiptPaperSize { get; set; } = "A4";
        public string A4InvoiceTemplate { get; set; } = A4InvoiceTemplates.Classic;
        public bool ShowPrintPreview { get; set; } = true;
    }
}

/// <summary>Placeholder A4 invoice print templates — visual design to be finalized later.</summary>
public static class A4InvoiceTemplates
{
    public const string Classic = "Classic";
    public const string Compact = "Compact";
    public const string Modern = "Modern";

    public static readonly IReadOnlyList<(string Id, string ArabicName, string Description)> All =
    [
        (Classic, "كلاسيكي", "التخطيط الحالي — بانر أزرق وجدول واضح"),
        (Compact, "مضغوط", "مسافات أصغر مناسب للفواتير الطويلة (تصميم لاحق)"),
        (Modern, "عصري", "عنوان هادئ وخطوط أنظف (تصميم لاحق)")
    ];

    public static string Normalize(string? value) =>
        value switch
        {
            Compact => Compact,
            Modern => Modern,
            _ => Classic
        };

    public static string GetArabicName(string? value) =>
        All.FirstOrDefault(x => x.Id == Normalize(value)).ArabicName ?? "كلاسيكي";
}
