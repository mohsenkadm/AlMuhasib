using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using AlMuhasib.Core.Entities.CarTrade;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Shared.Services;

namespace AlMuhasib.UI.Services;

public sealed class CarTradePrintService : ICarTradePrintService
{
    private static readonly CultureInfo ArabicCulture = CultureInfo.GetCultureInfo("ar-IQ");
    private static readonly Brush BorderBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44));
    private static readonly Brush HeaderBg = new SolidColorBrush(Color.FromRgb(0xFF, 0xF3, 0xE0));
    private static readonly Brush SaleHeaderBg = new SolidColorBrush(Color.FromRgb(0xE8, 0xF5, 0xE9));
    private static readonly Brush LightBg = new SolidColorBrush(Color.FromRgb(0xFA, 0xFA, 0xFA));
    private static readonly Brush AccentBrush = new SolidColorBrush(Color.FromRgb(0xE6, 0x51, 0x00));
    private static readonly Brush SaleAccentBrush = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32));

    public void PrintTransaction(CarTradeTransaction transaction, int copies = 1)
    {
        if (transaction.IsSold)
            PrintSale(transaction, copies);
        else
            PrintPurchase(transaction, copies);
    }

    public void PrintPurchase(CarTradeTransaction transaction, int copies = 1)
    {
        var document = BuildPurchaseDocument(transaction);
        DocumentPrintHelper.PrintWithPreview(document, $"شراء سيارة {transaction.TransactionNumber}", defaultCopies: copies);
    }

    public void PrintSale(CarTradeTransaction transaction, int copies = 1)
    {
        if (!transaction.IsSold)
            throw new InvalidOperationException("لا يمكن طباعة وصل بيع لسيارة غير مباعة");

        var document = BuildSaleDocument(transaction);
        DocumentPrintHelper.PrintWithPreview(document, $"بيع سيارة {transaction.TransactionNumber}", defaultCopies: copies);
    }

    public void PrintPaymentReceipt(CarTradeTransaction transaction, CarTradePayment payment, int copies = 1)
    {
        var document = BuildPaymentDocument(transaction, payment);
        DocumentPrintHelper.PrintWithPreview(document, $"وصل تسديد {transaction.TransactionNumber}", defaultCopies: copies);
    }

    public void PrintTransactions(IEnumerable<CarTradeTransaction> transactions, int copiesEach = 1)
    {
        foreach (var transaction in transactions)
            PrintTransaction(transaction, copiesEach);
    }

    private static FlowDocument BuildPurchaseDocument(CarTradeTransaction transaction)
    {
        var doc = CreateDocument();
        PrintBrandingFlowDocumentHelper.PrependBrandingHeader(doc);
        doc.Blocks.Add(BuildTitleBlock(
            "شراء سيارة",
            transaction.TransactionDate,
            transaction.TransactionNumber,
            AccentBrush,
            HeaderBg));
        doc.Blocks.Add(BuildSinglePartyBlock("البائع", transaction.SellerName, transaction.SellerPhone, HeaderBg));
        doc.Blocks.Add(BuildCarBlock(transaction));
        doc.Blocks.Add(BuildPurchaseAmountsBlock(transaction));
        if (!string.IsNullOrWhiteSpace(transaction.Notes))
            doc.Blocks.Add(BuildNotesBlock(transaction.Notes, HeaderBg));
        doc.Blocks.Add(BuildSignaturesBlock("توقيع البائع", "توقيع المعرض"));
        return doc;
    }

    private static FlowDocument BuildSaleDocument(CarTradeTransaction transaction)
    {
        var saleDate = transaction.SaleDate ?? transaction.TransactionDate;
        var doc = CreateDocument();
        PrintBrandingFlowDocumentHelper.PrependBrandingHeader(doc);
        doc.Blocks.Add(BuildTitleBlock(
            "بيع سيارة",
            saleDate,
            transaction.TransactionNumber,
            SaleAccentBrush,
            SaleHeaderBg));
        doc.Blocks.Add(BuildPartiesBlock(transaction, SaleHeaderBg));
        doc.Blocks.Add(BuildCarBlock(transaction));
        doc.Blocks.Add(BuildSaleAmountsBlock(transaction));
        if (!string.IsNullOrWhiteSpace(transaction.Notes))
            doc.Blocks.Add(BuildNotesBlock(transaction.Notes, SaleHeaderBg));
        doc.Blocks.Add(BuildSignaturesBlock("توقيع البائع (المعرض)", "توقيع المشتري"));
        return doc;
    }

    private static FlowDocument BuildPaymentDocument(CarTradeTransaction transaction, CarTradePayment payment)
    {
        var doc = CreateDocument();
        PrintBrandingFlowDocumentHelper.PrependBrandingHeader(doc);

        var isSale = payment.PaymentKind == CarTradePaymentKind.Sale;
        var title = new Paragraph(new Run(isSale ? "وصل تسديد من مشتري" : "وصل تسديد للبائع"))
        {
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Foreground = isSale ? SaleAccentBrush : AccentBrush,
            Margin = new Thickness(0, 0, 0, 12)
        };
        doc.Blocks.Add(title);

        var table = CreateTable(2, 6);
        AddRow(table, 0, "رقم العملية", transaction.TransactionNumber, "التاريخ", payment.PaymentDate.ToString("yyyy/MM/dd", ArabicCulture));
        AddRow(table, 1, "نوع العملية", isSale ? "بيع" : "شراء", "اسم السيارة", transaction.CarName);
        AddRow(table, 2, "الطرف", GetPaymentPartyName(transaction, payment), "المبلغ المسدد", FormatMoney(payment.Amount));
        AddRow(table, 3, "المتبقي قبل", FormatMoney(payment.RemainingBefore), "المتبقي بعد", FormatMoney(payment.RemainingAfter));
        var total = isSale ? transaction.SalePrice : transaction.PurchasePrice;
        var paid = isSale ? transaction.SaleAmountPaid : transaction.AmountPaid;
        AddRow(table, 4, "إجمالي العملية", FormatMoney(total), "المدفوع الكلي", FormatMoney(paid));
        if (!string.IsNullOrWhiteSpace(payment.Notes))
            AddRow(table, 5, "ملاحظات", payment.Notes, string.Empty, string.Empty);

        doc.Blocks.Add(table);
        doc.Blocks.Add(BuildSignaturesBlock("توقيع المستلم", "توقيع الدافع"));
        return doc;
    }

    private static FlowDocument CreateDocument() => new()
    {
        FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
        FontSize = 12,
        FlowDirection = FlowDirection.RightToLeft,
        PagePadding = new Thickness(36, 10, 36, 28)
    };

    private static Block BuildTitleBlock(string title, DateTime date, string number, Brush accent, Brush headerBg)
    {
        var table = CreateTable(3, 1, [2.2, 3.6, 2.2]);
        var row = new TableRow();

        var dayCell = new TableCell(CreateStackedFieldBlock([
            FieldLine("اليوم", date.ToString("dddd", ArabicCulture)),
            FieldLine("الساعة", DateTime.Now.ToString("HH:mm", ArabicCulture))
        ]))
        {
            BorderBrush = BorderBrush,
            Padding = new Thickness(6, 4, 6, 4),
            Background = headerBg
        };

        var titleCell = new TableCell(new Paragraph(new Run(title))
        {
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Foreground = accent,
            Margin = new Thickness(0, 8, 0, 8)
        })
        {
            TextAlignment = TextAlignment.Center
        };

        var dateCell = new TableCell(CreateStackedFieldBlock([
            FieldLine("التاريخ", date.ToString("yyyy/MM/dd", ArabicCulture)),
            FieldLine(string.Empty, number, boldValue: true, centerValue: true)
        ]))
        {
            BorderBrush = BorderBrush,
            Padding = new Thickness(6, 4, 6, 4),
            TextAlignment = TextAlignment.Left,
            Background = headerBg
        };

        row.Cells.Add(dayCell);
        row.Cells.Add(titleCell);
        row.Cells.Add(dateCell);
        table.RowGroups[0].Rows.Add(row);
        return WrapBlock(table, new Thickness(0, 0, 0, 10));
    }

    private static Block BuildSinglePartyBlock(string title, string name, string phone, Brush headerBg)
    {
        var section = CreatePartyBox(title, [
            FieldLine("الاسم", name),
            FieldLine("الهاتف", phone)
        ], headerBg);
        return WrapBlock(section, new Thickness(0, 0, 0, 10));
    }

    private static Block BuildPartiesBlock(CarTradeTransaction transaction, Brush headerBg)
    {
        var table = CreateTable(2, 1, [1, 1]);
        var row = new TableRow();
        row.Cells.Add(WrapCell(CreatePartyBox("البائع (المعرض)", [
            FieldLine("الاسم", transaction.SellerName),
            FieldLine("الهاتف", transaction.SellerPhone)
        ], headerBg), padding: new Thickness(0, 0, 6, 0)));
        row.Cells.Add(WrapCell(CreatePartyBox("المشتري", [
            FieldLine("الاسم", transaction.BuyerName),
            FieldLine("الهاتف", transaction.BuyerPhone)
        ], headerBg), padding: new Thickness(6, 0, 0, 0)));
        table.RowGroups[0].Rows.Add(row);
        return WrapBlock(table, new Thickness(0, 0, 0, 10));
    }

    private static Block BuildCarBlock(CarTradeTransaction transaction)
    {
        var block = CreateStackedFieldBlock([
            SectionLabel("بيانات السيارة"),
            FieldLine("اسم السيارة", transaction.CarName),
            FieldLine("النوع", transaction.CarType),
            FieldLine("اللون", transaction.CarColor),
            FieldLine("رقم اللوحة", transaction.PlateNumber),
            FieldLine("رقم الشاصي", transaction.ChassisNumber)
        ]);

        var cell = new TableCell(block)
        {
            Background = LightBg,
            BorderBrush = BorderBrush,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(12, 10, 12, 10)
        };

        var table = CreateTable(1, 1);
        table.RowGroups[0].Rows.Add(new TableRow { Cells = { cell } });
        return WrapBlock(table, new Thickness(0, 0, 0, 10));
    }

    private static Block BuildPurchaseAmountsBlock(CarTradeTransaction transaction)
    {
        var block = CreateStackedFieldBlock([
            SectionLabel("بيانات الشراء"),
            FieldLine("سعر الشراء", FormatMoney(transaction.PurchasePrice)),
            FieldLine("طريقة الدفع", GetPaymentModeLabel(transaction.PaymentMode)),
            FieldLine("المبلغ المدفوع للبائع", FormatMoney(transaction.AmountPaid)),
            FieldLine("المبلغ المتبقي للبائع", FormatMoney(transaction.RemainingAmount))
        ]);
        return WrapAmountBlock(block);
    }

    private static Block BuildSaleAmountsBlock(CarTradeTransaction transaction)
    {
        var block = CreateStackedFieldBlock([
            SectionLabel("بيانات البيع"),
            FieldLine("سعر الشراء (تكلفة)", FormatMoney(transaction.PurchasePrice)),
            FieldLine("سعر البيع", FormatMoney(transaction.SalePrice)),
            FieldLine("تاريخ البيع", (transaction.SaleDate ?? transaction.TransactionDate).ToString("yyyy/MM/dd", ArabicCulture)),
            FieldLine("طريقة الدفع من المشتري", GetPaymentModeLabel(transaction.SalePaymentMode)),
            FieldLine("المبلغ المدفوع من المشتري", FormatMoney(transaction.SaleAmountPaid)),
            FieldLine("المبلغ المتبقي على المشتري", FormatMoney(transaction.SaleRemainingAmount))
        ]);
        return WrapAmountBlock(block);
    }

    private static Block WrapAmountBlock(Section block)
    {
        var cell = new TableCell(block)
        {
            Background = LightBg,
            BorderBrush = BorderBrush,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(12, 10, 12, 10)
        };

        var table = CreateTable(1, 1);
        table.RowGroups[0].Rows.Add(new TableRow { Cells = { cell } });
        return WrapBlock(table, new Thickness(0, 0, 0, 10));
    }

    private static Block BuildNotesBlock(string notes, Brush headerBg)
    {
        var paragraph = new Paragraph(new Run(notes.Trim()))
        {
            Margin = new Thickness(0),
            TextAlignment = TextAlignment.Right
        };

        var cell = new TableCell(paragraph)
        {
            Background = LightBg,
            BorderBrush = BorderBrush,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(12, 10, 12, 10)
        };

        var table = CreateTable(1, 2);
        var headerRow = new TableRow();
        headerRow.Cells.Add(new TableCell(SectionLabel("ملاحظات"))
        {
            Background = headerBg,
            BorderBrush = BorderBrush,
            BorderThickness = new Thickness(1, 1, 1, 0),
            Padding = new Thickness(8, 6, 8, 6)
        });
        table.RowGroups[0].Rows.Add(headerRow);
        table.RowGroups[0].Rows.Add(new TableRow { Cells = { cell } });
        return WrapBlock(table, new Thickness(0, 0, 0, 10));
    }

    private static Block BuildSignaturesBlock(string left, string right)
    {
        var table = CreateTable(2, 1, [1, 1]);
        var row = new TableRow();
        row.Cells.Add(SignatureCell(left));
        row.Cells.Add(SignatureCell(right));
        table.RowGroups[0].Rows.Add(row);
        return WrapBlock(table, new Thickness(0, 16, 0, 0));
    }

    private static TableCell SignatureCell(string label) => new(new Paragraph(new Run(label))
    {
        FontWeight = FontWeights.SemiBold,
        TextAlignment = TextAlignment.Center,
        Margin = new Thickness(0, 48, 0, 0)
    })
    {
        BorderBrush = BorderBrush,
        BorderThickness = new Thickness(0, 1, 0, 0),
        Padding = new Thickness(8, 4, 8, 4)
    };

    private static void AddRow(Table table, int rowIndex, string label1, string value1, string label2, string value2)
    {
        var row = table.RowGroups[0].Rows[rowIndex];
        row.Cells.Add(DataCell(FieldLine(label1, value1)));
        row.Cells.Add(DataCell(FieldLine(label2, value2)));
    }

    private static TableCell DataCell(Block content) => new(content)
    {
        Background = LightBg,
        BorderBrush = BorderBrush,
        BorderThickness = new Thickness(1),
        Padding = new Thickness(8, 6, 8, 6)
    };

    private static Table CreateTable(int columns, int rows, double[]? starWidths = null)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0) };
        for (var i = 0; i < columns; i++)
        {
            var width = starWidths is not null && i < starWidths.Length
                ? new GridLength(starWidths[i], GridUnitType.Star)
                : new GridLength(1, GridUnitType.Star);
            table.Columns.Add(new TableColumn { Width = width });
        }

        var group = new TableRowGroup();
        for (var r = 0; r < rows; r++)
            group.Rows.Add(new TableRow());
        table.RowGroups.Add(group);
        return table;
    }

    private static Block CreatePartyBox(string title, IEnumerable<Block> fieldBlocks, Brush headerBg)
    {
        var section = new Section();
        var headerTable = CreateTable(1, 1);
        var headerRow = new TableRow();
        headerRow.Cells.Add(new TableCell(new Paragraph(new Run(title))
        {
            FontWeight = FontWeights.Bold,
            FontSize = 13,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 2, 0, 2)
        })
        {
            Background = headerBg,
            BorderBrush = BorderBrush,
            BorderThickness = new Thickness(1, 1, 1, 0),
            Padding = new Thickness(4, 6, 4, 6)
        });
        headerTable.RowGroups[0].Rows.Add(headerRow);
        section.Blocks.Add(headerTable);

        var bodyTable = CreateTable(1, 1);
        var bodyRow = new TableRow();
        var bodySection = new Section { Margin = new Thickness(0) };
        foreach (var block in fieldBlocks)
            bodySection.Blocks.Add(block);

        bodyRow.Cells.Add(new TableCell(bodySection)
        {
            Background = LightBg,
            BorderBrush = BorderBrush,
            BorderThickness = new Thickness(1, 0, 1, 1),
            Padding = new Thickness(10, 8, 10, 10)
        });
        bodyTable.RowGroups[0].Rows.Add(bodyRow);
        section.Blocks.Add(bodyTable);
        return section;
    }

    private static Section CreateStackedFieldBlock(IEnumerable<Block> lines)
    {
        var section = new Section { Margin = new Thickness(0) };
        foreach (var line in lines)
            section.Blocks.Add(line);
        return section;
    }

    private static Paragraph SectionLabel(string text) => new(new Run(text))
    {
        FontWeight = FontWeights.Bold,
        FontSize = 12,
        TextAlignment = TextAlignment.Center,
        Margin = new Thickness(0, 0, 0, 6)
    };

    private static Paragraph FieldLine(string label, string? value, bool boldValue = false, bool centerValue = false)
    {
        var display = string.IsNullOrWhiteSpace(value) ? "........................" : value.Trim();
        var paragraph = new Paragraph { Margin = new Thickness(0, 0, 0, 5), LineHeight = 18 };

        if (!string.IsNullOrWhiteSpace(label))
            paragraph.Inlines.Add(new Run($"{label} : ") { FontWeight = FontWeights.SemiBold });

        paragraph.Inlines.Add(new Run(display)
        {
            FontWeight = boldValue ? FontWeights.Bold : FontWeights.Normal,
            FontSize = boldValue ? 13 : 12
        });

        if (centerValue)
            paragraph.TextAlignment = TextAlignment.Center;

        return paragraph;
    }

    private static TableCell WrapCell(Block content, Thickness padding) => new(content)
    {
        Padding = padding
    };

    private static Block WrapBlock(Block content, Thickness margin)
    {
        content.Margin = margin;
        return content;
    }

    private static string FormatMoney(decimal amount) =>
        amount.ToString("N0", ArabicCulture) + " د.ع";

    private static string GetPaymentModeLabel(CarTradePaymentMode mode) => mode switch
    {
        CarTradePaymentMode.FullCash => "نقدي",
        _ => "آجل"
    };

    private static string GetPaymentPartyName(CarTradeTransaction transaction, CarTradePayment payment) =>
        payment.PaymentKind == CarTradePaymentKind.Sale
            ? transaction.BuyerName
            : transaction.SellerName;
}
