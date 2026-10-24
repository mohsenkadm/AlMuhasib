using System.Globalization;
using AlMuhasib.Core;
using AlMuhasib.Core.Enums.Customs;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Customs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AlMuhasib.Shared.Services;

/// <summary>
/// QuestPDF portrait Customs transport invoice (internal / external).
/// Branding header image/text from print settings at top.
/// </summary>
internal static class CustomsTransportInvoicePdfGenerator
{
    private static bool _licenseSet;

    private static readonly string[] ArabicMonths =
    [
        "", "كانون الثاني", "شباط", "آذار", "نيسان", "أيار", "حزيران",
        "تموز", "آب", "أيلول", "تشرين الأول", "تشرين الثاني", "كانون الأول"
    ];

    private static void EnsureLicense()
    {
        if (_licenseSet) return;
        QuestPDF.Settings.License = LicenseType.Community;
        _licenseSet = true;
    }

    public static byte[] Generate(InvoicePrintModel m)
    {
        EnsureLicense();
        var branding = PrintBrandingProvider.Current;
        var data = m.CustomsTransport ?? new CustomsTransportInvoicePrintData
        {
            Title = m.Title,
            InvoiceNumber = m.InvoiceNumber,
            InvoiceDate = m.Date,
            CustomerName = m.PartyName,
            CurrencyLabel = m.CurrencyLabel,
            GrandTotal = m.GrandTotal
        };

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(22);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontFamily("Arial").FontColor("#212121"));
                page.ContentFromRightToLeft();

