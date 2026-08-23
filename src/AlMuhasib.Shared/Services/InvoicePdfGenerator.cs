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

                    col.Item().PaddingTop(10).Text(t =>
                    {
                        t.Span("تاريخ الفاتورة  ").SemiBold().FontColor(MutedColor);
                        t.Span(m.Date.ToString("yyyy/MM/dd")).SemiBold();
                        if (m.CreditDueDate.HasValue)
                        {
                            t.Span("    تاريخ الاستحقاق  ").SemiBold().FontColor(MutedColor);
                            t.Span(m.CreditDueDate.Value.ToString("yyyy/MM/dd")).SemiBold();
                        }
                    });

                    col.Item().PaddingTop(10).Row(row =>
                    {
                        var customerRows = new List<(string Label, string Value)>();
                        AddInfoRow(customerRows, "الاسم", m.PartyName);
                        AddInfoRow(customerRows, "الهاتف", m.PartyPhone);
                        AddInfoRow(customerRows, "العنوان", m.PartyAddress);
                        AddInfoRow(customerRows, "رقم الملف", m.FileNumber);
                        if (customerRows.Count == 0)
                            customerRows.Add(("الاسم", "—"));

                        var invoiceRows = new List<(string Label, string Value)>();
                        AddInfoRow(invoiceRows, "رقم الفاتورة", m.InvoiceNumber);
                        AddInfoRow(invoiceRows, "الاسم", m.SalesRepresentativeName);
                        AddInfoRow(invoiceRows, "الهاتف", m.SalesRepresentativePhone);
                        AddInfoRow(invoiceRows, "السائق", m.DriverName);
                        if (invoiceRows.Count == 0)
                            invoiceRows.Add(("رقم الفاتورة", string.IsNullOrWhiteSpace(m.InvoiceNumber) ? "—" : m.InvoiceNumber));

                        row.RelativeItem().Element(card => DetailsCard(card, customerRows));
                        row.ConstantItem(10);
                        row.RelativeItem().Element(card => DetailsCard(card, invoiceRows));
                    });

                    // ── جدول البنود والمجاميع ──
                    col.Item().PaddingTop(14).Text(hideAmounts ? "تفاصيل المواد" : "المبالغ الإجمالية")
                        .FontSize(12).Bold();

                    var layout = InvoicePrintLayoutHelper.Resolve(m, compact: m.Items.Count > 18);

                    col.Item().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            if (layout.HideAmounts)
                            {
                                c.ConstantColumn(90);
                                return;
                            }

                            c.ConstantColumn(60);
                            if (layout.ShowWarehouse)
                                c.ConstantColumn(72);
                            c.ConstantColumn(88);
                            if (layout.ShowLineDiscount)
                                c.ConstantColumn(48);
                            c.ConstantColumn(98);
                        });

                        table.Header(h =>
                        {
                            foreach (var title in InvoicePrintLayoutHelper.BuildColumnTitles(layout, currency))
                            {
                                var cell = h.Cell().Element(GridHeaderCell);
                                if (title != "الوصف")
                                    cell.AlignCenter();
                                cell.Text(title);
                            }
                        });

                        foreach (var item in m.Items)
                        {
                            table.Cell().Element(GridCell).Text(
                                InvoicePrintLayoutHelper.FormatItemName(item, m, layout.HideAmounts && layout.ShowWarehouse));
                            table.Cell().Element(GridCell).AlignCenter().Text(FormatNumber(item.Quantity));
                            if (!layout.HideAmounts)
                            {
                                if (layout.ShowWarehouse)
                                    table.Cell().Element(GridCell).AlignCenter().Text(item.WarehouseName ?? "—");
                                table.Cell().Element(GridCell).AlignCenter().Text(FormatNumber(item.UnitPrice));
                                if (layout.ShowLineDiscount)
                                    table.Cell().Element(GridCell).AlignCenter().Text(
                                        InvoicePrintLayoutHelper.FormatDiscountPercent(item.DiscountPercent));
                                table.Cell().Element(GridCell).AlignCenter().Text(FormatNumber(item.TotalPrice));
                            }
                        }
                    });

                    if (!hideAmounts)
                    {
                        var amountEntries = new List<(string Label, string Value, bool Emphasize)>();
                        if (!string.IsNullOrWhiteSpace(m.PaymentMethod))
                            amountEntries.Add(("طريقة الدفع", m.PaymentMethod, false));
                        amountEntries.Add(("المجموع الفرعي", FormatNumber(m.Subtotal), false));
                        if (m.DiscountAmount != 0)
                        {
                            amountEntries.Add(("الخصم", FormatNumber(m.DiscountAmount), false));
                            amountEntries.Add(("المبلغ بعد الخصم", FormatNumber(m.Subtotal - m.DiscountAmount), false));
                        }
                        if (m.TransportFeeAmount != 0)
                            amountEntries.Add(("أجور النقل", FormatNumber(m.TransportFeeAmount), false));
                        if (m.TaxRate != 0 || m.TaxAmount != 0)
                            amountEntries.Add((m.TaxRate != 0 ? $"الضريبة {m.TaxRate:0.##}%" : "الضريبة", FormatNumber(m.TaxAmount), false));
                        if (m.CompanyFeeAmount is { } fee && fee != 0)
                            amountEntries.Add(("نسبة الشركة", FormatNumber(fee), false));
                        if (m.RoundingAmount != 0)
                            amountEntries.Add(("التقريب", FormatNumber(m.RoundingAmount), false));
                        amountEntries.Add(("الإجمالي المستحق", FormatNumber(m.GrandTotal), true));
                        if (m.PaidAmount != 0 || m.RemainingAmount != 0)
                        {
                            amountEntries.Add(("المدفوع", FormatNumber(m.PaidAmount), false));
                            amountEntries.Add(("المتبقي", FormatNumber(m.RemainingAmount), false));
                        }

                        col.Item().PaddingTop(8).Row(totalsRow =>
                        {
                            totalsRow.RelativeItem();
                            totalsRow.ConstantItem(320).Table(totals =>
                            {
                                totals.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(1.3f);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1.3f);
                                    c.RelativeColumn(1);
                                });

                                for (var i = 0; i < amountEntries.Count; i += 2)
                                {
                                    PdfAmountPair(totals, amountEntries[i]);
                                    if (i + 1 < amountEntries.Count)
                                        PdfAmountPair(totals, amountEntries[i + 1]);
                                    else
                                    {
                                        totals.Cell().Element(TotalCell).Text(" ");
                                        totals.Cell().Element(TotalCell).Text(" ");
                                    }
                                }
                            });
                        });
                    }

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
        IReadOnlyList<(string Label, string Value)> rows)
    {
        container.Border(0.7f).BorderColor(GridColor).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(92);
                columns.RelativeColumn();
            });

            foreach (var (label, value) in rows)
            {
                table.Cell()
                    .Background(GridHeaderColor)
                    .Border(0.7f).BorderColor(GridColor)
                    .PaddingVertical(5).PaddingHorizontal(7)
                    .Text(string.IsNullOrWhiteSpace(label) ? " " : $"{label}:")
                    .SemiBold();
                table.Cell()
                    .Border(0.7f).BorderColor(GridColor)
                    .PaddingVertical(5).PaddingHorizontal(7)
                    .Text(string.IsNullOrWhiteSpace(value) ? " " : value);
            }
        });
    }

    private static void AddInfoRow(List<(string Label, string Value)> rows, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            rows.Add((label, value));
    }

    private static void PdfAmountPair(TableDescriptor table, (string Label, string Value, bool Emphasize) entry)
    {
        var style = entry.Emphasize ? TotalStrongCell : (Func<IContainer, IContainer>)TotalCell;
        table.Cell().Element(style).Text(entry.Label).Bold().FontSize(entry.Emphasize ? 9.5f : 8.5f);
        table.Cell().Element(style).AlignCenter().Text(entry.Value).Bold().FontSize(entry.Emphasize ? 9.5f : 8.5f);
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
