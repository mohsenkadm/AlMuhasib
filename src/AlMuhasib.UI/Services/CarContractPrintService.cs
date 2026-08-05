using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AlMuhasib.Core.Entities.Car;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Shared.Services;

namespace AlMuhasib.UI.Services;

public sealed class CarContractPrintService : ICarContractPrintService
{
    private static readonly CultureInfo ArabicCulture = CultureInfo.GetCultureInfo("ar-IQ");
    private static readonly Brush BorderBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44));
    private static readonly Brush HeaderBg = new SolidColorBrush(Color.FromRgb(0xD9, 0xD9, 0xD9));
    private static readonly Brush LightBg = new SolidColorBrush(Color.FromRgb(0xF7, 0xF7, 0xF7));
    private static readonly Brush TermsTitleBg = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xE8));

    public void PrintContract(CarSaleContract contract, int copies = 5)
    {
        var document = BuildFlowDocument(contract);
        DocumentPrintHelper.PrintWithPreview(document, $"عقد {contract.ContractNumber}", defaultCopies: copies);
    }

    public void PrintContracts(IEnumerable<CarSaleContract> contracts, int copiesEach = 1)
    {
        foreach (var contract in contracts)
            PrintContract(contract, copiesEach);
    }

    private static FlowDocument BuildFlowDocument(CarSaleContract contract)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
            FontSize = 12,
            FlowDirection = FlowDirection.RightToLeft,
            PagePadding = new Thickness(26, 10, 26, 22)
        };

        // هيدر طبيعي بالحجم الكامل (بدون ضغط) لملء صفحة A4 بشكل متوازن
        PrintBrandingFlowDocumentHelper.PrependBrandingHeader(doc);

        doc.Blocks.Add(BuildTitleRow(contract));
        doc.Blocks.Add(BuildPartiesRow(contract));
        doc.Blocks.Add(BuildDetailsRow(contract));
        doc.Blocks.Add(BuildNotesRow(contract));
        doc.Blocks.Add(BuildTermsBlock());
        doc.Blocks.Add(BuildSignaturesRow(contract));

        return doc;
    }

    private static Block BuildTitleRow(CarSaleContract contract)
    {
        var table = CreateTable(3, 1, [2.2, 3.6, 2.2]);
        var row = new TableRow();

        var dayName = contract.ContractDate.ToString("dddd", ArabicCulture);
        var dayCell = new TableCell(CreateStackedFieldBlock([
            FieldLine("اليوم", dayName),
            FieldLine("الساعة", DateTime.Now.ToString("HH:mm", ArabicCulture))
        ]))
        {
            BorderBrush = BorderBrush,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(4, 4, 4, 4)
        };

        var titleCell = new TableCell(new Paragraph(new Run("عقد بيع وشراء"))
        {
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 6, 0, 6)
        })
        {
            BorderBrush = BorderBrush,
            BorderThickness = new Thickness(0),
            TextAlignment = TextAlignment.Center
        };

        var dateCell = new TableCell(CreateStackedFieldBlock([
            FieldLine("التاريخ", FormatContractDate(contract.ContractDate)),
            FieldLine(string.Empty, contract.ContractNumber, boldValue: true, centerValue: true)
        ]))
        {
            BorderBrush = BorderBrush,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(4, 4, 4, 4),
            TextAlignment = TextAlignment.Left
        };

        row.Cells.Add(dayCell);
        row.Cells.Add(titleCell);
        row.Cells.Add(dateCell);
        table.RowGroups[0].Rows.Add(row);

        return WrapBlock(table, new Thickness(0, 4, 0, 8));
    }

    private static Block BuildPartiesRow(CarSaleContract contract)
    {
        var table = CreateTable(2, 1, [1, 1]);

        var buyerBox = CreatePartyBox("المشتري", [
            FieldLine("المشتري", contract.BuyerName),
            FieldLine("العنوان", contract.BuyerAddress),
            FieldLine("رقم الهوية", contract.BuyerIdNumber),
            FieldLine("تاريخ الهوية", FormatOptionalDate(contract.BuyerIdDate)),
            FieldLine("الهاتف", contract.BuyerPhone)
        ]);

        var sellerBox = CreatePartyBox("البائع", [
            FieldLine("البائع", contract.SellerName),
            FieldLine("العنوان", contract.SellerAddress),
            FieldLine("رقم الهوية", contract.SellerIdNumber),
            FieldLine("تاريخ الهوية", FormatOptionalDate(contract.SellerIdDate)),
            FieldLine("الهاتف", contract.SellerPhone)
        ]);

        var row = new TableRow();
        row.Cells.Add(WrapCell(sellerBox, padding: new Thickness(0, 0, 4, 0)));
        row.Cells.Add(WrapCell(buyerBox, padding: new Thickness(4, 0, 0, 0)));
        table.RowGroups[0].Rows.Add(row);

        return WrapBlock(table, new Thickness(0, 0, 0, 8));
    }

    private static Block BuildDetailsRow(CarSaleContract contract)
    {
        var table = CreateTable(2, 1, [1, 1]);

        var carBlock = CreateStackedFieldBlock([
            SectionLabel("صاحب السنوية"),
            FieldLine("الاسم", contract.AnnualOwnerName),
            FieldLine("رقم السيارة", contract.PlateNumber),
            FieldLine("نوع السيارة", contract.CarType),
            FieldLine("الموديل", contract.CarModel),
            FieldLine("اللون", contract.CarColor),
            FieldLine("رقم الشاصي", contract.ChassisNumber)
        ]);

        var moneyBlock = CreateStackedFieldBlock([
            SectionLabel("العنوان"),
            FieldLine("العنوان", contract.AnnualOwnerAddress),
            FieldLine("سعر السيارة", FormatContractPrice(contract)),
            FieldLine("السعر كتابة", FormatContractPriceInWords(contract)),
            FieldLine("المبلغ الواصل", FormatMoney(contract.AmountReceived)),
            FieldLine("المتبقي", FormatContractRemaining(contract))
        ]);

        var row = new TableRow();
        row.Cells.Add(WrapCell(carBlock, bordered: true, padding: new Thickness(10, 6, 12, 6)));
        row.Cells.Add(WrapCell(moneyBlock, bordered: true, padding: new Thickness(12, 6, 10, 6)));
        table.RowGroups[0].Rows.Add(row);

        return WrapBlock(table, new Thickness(0, 0, 0, 8));
    }

    private static Block BuildNotesRow(CarSaleContract contract)
    {
        var notes = string.IsNullOrWhiteSpace(contract.Notes) ? Dots(48) : contract.Notes.Trim();
        var paragraph = FieldLine("الملاحظات", notes, fullWidth: true);
        paragraph.Margin = new Thickness(0, 2, 0, 6);
        paragraph.FontSize = 12;
        return paragraph;
    }

    private static Block BuildTermsBlock()
    {
        var body = new Section
        {
            Margin = new Thickness(0),
            FlowDirection = FlowDirection.RightToLeft
        };

        for (var i = 0; i < CarContractPrintTerms.Clauses.Length; i++)
        {
            var paragraph = new Paragraph
            {
                FontSize = 11.5,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Justify,
                FlowDirection = FlowDirection.RightToLeft,
                LineHeight = 18,
                Margin = new Thickness(0, 0, 0, 5)
            };
            paragraph.Inlines.Add(new Run($"{i + 1}. ")
            {
                FontWeight = FontWeights.ExtraBold,
                FontSize = 12
            });
            paragraph.Inlines.Add(new Run(CarContractPrintTerms.Clauses[i])
            {
                FontWeight = FontWeights.Bold
            });
            body.Blocks.Add(paragraph);
        }

        var titleCell = new TableCell(new Paragraph(new Run(CarContractPrintTerms.Title))
        {
            FontWeight = FontWeights.ExtraBold,
            FontSize = 14,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 3, 0, 3)
        })
        {
            Background = TermsTitleBg,
            BorderBrush = BorderBrush,
            BorderThickness = new Thickness(1, 1, 1, 0),
            Padding = new Thickness(6, 6, 6, 6)
        };

        var bodyCell = new TableCell(body)
        {
            BorderBrush = BorderBrush,
            BorderThickness = new Thickness(1),
            Background = LightBg,
            Padding = new Thickness(10, 8, 10, 8)
        };

        var table = CreateTable(1, 0);
        var titleRow = new TableRow();
        titleRow.Cells.Add(titleCell);
        var bodyRow = new TableRow();
        bodyRow.Cells.Add(bodyCell);
        table.RowGroups[0].Rows.Add(titleRow);
        table.RowGroups[0].Rows.Add(bodyRow);

        return WrapBlock(table, new Thickness(0, 8, 0, 0));
    }

    private static Block BuildSignaturesRow(CarSaleContract contract)
    {
        var table = CreateTable(4, 1, [1, 1, 1, 1]);
        var labels = new[]
        {
            "توقيع الطرف الأول البائع",
            "الشاهد",
            "الشاهد",
            "توقيع الطرف الثاني المشتري"
        };
        var names = new[]
        {
            contract.SellerName,
            contract.WitnessOneName,
            contract.WitnessTwoName,
            contract.BuyerName
        };

        var row = new TableRow();
        for (var i = 0; i < labels.Length; i++)
        {
            row.Cells.Add(new TableCell(new BlockUIContainer(CreateSignatureBlock(labels[i], names[i])))
            {
                BorderBrush = BorderBrush,
                BorderThickness = new Thickness(0.5),
                Padding = new Thickness(4, 8, 4, 8),
                TextAlignment = TextAlignment.Center
            });
        }

        table.RowGroups[0].Rows.Add(row);
        // مسافة كافية لدفع التوقيعات نحو أسفل صفحة A4
        return WrapBlock(table, new Thickness(0, 28, 0, 0));
    }

    private static UIElement CreateSignatureBlock(string label, string? name)
    {
        var panel = new System.Windows.Controls.StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };
        panel.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = label,
            FontWeight = FontWeights.SemiBold,
            FontSize = 11.5,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 6)
        });
        panel.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = string.IsNullOrWhiteSpace(name) ? Dots(18) : name.Trim(),
            FontSize = 11.5,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 22)
        });
        panel.Children.Add(new System.Windows.Controls.Border
        {
            BorderBrush = BorderBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Height = 1,
            Width = 120,
            Margin = new Thickness(0, 0, 0, 2)
        });
        return panel;
    }

    private static Table CreateTable(int columns, int rows, double[]? starWidths = null)
    {
        var table = new Table
        {
            CellSpacing = 0,
            Margin = new Thickness(0)
        };

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

    private static Block CreatePartyBox(string title, IEnumerable<Block> fieldBlocks)
    {
        var section = new Section();

        var headerTable = CreateTable(1, 1);
        var headerRow = new TableRow();
        headerRow.Cells.Add(new TableCell(new Paragraph(new Run(title))
        {
            FontWeight = FontWeights.Bold,
            FontSize = 13.5,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 2, 0, 2)
        })
        {
            Background = HeaderBg,
            BorderBrush = BorderBrush,
            BorderThickness = new Thickness(1, 1, 1, 0),
            Padding = new Thickness(3, 6, 3, 6)
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
            Padding = new Thickness(8, 6, 8, 8)
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
        FontSize = 12.5,
        TextAlignment = TextAlignment.Center,
        Margin = new Thickness(0, 0, 0, 4)
    };

    private static Paragraph FieldLine(
        string label,
        string? value,
        bool boldValue = false,
        bool centerValue = false,
        bool fullWidth = false)
    {
        var display = string.IsNullOrWhiteSpace(value) ? Dots(fullWidth ? 64 : 28) : value.Trim();
        var paragraph = new Paragraph { Margin = new Thickness(0, 0, 0, 3), LineHeight = 17 };

        if (!string.IsNullOrWhiteSpace(label))
        {
            paragraph.Inlines.Add(new Run($"{label} : ")
            {
                FontWeight = FontWeights.SemiBold,
                FontSize = 12
            });
        }

        paragraph.Inlines.Add(new Run(display)
        {
            FontWeight = boldValue ? FontWeights.Bold : FontWeights.Normal,
            FontSize = boldValue ? 13.5 : 12
        });

        if (centerValue)
            paragraph.TextAlignment = TextAlignment.Center;

        return paragraph;
    }

    private static TableCell WrapCell(
        Block content,
        bool bordered = false,
        Thickness? padding = null)
    {
        var cell = content is Section section
            ? new TableCell(section)
            : new TableCell(content);

        if (bordered)
        {
            cell.BorderBrush = BorderBrush;
            cell.BorderThickness = new Thickness(1);
            cell.Background = LightBg;
        }

        cell.Padding = padding ?? new Thickness(0);
        return cell;
    }

    private static Block WrapBlock(Table table, Thickness margin)
    {
        table.Margin = margin;
        return table;
    }

    private static string FormatContractDate(DateTime date) =>
        $"{date.Day} / {date.Month} / {date.Year}";

    private static string FormatOptionalDate(DateTime? date) =>
        date.HasValue ? date.Value.ToString("yyyy/MM/dd", ArabicCulture) : string.Empty;

    private static string FormatMoney(decimal amount) =>
        $"{amount.ToString("N0", ArabicCulture)} دولار";

    private static string FormatContractPrice(CarSaleContract contract) =>
        contract.IsAgreedPrice ? "المبلغ المتفق عليه" : FormatMoney(contract.CarPrice);

    private static string FormatContractPriceInWords(CarSaleContract contract) =>
        contract.IsAgreedPrice ? "المبلغ المتفق عليه" : contract.CarPriceInWords;

    private static string FormatContractRemaining(CarSaleContract contract) =>
        contract.IsAgreedPrice ? "المبلغ المتفق عليه" : FormatMoney(contract.RemainingAmount);

    private static string Dots(int count) => new('.', count);
}
