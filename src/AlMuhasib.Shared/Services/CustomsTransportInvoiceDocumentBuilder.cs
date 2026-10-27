using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using AlMuhasib.Core;
using AlMuhasib.Core.Enums.Customs;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Customs;

namespace AlMuhasib.Shared.Services;

/// <summary>
/// Portrait Customs transport invoice (internal / external) matching company paper forms.
/// Print branding (إعدادات الطابعة) is prepended at the top.
/// </summary>
public static class CustomsTransportInvoiceDocumentBuilder
{
    private static readonly SolidColorBrush Navy = Freeze(Color.FromRgb(0x15, 0x25, 0x4C));
    private static readonly SolidColorBrush InfoBarBg = Freeze(Color.FromRgb(0xEE, 0xF1, 0xF5));
    private static readonly SolidColorBrush GridBrush = Freeze(Color.FromRgb(0x90, 0xA4, 0xAE));
    private static readonly SolidColorBrush YellowBox = Freeze(Color.FromRgb(0xFF, 0xF8, 0xE1));
    private static readonly SolidColorBrush YellowBorder = Freeze(Color.FromRgb(0xF9, 0xA8, 0x25));
    private static readonly SolidColorBrush GreenBorder = Freeze(Color.FromRgb(0x2E, 0x7D, 0x32));
    private static readonly SolidColorBrush GreenBg = Freeze(Color.FromRgb(0xE8, 0xF5, 0xE9));
    private static readonly SolidColorBrush Ink = Freeze(Color.FromRgb(0x21, 0x21, 0x21));
    private static readonly SolidColorBrush Muted = Freeze(Color.FromRgb(0x54, 0x6E, 0x7A));
    private static readonly SolidColorBrush AltRow = Freeze(Color.FromRgb(0xFA, 0xFB, 0xFC));

    private static readonly string[] ArabicMonths =
    [
        "", "كانون الثاني", "شباط", "آذار", "نيسان", "أيار", "حزيران",
        "تموز", "آب", "أيلول", "تشرين الأول", "تشرين الثاني", "كانون الأول"
    ];

    public static Size ResolvePageSize(InvoicePrintModel m) =>
        InvoicePaperSizes.GetPageSize(
            string.IsNullOrWhiteSpace(m.PaperSize) ? InvoicePaperSizes.A4 : m.PaperSize);

    public static FlowDocument Build(InvoicePrintModel m)
    {
        var data = m.CustomsTransport ?? new CustomsTransportInvoicePrintData
        {
            Title = m.Title,
            InvoiceNumber = m.InvoiceNumber,
            InvoiceDate = m.Date,
            CustomerName = m.PartyName,
            CurrencyLabel = m.CurrencyLabel,
            GrandTotal = m.GrandTotal
        };

        var pageSize = ResolvePageSize(m);
        var pagePadding = new Thickness(28, 16, 28, 16);
        var contentWidth = pageSize.Width - pagePadding.Left - pagePadding.Right;

        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
            FontSize = 11,
            Foreground = Ink,
            FlowDirection = FlowDirection.RightToLeft,
            PageWidth = pageSize.Width,
            PageHeight = pageSize.Height,
            PagePadding = pagePadding,
            ColumnWidth = contentWidth
        };

        PrintBrandingFlowDocumentHelper.PrependBrandingHeader(doc, branding: null, maxHeaderImageHeight: 92);

        if (data.TransportKind == CustomsTransportKind.Internal)
            BuildInternalBody(doc, data, contentWidth);
        else
            BuildExternalBody(doc, data, contentWidth);

        PrintBrandingFlowDocumentHelper.AppendBrandingFooter(
            doc,
            systemLine: $"معاينة الطباعة — {data.Title} — الصفحة 1 من 1");

