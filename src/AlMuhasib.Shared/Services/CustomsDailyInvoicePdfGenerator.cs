using AlMuhasib.Core;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Customs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AlMuhasib.Shared.Services;

/// <summary>
/// QuestPDF landscape daily Customs form. Branding header image/text from print settings at top.
/// </summary>
internal static class CustomsDailyInvoicePdfGenerator
{
    private static bool _licenseSet;

    private static readonly string[] Headers =
    [
        "ت", "اسم السائق", "رقم العجلة", "تاريخ التفريغ", "رقم الإرسالية وعديدها",
        "سيل", "إشعاع", "رسم", "فحص سيطرة نوعية", "نقل بري مانيڤست",
        "أتعاب التخليص", "المصاريف", "ملاحظات"
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
        var data = m.CustomsDaily ?? new CustomsDailyInvoicePrintData
        {
            CustomerName = m.PartyName,
            InvoiceNumber = m.InvoiceNumber,
            InvoiceDate = m.Date,
            CurrencyLabel = m.CurrencyLabel,
            InvoiceTotal = m.GrandTotal,
            PaidAmount = m.PaidAmount,
            RemainingAmount = m.RemainingAmount
        };
        var currency = string.IsNullOrWhiteSpace(data.CurrencyLabel) ? "د.ع" : data.CurrencyLabel;
        var minRows = Math.Max(data.MinRows, data.Rows.Count);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(14);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Arial").FontColor("#212121"));
                page.ContentFromRightToLeft();

                page.Content().Column(col =>
                {
                    // هيدر إعدادات الطابعة
                    AppendBrandingHeader(col, branding);

                    // Info bar
                    col.Item().PaddingTop(4).Background("#EEF1F5").Border(0.6f).BorderColor("#90A4AE").Padding(8).Row(row =>
                    {
                        row.RelativeItem(2).Text(t =>
                        {
                            t.Span("العميل: ").SemiBold().FontColor("#546E7A");
                            t.Span(data.CustomerName).SemiBold().FontSize(10);
                        });
                        row.RelativeItem().Text(t =>
                        {
                            t.Span("رقم الفاتورة: ").SemiBold().FontColor("#546E7A");
                            t.Span(data.InvoiceNumber).SemiBold();
                        });
                        row.RelativeItem().Text(t =>
                        {
                            t.Span("تاريخ الفاتورة: ").SemiBold().FontColor("#546E7A");
                            t.Span(data.InvoiceDate.ToString("yyyy/MM/dd")).SemiBold();
                        });
                    });

                    // Main table
                    col.Item().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(22);
                            c.RelativeColumn(1.4f);
                            c.RelativeColumn(1.0f);
                            c.RelativeColumn(1.0f);
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(0.7f);
                            c.RelativeColumn(0.7f);
                            c.RelativeColumn(0.7f);
                            c.RelativeColumn(1.0f);
                            c.RelativeColumn(1.0f);
                            c.RelativeColumn(0.95f);
                            c.RelativeColumn(0.85f);
                            c.RelativeColumn(0.9f);
                        });

                        table.Header(h =>
                        {
                            foreach (var title in Headers)
                            {
                                h.Cell().Element(HeaderCell).AlignCenter().Text(title).FontSize(7.5f).Bold().FontColor(Colors.White);
                            }

                            for (var i = 0; i < Headers.Length; i++)
                            {
                                var text = i is >= 5 and <= 11 ? currency : (i == 4 ? "العملة / الوحدة:" : "");
                                h.Cell().Element(UnitCell).AlignCenter().Text(text).FontSize(7).SemiBold();
                            }
                        });

                        for (var i = 0; i < minRows; i++)
                        {
                            var r = i < data.Rows.Count
                                ? data.Rows[i]
                                : new CustomsDailyInvoicePrintRow { Number = i + 1 };

                            void Cell(string text, bool center = true) =>
                                table.Cell().Element(BodyCell).AlignCenter().Text(string.IsNullOrWhiteSpace(text) ? "—" : text).FontSize(7.5f);

                            Cell(r.Number.ToString());
                            Cell(r.DriverName);
                            Cell(r.WheelNumber);
                            Cell(r.UnloadingDate);
                            Cell(r.Consignment);
                            Cell(Fmt(r.Seal, currency));
                            Cell(Fmt(r.Radiation, currency));
                            Cell(Fmt(r.Duty, currency));
                            Cell(Fmt(r.QualityControl, currency));
                            Cell(Fmt(r.LandTransport, currency));
                            Cell(Fmt(r.ClearanceFees, currency));
                            Cell(Fmt(r.Expenses, currency));
                            Cell(r.Notes);
                        }
                    });

                    // Summary cards
                    col.Item().PaddingTop(8).Row(row =>
                    {
                        SummaryCard(row, "إجمالي الرسوم", data.TotalDuties, currency, false);
                        SummaryCard(row, "إجمالي الفحوصات", data.TotalInspections, currency, false);
                        SummaryCard(row, "إجمالي النقل البري", data.TotalLandTransport, currency, false);
                        SummaryCard(row, "أتعاب التخليص", data.TotalClearanceFees, currency, false);
                        SummaryCard(row, "إجمالي المصاريف", data.TotalExpenses, currency, false);
                        SummaryCard(row, "إجمالي الفاتورة", data.InvoiceTotal, currency, true);
                    });

                    // Paid / remaining
                    col.Item().PaddingTop(8).Background("#FFF8E1").Border(1).BorderColor("#FFE082").Padding(8).Row(row =>
                    {
                        row.RelativeItem().Text(t =>
                        {
                            t.Span("المسدد (المدفوع): ").SemiBold().FontColor("#546E7A");
                            t.Span($"{currency} {data.PaidAmount:N0}").Bold().FontColor("#2E7D32").FontSize(11);
                        });
                        row.RelativeItem().AlignLeft().Text(t =>
                        {
                            t.Span("المتبقي بذمة العميل: ").SemiBold().FontColor("#546E7A");
                            t.Span($"{currency} {data.RemainingAmount:N0}").Bold().FontColor("#C62828").FontSize(11);
                        });
                    });

                    // Signatures
                    col.Item().PaddingTop(16).Row(row =>
                    {
                        row.RelativeItem().AlignCenter().Column(c =>
                        {
                            c.Item().Text("منظم الفاتورة / المحاسب").FontSize(9).FontColor("#546E7A").SemiBold();
                            c.Item().PaddingTop(14).Text("____________________").FontColor("#90A4AE");
                        });
                        row.RelativeItem().AlignCenter().Column(c =>
                        {
                            c.Item().Text("ختم الشركة").FontSize(9).FontColor("#64B5F6").SemiBold();
                            c.Item().PaddingTop(4).Text("◯").FontSize(22).FontColor("#64B5F6");
                        });
                        row.RelativeItem().AlignCenter().Column(c =>
                        {
                            c.Item().Text("توقيع واستلام العميل").FontSize(9).FontColor("#546E7A").SemiBold();
                            c.Item().PaddingTop(14).Text("____________________").FontColor("#90A4AE");
                        });
                    });

                    if (branding.ShowFooterText && !string.IsNullOrWhiteSpace(branding.FooterText))
                        col.Item().PaddingTop(8).AlignCenter().Text(branding.FooterText).FontSize(8).FontColor("#757575");

                    col.Item().PaddingTop(4).AlignCenter()
                        .Text("معاينة الطباعة — فاتورة الوجهة — صفحة 1 من 1")
                        .FontSize(7.5f).FontColor("#9E9E9E");
                });
            });
        }).GeneratePdf();
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

    private static void SummaryCard(RowDescriptor row, string title, decimal amount, string currency, bool highlight)
    {
        row.RelativeItem().PaddingHorizontal(2).Border(1.2f).BorderColor("#64B5F6").Padding(6).AlignCenter().Column(c =>
        {
            c.Item().Text(title).FontSize(7.5f).SemiBold().FontColor("#546E7A");
            c.Item().PaddingTop(3).Text($"{currency} {amount:N0}")
                .FontSize(10).Bold().FontColor(highlight ? "#EF6C00" : "#212121");
        });
    }

    private static IContainer HeaderCell(IContainer c) =>
        c.Background("#15254C").Border(0.4f).BorderColor(Colors.White).Padding(3);

    private static IContainer UnitCell(IContainer c) =>
        c.Background("#E3F2FD").Border(0.4f).BorderColor("#90A4AE").Padding(2);

    private static IContainer BodyCell(IContainer c) =>
        c.Border(0.4f).BorderColor("#90A4AE").PaddingVertical(3).PaddingHorizontal(2);

    private static string Fmt(decimal? amount, string currency) =>
        amount is null or 0 ? "—" : $"{currency} {amount.Value:N0}";
}
