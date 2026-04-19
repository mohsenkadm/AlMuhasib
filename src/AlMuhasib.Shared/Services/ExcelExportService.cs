using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AlMuhasib.Core.Interfaces.Services;
using ClosedXML.Excel;

namespace AlMuhasib.Shared.Services;

public class ExcelExportService : IExportService
{
    public byte[] ExportToExcel<T>(IEnumerable<T> data, string sheetName = "Sheet1")
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName);

        // RTL sheet
        worksheet.RightToLeft = true;

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Header row
        for (int col = 0; col < properties.Length; col++)
        {
            var cell = worksheet.Cell(1, col + 1);
            cell.Value = properties[col].Name;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1A237E");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // Data rows
        int row = 2;
        foreach (var item in data)
        {
            for (int col = 0; col < properties.Length; col++)
            {
                var value = properties[col].GetValue(item);
                var cell = worksheet.Cell(row, col + 1);
                if (value is decimal d)
                    cell.Value = d;
                else if (value is int i)
                    cell.Value = i;
                else if (value is DateTime dt)
                    cell.Value = dt.ToString("yyyy/MM/dd");
                else
                    cell.Value = value?.ToString() ?? string.Empty;
            }
            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task ExportToExcelFileAsync<T>(IEnumerable<T> data, string filePath, string sheetName = "Sheet1")
    {
        var bytes = ExportToExcel(data, sheetName);
        await File.WriteAllBytesAsync(filePath, bytes);
    }

    public void ExportToExcel(string filePath, string sheetName, string[] columns, IList<object[]> rows)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName);
        worksheet.RightToLeft = true;

        // Header
        for (int col = 0; col < columns.Length; col++)
        {
            var cell = worksheet.Cell(1, col + 1);
            cell.Value = columns[col];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1A237E");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // Data
        for (int r = 0; r < rows.Count; r++)
        {
            var rowData = rows[r];
            for (int col = 0; col < rowData.Length && col < columns.Length; col++)
            {
                var cell = worksheet.Cell(r + 2, col + 1);
                var value = rowData[col];
                if (value is decimal d)
                    cell.Value = d;
                else if (value is int i)
                    cell.Value = i;
                else
                    cell.Value = value?.ToString() ?? string.Empty;
            }
        }

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }

    public void PrintTable(string title, string[] columns, IList<object[]> rows)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
            FontSize = 12,
            FlowDirection = FlowDirection.RightToLeft,
            PagePadding = new Thickness(40)
        };

        // Title
        doc.Blocks.Add(new Paragraph(new Run(title))
        {
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 16)
        });

        // Date
        doc.Blocks.Add(new Paragraph(new Run($"التاريخ: {DateTime.Now:yyyy/MM/dd}"))
        {
            FontSize = 11,
            Foreground = Brushes.Gray,
            TextAlignment = TextAlignment.Left,
            Margin = new Thickness(0, 0, 0, 12)
        });

        // Table
        var table = new Table { CellSpacing = 0 };
        table.Columns.Clear();
        foreach (var _ in columns)
            table.Columns.Add(new TableColumn());

        // Header row
        var headerGroup = new TableRowGroup();
        var headerRow = new TableRow { Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x23, 0x7E)) };
        foreach (var col in columns)
        {
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run(col))
            {
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center
            })
            {
                Padding = new Thickness(6, 4, 6, 4),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(0, 0, 1, 0)
            });
        }
        headerGroup.Rows.Add(headerRow);
        table.RowGroups.Add(headerGroup);

        // Data rows
        var dataGroup = new TableRowGroup();
        bool alternate = false;
        foreach (var rowData in rows)
        {
            var row = new TableRow();
            if (alternate)
                row.Background = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5));
            alternate = !alternate;

            for (int col = 0; col < columns.Length; col++)
            {
                var val = col < rowData.Length ? rowData[col]?.ToString() ?? "" : "";
                row.Cells.Add(new TableCell(new Paragraph(new Run(val))
                {
                    TextAlignment = TextAlignment.Center
                })
                {
                    Padding = new Thickness(6, 3, 6, 3),
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(0, 0, 0, 1)
                });
            }
            dataGroup.Rows.Add(row);
        }
        table.RowGroups.Add(dataGroup);
        doc.Blocks.Add(table);

        // Footer
        doc.Blocks.Add(new Paragraph(new Run($"إجمالي السجلات: {rows.Count}"))
        {
            FontSize = 11,
            Margin = new Thickness(0, 12, 0, 0)
        });

        var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
        var printDialog = new PrintDialog();
        if (printDialog.ShowDialog() == true)
        {
            paginator.PageSize = new Size(printDialog.PrintableAreaWidth, printDialog.PrintableAreaHeight);
            printDialog.PrintDocument(paginator, title);
        }
    }

    public void PrintInvoice(InvoicePrintModel m)
    {
        // ── A4 page dimensions (96 DPI) ──
        const double A4Width = 793.7;   // 210mm
        const double A4Height = 1122.5; // 297mm

        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
            FontSize = 12,
            FlowDirection = FlowDirection.RightToLeft,
            PageWidth = A4Width,
            PageHeight = A4Height,
            PagePadding = new Thickness(50, 40, 50, 40),
            ColumnWidth = A4Width // single column
        };

        var primaryColor = Color.FromRgb(0x15, 0x65, 0xC0);
        var primaryBrush = new SolidColorBrush(primaryColor);
        var darkColor = Color.FromRgb(0x0D, 0x47, 0xA1);
        var darkBrush = new SolidColorBrush(darkColor);
        var lightBg = Color.FromRgb(0xF5, 0xF7, 0xFA);
        var borderColor = Color.FromRgb(0xE0, 0xE0, 0xE0);
        var borderBrush = new SolidColorBrush(borderColor);
        var accentColor = Color.FromRgb(0xE6, 0x51, 0x00);

        // ═══════════════════════════════════════════════
        // HEADER SECTION with colored banner
        // ═══════════════════════════════════════════════
        var headerTable = new Table { CellSpacing = 0 };
        headerTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        var headerGroup = new TableRowGroup();
        var headerRow = new TableRow { Background = primaryBrush };
        headerRow.Cells.Add(new TableCell(new Paragraph(new Run(m.Title))
        {
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            TextAlignment = TextAlignment.Center
        })
        { Padding = new Thickness(0, 14, 0, 14) });
        headerGroup.Rows.Add(headerRow);
        headerTable.RowGroups.Add(headerGroup);
        doc.Blocks.Add(headerTable);

        // Thin accent line under header
        var accentLine = new Table { CellSpacing = 0 };
        accentLine.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        var accentGroup = new TableRowGroup();
        var accentRow = new TableRow { Background = new SolidColorBrush(accentColor) };
        accentRow.Cells.Add(new TableCell(new Paragraph(new Run(" ")) { FontSize = 1 }) { Padding = new Thickness(0, 2, 0, 2) });
        accentGroup.Rows.Add(accentRow);
        accentLine.RowGroups.Add(accentGroup);
        doc.Blocks.Add(accentLine);

        // ═══════════════════════════════════════════════
        // INVOICE INFO — Two columns side by side
        // ═══════════════════════════════════════════════
        var infoTable = new Table { CellSpacing = 0, Margin = new Thickness(0, 16, 0, 16) };
        infoTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        infoTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

        var infoGroup = new TableRowGroup();

        void AddInfoRow(string leftLabel, string leftVal, string? rightLabel = null, string? rightVal = null)
        {
            var r = new TableRow();
            // Left cell
            var leftPara = new Paragraph();
            leftPara.Inlines.Add(new Run(leftLabel + ": ") { FontWeight = FontWeights.Bold, Foreground = darkBrush });
            leftPara.Inlines.Add(new Run(leftVal));
            r.Cells.Add(new TableCell(leftPara) { Padding = new Thickness(8, 5, 8, 5) });

            // Right cell
            if (rightLabel != null && rightVal != null)
            {
                var rightPara = new Paragraph();
                rightPara.Inlines.Add(new Run(rightLabel + ": ") { FontWeight = FontWeights.Bold, Foreground = darkBrush });
                rightPara.Inlines.Add(new Run(rightVal));
                r.Cells.Add(new TableCell(rightPara) { Padding = new Thickness(8, 5, 8, 5) });
            }
            else
            {
                r.Cells.Add(new TableCell(new Paragraph(new Run(""))) { Padding = new Thickness(8, 5, 8, 5) });
            }
            infoGroup.Rows.Add(r);
        }

        AddInfoRow("رقم الفاتورة", m.InvoiceNumber, "التاريخ", m.Date.ToString("yyyy/MM/dd"));
        AddInfoRow(m.PartyLabel, string.IsNullOrWhiteSpace(m.PartyName) ? "—" : m.PartyName, "المخزن", m.WarehouseName);
        AddInfoRow("طريقة الدفع", m.PaymentMethod,
            m.CreditDueDate.HasValue ? "تاريخ الاستحقاق" : null,
            m.CreditDueDate?.ToString("yyyy/MM/dd"));
        if (!string.IsNullOrWhiteSpace(m.FileNumber))
            AddInfoRow("رقم الملف", m.FileNumber);
        if (!string.IsNullOrWhiteSpace(m.Notes))
            AddInfoRow("ملاحظات", m.Notes);

        infoTable.RowGroups.Add(infoGroup);

        // Wrap info in a bordered section
        doc.Blocks.Add(infoTable);

        // Separator
        doc.Blocks.Add(new Paragraph(new Run(" ")) { FontSize = 4, Margin = new Thickness(0) });

        // ═══════════════════════════════════════════════
        // ITEMS TABLE
        // ═══════════════════════════════════════════════
        var itemsTable = new Table { CellSpacing = 0, BorderBrush = borderBrush, BorderThickness = new Thickness(1) };
        var colWidths = new[] { 45.0, 250.0, 80.0, 110.0, 120.0 };
        foreach (var w in colWidths)
            itemsTable.Columns.Add(new TableColumn { Width = new GridLength(w) });

        // Header
        var itemHeaderGroup = new TableRowGroup();
        var itemHeaderRow = new TableRow { Background = primaryBrush };
        foreach (var col in new[] { "#", "المادة", "الكمية", "سعر الوحدة", "الإجمالي" })
        {
            itemHeaderRow.Cells.Add(new TableCell(new Paragraph(new Run(col))
            {
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                FontSize = 12
            })
            {
                Padding = new Thickness(6, 8, 6, 8),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x0D, 0x47, 0xA1)),
                BorderThickness = new Thickness(0, 0, 1, 0)
            });
        }
        itemHeaderGroup.Rows.Add(itemHeaderRow);
        itemsTable.RowGroups.Add(itemHeaderGroup);

        // Data rows
        var dataGroup = new TableRowGroup();
        bool alt = false;
        foreach (var item in m.Items)
        {
            var itemRow = new TableRow();
            if (alt) itemRow.Background = new SolidColorBrush(lightBg);
            alt = !alt;

            void AddItemCell(string text, bool bold = false, TextAlignment align = TextAlignment.Center)
            {
                itemRow.Cells.Add(new TableCell(new Paragraph(new Run(text))
                {
                    TextAlignment = align,
                    FontWeight = bold ? FontWeights.Bold : FontWeights.Normal
                })
                {
                    Padding = new Thickness(6, 6, 6, 6),
                    BorderBrush = borderBrush,
                    BorderThickness = new Thickness(0, 0, 0, 1)
                });
            }

            AddItemCell(item.Number.ToString());
            AddItemCell(item.ItemName, align: TextAlignment.Right);
            AddItemCell(item.Quantity.ToString("N0"));
            AddItemCell(item.UnitPrice.ToString("N0") + " د.ع");
            AddItemCell(item.TotalPrice.ToString("N0") + " د.ع", bold: true);
            dataGroup.Rows.Add(itemRow);
        }
        itemsTable.RowGroups.Add(dataGroup);
        doc.Blocks.Add(itemsTable);

        // ═══════════════════════════════════════════════
        // TOTALS SECTION — right-aligned box
        // ═══════════════════════════════════════════════
        var totalsTable = new Table { CellSpacing = 0, Margin = new Thickness(0, 12, 0, 0) };
        totalsTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        totalsTable.Columns.Add(new TableColumn { Width = new GridLength(200) });
        totalsTable.Columns.Add(new TableColumn { Width = new GridLength(160) });

        var totalsGroup = new TableRowGroup();

        void AddTotalRow(string label, decimal amount, bool isBold = false, bool isHighlighted = false)
        {
            var r = new TableRow();
            if (isHighlighted)
                r.Background = primaryBrush;

            // Spacer column
            r.Cells.Add(new TableCell(new Paragraph(new Run(""))));

            // Label
            r.Cells.Add(new TableCell(new Paragraph(new Run(label))
            {
                FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
                FontSize = isBold ? 14 : 12,
                TextAlignment = TextAlignment.Right,
                Foreground = isHighlighted ? Brushes.White : Brushes.Black
            })
            { Padding = new Thickness(8, 6, 8, 6), BorderBrush = borderBrush, BorderThickness = new Thickness(0, 0, 0, isHighlighted ? 0 : 1) });

            // Amount
            r.Cells.Add(new TableCell(new Paragraph(new Run(amount.ToString("N0") + " د.ع"))
            {
                FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
                FontSize = isBold ? 14 : 12,
                TextAlignment = TextAlignment.Center,
                Foreground = isHighlighted ? Brushes.White : darkBrush
            })
            { Padding = new Thickness(8, 6, 8, 6), BorderBrush = borderBrush, BorderThickness = new Thickness(0, 0, 0, isHighlighted ? 0 : 1) });

            totalsGroup.Rows.Add(r);
        }

        AddTotalRow("المجموع الفرعي", m.Subtotal);
        if (m.RoundingAmount != 0)
            AddTotalRow("التقريب", m.RoundingAmount);
        AddTotalRow("الإجمالي الكلي", m.GrandTotal, isBold: true, isHighlighted: true);

        totalsTable.RowGroups.Add(totalsGroup);
        doc.Blocks.Add(totalsTable);

        // ═══════════════════════════════════════════════
        // INSTALLMENT SCHEDULE (if applicable)
        // ═══════════════════════════════════════════════
        if (m.Schedule is { Count: > 0 })
        {
            doc.Blocks.Add(new Paragraph(new Run(" ")) { FontSize = 6, Margin = new Thickness(0) });

            // Schedule header
            var schedTitleTable = new Table { CellSpacing = 0 };
            schedTitleTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            var schedTitleGroup = new TableRowGroup();
            var schedTitleRow = new TableRow { Background = new SolidColorBrush(accentColor) };
            schedTitleRow.Cells.Add(new TableCell(new Paragraph(new Run($"جدول الأقساط — {m.NumberOfInstallments} قسط — مبلغ القسط: {m.InstallmentAmount:N0} د.ع"))
            {
                FontSize = 13, FontWeight = FontWeights.Bold,
                Foreground = Brushes.White, TextAlignment = TextAlignment.Center
            })
            { Padding = new Thickness(0, 8, 0, 8) });
            schedTitleGroup.Rows.Add(schedTitleRow);
            schedTitleTable.RowGroups.Add(schedTitleGroup);
            doc.Blocks.Add(schedTitleTable);

            var schedTable = new Table { CellSpacing = 0, BorderBrush = borderBrush, BorderThickness = new Thickness(1) };
            foreach (var w in new[] { 70.0, 200.0, 160.0 })
                schedTable.Columns.Add(new TableColumn { Width = new GridLength(w) });

            var schedHeader = new TableRowGroup();
            var schedHeaderRow = new TableRow { Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x8F, 0x00)) };
            foreach (var col in new[] { "رقم القسط", "تاريخ الاستحقاق", "المبلغ" })
            {
                schedHeaderRow.Cells.Add(new TableCell(new Paragraph(new Run(col))
                {
                    Foreground = Brushes.White, FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center
                })
                { Padding = new Thickness(6, 6, 6, 6) });
            }
            schedHeader.Rows.Add(schedHeaderRow);
            schedTable.RowGroups.Add(schedHeader);

            var schedData = new TableRowGroup();
            bool a = false;
            foreach (var s in m.Schedule)
            {
                var sr = new TableRow();
                if (a) sr.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xF8, 0xE1));
                a = !a;
                foreach (var val in new[] { s.Number.ToString(), s.DueDate.ToString("yyyy/MM/dd"), s.Amount.ToString("N0") + " د.ع" })
                    sr.Cells.Add(new TableCell(new Paragraph(new Run(val)) { TextAlignment = TextAlignment.Center })
                    { Padding = new Thickness(6, 5, 6, 5), BorderBrush = borderBrush, BorderThickness = new Thickness(0, 0, 0, 1) });
                schedData.Rows.Add(sr);
            }
            schedTable.RowGroups.Add(schedData);
            doc.Blocks.Add(schedTable);
        }

        // ═══════════════════════════════════════════════
        // SIGNATURE AREA
        // ═══════════════════════════════════════════════
        var sigTable = new Table { CellSpacing = 0, Margin = new Thickness(0, 30, 0, 0) };
        sigTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        sigTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        var sigGroup = new TableRowGroup();
        var sigRow = new TableRow();
        sigRow.Cells.Add(new TableCell(new Paragraph(new Run("توقيع المستلم: _______________"))
        { TextAlignment = TextAlignment.Center, Foreground = new SolidColorBrush(Color.FromRgb(0x75, 0x75, 0x75)) })
        { Padding = new Thickness(0, 8, 0, 8) });
        sigRow.Cells.Add(new TableCell(new Paragraph(new Run("توقيع البائع: _______________"))
        { TextAlignment = TextAlignment.Center, Foreground = new SolidColorBrush(Color.FromRgb(0x75, 0x75, 0x75)) })
        { Padding = new Thickness(0, 8, 0, 8) });
        sigGroup.Rows.Add(sigRow);
        sigTable.RowGroups.Add(sigGroup);
        doc.Blocks.Add(sigTable);

        // ═══════════════════════════════════════════════
        // FOOTER
        // ═══════════════════════════════════════════════
        doc.Blocks.Add(new Paragraph(new Run("")) { FontSize = 4, Margin = new Thickness(0, 8, 0, 0) });

        // Footer line
        var footerLine = new Table { CellSpacing = 0 };
        footerLine.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        var footerLineGroup = new TableRowGroup();
        var footerLineRow = new TableRow { Background = borderBrush };
        footerLineRow.Cells.Add(new TableCell(new Paragraph(new Run(" ")) { FontSize = 1 }) { Padding = new Thickness(0, 1, 0, 1) });
        footerLineGroup.Rows.Add(footerLineRow);
        footerLine.RowGroups.Add(footerLineGroup);
        doc.Blocks.Add(footerLine);

        doc.Blocks.Add(new Paragraph(new Run($"طُبع بتاريخ: {DateTime.Now:yyyy/MM/dd HH:mm}"))
        {
            FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E)),
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 6, 0, 0)
        });

        // ── Print with A4 page size ──
        var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
        var dlg = new PrintDialog();
        if (dlg.ShowDialog() == true)
        {
            paginator.PageSize = new Size(A4Width, A4Height);
            dlg.PrintDocument(paginator, m.Title);
        }
    }
}
