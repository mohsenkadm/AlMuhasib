using System.Globalization;
using AlMuhasib.Core;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Customs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AlMuhasib.Shared.Services;

/// <summary>
/// QuestPDF portrait Customs outstanding-balance claim.
/// Branding header image/text from print settings at top.
/// </summary>
internal static class CustomsOutstandingStatementPdfGenerator
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

    public static byte[] Generate(StatementPrintModel m)
    {
        EnsureLicense();
        var branding = PrintBrandingProvider.Current;
        var data = m.CustomsOutstandingStatement ?? new CustomsOutstandingStatementPrintData
        {
            Title = m.Title,
            CustomerName = m.PartyName,
            DocumentDate = DateTime.Today
        };

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(22);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial").FontColor("#212121"));
                page.ContentFromRightToLeft();

                page.Content().Column(col =>
                {
                    AppendBrandingHeader(col, branding);

                    col.Item().PaddingTop(8).Background("#EEF1F5").Border(0.7f).BorderColor("#90A4AE").Padding(10).Row(row =>
                    {
                        row.RelativeItem(1.3f).AlignCenter().AlignMiddle()
                            .Text(data.Title).Bold().FontSize(13).FontColor("#C62828");
                        row.RelativeItem().Column(meta =>
                        {
                            meta.Item().Text(t =>
                            {
                                t.Span("رقم المستند: ").SemiBold().FontColor("#546E7A");
                                t.Span(data.DocumentNumber).SemiBold();
                            });
                            meta.Item().PaddingTop(2).Text(t =>
                            {
                                t.Span("التاريخ: ").SemiBold().FontColor("#546E7A");
                                t.Span(FormatArabicDate(data.DocumentDate)).SemiBold();
                            });
                        });
                    });

                    col.Item().PaddingTop(10).Background("#EEF1F5").Border(0.8f).BorderColor("#90A4AE").Padding(12).Column(box =>
                    {
                        box.Item().Text(t =>
                        {
                            t.Span("المستدين / المطلوب: ").SemiBold().FontColor("#546E7A");
                            t.Span(data.CustomerName).SemiBold().FontSize(11);
                        });
                        box.Item().PaddingTop(4).Text(t =>
                        {
                            t.Span("مرجع الفاتورة الأصلية: ").SemiBold().FontColor("#546E7A");
                            t.Span(data.InvoiceReferenceText).SemiBold();
                        });
                        box.Item().PaddingTop(4).Text(t =>
                        {
                            t.Span("حالة الحساب: ").SemiBold().FontColor("#546E7A");
                            t.Span(data.AccountStatusText).Bold().FontColor("#C62828");
                        });
                    });

                    col.Item().PaddingTop(12).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(24);
                            c.RelativeColumn(1.7f);
                            c.RelativeColumn(1.0f);
                            c.RelativeColumn(1.15f);
                            c.RelativeColumn(1.05f);
                        });

                        table.Header(h =>
                        {
                            Header(h, "ت");
                            Header(h, "البيان / مرجع الحساب");
                            Header(h, "المبلغ الكلي للفاتورة");
                            Header(h, data.PaidColumnHeader);
                            Header(h, "المبلغ المتبقي (الدين)");
                        });

                        if (data.Rows.Count == 0)
                        {
                            table.Cell().Element(BodyCell).AlignCenter().Text("—");
                            table.Cell().Element(BodyCell).Text("لا توجد ديون متبقية");
                            table.Cell().Element(BodyCell).AlignCenter().Text("—");
                            table.Cell().Element(BodyCell).AlignCenter().Text("—");
                            table.Cell().Element(BodyCell).AlignCenter().Text("—");
                        }
                        else
                        {
                            foreach (var r in data.Rows)
                            {
                                table.Cell().Element(BodyCell).AlignCenter()
                                    .Text(r.Number.ToString(CultureInfo.InvariantCulture));
                                table.Cell().Element(BodyCell).Text(r.Description);
                                table.Cell().Element(BodyCell).AlignCenter().Text(r.InvoiceTotalDisplay).SemiBold();
                                table.Cell().Element(BodyCell).AlignCenter().Text(r.PaidAmountDisplay).Bold().FontColor("#2E7D32");
                                table.Cell().Element(BodyCell).AlignCenter().Text(r.RemainingAmountDisplay).Bold().FontColor("#C62828");
                            }
                        }
                    });

                    col.Item().PaddingTop(10).Border(1.4f).BorderColor("#E57373").Background("#FFEBEE").Padding(12).Column(box =>
                    {
                        box.Item().Text("تسوية وتفصيل الدين المتبقي").Bold().FontSize(11).FontColor("#C62828");
                        box.Item().PaddingTop(4).Text($"{data.BaseInvoiceTotalLabel}: {data.BaseInvoiceTotalDisplay}");
                        box.Item().PaddingTop(2).Text($"{data.ReceiptsTotalLabel}: - {data.ReceiptsTotalDisplay}")
                            .SemiBold().FontColor("#2E7D32");
                        box.Item().PaddingTop(2).Text($"{data.RemainingLabel}: {data.RemainingDisplay}")
                            .Bold().FontColor("#C62828");
                        box.Item().PaddingTop(4).Text(data.AmountInWords).SemiBold();
                    });

                    col.Item().PaddingTop(10).Background("#B71C1C").Padding(12).Row(row =>
                    {
                        row.RelativeItem().AlignMiddle()
                            .Text("إجمالي المبلغ المستحق للدفع (Balance Due):")
                            .Bold().FontSize(11).FontColor(Colors.White);
                        row.ConstantItem(160).AlignMiddle().AlignLeft()
                            .Text(data.BalanceDueDisplay)
                            .Bold().FontSize(15).FontColor(Colors.White);
                    });

                    var organizer = string.IsNullOrWhiteSpace(branding.CompanyName)
                        ? $"{data.OrganizerSignatureLabel}:"
                        : $"{data.OrganizerSignatureLabel} ({branding.CompanyName}):";

                    col.Item().PaddingTop(22).Row(row =>
                    {
                        row.RelativeItem().Border(0.7f).BorderColor("#90A4AE").Padding(10).AlignCenter().Column(c =>
                        {
                            c.Item().Text(organizer).SemiBold();
                            c.Item().PaddingTop(20).Text("________________").FontColor("#546E7A");
                        });
                        row.ConstantItem(12);
                        row.RelativeItem().Border(0.7f).BorderColor("#90A4AE").Padding(10).AlignCenter().Column(c =>
                        {
                            c.Item().Text(data.PayerSignatureLabel + ":").SemiBold();
                            c.Item().PaddingTop(20).Text("________________").FontColor("#546E7A");
                        });
                    });
                });
            });
        }).GeneratePdf();
    }

    private static void Header(TableCellDescriptor h, string title) =>
        h.Cell().Element(HeaderCell).AlignCenter().Text(title).FontSize(8).Bold().FontColor(Colors.White);

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
        c.Background("#15254C").Border(0.4f).BorderColor(Colors.White).PaddingVertical(6).PaddingHorizontal(3);

    private static IContainer BodyCell(IContainer c) =>
        c.Border(0.5f).BorderColor("#90A4AE").Padding(5);

    private static string FormatArabicDate(DateTime date)
    {
        var month = date.Month is >= 1 and <= 12 ? ArabicMonths[date.Month] : date.Month.ToString(CultureInfo.InvariantCulture);
        return $"{date.Day} {month} {date.Year}";
    }
}