                page.Content().Column(col =>
                {
                    AppendBrandingHeader(col, branding);

                    if (data.TransportKind == CustomsTransportKind.Internal)
                        AppendInternal(col, data);
                    else
                        AppendExternal(col, data);
                });
            });
        }).GeneratePdf();
    }

    private static void AppendInternal(ColumnDescriptor col, CustomsTransportInvoicePrintData data)
    {
        col.Item().PaddingTop(6).AlignCenter().Text(data.Title).Bold().FontSize(14);
        col.Item().PaddingTop(6).Text(t =>
        {
            t.Span("رقم الفاتورة: ").SemiBold().FontColor("#546E7A");
            t.Span(data.InvoiceNumber).SemiBold();
        });
        col.Item().Text(t =>
        {
            t.Span("التاريخ: ").SemiBold().FontColor("#546E7A");
            t.Span(FormatArabicDate(data.InvoiceDate)).SemiBold();
        });

        col.Item().PaddingTop(8).Background("#EEF1F5").Border(0.8f).BorderColor("#90A4AE").Padding(10).Column(info =>
        {
            info.Item().Text(t =>
            {
                t.Span("المطلوب من السادة: ").SemiBold().FontColor("#546E7A");
                t.Span(data.CustomerName).SemiBold();
            });
            info.Item().PaddingTop(3).Text(t =>
            {
                t.Span("مسار النقل الداخلي: ").SemiBold().FontColor("#546E7A");
                t.Span(string.IsNullOrWhiteSpace(data.TransportRoute) ? "—" : data.TransportRoute).SemiBold();
            });
            info.Item().PaddingTop(3).Text(t =>
            {
                t.Span("نوع الخدمة: ").SemiBold().FontColor("#546E7A");
                t.Span(string.IsNullOrWhiteSpace(data.ServiceDescription) ? "—" : data.ServiceDescription).SemiBold();
            });
        });

        string[] headers =
        [
            "ت", "رقم الفاتورة", "السائق السوري", "رقم السيارة", "السائق العراقي",
            "رقم السيارة", "تاريخ التحميل", "تاريخ التفريغ", "غرامات تأخيرية", "الملاحظات"
        ];

        col.Item().PaddingTop(8).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(22);
                c.RelativeColumn(1.1f);
                c.RelativeColumn(1.2f);
                c.RelativeColumn(0.9f);
                c.RelativeColumn(1.1f);
                c.RelativeColumn(0.9f);
                c.RelativeColumn(0.9f);
                c.RelativeColumn(0.9f);
                c.RelativeColumn(1.0f);
                c.RelativeColumn(0.9f);
            });

            table.Header(h =>
            {
                foreach (var title in headers)
                    h.Cell().Element(HeaderCell).AlignCenter().Text(title).FontSize(7.5f).Bold().FontColor(Colors.White);
            });

            var rows = data.Rows.Count == 0
                ? [new CustomsTransportInvoicePrintRow { Number = 1 }]
                : data.Rows;
            foreach (var r in rows)
            {
                table.Cell().Element(BodyCell).AlignCenter().Text(r.Number.ToString(CultureInfo.InvariantCulture));
                table.Cell().Element(BodyCell).AlignCenter().Text(r.ReferenceNumber);
                table.Cell().Element(BodyCell).Text(r.SyrianDriver);
                table.Cell().Element(BodyCell).AlignCenter().Text(r.SyrianPlate);
                table.Cell().Element(BodyCell).Text(r.IraqiDriver);
                table.Cell().Element(BodyCell).AlignCenter().Text(r.IraqiPlate);
                table.Cell().Element(BodyCell).AlignCenter().Text(r.LoadingDate);
                table.Cell().Element(BodyCell).AlignCenter().Text(r.UnloadingDate);
                table.Cell().Element(BodyCell).AlignCenter().Text(FormatMoney(r.DelayPenaltyAmount, data)).Bold();
                table.Cell().Element(BodyCell).Text(r.Notes);
            }
        });

        col.Item().PaddingTop(10).Border(1.5f).BorderColor("#F9A825").Background("#FFF8E1").Padding(12).Column(box =>
        {
            box.Item().Text("تفاصيل الحساب المالي للنقل الداخلي:").Bold().FontSize(11);
            foreach (var g in data.PriceGroups)
            {
                var line = data.IsUsd
                    ? $"{g.VehicleCount} سيارات × ${g.UnitPrice:N2} = ${g.LineTotal:N2}"
                    : $"{g.VehicleCount} سيارات × {g.UnitPrice:N0} د.ع = {g.LineTotal:N0} دينار عراقي";
                box.Item().PaddingTop(2).Text(line);
            }

            var penalty = data.IsUsd
                ? $"${data.DelayPenaltiesTotal:N2}"
                : $"{data.DelayPenaltiesTotal:N0} دينار عراقي";
            box.Item().PaddingTop(2).Text($"إجمالي الغرامات التأخيرية: {penalty}");
            box.Item().PaddingTop(4).Text(data.AmountInWords).SemiBold();
        });

        AppendGrandTotal(col, data, useGreen: false);
        AppendSignatures(col, data);
    }

    private static void AppendExternal(ColumnDescriptor col, CustomsTransportInvoicePrintData data)
    {
        col.Item().PaddingTop(6).Background("#15254C").Padding(10).Column(bar =>
        {
            bar.Item().AlignCenter().Text(data.Title).Bold().FontSize(13).FontColor(Colors.White);
            bar.Item().PaddingTop(3).AlignCenter()
                .Text($"رقم الفاتورة: {data.InvoiceNumber}    |    التاريخ: {FormatArabicDate(data.InvoiceDate)}")
                .FontSize(9).FontColor(Colors.White);
        });

        col.Item().PaddingTop(6).Background("#EEF1F5").Border(0.6f).BorderColor("#90A4AE").Padding(8).Text(t =>
        {
            t.Span("المستورد / الشركة: ").SemiBold().FontColor("#546E7A");
            t.Span(data.CustomerName).SemiBold().FontSize(11);
        });

        string[] headers = ["ت", "السيارة السورية", "اسم السائق", "البضاعة", "رقم الفاتورة", "السعر ($)"];
        col.Item().PaddingTop(8).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(24);
                c.RelativeColumn(1.1f);
                c.RelativeColumn(1.4f);
                c.RelativeColumn(1.2f);
                c.RelativeColumn(1.2f);
                c.RelativeColumn(0.9f);
            });

            table.Header(h =>
            {
                foreach (var title in headers)
                    h.Cell().Element(HeaderCell).AlignCenter().Text(title).FontSize(8.5f).Bold().FontColor(Colors.White);
            });

            var rows = data.Rows.Count == 0
                ? [new CustomsTransportInvoicePrintRow { Number = 1 }]
                : data.Rows;
            foreach (var r in rows)
            {
                table.Cell().Element(BodyCell).AlignCenter().Text(r.Number.ToString(CultureInfo.InvariantCulture));
                table.Cell().Element(BodyCell).AlignCenter().Text(r.SyrianPlate);
                table.Cell().Element(BodyCell).Text(r.SyrianDriver);
                table.Cell().Element(BodyCell).AlignCenter().Text(r.CargoType);
                table.Cell().Element(BodyCell).AlignCenter().Text(r.ReferenceNumber);
                table.Cell().Element(BodyCell).AlignCenter().Text(FormatMoney(r.VehiclePrice, data)).Bold();
            }
        });

        col.Item().PaddingTop(10).Border(1.2f).BorderColor("#F9A825").Background("#FFF8E1").Padding(12).Column(box =>
        {
            box.Item().Text("تفصيل واحتساب الحساب المالي:").Bold().FontSize(11);
            foreach (var g in data.PriceGroups)
            {
                var carWord = g.VehicleCount == 1 ? "سيارة" : "سيارات";
                box.Item().PaddingTop(2).Text($"{g.VehicleCount} {carWord} × ${g.UnitPrice:N0} = ${g.LineTotal:N2}");
            }
        });

        AppendGrandTotal(col, data, useGreen: true);
        AppendSignatures(col, data);
    }

    private static void AppendGrandTotal(ColumnDescriptor col, CustomsTransportInvoicePrintData data, bool useGreen)
    {
        var amount = data.IsUsd ? $"${data.GrandTotal:N2}" : $"{data.GrandTotal:N0} IQD";
        if (useGreen)
        {
            col.Item().PaddingTop(8).Border(1.5f).BorderColor("#2E7D32").Background("#E8F5E9").Padding(10).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("المجموع الكلي المطلوب").Bold().FontSize(12);
                    if (!string.IsNullOrWhiteSpace(data.AmountInWords))
                        c.Item().PaddingTop(2).Text(data.AmountInWords).FontSize(9).FontColor("#546E7A");
                });
                row.ConstantItem(140).AlignMiddle().AlignLeft().Text(amount).Bold().FontSize(16).FontColor("#2E7D32");
            });
        }
        else
        {
            col.Item().PaddingTop(8).Background("#15254C").Padding(10).Row(row =>
            {
                row.RelativeItem().AlignMiddle().Text("المجموع الكلي المطلوب").Bold().FontSize(12).FontColor(Colors.White);
                row.ConstantItem(140).AlignMiddle().AlignLeft().Text(amount).Bold().FontSize(16).FontColor(Colors.White);
            });
        }
    }

    private static void AppendSignatures(ColumnDescriptor col, CustomsTransportInvoicePrintData data)
    {
        col.Item().PaddingTop(16).Row(row =>
        {
            row.RelativeItem().Border(0.8f).BorderColor("#90A4AE").Padding(10).AlignCenter().Column(c =>
            {
                c.Item().Text(data.OrganizerLabel).SemiBold();
                c.Item().PaddingTop(18).Text("________________").FontColor("#546E7A");
            });
            row.ConstantItem(10);
            var recipient = data.RecipientLabel
                + (string.IsNullOrWhiteSpace(data.CustomerName) ? string.Empty : $": {data.CustomerName}");
            row.RelativeItem().Border(0.8f).BorderColor("#90A4AE").Padding(10).AlignCenter().Column(c =>
            {
                c.Item().Text(recipient).SemiBold();
                c.Item().PaddingTop(18).Text("________________").FontColor("#546E7A");
            });
        });
    }

    private static void AppendBrandingHeader(ColumnDescriptor col, Core.Models.PrintBrandingSnapshot branding)
    {
        if (branding.ShowHeaderImage && branding.HeaderImageData is { Length: > 0 })
        {
            col.Item().MaxHeight(88).AlignCenter().Image(branding.HeaderImageData).FitWidth();
            return;
        }

        if (!branding.ShowHeaderText || string.IsNullOrWhiteSpace(branding.CompanyName))
            return;

        col.Item().Background("#15254C").Padding(10).Row(row =>
        {
            row.RelativeItem().AlignRight().Column(c =>
            {
                c.Item().Text(branding.CompanyName).FontSize(14).Bold().FontColor(Colors.White);
                if (!string.IsNullOrWhiteSpace(branding.Details))
                    c.Item().Text(branding.Details).FontSize(8).FontColor("#BBDEFB");
            });
            row.RelativeItem().AlignLeft().Column(c =>
            {
                if (!string.IsNullOrWhiteSpace(branding.PhonePrimary))
                    c.Item().Text(branding.PhonePrimary).FontSize(8).FontColor(Colors.White);
                if (!string.IsNullOrWhiteSpace(branding.PhoneSecondary))
                    c.Item().Text(branding.PhoneSecondary).FontSize(8).FontColor(Colors.White);
                if (!string.IsNullOrWhiteSpace(branding.Address))
                    c.Item().Text(branding.Address).FontSize(8).FontColor("#BBDEFB");
            });
        });
    }

    private static IContainer HeaderCell(IContainer c) =>
        c.Background("#15254C").Border(0.4f).BorderColor(Colors.White).Padding(3);

    private static IContainer BodyCell(IContainer c) =>
        c.Border(0.5f).BorderColor("#90A4AE").Padding(3);

    private static string FormatMoney(decimal amount, CustomsTransportInvoicePrintData data) =>
        data.IsUsd
            ? (amount == 0 ? "$0" : $"${amount:N0}")
            : (amount == 0 ? $"0 {data.CurrencyLabel}" : $"{amount:N0} {data.CurrencyLabel}");

    private static string FormatArabicDate(DateTime date)
    {
        var month = date.Month is >= 1 and <= 12 ? ArabicMonths[date.Month] : date.Month.ToString(CultureInfo.InvariantCulture);
        return $"{date.Day} {month} {date.Year}";
    }
}
