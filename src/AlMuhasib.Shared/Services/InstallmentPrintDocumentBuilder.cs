using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.Shared.Services;

public static class InstallmentPrintDocumentBuilder
{
    private const double A4Width = 793.7;
    private const double A4Height = 1122.5;

    public static FlowDocument BuildPlanDetailDocument(InstallmentPlanDetailPrintModel model, string? sectionTitle = null)
    {
        var doc = CreateBaseDocument();
        AppendSectionHeader(doc, sectionTitle ?? "كشف الأقساط التفصيلي");
        AppendPlanMeta(doc, model);
        AppendScheduleTable(doc, model.Schedule);
        AppendPlanStatistics(doc, model.Schedule, model.TotalAmount);
        return doc;
    }

    public static FlowDocument BuildMultiPlanDocument(IReadOnlyList<InstallmentPlanDetailPrintModel> plans, string title)
    {
        var doc = CreateBaseDocument();
        AppendSectionHeader(doc, title);

        if (plans.Count > 0)
        {
            doc.Blocks.Add(new Paragraph(new Run($"العميل: {plans[0].CustomerName}"))
            {
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8)
            });
        }

        decimal grandTotal = 0, grandPaid = 0, grandRemaining = 0;
        var grandPaidCount = 0;

        for (var i = 0; i < plans.Count; i++)
        {
            var plan = plans[i];
            if (i > 0)
                doc.Blocks.Add(new Paragraph(new Run(" ")) { Margin = new Thickness(0, 10, 0, 0) });

            doc.Blocks.Add(new Paragraph(new Run($"فاتورة {plan.InvoiceNumber}"))
            {
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x15, 0x65, 0xC0)),
                Margin = new Thickness(0, 4, 0, 4)
            });
            AppendPlanMeta(doc, plan, compact: true);
            AppendScheduleTable(doc, plan.Schedule);
            AppendPlanStatistics(doc, plan.Schedule, plan.TotalAmount);

            grandTotal += plan.TotalAmount;
            grandPaid += plan.Schedule.Sum(s => s.PaidAmount);
            grandRemaining += plan.Schedule.Sum(s => s.RemainingAmount);
            grandPaidCount += plan.Schedule.Count(s => s.StatusText is "مسدد" or "مسدد جزئياً" && s.PaidAmount > 0);
        }

        AppendGrandStatistics(doc, plans.Count, grandTotal, grandPaid, grandRemaining, grandPaidCount);
        return doc;
    }

    public static FlowDocument BuildPlansSummaryDocument(InstallmentPlansSummaryPrintModel model)
    {
        var doc = CreateBaseDocument();
        AppendSectionHeader(doc, model.Title);

        doc.Blocks.Add(new Paragraph(new Run($"التاريخ: {DateTime.Now:yyyy/MM/dd}"))
        {
            FontSize = 11,
            Foreground = Brushes.Gray,
            Margin = new Thickness(0, 0, 0, 10)
        });

        var table = new Table { CellSpacing = 0 };
        foreach (var _ in model.Columns)
            table.Columns.Add(new TableColumn());

        var headerGroup = new TableRowGroup();
        var headerRow = new TableRow { Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x23, 0x7E)) };
        foreach (var col in model.Columns)
        {
            headerRow.Cells.Add(CreateCell(col, bold: true, foreground: Brushes.White, padding: new Thickness(6, 4, 6, 4)));
        }
        headerGroup.Rows.Add(headerRow);
        table.RowGroups.Add(headerGroup);

        var dataGroup = new TableRowGroup();
        var alternate = false;
        foreach (var rowData in model.Rows)
        {
            var row = new TableRow();
            if (alternate)
                row.Background = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5));
            alternate = !alternate;

            for (var col = 0; col < model.Columns.Length; col++)
            {
                var val = col < rowData.Length ? rowData[col]?.ToString() ?? "" : "";
                row.Cells.Add(CreateCell(val, padding: new Thickness(5, 3, 5, 3)));
            }
            dataGroup.Rows.Add(row);
        }
        table.RowGroups.Add(dataGroup);
        doc.Blocks.Add(table);

        AppendStatisticsCards(doc,
            ("عدد الخطط", model.PlanCount.ToString("N0")),
            ("إجمالي المبالغ", $"{model.TotalAmount:N0} د.ع"),
            ("المسدد", $"{model.PaidAmount:N0} د.ع"),
            ("المتبقي", $"{model.RemainingAmount:N0} د.ع"),
            ("نسبة التحصيل", model.TotalAmount > 0 ? $"{model.PaidAmount * 100m / model.TotalAmount:N1}%" : "0%"),
            ("أقساط مسددة", model.PaidInstallmentCount.ToString("N0")));

        return doc;
    }

    public static void PrintDocument(FlowDocument doc, string previewTitle) =>
        DocumentPrintHelper.PrintWithPreview(doc, previewTitle, new Size(A4Width, A4Height));

    private static FlowDocument CreateBaseDocument()
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
            FontSize = 11,
            FlowDirection = FlowDirection.RightToLeft,
            PageWidth = A4Width,
            PageHeight = A4Height,
            PagePadding = new Thickness(36, 28, 36, 28),
            ColumnWidth = A4Width
        };
        PrintBrandingFlowDocumentHelper.PrependBrandingHeader(doc);
        return doc;
    }

    private static void AppendSectionHeader(FlowDocument doc, string title)
    {
        doc.Blocks.Add(new Paragraph(new Run(title))
        {
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 12)
        });
    }

    private static void AppendPlanMeta(FlowDocument doc, InstallmentPlanDetailPrintModel model, bool compact = false)
    {
        var filePart = string.IsNullOrWhiteSpace(model.FileNumber) ? "" : $"    |    رقم الإضبارة: {model.FileNumber}";
        doc.Blocks.Add(new Paragraph
        {
            FontSize = compact ? 10 : 11,
            Margin = new Thickness(0, 0, 0, compact ? 4 : 8),
            Inlines =
            {
                new Run($"العميل: {model.CustomerName}") { FontWeight = FontWeights.SemiBold },
                new Run("    |    "),
                new Run($"رقم الفاتورة: {model.InvoiceNumber}"),
                new Run(filePart),
                new Run("    |    "),
                new Run($"تاريخ البدء: {model.StartDate:yyyy/MM/dd}"),
                new Run("    |    "),
                new Run($"النوع: {model.InstallmentTypeLabel}")
            }
        });
    }

    private static void AppendScheduleTable(FlowDocument doc, IReadOnlyList<InstallmentPrintRow> schedule)
    {
        var columns = new[] { "ت", "تاريخ الاستحقاق", "المبلغ", "المسدد", "المتبقي", "الحالة", "تاريخ التسديد" };
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 8) };
        foreach (var _ in columns)
            table.Columns.Add(new TableColumn());

        var headerGroup = new TableRowGroup();
        var headerRow = new TableRow { Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x23, 0x7E)) };
        foreach (var col in columns)
            headerRow.Cells.Add(CreateCell(col, bold: true, foreground: Brushes.White));
        headerGroup.Rows.Add(headerRow);
        table.RowGroups.Add(headerGroup);

        var dataGroup = new TableRowGroup();
        var alternate = false;
        foreach (var s in schedule)
        {
            var row = new TableRow();
            if (s.StatusText == "مسدد")
                row.Background = new SolidColorBrush(Color.FromRgb(0xE8, 0xF5, 0xE9));
            else if (s.StatusText == "متأخر")
                row.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xEB, 0xEE));
            else if (alternate)
                row.Background = new SolidColorBrush(Color.FromRgb(0xFA, 0xFA, 0xFA));
            alternate = !alternate;

            foreach (var val in new[]
                     {
                         s.Number.ToString(),
                         s.DueDate.ToString("yyyy/MM/dd"),
                         s.Amount.ToString("N0"),
                         s.PaidAmount.ToString("N0"),
                         s.RemainingAmount.ToString("N0"),
                         s.StatusText,
                         s.PaymentDate?.ToString("yyyy/MM/dd") ?? "—"
                     })
                row.Cells.Add(CreateCell(val));

            dataGroup.Rows.Add(row);
        }
        table.RowGroups.Add(dataGroup);
        doc.Blocks.Add(table);
    }

    private static void AppendPlanStatistics(FlowDocument doc, IReadOnlyList<InstallmentPrintRow> schedule, decimal totalAmount)
    {
        var paid = schedule.Sum(s => s.PaidAmount);
        var remaining = schedule.Sum(s => s.RemainingAmount);
        var paidCount = schedule.Count(s => s.StatusText is "مسدد" or "مسدد جزئياً" && s.PaidAmount > 0);
        var unpaidCount = schedule.Count(s => s.RemainingAmount > 0);
        var overdueCount = schedule.Count(s => s.StatusText == "متأخر");

        AppendStatisticsCards(doc,
            ("إجمالي الأقساط", schedule.Count.ToString("N0")),
            ("المسدد", $"{paid:N0} د.ع"),
            ("المتبقي", $"{remaining:N0} د.ع"),
            ("مسددة", paidCount.ToString("N0")),
            ("غير مسددة", unpaidCount.ToString("N0")),
            ("متأخرة", overdueCount.ToString("N0")));

        var pct = totalAmount > 0 ? paid * 100m / totalAmount : 0;
        doc.Blocks.Add(new Paragraph(new Run($"نسبة التحصيل لهذه الفاتورة: {pct:N1}%"))
        {
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 4, 0, 12),
            Foreground = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32))
        });
    }

    private static void AppendGrandStatistics(FlowDocument doc, int planCount, decimal total, decimal paid, decimal remaining, int paidInstallments)
    {
        doc.Blocks.Add(new Paragraph(new Run("الملخص الكلي"))
        {
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 16, 0, 8),
            Foreground = new SolidColorBrush(Color.FromRgb(0x1A, 0x23, 0x7E))
        });

        AppendStatisticsCards(doc,
            ("عدد الفواتير", planCount.ToString("N0")),
            ("إجمالي المبالغ", $"{total:N0} د.ع"),
            ("إجمالي المسدد", $"{paid:N0} د.ع"),
            ("إجمالي المتبقي", $"{remaining:N0} د.ع"),
            ("نسبة التحصيل", total > 0 ? $"{paid * 100m / total:N1}%" : "0%"),
            ("أقساط مسددة", paidInstallments.ToString("N0")));
    }

    private static void AppendStatisticsCards(FlowDocument doc, params (string Label, string Value)[] cards)
    {
        var table = new Table { CellSpacing = 8, Margin = new Thickness(0, 8, 0, 8) };
        for (var i = 0; i < 3; i++)
            table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

        var group = new TableRowGroup();
        for (var row = 0; row < 2; row++)
        {
            var tableRow = new TableRow();
            for (var col = 0; col < 3; col++)
            {
                var idx = row * 3 + col;
                if (idx >= cards.Length)
                {
                    tableRow.Cells.Add(new TableCell(new Paragraph()) { Padding = new Thickness(4) });
                    continue;
                }

                var (label, value) = cards[idx];
                var panel = new Paragraph { TextAlignment = TextAlignment.Center, Margin = new Thickness(0) };
                panel.Inlines.Add(new Run(label + "\n") { FontSize = 10, Foreground = Brushes.Gray });
                panel.Inlines.Add(new Run(value) { FontSize = 13, FontWeight = FontWeights.Bold });

                tableRow.Cells.Add(new TableCell(panel)
                {
                    Background = new SolidColorBrush(Color.FromRgb(0xF3, 0xF4, 0xF6)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0xD1, 0xD5, 0xDB)),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(10, 8, 10, 8)
                });
            }
            group.Rows.Add(tableRow);
        }
        table.RowGroups.Add(group);
        doc.Blocks.Add(table);
    }

    private static TableCell CreateCell(string text, bool bold = false, Brush? foreground = null, Thickness? padding = null) =>
        new(new Paragraph(new Run(text))
        {
            TextAlignment = TextAlignment.Center,
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            Foreground = foreground ?? Brushes.Black,
            FontSize = 10.5
        })
        {
            Padding = padding ?? new Thickness(4, 3, 4, 3),
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(0, 0, 1, 1)
        };
}
