using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AlMuhasib.Core;
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

    public void PrintTable(string title, string[] columns, IList<object[]> rows, IList<string>? summaryLines = null)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
            FontSize = 12,
            FlowDirection = FlowDirection.RightToLeft,
            PagePadding = new Thickness(40, 10, 40, 40)
        };

        PrintBrandingFlowDocumentHelper.PrependBrandingHeader(doc);

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

        if (summaryLines is { Count: > 0 })
        {
            foreach (var line in summaryLines)
            {
                doc.Blocks.Add(new Paragraph(new Run(line))
                {
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 8, 0, 0)
                });
            }
        }
        else
        {
            doc.Blocks.Add(new Paragraph(new Run($"إجمالي السجلات: {rows.Count}"))
            {
                FontSize = 11,
                Margin = new Thickness(0, 12, 0, 0)
            });
        }

        PrintBrandingFlowDocumentHelper.AppendBrandingFooter(doc, systemLine: $"طُبع بتاريخ: {DateTime.Now:yyyy/MM/dd HH:mm}");

        DocumentPrintHelper.PrintWithPreview(doc, title);
    }

    public void PrintInvoice(InvoicePrintModel m)
    {
        if (m.Schedule is { Count: > 0 } && m.Title.Contains("أقساط"))
        {
            PrintInstallmentInvoiceLikeReference(m);
            return;
        }

        if (!m.IsGoldInvoice && m.Schedule is not { Count: > 0 })
        {
            PrintModernInvoice(m);
            return;
        }

        // ── A4 page dimensions (96 DPI) ──
        const double A4Width = 793.7;   // 210mm
        const double A4Height = 1122.5; // 297mm
        var theme = InvoiceA4TemplateTheme.Resolve(m.A4TemplateId);
        var compactScheduleMode = theme.ForceCompactMetrics || m.Schedule is { Count: >= 14 };

        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
            FontSize = compactScheduleMode ? 10 : 11,
            FlowDirection = FlowDirection.RightToLeft,
            PageWidth = A4Width,
            PageHeight = A4Height,
            PagePadding = compactScheduleMode ? theme.CompactPagePadding : theme.PagePadding,
            ColumnWidth = A4Width // single column
        };

        PrintBrandingFlowDocumentHelper.PrependBrandingHeader(doc);

        var hideAmounts = m.HideAmounts;
        var isGold = m.IsGoldInvoice && !hideAmounts;
        // Use the selected branded template for every invoice type so standard
        // sales invoices have the same polished visual hierarchy as premium layouts.
        var useClearPrint = false;

        var primaryColor = useClearPrint ? Colors.Black : theme.Primary;
        var primaryBrush = new SolidColorBrush(primaryColor);
        var darkColor = useClearPrint ? Colors.Black : theme.Dark;
        var darkBrush = new SolidColorBrush(darkColor);
        var lightBg = useClearPrint ? Color.FromRgb(0xF5, 0xF5, 0xF5) : theme.LightBg;
        var borderColor = useClearPrint ? Color.FromRgb(0x42, 0x42, 0x42) : theme.Border;
        var borderBrush = new SolidColorBrush(borderColor);
        var accentColor = theme.Accent;
        var infoPadding = compactScheduleMode ? new Thickness(5, 2, 5, 2) : new Thickness(8, 5, 8, 5);
        var currencySuffix = $" {(string.IsNullOrWhiteSpace(m.CurrencyLabel) ? "د.ع" : m.CurrencyLabel)}";

        // ═══════════════════════════════════════════════
        // TITLE — clear print: bordered B&W; gold: themed banner
        // ═══════════════════════════════════════════════
        var headerTable = new Table { CellSpacing = 0 };
        headerTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        var headerGroup = new TableRowGroup();
        var headerRow = new TableRow();
        if (!useClearPrint && theme.UseSolidBanner)
            headerRow.Background = primaryBrush;
        var titleCell = new TableCell(new Paragraph(new Run(m.Title))
        {
            FontSize = compactScheduleMode ? theme.CompactTitleFontSize : theme.TitleFontSize,
            FontWeight = FontWeights.Bold,
            Foreground = useClearPrint || !theme.UseSolidBanner ? darkBrush : Brushes.White,
            TextAlignment = TextAlignment.Center
        })
        {
            Padding = compactScheduleMode ? new Thickness(0, 8, 0, 8) : new Thickness(0, 14, 0, 14)
        };
        if (useClearPrint)
        {
            titleCell.BorderBrush = borderBrush;
            titleCell.BorderThickness = new Thickness(1);
        }
        headerRow.Cells.Add(titleCell);
        headerGroup.Rows.Add(headerRow);
        headerTable.RowGroups.Add(headerGroup);
        doc.Blocks.Add(headerTable);

        if (!useClearPrint && theme.ShowAccentLine)
        {
            var accentLine = new Table { CellSpacing = 0 };
            accentLine.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            var accentGroup = new TableRowGroup();
            var accentRow = new TableRow { Background = new SolidColorBrush(accentColor) };
            accentRow.Cells.Add(new TableCell(new Paragraph(new Run(" ")) { FontSize = 1 })
            {
                Padding = new Thickness(0, theme.UseSolidBanner ? 2 : 3, 0, theme.UseSolidBanner ? 2 : 3)
            });
            accentGroup.Rows.Add(accentRow);
            accentLine.RowGroups.Add(accentGroup);
            doc.Blocks.Add(accentLine);
        }

        // ═══════════════════════════════════════════════
        // PARTIES + INVOICE META
        // Clear print: customer (right) / representative (left) then invoice fields
        // ═══════════════════════════════════════════════
        Paragraph LabeledLine(string label, string value)
        {
            var p = new Paragraph { Margin = new Thickness(0, 1, 0, 1) };
            p.Inlines.Add(new Run(label + ": ") { FontWeight = FontWeights.Bold, Foreground = darkBrush });
            p.Inlines.Add(new Run(string.IsNullOrWhiteSpace(value) ? "—" : value));
            return p;
        }

        if (useClearPrint)
        {
            var partiesTable = new Table
            {
                CellSpacing = 0,
                Margin = compactScheduleMode ? new Thickness(0, 6, 0, 4) : new Thickness(0, 10, 0, 6)
            };
            partiesTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            partiesTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            var partiesGroup = new TableRowGroup();
            var partiesRow = new TableRow();

            // RTL: first cell = right side (customer), second = left (representative)
            var customerCell = new TableCell { Padding = infoPadding };
            customerCell.Blocks.Add(LabeledLine(m.PartyLabel, string.IsNullOrWhiteSpace(m.PartyName) ? "—" : m.PartyName));
            customerCell.Blocks.Add(LabeledLine("الهاتف", string.IsNullOrWhiteSpace(m.PartyPhone) ? "—" : m.PartyPhone));
            customerCell.Blocks.Add(LabeledLine("العنوان", string.IsNullOrWhiteSpace(m.PartyAddress) ? "—" : m.PartyAddress));
            partiesRow.Cells.Add(customerCell);

            if (!string.IsNullOrWhiteSpace(m.SalesRepresentativeName))
            {
                var repCell = new TableCell { Padding = infoPadding };
                repCell.Blocks.Add(LabeledLine("المندوب", m.SalesRepresentativeName));
                partiesRow.Cells.Add(repCell);
            }
            else
            {
                partiesRow.Cells.Add(new TableCell(new Paragraph(new Run(""))) { Padding = infoPadding });
            }

            partiesGroup.Rows.Add(partiesRow);
            partiesTable.RowGroups.Add(partiesGroup);
            doc.Blocks.Add(partiesTable);

            var metaTable = new Table
            {
                CellSpacing = 0,
                Margin = compactScheduleMode ? new Thickness(0, 0, 0, 6) : new Thickness(0, 0, 0, 10)
            };
            metaTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            metaTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            var metaGroup = new TableRowGroup();

            void AddMetaRow(string rightLabel, string rightVal, string? leftLabel = null, string? leftVal = null)
            {
                var r = new TableRow();
                r.Cells.Add(new TableCell(LabeledLine(rightLabel, rightVal)) { Padding = infoPadding });
                if (leftLabel != null)
                    r.Cells.Add(new TableCell(LabeledLine(leftLabel, leftVal ?? "—")) { Padding = infoPadding });
                else
                    r.Cells.Add(new TableCell(new Paragraph(new Run(""))) { Padding = infoPadding });
                metaGroup.Rows.Add(r);
            }

            AddMetaRow("رقم الفاتورة", m.InvoiceNumber, "التاريخ", m.Date.ToString("yyyy/MM/dd"));
            AddMetaRow("طريقة الدفع", m.PaymentMethod,
                m.CreditDueDate.HasValue ? "تاريخ الاستحقاق" : "المخزن",
                m.CreditDueDate.HasValue ? m.CreditDueDate.Value.ToString("yyyy/MM/dd") : m.WarehouseName);
            if (m.CreditDueDate.HasValue)
                AddMetaRow("المخزن", string.IsNullOrWhiteSpace(m.WarehouseName) ? "—" : m.WarehouseName);
            if (!string.IsNullOrWhiteSpace(m.DriverName))
                AddMetaRow("السائق", m.DriverName);
            if (!string.IsNullOrWhiteSpace(m.FileNumber))
                AddMetaRow("رقم الملف", m.FileNumber);
            if (!string.IsNullOrWhiteSpace(m.Notes))
                AddMetaRow("ملاحظات", m.Notes);

            metaTable.RowGroups.Add(metaGroup);
            doc.Blocks.Add(metaTable);
        }
        else
        {
            var infoTable = new Table
            {
                CellSpacing = 0,
                Margin = compactScheduleMode ? new Thickness(0, 6, 0, 6) : new Thickness(0, 10, 0, 10)
            };
            infoTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            infoTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            var infoGroup = new TableRowGroup();

            void AddInfoRow(string leftLabel, string leftVal, string? rightLabel = null, string? rightVal = null)
            {
                var r = new TableRow();
                var leftPara = new Paragraph();
                leftPara.Inlines.Add(new Run(leftLabel + ": ") { FontWeight = FontWeights.Bold, Foreground = darkBrush });
                leftPara.Inlines.Add(new Run(leftVal));
                r.Cells.Add(new TableCell(leftPara) { Padding = infoPadding });

                if (rightLabel != null && rightVal != null)
                {
                    var rightPara = new Paragraph();
                    rightPara.Inlines.Add(new Run(rightLabel + ": ") { FontWeight = FontWeights.Bold, Foreground = darkBrush });
                    rightPara.Inlines.Add(new Run(rightVal));
                    r.Cells.Add(new TableCell(rightPara) { Padding = infoPadding });
                }
                else
                {
                    r.Cells.Add(new TableCell(new Paragraph(new Run(""))) { Padding = infoPadding });
                }
                infoGroup.Rows.Add(r);
            }

            void AddSectionTitle(string title)
            {
                var row = new TableRow { Background = new SolidColorBrush(lightBg) };
                row.Cells.Add(new TableCell(new Paragraph(new Run(title))
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = darkBrush,
                    Margin = new Thickness(0)
                })
                {
                    ColumnSpan = 2,
                    Padding = new Thickness(8, 5, 8, 5),
                    BorderBrush = borderBrush,
                    BorderThickness = new Thickness(0, 0, 0, 1)
                });
                infoGroup.Rows.Add(row);
            }

            AddSectionTitle("بيانات الفاتورة");
            AddInfoRow("رقم الفاتورة", m.InvoiceNumber, "التاريخ", m.Date.ToString("yyyy/MM/dd"));
            AddInfoRow("طريقة الدفع", m.PaymentMethod,
                "تاريخ الاستحقاق",
                m.CreditDueDate?.ToString("yyyy/MM/dd") ?? string.Empty);
            AddInfoRow("المخزن", m.WarehouseName);

            AddSectionTitle($"بيانات {m.PartyLabel}");
            AddInfoRow("الاسم", m.PartyName, "الهاتف", m.PartyPhone ?? string.Empty);
            AddInfoRow("العنوان", m.PartyAddress ?? string.Empty, "البريد الإلكتروني", m.PartyEmail ?? string.Empty);

            if (!string.IsNullOrWhiteSpace(m.SalesRepresentativeName)
                || !string.IsNullOrWhiteSpace(m.SalesRepresentativePhone)
                || !string.IsNullOrWhiteSpace(m.SalesRepresentativeEmail))
            {
                AddSectionTitle("مندوب المبيعات");
                AddInfoRow("الاسم", m.SalesRepresentativeName ?? string.Empty,
                    "الهاتف", m.SalesRepresentativePhone ?? string.Empty);
                AddInfoRow("البريد الإلكتروني", m.SalesRepresentativeEmail ?? string.Empty);
            }

            if (isGold)
            {
                AddInfoRow("عملة التسعير", string.IsNullOrWhiteSpace(m.PricingCurrencyLabel) ? "—" : m.PricingCurrencyLabel,
                    "عملة الدفع", string.IsNullOrWhiteSpace(m.PaymentCurrencyLabel) ? "—" : m.PaymentCurrencyLabel);
                if (m.FxRate > 0)
                    AddInfoRow("سعر الصرف", m.FxRate.ToString("N2"));
            }
            if (!string.IsNullOrWhiteSpace(m.DriverName))
                AddInfoRow("السائق", m.DriverName);
            if (!string.IsNullOrWhiteSpace(m.FileNumber))
                AddInfoRow("رقم الملف", m.FileNumber);
            if (!string.IsNullOrWhiteSpace(m.Notes))
                AddInfoRow("ملاحظات", m.Notes);

            infoTable.RowGroups.Add(infoGroup);
            doc.Blocks.Add(infoTable);
        }

        doc.Blocks.Add(new Paragraph(new Run(" ")) { FontSize = 4, Margin = new Thickness(0) });

        // ═══════════════════════════════════════════════
        // ITEMS TABLE
        // ═══════════════════════════════════════════════
        var itemsTable = new Table { CellSpacing = 0, BorderBrush = borderBrush, BorderThickness = new Thickness(1) };
        if (hideAmounts)
        {
            itemsTable.Columns.Add(new TableColumn { Width = new GridLength(50) });
            itemsTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            itemsTable.Columns.Add(new TableColumn { Width = new GridLength(90) });
        }
        else if (isGold)
        {
            foreach (var w in new[] { 28.0, 130.0, 42.0, 58.0, 70.0, 70.0, 78.0, 70.0, 84.0 })
                itemsTable.Columns.Add(new TableColumn { Width = new GridLength(w) });
        }
        else
        {
            // Full-width professional layout: # | item* | qty | unit | total
            itemsTable.Columns.Add(new TableColumn { Width = new GridLength(compactScheduleMode ? 36 : 44) });
            itemsTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            itemsTable.Columns.Add(new TableColumn { Width = new GridLength(compactScheduleMode ? 70 : 80) });
            itemsTable.Columns.Add(new TableColumn { Width = new GridLength(compactScheduleMode ? 95 : 110) });
            itemsTable.Columns.Add(new TableColumn { Width = new GridLength(compactScheduleMode ? 105 : 120) });
        }

        var itemHeaderGroup = new TableRowGroup();
        var itemHeaderRow = new TableRow();
        if (!useClearPrint)
            itemHeaderRow.Background = primaryBrush;
        string[] headerCols;
        if (hideAmounts)
            headerCols = ["#", "المادة", "الكمية"];
        else if (isGold)
            headerCols = ["#", "المادة", "عيار", "وزن غ", "مثقال", "غرام", "قيمة ذهب", "أجور", "إجمالي"];
        else
            headerCols = ["#", "المادة", "الكمية", "سعر الوحدة", "الإجمالي"];
        foreach (var col in headerCols)
        {
            itemHeaderRow.Cells.Add(new TableCell(new Paragraph(new Run(col))
            {
                Foreground = useClearPrint ? Brushes.Black : Brushes.White,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                FontSize = isGold ? 10 : 12
            })
            {
                Padding = compactScheduleMode || isGold ? new Thickness(3, 4, 3, 4) : new Thickness(5, 6, 5, 6),
                BorderBrush = useClearPrint ? borderBrush : new SolidColorBrush(Color.FromRgb(0x0D, 0x47, 0xA1)),
                BorderThickness = useClearPrint ? new Thickness(0, 0, 1, 1) : new Thickness(0, 0, 1, 0)
            });
        }
        itemHeaderGroup.Rows.Add(itemHeaderRow);
        itemsTable.RowGroups.Add(itemHeaderGroup);

        var dataGroup = new TableRowGroup();
        bool alt = false;
        foreach (var item in m.Items)
        {
            var itemRow = new TableRow();
            if (alt) itemRow.Background = new SolidColorBrush(lightBg);
            alt = !alt;

            void AddItemCell(string text, bool bold = false, TextAlignment align = TextAlignment.Center, double fontSize = 11)
            {
                itemRow.Cells.Add(new TableCell(new Paragraph(new Run(text))
                {
                    TextAlignment = align,
                    FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                    FontSize = fontSize
                })
                {
                    Padding = compactScheduleMode || isGold ? new Thickness(3, 2, 3, 2) : new Thickness(5, 4, 5, 4),
                    BorderBrush = borderBrush,
                    BorderThickness = new Thickness(0, 0, 0, 1)
                });
            }

            AddItemCell(item.Number.ToString(), fontSize: isGold ? 10 : 11);
            var nameText = item.ItemName;
            if (!string.IsNullOrWhiteSpace(item.LineDirectionLabel))
                nameText = $"{nameText} ({item.LineDirectionLabel})";
            AddItemCell(nameText, align: TextAlignment.Right, fontSize: isGold ? 10 : 11);

            if (isGold)
            {
                AddItemCell((item.KaratValue ?? 0).ToString(), fontSize: 10);
                var weight = item.WeightGrams ?? item.Quantity;
                AddItemCell(weight.ToString("N3"), fontSize: 10);
                AddItemCell((item.MithqalPrice ?? 0).ToString("N0"), fontSize: 10);
                AddItemCell((item.PricePerGram ?? item.UnitPrice).ToString("N0"), fontSize: 10);
                AddItemCell((item.GoldValue ?? 0).ToString("N0"), fontSize: 10);
                AddItemCell((item.MakingCharge ?? 0).ToString("N0"), fontSize: 10);
                AddItemCell(item.TotalPrice.ToString("N0"), bold: true, fontSize: 10);
            }
            else
            {
                AddItemCell(item.Quantity.ToString("N0"));
                if (!hideAmounts)
                {
                    AddItemCell(item.UnitPrice.ToString("N0") + currencySuffix);
                    AddItemCell(item.TotalPrice.ToString("N0") + currencySuffix, bold: true);
                }
            }
            dataGroup.Rows.Add(itemRow);
        }
        itemsTable.RowGroups.Add(dataGroup);
        doc.Blocks.Add(itemsTable);

        // ═══════════════════════════════════════════════
        // TOTALS SECTION — right-aligned box (skipped for warehouse copy)
        // ═══════════════════════════════════════════════
        if (!hideAmounts)
        {
        var totalsTable = new Table
        {
            CellSpacing = 0,
            Margin = compactScheduleMode ? new Thickness(0, 4, 0, 0) : new Thickness(0, 8, 0, 0)
        };
        totalsTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        totalsTable.Columns.Add(new TableColumn { Width = new GridLength(200) });
        totalsTable.Columns.Add(new TableColumn { Width = new GridLength(160) });

        var totalsGroup = new TableRowGroup();

        void AddTotalRow(string label, decimal amount, bool isBold = false, bool isHighlighted = false, string? suffix = null)
        {
            var r = new TableRow();
            var highlightWithColor = isHighlighted && !useClearPrint;
            if (highlightWithColor)
                r.Background = primaryBrush;

            r.Cells.Add(new TableCell(new Paragraph(new Run(""))));

            r.Cells.Add(new TableCell(new Paragraph(new Run(label))
            {
                FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
                FontSize = isBold ? 14 : 12,
                TextAlignment = TextAlignment.Right,
                Foreground = highlightWithColor ? Brushes.White : Brushes.Black
            })
            {
                Padding = new Thickness(8, 6, 8, 6),
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(0, 0, 0, 1)
            });

            var amountText = amount.ToString("N0") + (suffix ?? currencySuffix);
            r.Cells.Add(new TableCell(new Paragraph(new Run(amountText))
            {
                FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
                FontSize = isBold ? 14 : 12,
                TextAlignment = TextAlignment.Center,
                Foreground = highlightWithColor ? Brushes.White : darkBrush
            })
            {
                Padding = new Thickness(8, 6, 8, 6),
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(0, 0, 0, 1)
            });

            totalsGroup.Rows.Add(r);
        }

        if (isGold)
        {
            AddTotalRow("قيمة الذهب", m.TotalGoldValue);
            AddTotalRow("الأجور", m.TotalMakingCharge);
            if (m.DiscountAmount != 0)
                AddTotalRow("الخصم", m.DiscountAmount);
            if (m.FxRate > 0)
                AddTotalRow("سعر الصرف", m.FxRate, suffix: string.Empty);
            if (m.TotalAmountIqd != 0)
                AddTotalRow("الإجمالي د.ع", m.TotalAmountIqd);
            if (m.TotalAmountUsd != 0)
                AddTotalRow("الإجمالي USD", m.TotalAmountUsd, suffix: " $");
            AddTotalRow("الإجمالي الكلي", m.GrandTotal, isBold: true, isHighlighted: true);
            AddTotalRow("المدفوع", m.PaidAmount);
            AddTotalRow("المتبقي", m.RemainingAmount);
        }
        else
        {
            AddTotalRow("المجموع الفرعي", m.Subtotal);
            if (m.RoundingAmount != 0)
                AddTotalRow("التقريب", m.RoundingAmount);
            if (m.TransportFeeAmount > 0)
                AddTotalRow("أجور النقل", m.TransportFeeAmount);
            if (m.DiscountAmount != 0)
            {
                AddTotalRow("الخصم", m.DiscountAmount);
                AddTotalRow("المبلغ بعد الخصم", m.Subtotal - m.DiscountAmount, isBold: true);
            }
            if (m.TaxRate > 0 || m.TaxAmount != 0)
                AddTotalRow($"الضريبة ({m.TaxRate:N2}%)", m.TaxAmount);
            AddTotalRow("الإجمالي الكلي", m.GrandTotal, isBold: true, isHighlighted: true);
            if (m.PaidAmount != 0 || m.RemainingAmount != 0)
            {
                AddTotalRow("المدفوع", m.PaidAmount);
                AddTotalRow("المتبقي", m.RemainingAmount);
            }
            if (m.CompanyFeeAmount is > 0)
                AddTotalRow("نسبة الشركة (8%)", m.CompanyFeeAmount.Value);
        }

        totalsTable.RowGroups.Add(totalsGroup);
        doc.Blocks.Add(totalsTable);
        }

        // ═══════════════════════════════════════════════
        // INSTALLMENT SCHEDULE (if applicable)
        // ═══════════════════════════════════════════════
        if (!hideAmounts && m.Schedule is { Count: > 0 })
        {
            doc.Blocks.Add(new Paragraph(new Run(" "))
            {
                FontSize = compactScheduleMode ? 1 : 4,
                Margin = new Thickness(0)
            });

            // Schedule header
            var schedTitleTable = new Table { CellSpacing = 0 };
            schedTitleTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            var schedTitleGroup = new TableRowGroup();
            var schedTitleRow = new TableRow { Background = new SolidColorBrush(accentColor) };
            schedTitleRow.Cells.Add(new TableCell(new Paragraph(new Run($"جدول الأقساط — {m.NumberOfInstallments} قسط — مبلغ القسط: {m.InstallmentAmount:N0} د.ع"))
            {
                FontSize = compactScheduleMode ? 10 : 11,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White, TextAlignment = TextAlignment.Center
            })
            { Padding = compactScheduleMode ? new Thickness(0, 3, 0, 3) : new Thickness(0, 5, 0, 5) });
            schedTitleGroup.Rows.Add(schedTitleRow);
            schedTitleTable.RowGroups.Add(schedTitleGroup);
            doc.Blocks.Add(schedTitleTable);

            var schedTable = new Table { CellSpacing = 0, BorderBrush = borderBrush, BorderThickness = new Thickness(1) };
            var schedWidths = compactScheduleMode
                ? new[] { 46.0, 132.0, 110.0 }
                : new[] { 56.0, 150.0, 120.0 };
            foreach (var w in schedWidths)
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
                { Padding = compactScheduleMode ? new Thickness(4, 2, 4, 2) : new Thickness(5, 4, 5, 4) });
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
                    sr.Cells.Add(new TableCell(new Paragraph(new Run(val))
                    {
                        TextAlignment = TextAlignment.Center,
                        FontSize = compactScheduleMode ? 10 : 11
                    })
                    {
                        Padding = compactScheduleMode ? new Thickness(4, 1, 4, 1) : new Thickness(5, 3, 5, 3),
                        BorderBrush = borderBrush,
                        BorderThickness = new Thickness(0, 0, 0, 1)
                    });
                schedData.Rows.Add(sr);
            }
            schedTable.RowGroups.Add(schedData);
            doc.Blocks.Add(schedTable);
        }

        // ═══════════════════════════════════════════════
        // SIGNATURE AREA
        // ═══════════════════════════════════════════════
        var sigTable = new Table
        {
            CellSpacing = 0,
            Margin = compactScheduleMode ? new Thickness(0, 4, 0, 0) : new Thickness(0, 12, 0, 0)
        };
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
        PrintBrandingFlowDocumentHelper.AppendBrandingFooter(doc, systemLine: $"طُبع بتاريخ: {DateTime.Now:yyyy/MM/dd HH:mm}");

        DocumentPrintHelper.PrintWithPreview(doc, m.Title, new Size(A4Width, A4Height));
    }

    /// <summary>
    /// طباعة فاتورة بيع/شراء/مرتجع بقالب A4 الاحترافي (نفس تنسيق ملف PDF).
    /// </summary>
    private void PrintModernInvoice(InvoicePrintModel m)
    {
        var doc = ModernInvoiceDocumentBuilder.Build(m);
        DocumentPrintHelper.PrintWithPreview(
            doc,
            m.Title,
            new Size(ModernInvoiceDocumentBuilder.PageWidth, ModernInvoiceDocumentBuilder.PageHeight));
    }

    private void PrintInstallmentInvoiceLikeReference(InvoicePrintModel m)
    {
        const double A4Width = 793.7;
        const double A4Height = 1122.5;
        var branding = PrintBrandingProvider.Current;
        var customerPhone = string.IsNullOrWhiteSpace(branding.PhonePrimary) ? "—" : branding.PhonePrimary;
        var companyAddress = string.IsNullOrWhiteSpace(branding.Address) ? "—" : branding.Address;

        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
            FontSize = 9.6,
            FlowDirection = FlowDirection.RightToLeft,
            PageWidth = A4Width,
            PageHeight = A4Height,
            PagePadding = new Thickness(14, 8, 14, 10),
            ColumnWidth = A4Width
        };

        PrintBrandingFlowDocumentHelper.PrependBrandingHeader(doc);

        // Boxed title similar to reference
        var titleTable = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 4) };
        titleTable.Columns.Add(new TableColumn { Width = new GridLength(230) });
        var titleGroup = new TableRowGroup();
        var titleRow = new TableRow();
        titleRow.Cells.Add(new TableCell(new Paragraph(new Run("جدول الأقساط"))
        {
            TextAlignment = TextAlignment.Center,
            FontWeight = FontWeights.Bold,
            FontSize = 16
        })
        {
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8, 3, 8, 3)
        });
        titleGroup.Rows.Add(titleRow);
        titleTable.RowGroups.Add(titleGroup);
        doc.Blocks.Add(titleTable);

        // Meta line close to reference style
        doc.Blocks.Add(new Paragraph
        {
            Margin = new Thickness(0, 0, 0, 4),
            Inlines =
            {
                new Run($"اسم العميل: {m.PartyName}") { FontWeight = FontWeights.SemiBold },
                new Run("    |    "),
                new Run($"الهاتف: {customerPhone}"),
                new Run("    |    "),
                new Run($"العنوان: {companyAddress}"),
                new Run("    |    "),
                new Run($"رقم الفاتورة: {m.InvoiceNumber}")
            }
        });

        // Installments table (main section)
        var schedTable = new Table { CellSpacing = 0, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1) };
        foreach (var w in new[] { 26.0, 72.0, 58.0, 56.0, 56.0, 56.0, 56.0, 58.0, 54.0, 48.0 })
            schedTable.Columns.Add(new TableColumn { Width = new GridLength(w) });

        var schedHeader = new TableRowGroup();
        var schedHeaderRow = new TableRow { Background = new SolidColorBrush(Color.FromRgb(0xE6, 0xE6, 0xE6)) };
        foreach (var col in new[] { "ت", "تاريخ الاستحقاق", "مبلغ القسط", "مبلغ التأمين", "مبلغ الخصم", "المسدد", "الباقي", "تاريخ التسديد", "الحالة", "التأخير" })
        {
            schedHeaderRow.Cells.Add(new TableCell(new Paragraph(new Run(col))
            {
                TextAlignment = TextAlignment.Center,
                FontWeight = FontWeights.Bold,
                FontSize = 8.6
            })
            {
                Padding = new Thickness(3, 1, 3, 1),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(0, 0, 1, 1)
            });
        }
        schedHeader.Rows.Add(schedHeaderRow);
        schedTable.RowGroups.Add(schedHeader);

        var schedData = new TableRowGroup();
        decimal totalPaid = 0;
        decimal totalRemaining = 0;
        int paidCount = 0;
        foreach (var s in m.Schedule!)
        {
            totalPaid += s.PaidAmount;
            totalRemaining += s.RemainingAmount;
            if (s.StatusText is "مسدد" or "مسدد جزئياً" && s.PaidAmount > 0)
                paidCount++;

            var rowBg = s.StatusText switch
            {
                "مسدد" => new SolidColorBrush(Color.FromRgb(0xE8, 0xF5, 0xE9)),
                "متأخر" => new SolidColorBrush(Color.FromRgb(0xFF, 0xEB, 0xEE)),
                _ => null
            };

            var values = new[]
            {
                s.Number.ToString(),
                s.DueDate.ToString("yyyy/MM/dd"),
                s.Amount.ToString("N0"),
                "0",
                "0",
                s.PaidAmount.ToString("N0"),
                s.RemainingAmount.ToString("N0"),
                s.PaymentDate?.ToString("yyyy/MM/dd") ?? "",
                s.StatusText,
                s.DelayDays?.ToString() ?? ""
            };

            var row = new TableRow();
            if (rowBg is not null)
                row.Background = rowBg;

            foreach (var val in values)
            {
                row.Cells.Add(new TableCell(new Paragraph(new Run(val))
                {
                    TextAlignment = TextAlignment.Center,
                    FontSize = 8.8
                })
                {
                    Padding = new Thickness(3, 0.8, 3, 0.8),
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(0, 0, 1, 1)
                });
            }
            schedData.Rows.Add(row);
        }
        schedTable.RowGroups.Add(schedData);
        doc.Blocks.Add(schedTable);

        // Summary stats under schedule
        var paidPct = m.GrandTotal > 0 ? totalPaid * 100m / m.GrandTotal : 0;
        doc.Blocks.Add(new Paragraph
        {
            Margin = new Thickness(0, 6, 0, 4),
            FontSize = 9.5,
            Inlines =
            {
                new Run($"إجمالي المسدد: {totalPaid:N0} د.ع") { FontWeight = FontWeights.SemiBold },
                new Run("    |    "),
                new Run($"إجمالي المتبقي: {totalRemaining:N0} د.ع") { FontWeight = FontWeights.SemiBold },
                new Run("    |    "),
                new Run($"أقساط مسددة: {paidCount} من {m.Schedule.Count}") { FontWeight = FontWeights.SemiBold },
                new Run("    |    "),
                new Run($"نسبة التحصيل: {paidPct:N1}%")
            }
        });

        // Items title
        doc.Blocks.Add(new Paragraph(new Run("تفاصيل القائمة"))
        {
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 6, 0, 3)
        });

        // Items table
        var itemsTable = new Table { CellSpacing = 0, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1) };
        foreach (var w in new[] { 250.0, 58.0, 64.0, 95.0, 112.0 })
            itemsTable.Columns.Add(new TableColumn { Width = new GridLength(w) });

        var itemsHeader = new TableRowGroup();
        var itemsHeaderRow = new TableRow { Background = new SolidColorBrush(Color.FromRgb(0xE6, 0xE6, 0xE6)) };
        foreach (var col in new[] { "اسم المنتج", "العدد", "الوحدة", "السعر", "الإجمالي" })
        {
            itemsHeaderRow.Cells.Add(new TableCell(new Paragraph(new Run(col))
            {
                TextAlignment = TextAlignment.Center,
                FontWeight = FontWeights.Bold,
                FontSize = 8.8
            })
            {
                Padding = new Thickness(3, 1, 3, 1),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(0, 0, 1, 1)
            });
        }
        itemsHeader.Rows.Add(itemsHeaderRow);
        itemsTable.RowGroups.Add(itemsHeader);

        var itemsData = new TableRowGroup();
        foreach (var i in m.Items)
        {
            var row = new TableRow();
            foreach (var val in new[] { i.ItemName, i.Quantity.ToString("N0"), "قطعة", i.UnitPrice.ToString("N0"), i.TotalPrice.ToString("N0") })
            {
                row.Cells.Add(new TableCell(new Paragraph(new Run(val))
                {
                    TextAlignment = TextAlignment.Center,
                    FontSize = 8.8
                })
                {
                    Padding = new Thickness(3, 0.8, 3, 0.8),
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(0, 0, 1, 1)
                });
            }
            itemsData.Rows.Add(row);
        }
        itemsTable.RowGroups.Add(itemsData);
        doc.Blocks.Add(itemsTable);

        // Totals row
        var totals = new Table { CellSpacing = 0, Margin = new Thickness(0, 4, 0, 0), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1) };
        foreach (var w in new[] { 122.0, 104.0, 104.0, 104.0, 104.0, 58.0 })
            totals.Columns.Add(new TableColumn { Width = new GridLength(w) });
        var totalsGroup = new TableRowGroup();
        var totalsHeader = new TableRow { Background = new SolidColorBrush(Color.FromRgb(0xE6, 0xE6, 0xE6)) };
        foreach (var col in new[] { "إجمالي الباقي", "إجمالي المسدد", "إجمالي الخصم", "إجمالي المبلغ", "الإجمالي", "" })
        {
            totalsHeader.Cells.Add(new TableCell(new Paragraph(new Run(col))
            {
                TextAlignment = TextAlignment.Center,
                FontWeight = FontWeights.Bold,
                FontSize = 8.6
            }) { Padding = new Thickness(3, 1, 3, 1), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(0, 0, 1, 1) });
        }
        totalsGroup.Rows.Add(totalsHeader);

        var totalsRow = new TableRow();
        foreach (var val in new[] { totalRemaining.ToString("N0"), totalPaid.ToString("N0"), "0", m.GrandTotal.ToString("N0"), "دينار", "" })
        {
            totalsRow.Cells.Add(new TableCell(new Paragraph(new Run(val))
            {
                TextAlignment = TextAlignment.Center,
                FontWeight = FontWeights.Bold,
                FontSize = 9
            }) { Padding = new Thickness(3, 2, 3, 2), BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0, 0, 1, 0) });
        }
        totalsGroup.Rows.Add(totalsRow);
        totals.RowGroups.Add(totalsGroup);
        doc.Blocks.Add(totals);

        PrintBrandingFlowDocumentHelper.AppendBrandingFooter(doc, systemLine: $"طُبع بتاريخ: {DateTime.Now:yyyy/MM/dd HH:mm}");
        DocumentPrintHelper.PrintWithPreview(doc, m.Title, new Size(A4Width, A4Height));
    }

    public string ExportInvoiceToPdf(InvoicePrintModel model)
    {
        var bytes = InvoicePdfGenerator.GenerateInvoice(model);
        return SavePdfToWhatsAppFolder(bytes, SanitizeFileName($"فاتورة_{model.InvoiceNumber}"));
    }

    public string ExportInstallmentPaymentReceiptToPdf(InstallmentPaymentReceiptPrintModel model)
    {
        var bytes = InvoicePdfGenerator.GeneratePaymentReceipt(model);
        var name = string.IsNullOrWhiteSpace(model.InvoiceNumber)
            ? $"إيصال_تسديد_{model.PaymentDate:yyyyMMdd_HHmm}"
            : $"إيصال_{model.InvoiceNumber}_{model.PaymentDate:yyyyMMdd_HHmm}";
        return SavePdfToWhatsAppFolder(bytes, SanitizeFileName(name));
    }

    public string ExportVoucherToPdf(VoucherPrintModel model)
    {
        var bytes = DocumentPdfGenerator.GenerateVoucher(model);
        var name = string.IsNullOrWhiteSpace(model.VoucherNumber)
            ? $"سند_{model.Date:yyyyMMdd_HHmm}"
            : $"سند_{model.VoucherNumber}";
        return SavePdfToWhatsAppFolder(bytes, SanitizeFileName(name));
    }

    public string ExportInvestorTransactionToPdf(InvestorTransactionPrintModel model)
    {
        var bytes = DocumentPdfGenerator.GenerateInvestorTransaction(model);
        var name = $"{model.TransactionTypeLabel}_{model.InvestorName}_{model.Date:yyyyMMdd_HHmm}";
        return SavePdfToWhatsAppFolder(bytes, SanitizeFileName(name));
    }

    public string ExportStatementToPdf(StatementPrintModel model)
    {
        var bytes = DocumentPdfGenerator.GenerateStatement(model);
        var party = string.IsNullOrWhiteSpace(model.PartyName) ? "كشف" : model.PartyName;
        var name = $"كشف_{party}_{DateTime.Now:yyyyMMdd_HHmm}";
        return SavePdfToWhatsAppFolder(bytes, SanitizeFileName(name));
    }

    private static string SavePdfToWhatsAppFolder(byte[] pdfBytes, string baseFileName)
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "AlMuhasib",
            "WhatsApp");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"{baseFileName}.pdf");
        if (File.Exists(path))
        {
            var stamp = DateTime.Now.ToString("HHmmss");
            path = Path.Combine(folder, $"{baseFileName}_{stamp}.pdf");
        }
        File.WriteAllBytes(path, pdfBytes);
        return path;
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Trim();
    }

    public void PrintThermalReceipt(InvoicePrintModel model) =>
        PrintThermalReceipt(model, null, null, true);

    public void PrintThermalReceipt(InvoicePrintModel model, string? paperSize,
        string? preferredPrinter, bool showPreview)
    {
        var sizeKey = PosReceiptPaperSizes.Normalize(paperSize);
        var pageSize = PosReceiptPaperSizes.GetPageSize(sizeKey);
        var doc = PosReceiptDocumentBuilder.Build(model, sizeKey);
        DocumentPrintHelper.PrintDocument(
            doc,
            $"إيصال {model.InvoiceNumber}",
            pageSize,
            preferredPrinter,
            showPreview);
    }

    public string ExportInstallmentContractToPdf(InvoicePrintModel model)
    {
        model.Title = "عقد تقسيط";
        return ExportInvoiceToPdf(model);
    }

    public void PrintInstallmentSchedule(InvoicePrintModel model)
    {
        if (model.Schedule is null || model.Schedule.Count == 0)
        {
            PrintTable("جدول الأقساط", ["#", "الاستحقاق", "المبلغ"], []);
            return;
        }
        var cols = new[] { "#", "تاريخ الاستحقاق", "المبلغ" };
        var rows = model.Schedule.Select(s => new object[] { s.Number, s.DueDate.ToString("yyyy/MM/dd"), s.Amount }).ToList();
        PrintTable($"جدول أقساط — {model.PartyName}", cols, rows,
            [$"فاتورة: {model.InvoiceNumber}", $"عدد الأقساط: {model.NumberOfInstallments}"]);
    }

    public void PrintInstallmentPlanDetail(InstallmentPlanDetailPrintModel model)
    {
        var doc = InstallmentPrintDocumentBuilder.BuildPlanDetailDocument(model);
        PrintBrandingFlowDocumentHelper.AppendBrandingFooter(doc, systemLine: $"طُبع بتاريخ: {DateTime.Now:yyyy/MM/dd HH:mm}");
        InstallmentPrintDocumentBuilder.PrintDocument(doc, "كشف الأقساط التفصيلي");
    }

    public void PrintInstallmentMultiPlanDetail(IReadOnlyList<InstallmentPlanDetailPrintModel> plans, string title)
    {
        var doc = InstallmentPrintDocumentBuilder.BuildMultiPlanDocument(plans, title);
        PrintBrandingFlowDocumentHelper.AppendBrandingFooter(doc, systemLine: $"طُبع بتاريخ: {DateTime.Now:yyyy/MM/dd HH:mm}");
        InstallmentPrintDocumentBuilder.PrintDocument(doc, title);
    }

    public void PrintInstallmentPlansSummary(InstallmentPlansSummaryPrintModel model)
    {
        var doc = InstallmentPrintDocumentBuilder.BuildPlansSummaryDocument(model);
        PrintBrandingFlowDocumentHelper.AppendBrandingFooter(doc, systemLine: $"طُبع بتاريخ: {DateTime.Now:yyyy/MM/dd HH:mm}");
        InstallmentPrintDocumentBuilder.PrintDocument(doc, model.Title);
    }
}
