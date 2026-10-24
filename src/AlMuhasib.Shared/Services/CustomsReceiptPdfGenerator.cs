using System.Globalization;
using AlMuhasib.Core;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Customs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AlMuhasib.Shared.Services;

/// <summary>
/// QuestPDF portrait Customs receipt voucher. Branding header from print settings at top.
/// </summary>
internal static class CustomsReceiptPdfGenerator
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

    public static byte[] Generate(VoucherPrintModel m)
    {
        EnsureLicense();
        var branding = PrintBrandingProvider.Current;
        var data = m.CustomsReceipt ?? new CustomsReceiptPrintData
        {
            Title = m.Title,
            ReceiptNumber = m.VoucherNumber,
            ReceiptDate = m.Date,
            CustomerName = m.PartyName ?? string.Empty,
            Amount = m.Amount,
            CurrencyLabel = m.CurrencyLabel,
            PaymentMethodLabel = m.VoucherTypeLabel,
            NoteText = m.Notes ?? string.Empty,
            TotalReceivedDisplay = $"{m.Amount:N0} {m.CurrencyLabel}"
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
                        row.RelativeItem(1.2f).AlignCenter().AlignMiddle()
                            .Text(data.Title).Bold().FontSize(14);
                        row.RelativeItem().Column(meta =>
                        {
                            meta.Item().Text(t =>
                            {
                                t.Span("رقم الوصل: ").SemiBold().FontColor("#546E7A");
                                t.Span(data.ReceiptNumber).SemiBold();
                            });
                            meta.Item().PaddingTop(2).Text(t =>
                            {
                                t.Span("التاريخ: ").SemiBold().FontColor("#546E7A");
                                t.Span(FormatArabicDate(data.ReceiptDate)).SemiBold();
                            });
                        });
                    });

                    col.Item().PaddingTop(10).Background("#EEF1F5").Border(0.8f).BorderColor("#90A4AE").Padding(12).Column(box =>
                    {
                        box.Item().Text(t =>
                        {
                            t.Span("استلمنا من السادة: ").SemiBold().FontColor("#546E7A");
                            t.Span(data.CustomerName).SemiBold().FontSize(11);
                        });
                        box.Item().PaddingTop(8).Row(r =>
                        {
                            r.AutoItem().AlignMiddle().Text("مبلغ وقدره: ").SemiBold().FontColor("#546E7A");
                            r.RelativeItem().Background("#FFF8E1").Border(1).BorderColor("#F9A825").Padding(6)
                                .Text(data.AmountInWords).Bold().FontSize(11);
                        });
                        box.Item().PaddingTop(8).Text(t =>
                        {
                            t.Span("طريقة الدفع: ").SemiBold().FontColor("#546E7A");
                            t.Span(data.PaymentMethodLabel).SemiBold();
                        });
                        box.Item().PaddingTop(6).Text(t =>
                        {
                            t.Span("وذلك عن: ").SemiBold().FontColor("#546E7A");
                            t.Span(data.PurposeText).SemiBold();
                        });
                    });

                    col.Item().PaddingTop(12).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1.6f);
                            c.RelativeColumn(1.0f);
                            c.RelativeColumn(1.15f);
                            c.RelativeColumn(1.15f);
                        });

                        table.Header(h =>
                        {
                            Header(h, "تفاصيل الحساب");
                            Header(h, "المبلغ الإجمالي للفاتورة");
                            Header(h, "المبلغ المقبوض (الدفعة الحالية)");
                            Header(h, "المبلغ المتبقي بذمة الزبون");
                        });

                        foreach (var row in data.Allocations)
                        {
                            table.Cell().Element(BodyCell).Text(row.AccountDetails);
                            table.Cell().Element(BodyCell).AlignCenter().Text(row.InvoiceTotalDisplay).SemiBold();
                            table.Cell().Element(BodyCell).AlignCenter().Text(row.PaidAmountDisplay).Bold().FontColor("#2E7D32");
                            table.Cell().Element(BodyCell).AlignCenter().Text(row.RemainingAmountDisplay).Bold().FontColor("#C62828");
                        }
                    });

                    col.Item().PaddingTop(10).Background("#1B5E20").Padding(12).Row(row =>
                    {
                        row.RelativeItem().AlignMiddle()
                            .Text("إجمالي المبلغ المستلم بموجب هذا الوصل:")
                            .Bold().FontSize(11).FontColor(Colors.White);
                        row.ConstantItem(160).AlignMiddle().AlignLeft()
                            .Text(data.TotalReceivedDisplay)
                            .Bold().FontSize(15).FontColor(Colors.White);
                    });

                    if (!string.IsNullOrWhiteSpace(data.NoteText))
                    {
                        col.Item().PaddingTop(10).Background("#F5F5F5").Border(0.7f).BorderColor("#90A4AE").Padding(10).Text(t =>
                        {
                            t.Span("ملاحظة: ").Bold().FontColor("#C62828");
                            t.Span(data.NoteText);
                        });
                    }

                    var receiver = string.IsNullOrWhiteSpace(branding.CompanyName)
                        ? $"{data.ReceiverSignatureLabel}:"
                        : $"{data.ReceiverSignatureLabel} ({branding.CompanyName}):";

                    col.Item().PaddingTop(22).Row(row =>
                    {
                        row.RelativeItem().Border(0.7f).BorderColor("#90A4AE").Padding(10).AlignCenter().Column(c =>
                        {
                            c.Item().Text(receiver).SemiBold();
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
        h.Cell().Element(HeaderCell).AlignCenter().Text(title).FontSize(8.5f).Bold().FontColor(Colors.White);

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
        c.Border(0.5f).BorderColor("#90A4AE").Padding(6);

    private static string FormatArabicDate(DateTime date)
    {
        var month = date.Month is >= 1 and <= 12 ? ArabicMonths[date.Month] : date.Month.ToString(CultureInfo.InvariantCulture);
        return $"{date.Day} {month} {date.Year}";
    }
}
