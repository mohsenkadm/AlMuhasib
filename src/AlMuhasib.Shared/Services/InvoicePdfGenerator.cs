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
                    col.Item().Background(Colors.Blue.Darken2).Padding(10)
                        .Text(m.Title).FontSize(18).Bold().FontColor(Colors.White).AlignCenter();
                    col.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Text($"رقم الفاتورة: {m.InvoiceNumber}");
                        row.RelativeItem().AlignRight().Text($"التاريخ: {m.Date:yyyy/MM/dd}");
                    });
                    col.Item().PaddingTop(4).Text($"{m.PartyLabel}: {m.PartyName}");
                    if (!string.IsNullOrWhiteSpace(m.WarehouseName))
                        col.Item().Text($"المخزن: {m.WarehouseName}");
                    col.Item().Text($"طريقة الدفع: {m.PaymentMethod}");
                    if (m.CreditDueDate.HasValue)
                        col.Item().Text($"تاريخ الاستحقاق: {m.CreditDueDate:yyyy/MM/dd}");
                    if (!string.IsNullOrWhiteSpace(m.FileNumber))
                        col.Item().Text($"رقم الملف: {m.FileNumber}");
                    if (!string.IsNullOrWhiteSpace(m.Notes))
                        col.Item().Text($"ملاحظات: {m.Notes}");
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.ConstantColumn(50);
                            c.ConstantColumn(55);
                            c.ConstantColumn(65);
                            c.ConstantColumn(30);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Element(HeaderCell).Text("البيان");
                            h.Cell().Element(HeaderCell).Text("الكمية");
                            h.Cell().Element(HeaderCell).Text("السعر");
                            h.Cell().Element(HeaderCell).Text("الإجمالي");
                            h.Cell().Element(HeaderCell).Text("#");
                        });

                        foreach (var item in m.Items)
                        {
                            table.Cell().Element(BodyCell).Text(item.ItemName);
                            table.Cell().Element(BodyCell).Text(item.Quantity.ToString("N0"));
                            table.Cell().Element(BodyCell).Text(item.UnitPrice.ToString("N0"));
                            table.Cell().Element(BodyCell).Text(item.TotalPrice.ToString("N0"));
                            table.Cell().Element(BodyCell).Text(item.Number.ToString());
                        }
                    });

                    if (m.NumberOfInstallments.HasValue)
                    {
                        col.Item().PaddingTop(8).Text(
                            $"عدد الأقساط: {m.NumberOfInstallments} | مبلغ القسط: {m.InstallmentAmount:N0} د.ع");
                        if (m.CompanyFeeAmount > 0)
                            col.Item().Text($"نسبة الشركة: {m.CompanyFeeAmount:N0} د.ع");
                    }

                    if (m.Schedule is { Count: > 0 })
                    {
                        col.Item().PaddingTop(8).Text("جدول الأقساط").Bold();
                        col.Item().Table(st =>
                        {
                            st.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(40);
                                c.RelativeColumn();
                                c.ConstantColumn(80);
                            });
                            st.Header(h =>
                            {
                                h.Cell().Element(HeaderCell).Text("#");
                                h.Cell().Element(HeaderCell).Text("الاستحقاق");
                                h.Cell().Element(HeaderCell).Text("المبلغ");
                            });
                            foreach (var s in m.Schedule)
                            {
                                st.Cell().Element(BodyCell).Text(s.Number.ToString());
                                st.Cell().Element(BodyCell).Text(s.DueDate.ToString("yyyy/MM/dd"));
                                st.Cell().Element(BodyCell).Text(s.Amount.ToString("N0"));
                            }
                        });
                    }

                    col.Item().PaddingTop(12).AlignLeft().Column(totals =>
                    {
                        totals.Item().Text($"المجموع: {m.Subtotal:N0} د.ع");
                        if (m.RoundingAmount != 0)
                            totals.Item().Text($"التقريب: {m.RoundingAmount:N0} د.ع");
                        if (m.TransportFeeAmount > 0)
                            totals.Item().Text($"أجور النقل: {m.TransportFeeAmount:N0} د.ع");
                        totals.Item().Text($"الإجمالي الكلي: {m.GrandTotal:N0} د.ع").Bold().FontSize(12);
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("طُبع بتاريخ: ");
                    t.Span(DateTime.Now.ToString("yyyy/MM/dd HH:mm")).SemiBold();
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

    private static IContainer HeaderCell(IContainer c) =>
        c.DefaultTextStyle(x => x.SemiBold()).Padding(4).Background(Colors.Grey.Lighten3).Border(0.5f).BorderColor(Colors.Grey.Medium);

    private static IContainer BodyCell(IContainer c) =>
        c.Padding(4).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2);
}