        return doc;
    }

    private static void BuildInternalBody(FlowDocument doc, CustomsTransportInvoicePrintData data, double contentWidth)
    {
        doc.Blocks.Add(CenteredTitle(data.Title, 16));
        doc.Blocks.Add(BuildMetaBlock(data));
        doc.Blocks.Add(BuildInternalInfoBox(data, contentWidth));
        doc.Blocks.Add(BuildInternalTable(data, contentWidth));
        doc.Blocks.Add(BuildInternalFinanceBox(data, contentWidth));
        doc.Blocks.Add(BuildGrandTotalBar(data, contentWidth, useGreen: false));
        doc.Blocks.Add(BuildSignatures(data, contentWidth));
    }

    private static void BuildExternalBody(FlowDocument doc, CustomsTransportInvoicePrintData data, double contentWidth)
    {
        doc.Blocks.Add(BuildExternalTitleBar(data, contentWidth));
        doc.Blocks.Add(BuildExternalCustomerBar(data, contentWidth));
        doc.Blocks.Add(BuildExternalTable(data, contentWidth));
        doc.Blocks.Add(BuildExternalFinanceBox(data, contentWidth));
        doc.Blocks.Add(BuildGrandTotalBar(data, contentWidth, useGreen: true));
        doc.Blocks.Add(BuildSignatures(data, contentWidth));
    }

    private static Block CenteredTitle(string text, double size)
    {
        return new Paragraph(new Run(text)
        {
            FontWeight = FontWeights.Bold,
            FontSize = size
        })
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 4, 0, 8)
        };
    }

    private static Block BuildMetaBlock(CustomsTransportInvoicePrintData data)
    {
        var p = new Paragraph { Margin = new Thickness(0, 0, 0, 8), TextAlignment = TextAlignment.Right };
        p.Inlines.Add(new Run("رقم الفاتورة: ") { FontWeight = FontWeights.Bold, Foreground = Muted });
        p.Inlines.Add(new Run(data.InvoiceNumber) { FontWeight = FontWeights.SemiBold });
        p.Inlines.Add(new LineBreak());
        p.Inlines.Add(new Run("التاريخ: ") { FontWeight = FontWeights.Bold, Foreground = Muted });
        p.Inlines.Add(new Run(FormatArabicDate(data.InvoiceDate)) { FontWeight = FontWeights.SemiBold });
        return p;
    }

    private static Block BuildExternalTitleBar(CustomsTransportInvoicePrintData data, double contentWidth)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 4, 0, 6) };
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth) });
        var row = new TableRow { Background = Navy };
        var p = new Paragraph { Margin = new Thickness(12, 10, 12, 10), TextAlignment = TextAlignment.Center };
        p.Inlines.Add(new Run(data.Title) { Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 14 });
        p.Inlines.Add(new LineBreak());
        p.Inlines.Add(new Run($"رقم الفاتورة: {data.InvoiceNumber}    |    التاريخ: {FormatArabicDate(data.InvoiceDate)}")
        {
            Foreground = Brushes.White,
            FontSize = 10
        });
        row.Cells.Add(new TableCell(p));
        var g = new TableRowGroup();
        g.Rows.Add(row);
        table.RowGroups.Add(g);
        return table;
    }

    private static Block BuildExternalCustomerBar(CustomsTransportInvoicePrintData data, double contentWidth)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 8) };
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth) });
        var row = new TableRow { Background = InfoBarBg };
        var p = new Paragraph { Margin = new Thickness(12, 8, 12, 8) };
        p.Inlines.Add(new Run("المستورد / الشركة: ") { FontWeight = FontWeights.Bold, Foreground = Muted });
        p.Inlines.Add(new Run(data.CustomerName) { FontWeight = FontWeights.SemiBold, FontSize = 12 });
        row.Cells.Add(new TableCell(p)
        {
            BorderBrush = GridBrush,
            BorderThickness = new Thickness(0.6)
        });
        var g = new TableRowGroup();
        g.Rows.Add(row);
        table.RowGroups.Add(g);
        return table;
    }

    private static Block BuildInternalInfoBox(CustomsTransportInvoicePrintData data, double contentWidth)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 10) };
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth) });
        var row = new TableRow { Background = InfoBarBg };
        var p = new Paragraph { Margin = new Thickness(12, 10, 12, 10) };
        AddLabeledLine(p, "المطلوب من السادة", data.CustomerName);
        p.Inlines.Add(new LineBreak());
        AddLabeledLine(p, "مسار النقل الداخلي", string.IsNullOrWhiteSpace(data.TransportRoute) ? "—" : data.TransportRoute);
        p.Inlines.Add(new LineBreak());
        AddLabeledLine(p, "نوع الخدمة", string.IsNullOrWhiteSpace(data.ServiceDescription) ? "—" : data.ServiceDescription);
        row.Cells.Add(new TableCell(p)
        {
            BorderBrush = GridBrush,
            BorderThickness = new Thickness(0.8)
        });
        var g = new TableRowGroup();
        g.Rows.Add(row);
        table.RowGroups.Add(g);
        return table;
    }

    private static void AddLabeledLine(Paragraph p, string label, string value)
    {
        p.Inlines.Add(new Run(label + ": ") { FontWeight = FontWeights.Bold, Foreground = Muted, FontSize = 11 });
        p.Inlines.Add(new Run(value) { FontWeight = FontWeights.SemiBold, FontSize = 11 });
    }

    private static Block BuildInternalTable(CustomsTransportInvoicePrintData data, double contentWidth)
    {
        string[] headers =
        [
            "ت", "رقم الفاتورة", "السائق السوري", "رقم السيارة", "السائق العراقي",
            "رقم السيارة", "تاريخ التحميل", "تاريخ التخريج", "غرامات تأخيرية", "الملاحظات"
        ];
        double[] weights = [0.35, 1.1, 1.2, 0.9, 1.1, 0.9, 0.9, 0.9, 1.0, 0.9];

        var table = CreateTable(contentWidth, weights);
        AddHeaderRow(table, headers);

        var body = new TableRowGroup();
        var currency = data.CurrencyLabel;
        for (var i = 0; i < data.Rows.Count; i++)
        {
            var r = data.Rows[i];
            var row = new TableRow();
            if (i % 2 == 1) row.Background = AltRow;
            row.Cells.Add(TextCell(r.Number.ToString(CultureInfo.InvariantCulture), center: true));
            row.Cells.Add(TextCell(r.ReferenceNumber, center: true));
            row.Cells.Add(TextCell(r.SyrianDriver));
            row.Cells.Add(TextCell(r.SyrianPlate, center: true));
            row.Cells.Add(TextCell(r.IraqiDriver));
            row.Cells.Add(TextCell(r.IraqiPlate, center: true));
            row.Cells.Add(TextCell(r.LoadingDate, center: true));
            row.Cells.Add(TextCell(r.UnloadingDate, center: true));
            row.Cells.Add(TextCell(FormatMoney(r.DelayPenaltyAmount, currency, data.IsUsd), center: true, bold: true));
            row.Cells.Add(TextCell(r.Notes));
            body.Rows.Add(row);
        }

        if (data.Rows.Count == 0)
        {
            var empty = new TableRow();
            for (var i = 0; i < headers.Length; i++)
                empty.Cells.Add(TextCell(i == 0 ? "1" : string.Empty, center: true));
            body.Rows.Add(empty);
        }

        table.RowGroups.Add(body);
        return table;
    }

    private static Block BuildExternalTable(CustomsTransportInvoicePrintData data, double contentWidth)
    {
        string[] headers = ["ت", "السيارة السورية", "اسم السائق", "البضاعة", "رقم الفاتورة", "الوجهة", "أجرة النقل ($)"];
        double[] weights = [0.35, 1.0, 1.2, 1.1, 1.1, 1.1, 0.9];

        var table = CreateTable(contentWidth, weights);
        AddHeaderRow(table, headers);

        var body = new TableRowGroup();
        for (var i = 0; i < data.Rows.Count; i++)
        {
            var r = data.Rows[i];
            var row = new TableRow();
            if (i % 2 == 1) row.Background = AltRow;
            row.Cells.Add(TextCell(r.Number.ToString(CultureInfo.InvariantCulture), center: true));
            row.Cells.Add(TextCell(r.SyrianPlate, center: true));
            row.Cells.Add(TextCell(r.SyrianDriver));
            row.Cells.Add(TextCell(r.CargoType, center: true));
            row.Cells.Add(TextCell(r.ReferenceNumber, center: true));
            row.Cells.Add(TextCell(r.Destination, center: true));
            row.Cells.Add(TextCell(FormatMoney(r.VehiclePrice, data.CurrencyLabel, data.IsUsd), center: true, bold: true));
            body.Rows.Add(row);
        }

        if (data.Rows.Count == 0)
        {
            var empty = new TableRow();
            for (var i = 0; i < headers.Length; i++)
                empty.Cells.Add(TextCell(i == 0 ? "1" : string.Empty, center: true));
            body.Rows.Add(empty);
        }

        table.RowGroups.Add(body);
        return table;
    }

    private static Block BuildInternalFinanceBox(CustomsTransportInvoicePrintData data, double contentWidth)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 10, 0, 8) };
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth) });
        var row = new TableRow { Background = YellowBox };
        var p = new Paragraph { Margin = new Thickness(14, 12, 14, 12) };
        p.Inlines.Add(new Run("تفاصيل الحساب المالي للنقل الداخلي:")
        {
            FontWeight = FontWeights.Bold,
            FontSize = 12
        });
        p.Inlines.Add(new LineBreak());

        foreach (var g in data.PriceGroups)
        {
            var unit = data.IsUsd ? $"${g.UnitPrice:N2}" : $"{g.UnitPrice:N0} د.ع";
            var line = data.IsUsd
                ? $"{g.VehicleCount} سيارات × {unit} = ${g.LineTotal:N2}"
                : $"{g.VehicleCount} سيارات × {unit} = {g.LineTotal:N0} دينار عراقي";
            p.Inlines.Add(new Run(line) { FontSize = 11 });
            p.Inlines.Add(new LineBreak());
        }

        var penaltyText = data.IsUsd
            ? $"${data.DelayPenaltiesTotal:N2}"
            : $"{data.DelayPenaltiesTotal:N0} دينار عراقي";
        p.Inlines.Add(new Run($"إجمالي الغرامات التأخيرية: {penaltyText}") { FontSize = 11 });
        p.Inlines.Add(new LineBreak());
        p.Inlines.Add(new Run(data.AmountInWords) { FontWeight = FontWeights.SemiBold, FontSize = 11 });

        row.Cells.Add(new TableCell(p)
        {
            BorderBrush = YellowBorder,
            BorderThickness = new Thickness(1.5)
        });
        var group = new TableRowGroup();
        group.Rows.Add(row);
        table.RowGroups.Add(group);
        return table;
    }

    private static Block BuildExternalFinanceBox(CustomsTransportInvoicePrintData data, double contentWidth)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 10, 0, 8) };
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth) });
        var row = new TableRow { Background = YellowBox };
        var p = new Paragraph { Margin = new Thickness(14, 12, 14, 12) };
        p.Inlines.Add(new Run("تفصيل واحتساب الحساب المالي:")
        {
            FontWeight = FontWeights.Bold,
            FontSize = 12
        });
        p.Inlines.Add(new LineBreak());

        foreach (var g in data.PriceGroups)
        {
            var carWord = g.VehicleCount == 1 ? "سيارة" : "سيارات";
            p.Inlines.Add(new Run($"{g.VehicleCount} {carWord} × ${g.UnitPrice:N0} = ${g.LineTotal:N2}")
            {
                FontSize = 11
            });
            p.Inlines.Add(new LineBreak());
        }

        row.Cells.Add(new TableCell(p)
        {
            BorderBrush = YellowBorder,
            BorderThickness = new Thickness(1.2)
        });
        var group = new TableRowGroup();
        group.Rows.Add(row);
        table.RowGroups.Add(group);
        return table;
    }

    private static Block BuildGrandTotalBar(CustomsTransportInvoicePrintData data, double width, bool useGreen)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 4, 0, 12) };
        table.Columns.Add(new TableColumn { Width = new GridLength(width * 0.55) });
        table.Columns.Add(new TableColumn { Width = new GridLength(width * 0.45) });

        var bg = useGreen ? GreenBg : Navy;
        var fg = useGreen ? GreenBorder : Brushes.White;
        var border = useGreen ? GreenBorder : Navy;

        var row = new TableRow { Background = bg };
        var label = new Paragraph(new Run("المجموع الكلي المطلوب")
        {
            FontWeight = FontWeights.Bold,
            FontSize = 13,
            Foreground = useGreen ? Ink : Brushes.White
        })
        {
            Margin = new Thickness(12, 10, 12, 4)
        };
        if (useGreen && !string.IsNullOrWhiteSpace(data.AmountInWords))
        {
            label.Inlines.Add(new LineBreak());
            label.Inlines.Add(new Run(data.AmountInWords) { FontSize = 10, FontWeight = FontWeights.Normal, Foreground = Muted });
        }

        var amountText = data.IsUsd
            ? $"${data.GrandTotal:N2}"
            : $"{data.GrandTotal:N0} IQD";
        var amount = new Paragraph(new Run(amountText)
        {
            FontWeight = FontWeights.Bold,
            FontSize = 18,
            Foreground = fg
        })
        {
            Margin = new Thickness(12, 10, 12, 10),
            TextAlignment = TextAlignment.Left
        };

        row.Cells.Add(new TableCell(label) { BorderBrush = border, BorderThickness = new Thickness(1.5, 1.5, 0, 1.5) });
        row.Cells.Add(new TableCell(amount) { BorderBrush = border, BorderThickness = new Thickness(0, 1.5, 1.5, 1.5) });
        var g = new TableRowGroup();
        g.Rows.Add(row);
        table.RowGroups.Add(g);
        return table;
    }

    private static Block BuildSignatures(CustomsTransportInvoicePrintData data, double contentWidth)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 16, 0, 4) };
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * 0.5) });
        table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * 0.5) });
        var row = new TableRow();

        row.Cells.Add(SignatureCell(data.OrganizerLabel));
        row.Cells.Add(SignatureCell(data.RecipientLabel + (string.IsNullOrWhiteSpace(data.CustomerName) ? string.Empty : $": {data.CustomerName}")));

        var g = new TableRowGroup();
        g.Rows.Add(row);
        table.RowGroups.Add(g);
        return table;
    }

    private static TableCell SignatureCell(string title)
    {
        var p = new Paragraph { Margin = new Thickness(10, 8, 10, 28), TextAlignment = TextAlignment.Center };
        p.Inlines.Add(new Run(title) { FontWeight = FontWeights.SemiBold, FontSize = 11 });
        p.Inlines.Add(new LineBreak());
        p.Inlines.Add(new Run("________________") { Foreground = Muted, FontSize = 12 });
        return new TableCell(p)
        {
            BorderBrush = GridBrush,
            BorderThickness = new Thickness(0.8),
            Padding = new Thickness(4)
        };
    }

    private static Table CreateTable(double contentWidth, double[] weights)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 6) };
        var sum = weights.Sum();
        foreach (var w in weights)
            table.Columns.Add(new TableColumn { Width = new GridLength(contentWidth * (w / sum)) });
        return table;
    }

    private static void AddHeaderRow(Table table, string[] headers)
    {
        var group = new TableRowGroup();
        var headerRow = new TableRow { Background = Navy };
        foreach (var title in headers)
        {
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run(title)
            {
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 9
            })
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(2, 6, 2, 6)
            })
            {
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(0.4),
                Padding = new Thickness(1)
            });
        }
        group.Rows.Add(headerRow);
        table.RowGroups.Add(group);
    }

    private static TableCell TextCell(string text, bool center = false, bool bold = false, double fontSize = 9.5)
    {
        var p = new Paragraph(new Run(string.IsNullOrWhiteSpace(text) ? string.Empty : text)
        {
            FontSize = fontSize,
            FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal
        })
        {
            TextAlignment = center ? TextAlignment.Center : TextAlignment.Right,
            Margin = new Thickness(3, 4, 3, 4)
        };
        return new TableCell(p)
        {
            BorderBrush = GridBrush,
            BorderThickness = new Thickness(0.5),
            Padding = new Thickness(0)
        };
    }

    private static string FormatMoney(decimal amount, string currency, bool isUsd)
    {
        if (isUsd)
            return amount == 0 ? "$0" : $"${amount:N0}";
        return amount == 0 ? $"0 {currency}" : $"{amount:N0} {currency}";
    }

    private static string FormatArabicDate(DateTime date)
    {
        var month = date.Month is >= 1 and <= 12 ? ArabicMonths[date.Month] : date.Month.ToString(CultureInfo.InvariantCulture);
        return $"{date.Day} {month} {date.Year}";
    }

    private static SolidColorBrush Freeze(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }
}
