using System.Globalization;
using Qaid.TelegramBot.Application.Abstractions;
using Qaid.TelegramBot.Application.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Qaid.TelegramBot.Infrastructure.Reports;

public sealed class QuestReportPdfGenerator : IReportPdfGenerator
{
    private static bool _licenseSet;

    public QuestReportPdfGenerator()
    {
        if (!_licenseSet)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            _licenseSet = true;
        }
    }

    public byte[] Generate(ReportDocumentModel model)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));
                page.ContentFromRightToLeft();

                page.Header().Column(col =>
                {
                    col.Item().Background(Colors.Teal.Darken2).Padding(12).Column(h =>
                    {
                        h.Item().Text("نظام قيد المحاسبي").FontSize(11).FontColor(Colors.White).AlignCenter();
                        h.Item().PaddingTop(2).Text(model.Title).FontSize(16).Bold().FontColor(Colors.White).AlignCenter();
                    });

                    col.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Text(text =>
                        {
                            text.Span("الشركة: ").SemiBold();
                            text.Span(string.IsNullOrWhiteSpace(model.CompanyName) ? "—" : model.CompanyName);
                        });
                        row.RelativeItem().AlignLeft().Text(text =>
                        {
                            text.Span("تاريخ الطباعة: ").SemiBold();
                            text.Span(model.GeneratedAtLabel);
                        });
                    });

                    if (!string.IsNullOrWhiteSpace(model.PeriodLabel))
                    {
                        col.Item().PaddingTop(2).Text(text =>
                        {
                            text.Span("الفترة: ").SemiBold();
                            text.Span(model.PeriodLabel);
                        });
                    }

                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().PaddingTop(10).Column(col =>
                {
                    if (model.Summary.Count > 0)
                    {
                        col.Item().Text("الملخص").FontSize(12).Bold().FontColor(Colors.Teal.Darken2);
                        col.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                            });

                            var i = 0;
                            foreach (var item in model.Summary)
                            {
                                var bg = i % 2 == 0 ? Colors.Grey.Lighten4 : Colors.White;
                                table.Cell().Background(bg).Border(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(5).Text(item.Label).SemiBold();
                                table.Cell().Background(bg).Border(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(5).AlignLeft().Text(item.Value);
                                i++;
                            }
                        });
                    }

                    foreach (var t in model.Tables)
                    {
                        col.Item().PaddingTop(14).Text(
                            string.IsNullOrWhiteSpace(t.Title) ? "التفاصيل" : t.Title)
                            .FontSize(12).Bold().FontColor(Colors.Teal.Darken2);

                        if (t.Headers.Count == 0 || t.Rows.Count == 0)
                        {
                            col.Item().PaddingTop(4).Text("لا توجد صفوف تفصيلية.").FontColor(Colors.Grey.Darken1);
                            continue;
                        }

                        col.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                foreach (var _ in t.Headers)
                                    c.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                foreach (var h in t.Headers)
                                {
                                    header.Cell().Background(Colors.Teal.Darken1).Padding(4)
                                        .Text(h).FontColor(Colors.White).FontSize(8).SemiBold();
                                }
                            });

                            var rowIndex = 0;
                            foreach (var row in t.Rows)
                            {
                                var bg = rowIndex % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                                for (var c = 0; c < t.Headers.Count; c++)
                                {
                                    var cell = c < row.Count ? row[c] : "";
                                    table.Cell().Background(bg).BorderBottom(0.4f).BorderColor(Colors.Grey.Lighten2)
                                        .Padding(3).Text(cell).FontSize(8);
                                }
                                rowIndex++;
                            }
                        });

                        if (t.Rows.Count >= 250)
                        {
                            col.Item().PaddingTop(4)
                                .Text("تم اختصار الجدول إلى أول 250 صفاً.")
                                .FontSize(8).FontColor(Colors.Grey.Darken1);
                        }
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken1));
                    text.Span("قيد — تقارير تيليجرام | صفحة ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                    text.Span($" | {DateTime.UtcNow.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture)} UTC");
                });
            });
        }).GeneratePdf();
    }
}
