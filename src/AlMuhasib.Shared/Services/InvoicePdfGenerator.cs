using AlMuhasib.Core;
using AlMuhasib.Core.Interfaces.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AlMuhasib.Shared.Services;

internal static class InvoicePdfGenerator
{
    private static bool _licenseSet;

    private static void EnsureLicense()
    {
        if (_licenseSet) return;
        QuestPDF.Settings.License = LicenseType.Community;
        _licenseSet = true;
    }

    public static byte[] GenerateInvoice(InvoicePrintModel m)
    {
        EnsureLicense();
        var branding = PrintBrandingProvider.Current;
        var currency = string.IsNullOrWhiteSpace(m.CurrencyLabel) ? "د.ع" : m.CurrencyLabel;
        var hideAmounts = m.HideAmounts;
        var columnCount = hideAmounts ? 2 : 4;
        var brandedHeader = branding.HasHeaderContent;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(32);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial").FontColor(InkColor));
                page.ContentFromRightToLeft();

                page.Content().Column(col =>
                {
                    if (branding.ShowHeaderText && !string.IsNullOrWhiteSpace(branding.CompanyName))
                    {
                        col.Item().AlignCenter().Text(branding.CompanyName).FontSize(13).Bold();
                        if (!string.IsNullOrWhiteSpace(branding.Details))
                            col.Item().AlignCenter().Text(branding.Details).FontSize(9).FontColor(MutedColor);
                        col.Item().PaddingVertical(6).LineHorizontal(0.8f).LineColor(Colors.Grey.Lighten1);
                    }

                    col.Item().PaddingTop(4).AlignCenter().Text(m.Title).FontSize(18).Bold();

                    // سطر بيانات الشركة يظهر فقط بدون ترويسة مطبوعة، لتفادي تكرار نفس البيانات.
                    var companyParts = brandedHeader
                        ? []
                        : new[] { branding.CompanyName, branding.Address, branding.PhonePrimary }
                            .Where(part => !string.IsNullOrWhiteSpace(part))
                            .ToArray();
                    if (companyParts.Length > 0)
                    {
                        col.Item().PaddingTop(10).AlignCenter()
                            .Text(string.Join("  |  ", companyParts)).FontSize(10).SemiBold();
                    }

                    // ── جدول بيانات الفاتورة ──
                    col.Item().PaddingTop(14).Table(meta =>
                    {
                        meta.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(160);
                            c.RelativeColumn();
                        });

                        void MetaRow(string label, string value)
                        {
                            meta.Cell().Element(GridCell).Text($"{label}:").SemiBold();
                            meta.Cell().Element(GridCell).Text(value);
                        }

                        MetaRow("رقم الفاتورة", m.InvoiceNumber);
                        MetaRow("تاريخ الفاتورة", m.Date.ToString("yyyy/MM/dd"));
                        if (m.CreditDueDate.HasValue)
                            MetaRow("تاريخ الاستحقاق", m.CreditDueDate.Value.ToString("yyyy/MM/dd"));
                        if (!hideAmounts && !string.IsNullOrWhiteSpace(m.PaymentMethod))
                            MetaRow("طريقة الدفع", m.PaymentMethod);
                        if (!string.IsNullOrWhiteSpace(m.WarehouseName))
                            MetaRow("المخزن", m.WarehouseName);
                        if (!string.IsNullOrWhiteSpace(m.FileNumber))
                            MetaRow("رقم الملف", m.FileNumber!);
                        if (!string.IsNullOrWhiteSpace(m.DriverName))
                            MetaRow("السائق", m.DriverName!);
                    });

                    // ── بطاقة العميل يميناً، وبطاقة المندوب يساراً فقط عند وجود مندوب ──
                    var hasSalesRepresentative =
                        !string.IsNullOrWhiteSpace(m.SalesRepresentativeName)
                        || !string.IsNullOrWhiteSpace(m.SalesRepresentativePhone)
                        || !string.IsNullOrWhiteSpace(m.SalesRepresentativeEmail);

                    col.Item().PaddingTop(14).Row(row =>
                    {
                        var customerCard = row.RelativeItem();
                        if (!hasSalesRepresentative)
                            customerCard = row.ConstantItem(240);

                        customerCard.Element(card => DetailsCard(
                            card,
                            $"بيانات {m.PartyLabel}",
                            [
                                ("الاسم", string.IsNullOrWhiteSpace(m.PartyName) ? "—" : m.PartyName),
                                ("الهاتف", string.IsNullOrWhiteSpace(m.PartyPhone) ? "—" : m.PartyPhone!),
                                ("العنوان", string.IsNullOrWhiteSpace(m.PartyAddress) ? "—" : m.PartyAddress!),
                                ("البريد الإلكتروني", string.IsNullOrWhiteSpace(m.PartyEmail) ? "—" : m.PartyEmail!)
                            ]));

                        if (hasSalesRepresentative)
                        {
                            row.ConstantItem(10);
                            row.RelativeItem().Element(card => DetailsCard(
                                card,
                                "مندوب المبيعات",
                                [
                                    ("الاسم", string.IsNullOrWhiteSpace(m.SalesRepresentativeName) ? "—" : m.SalesRepresentativeName!),
                                    ("الهاتف", string.IsNullOrWhiteSpace(m.SalesRepresentativePhone) ? "—" : m.SalesRepresentativePhone!),
                                    ("البريد الإلكتروني", string.IsNullOrWhiteSpace(m.SalesRepresentativeEmail) ? "—" : m.SalesRepresentativeEmail!),
                                    ("", "")
                                ]));
                        }
                        else
                        {
                            row.RelativeItem();
                        }
                    });

                    // ── جدول البنود والمجاميع ──
                    col.Item().PaddingTop(14).Text(hideAmounts ? "تفاصيل المواد" : "المبالغ الإجمالية")
                        .FontSize(12).Bold();

                    col.Item().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            if (hideAmounts)
                            {
                                c.ConstantColumn(90);
                            }
                            else
                            {
                                c.ConstantColumn(60);
                                c.ConstantColumn(95);
                                c.ConstantColumn(105);
                            }
                        });

                        table.Header(h =>
                        {
                            h.Cell().Element(GridHeaderCell).Text("الوصف");
                            h.Cell().Element(GridHeaderCell).AlignCenter().Text("الكمية");
                            if (!hideAmounts)
                            {
                                h.Cell().Element(GridHeaderCell).AlignCenter().Text($"سعر الوحدة ({currency})");
                                h.Cell().Element(GridHeaderCell).AlignCenter().Text($"الإجمالي ({currency})");
                            }
                        });

                        foreach (var item in m.Items)
                        {
                            table.Cell().Element(GridCell).Column(desc =>
                            {
                                desc.Item().Text($"{item.Number}. {item.ItemName}");
                                if (m.PharmacyUsageReceipt && !string.IsNullOrWhiteSpace(item.UsageInstructions))
                                {
                                    desc.Item().Text($"طريقة الاستخدام: {item.UsageInstructions}")
                                        .FontSize(8.5f).FontColor(MutedColor);
                                }
                            });
                            table.Cell().Element(GridCell).AlignCenter().Text(FormatNumber(item.Quantity));
                            if (!hideAmounts)
                            {
                                table.Cell().Element(GridCell).AlignCenter().Text(FormatNumber(item.UnitPrice));
                                table.Cell().Element(GridCell).AlignCenter().Text(FormatNumber(item.TotalPrice));
                            }
                        }

                        if (!hideAmounts)
                        {
                            void TotalRow(string label, decimal value, bool emphasize = false)
                            {
                                var style = emphasize ? TotalStrongCell : (Func<IContainer, IContainer>)TotalCell;
                                table.Cell().ColumnSpan((uint)(columnCount - 1)).Element(style)
                                    .Text(label).Bold().FontSize(emphasize ? 11 : 10);
                                table.Cell().Element(style).AlignCenter()
                                    .Text(FormatNumber(value)).Bold().FontSize(emphasize ? 11 : 10);
                            }

                            TotalRow("المجموع الفرعي", m.Subtotal);
                            if (m.DiscountAmount != 0)
                            {
                                TotalRow("الخصم", m.DiscountAmount);
                                TotalRow("المبلغ بعد الخصم", m.Subtotal - m.DiscountAmount);
                            }
                            if (m.TransportFeeAmount != 0)
                                TotalRow("أجور النقل", m.TransportFeeAmount);
                            if (m.TaxRate != 0 || m.TaxAmount != 0)
                                TotalRow(m.TaxRate != 0 ? $"الضريبة {m.TaxRate:0.##}%" : "الضريبة", m.TaxAmount);
                            if (m.CompanyFeeAmount is { } fee && fee != 0)
                                TotalRow("نسبة الشركة", fee);
                            if (m.RoundingAmount != 0)
                                TotalRow("التقريب", m.RoundingAmount);

                            TotalRow("الإجمالي المستحق", m.GrandTotal, emphasize: true);

                            if (m.PaidAmount != 0 || m.RemainingAmount != 0)
                            {
                                TotalRow("المدفوع", m.PaidAmount);
                                TotalRow("المتبقي", m.RemainingAmount);
                            }
                        }
                    });

                    if (!hideAmounts && m.NumberOfInstallments.HasValue)
                    {
                        col.Item().PaddingTop(10).Text(
                            $"عدد الأقساط: {m.NumberOfInstallments} — مبلغ القسط: {m.InstallmentAmount:N0} {currency}");
                    }

                    if (!hideAmounts && m.Schedule is { Count: > 0 })
                    {
                        col.Item().PaddingTop(12).Text("جدول الأقساط").FontSize(12).Bold();
                        col.Item().PaddingTop(6).Table(st =>
                        {
                            st.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(50);
                                c.RelativeColumn();
                                c.ConstantColumn(110);
                            });
                            st.Header(h =>
                            {
                                h.Cell().Element(GridHeaderCell).AlignCenter().Text("#");
                                h.Cell().Element(GridHeaderCell).AlignCenter().Text("تاريخ الاستحقاق");
                                h.Cell().Element(GridHeaderCell).AlignCenter().Text("المبلغ");
                            });
                            foreach (var s in m.Schedule)
                            {
                                st.Cell().Element(GridCell).AlignCenter().Text(s.Number.ToString());
                                st.Cell().Element(GridCell).AlignCenter().Text(s.DueDate.ToString("yyyy/MM/dd"));
                                st.Cell().Element(GridCell).AlignCenter().Text($"{FormatNumber(s.Amount)} {currency}");
                            }
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(m.Notes))
                    {
                        col.Item().PaddingTop(10).Text(t =>
                        {
                            t.Span("ملاحظات: ").Bold();
                            t.Span(m.Notes);
                        });
                    }

                    col.Item().PaddingTop(24).Row(row =>
                    {
                        row.RelativeItem().AlignCenter().Text("توقيع المستلم: _______________").FontColor(MutedColor);
                        row.RelativeItem().AlignCenter().Text("توقيع البائع: _______________").FontColor(MutedColor);
                    });
                });

                page.Footer().Column(footer =>
                {
                    if (branding.ShowFooterText && !string.IsNullOrWhiteSpace(branding.FooterText))
                        footer.Item().AlignCenter().Text(branding.FooterText).FontSize(8).FontColor(Colors.Grey.Darken1);
                    footer.Item().AlignCenter().Text(t =>
                    {
                        t.Span("طُبع بتاريخ: ");
                        t.Span(DateTime.Now.ToString("yyyy/MM/dd HH:mm")).SemiBold();
                    });
                });
            });
        }).GeneratePdf();
    }

    public static byte[] GeneratePaymentReceipt(InstallmentPaymentReceiptPrintModel m)
    {
        EnsureLicense();
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));
                page.ContentFromRightToLeft();

                page.Header().Column(col =>
                {
                    col.Item().Background(Colors.Green.Darken2).Padding(10)
                        .Text("إيصال تسديد قسط").FontSize(18).Bold().FontColor(Colors.White).AlignCenter();
                    col.Item().PaddingTop(8).Text($"العميل: {m.CustomerName}");
                    if (!string.IsNullOrWhiteSpace(m.FileNumber))
                        col.Item().Text($"رقم الملف: {m.FileNumber}");
                    col.Item().Text($"فاتورة الأقساط: {m.InvoiceNumber}");
                    col.Item().Text($"تاريخ التسديد: {m.PaymentDate:yyyy/MM/dd HH:mm}");
                    if (!string.IsNullOrWhiteSpace(m.CashBoxName))
                        col.Item().Text($"الصندوق: {m.CashBoxName}");
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(35);
                            c.RelativeColumn();
                            c.ConstantColumn(70);
                            c.ConstantColumn(70);
                            c.ConstantColumn(70);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Element(HeaderCell).Text("#");
                            h.Cell().Element(HeaderCell).Text("استحقاق");
                            h.Cell().Element(HeaderCell).Text("المسدّد");
                            h.Cell().Element(HeaderCell).Text("المتبقي");
                            h.Cell().Element(HeaderCell).Text("الحالة");
                        });

                        foreach (var line in m.Lines)
                        {
                            table.Cell().Element(BodyCell).Text(line.SequenceNumber.ToString());
                            table.Cell().Element(BodyCell).Text(line.DueDate.ToString("yyyy/MM/dd"));
                            table.Cell().Element(BodyCell).Text(line.PaidAmount.ToString("N0"));
                            table.Cell().Element(BodyCell).Text(line.RemainingAfter.ToString("N0"));
                            table.Cell().Element(BodyCell).Text(line.StatusText);
                        }
                    });

                    col.Item().PaddingTop(12).Text($"إجمالي المسدّد: {m.TotalPaid:N0} د.ع").Bold().FontSize(12);
                    if (m.PlanRemainingTotal.HasValue)
                        col.Item().Text($"المتبقي على الخطة: {m.PlanRemainingTotal:N0} د.ع");

                    if (!string.IsNullOrWhiteSpace(m.Notes))
                        col.Item().PaddingTop(8).Text($"ملاحظات: {m.Notes}");
                });

                page.Footer().AlignCenter().Text("شكراً لتسديدكم — المحاسب");
            });
        }).GeneratePdf();
    }

    private static void DetailsCard(
        IContainer container,
        string title,
        IReadOnlyList<(string Label, string Value)> rows)
    {
        container.Border(0.7f).BorderColor(GridColor).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(92);
                columns.RelativeColumn();
            });

            table.Cell().ColumnSpan(2)
                .Background(GridHeaderColor)
                .BorderBottom(0.7f).BorderColor(GridColor)
                .PaddingVertical(6).PaddingHorizontal(7)
                .AlignCenter()
                .Text(title).FontSize(11).SemiBold().FontColor(MutedColor);

            foreach (var (label, value) in rows)
            {
                table.Cell()
                    .Background(GridHeaderColor)
                    .BorderLeft(0.7f).BorderBottom(0.7f).BorderColor(GridColor)
                    .PaddingVertical(5).PaddingHorizontal(7)
                    .Text(string.IsNullOrWhiteSpace(label) ? " " : $"{label}:")
                    .SemiBold();
                table.Cell()
                    .BorderBottom(0.7f).BorderColor(GridColor)
                    .PaddingVertical(5).PaddingHorizontal(7)
                    .Text(string.IsNullOrWhiteSpace(value) ? " " : value);
            }
        });
    }

    private static string FormatNumber(decimal value) =>
        value == decimal.Truncate(value) ? value.ToString("N0") : value.ToString("N2");

    private const string InkColor = "#1F2428";
    private const string MutedColor = "#6B7280";
    private const string GridColor = "#BDC3C7";
    private const string GridHeaderColor = "#F3F4F6";
    private const string TotalRowColor = "#FAFAFA";

    private static IContainer GridCell(IContainer c) =>
        c.Border(0.7f).BorderColor(GridColor).PaddingVertical(5).PaddingHorizontal(7);

    private static IContainer GridHeaderCell(IContainer c) =>
        c.Background(GridHeaderColor)
            .Border(0.7f).BorderColor(GridColor)
            .PaddingVertical(5).PaddingHorizontal(7)
            .DefaultTextStyle(x => x.SemiBold().FontColor(MutedColor));

    private static IContainer TotalCell(IContainer c) =>
        c.Background(TotalRowColor).Border(0.7f).BorderColor(GridColor).PaddingVertical(5).PaddingHorizontal(7);

    private static IContainer TotalStrongCell(IContainer c) =>
        c.Background(GridHeaderColor).Border(0.7f).BorderColor(GridColor).PaddingVertical(6).PaddingHorizontal(7);

    private static IContainer HeaderCell(IContainer c) =>
        c.DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White))
            .Background(Colors.Blue.Darken2)
            .Padding(4)
            .Border(0.5f)
            .BorderColor(Colors.Blue.Darken3);

    private static IContainer BodyCell(IContainer c) =>
        c.Padding(4).BorderBottom(0.5f).BorderColor(Colors.Grey.Medium);
}
