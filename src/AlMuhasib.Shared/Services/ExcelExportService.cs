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
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
            FontSize = 12,
            FlowDirection = FlowDirection.RightToLeft,
            PagePadding = new Thickness(50, 40, 50, 40)
        };

        // ── Company header ───────────────────────────────
        doc.Blocks.Add(new Paragraph(new Run(m.Title))
        {
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Foreground = new SolidColorBrush(Color.FromRgb(0x1A, 0x23, 0x7E)),
            Margin = new Thickness(0, 0, 0, 4)
        });

        // Separator line
        var sepTable = new Table { CellSpacing = 0 };
        sepTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        var sepGroup = new TableRowGroup();
        var sepRow = new TableRow { Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x23, 0x7E)) };
        sepRow.Cells.Add(new TableCell(new Paragraph(new Run(" "))) { Padding = new Thickness(0, 2, 0, 2) });
        sepGroup.Rows.Add(sepRow);
        sepTable.RowGroups.Add(sepGroup);
        doc.Blocks.Add(sepTable);

        // ── Invoice info grid ────────────────────────────
        var infoTable = new Table { CellSpacing = 0, Margin = new Thickness(0, 12, 0, 12) };
        infoTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        infoTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

        var infoGroup = new TableRowGroup();

        void AddInfoRow(string label, string value)
        {
            var r = new TableRow();
            r.Cells.Add(new TableCell(new Paragraph(new Run($"{label}: "))
                { FontWeight = FontWeights.Bold }) { Padding = new Thickness(4, 3, 4, 3) });
            r.Cells.Add(new TableCell(new Paragraph(new Run(value)))
                { Padding = new Thickness(4, 3, 4, 3) });
            infoGroup.Rows.Add(r);
        }

        AddInfoRow("رقم الفاتورة", m.InvoiceNumber);
        AddInfoRow("التاريخ", m.Date.ToString("yyyy/MM/dd"));
        AddInfoRow(m.PartyLabel, m.PartyName);
        AddInfoRow("المخزن", m.WarehouseName);
        AddInfoRow("طريقة الدفع", m.PaymentMethod);
        if (m.CreditDueDate.HasValue)
            AddInfoRow("تاريخ الاستحقاق", m.CreditDueDate.Value.ToString("yyyy/MM/dd"));
        if (!string.IsNullOrWhiteSpace(m.FileNumber))
            AddInfoRow("رقم الملف", m.FileNumber);
        if (!string.IsNullOrWhiteSpace(m.Notes))
            AddInfoRow("ملاحظات", m.Notes);

        infoTable.RowGroups.Add(infoGroup);
        doc.Blocks.Add(infoTable);

        // ── Items table ──────────────────────────────────
        var itemsTable = new Table { CellSpacing = 0, BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(1) };
        var colWidths = new[] { 40.0, 200.0, 80.0, 100.0, 110.0 };
        foreach (var w in colWidths)
            itemsTable.Columns.Add(new TableColumn { Width = new GridLength(w) });

        var headerGroup2 = new TableRowGroup();
        var headerRow2 = new TableRow { Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x23, 0x7E)) };
        foreach (var col in new[] { "#", "المادة", "الكمية", "سعر الوحدة", "الإجمالي" })
        {
            headerRow2.Cells.Add(new TableCell(new Paragraph(new Run(col))
            {
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                FontSize = 11
            })
            {
                Padding = new Thickness(4, 5, 4, 5),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(0, 0, 1, 0)
            });
        }
        headerGroup2.Rows.Add(headerRow2);
        itemsTable.RowGroups.Add(headerGroup2);

        var dataGroup2 = new TableRowGroup();
        bool alt = false;
        foreach (var item in m.Items)
        {
            var itemRow = new TableRow();
            if (alt) itemRow.Background = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5));
            alt = !alt;

            void AddCell(string text, bool bold = false)
            {
                itemRow.Cells.Add(new TableCell(new Paragraph(new Run(text))
                {
                    TextAlignment = TextAlignment.Center,
                    FontWeight = bold ? FontWeights.Bold : FontWeights.Normal
                })
                {
                    Padding = new Thickness(4, 4, 4, 4),
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(0, 0, 0, 1)
                });
            }

            AddCell(item.Number.ToString());
            AddCell(item.ItemName);
            AddCell(item.Quantity.ToString("N0"));
            AddCell(item.UnitPrice.ToString("N0") + " د.ع");
            AddCell(item.TotalPrice.ToString("N0") + " د.ع", bold: true);
            dataGroup2.Rows.Add(itemRow);
        }
        itemsTable.RowGroups.Add(dataGroup2);
        doc.Blocks.Add(itemsTable);

        // ── Totals ────────────────────────────────────────
        var totalsTable = new Table { CellSpacing = 0, Margin = new Thickness(0, 8, 0, 0) };
        totalsTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        totalsTable.Columns.Add(new TableColumn { Width = new GridLength(160) });

        var totalsGroup = new TableRowGroup();

        void AddTotalRow(string label, decimal amount, bool isBold = false, Color? bg = null)
        {
            var r = new TableRow();
            if (bg.HasValue) r.Background = new SolidColorBrush(bg.Value);
            r.Cells.Add(new TableCell(new Paragraph(new Run(label))
                { FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal, TextAlignment = TextAlignment.Left })
                { Padding = new Thickness(4, 4, 4, 4) });
            r.Cells.Add(new TableCell(new Paragraph(new Run(amount.ToString("N0") + " د.ع"))
                { FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal, TextAlignment = TextAlignment.Center,
                  Foreground = isBold ? new SolidColorBrush(Color.FromRgb(0x1A, 0x23, 0x7E)) : Brushes.Black })
                { Padding = new Thickness(4, 4, 4, 4) });
            totalsGroup.Rows.Add(r);
        }

        AddTotalRow("المجموع الفرعي", m.Subtotal);
        if (m.RoundingAmount != 0)
            AddTotalRow("التقريب", m.RoundingAmount);
        AddTotalRow("الإجمالي الكلي", m.GrandTotal, isBold: true, bg: Color.FromRgb(0xE8, 0xEA, 0xF6));

        totalsTable.RowGroups.Add(totalsGroup);
        doc.Blocks.Add(totalsTable);

        // ── Installment schedule ──────────────────────────
        if (m.Schedule is { Count: > 0 })
        {
            doc.Blocks.Add(new Paragraph(new Run("جدول الأقساط"))
            {
                FontSize = 14, FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 16, 0, 6)
            });

            var schedTable = new Table { CellSpacing = 0 };
            foreach (var _ in new[] { 60.0, 150.0, 120.0 })
                schedTable.Columns.Add(new TableColumn { Width = new GridLength(_) });

            var schedHeader = new TableRowGroup();
            var schedHeaderRow = new TableRow { Background = new SolidColorBrush(Color.FromRgb(0xE6, 0x51, 0x00)) };
            foreach (var col in new[] { "رقم القسط", "تاريخ الاستحقاق", "المبلغ" })
            {
                schedHeaderRow.Cells.Add(new TableCell(new Paragraph(new Run(col))
                {
                    Foreground = Brushes.White, FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center
                }) { Padding = new Thickness(4, 5, 4, 5) });
            }
            schedHeader.Rows.Add(schedHeaderRow);
            schedTable.RowGroups.Add(schedHeader);

            var schedData = new TableRowGroup();
            bool a = false;
            foreach (var s in m.Schedule)
            {
                var sr = new TableRow();
                if (a) sr.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xF3, 0xE0));
                a = !a;
                foreach (var val in new[] { s.Number.ToString(), s.DueDate.ToString("yyyy/MM/dd"), s.Amount.ToString("N0") + " د.ع" })
                    sr.Cells.Add(new TableCell(new Paragraph(new Run(val)) { TextAlignment = TextAlignment.Center })
                        { Padding = new Thickness(4, 3, 4, 3), BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0, 0, 0, 1) });
                schedData.Rows.Add(sr);
            }
            schedTable.RowGroups.Add(schedData);
            doc.Blocks.Add(schedTable);
        }

        // ── Footer ───────────────────────────────────────
        doc.Blocks.Add(new Paragraph(new Run($"طُبع بتاريخ: {DateTime.Now:yyyy/MM/dd HH:mm}"))
        {
            FontSize = 10, Foreground = Brushes.Gray,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 16, 0, 0)
        });

        var paginator2 = ((IDocumentPaginatorSource)doc).DocumentPaginator;
        var dlg = new PrintDialog();
        if (dlg.ShowDialog() == true)
        {
            paginator2.PageSize = new Size(dlg.PrintableAreaWidth, dlg.PrintableAreaHeight);
            dlg.PrintDocument(paginator2, m.Title);
        }
    }
}
