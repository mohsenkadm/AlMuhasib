using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Print;
using AlMuhasib.Shared.Services;

namespace AlMuhasib.UI.Services;

/// <summary>
/// طباعة عقد بيع وشراء لمعرض السيارات من فاتورة المبيعات —
/// الهيدر والفوتر من إعدادات الطباعة، والمحتوى وفق نموذج العقد المعتمد.
/// </summary>
public sealed class ShowroomSaleContractPrintService : IShowroomSaleContractPrintService
{
    private static readonly CultureInfo ArabicCulture = CultureInfo.GetCultureInfo("ar-IQ");
    private static readonly Brush BorderBrush = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33));
    private static readonly Brush HeaderBg = new SolidColorBrush(Color.FromRgb(0xEE, 0xEE, 0xEE));
    private static readonly Brush LightBg = new SolidColorBrush(Color.FromRgb(0xFA, 0xFA, 0xFA));
    private static readonly Brush WarningBg = new SolidColorBrush(Color.FromRgb(0xE3, 0xF2, 0xFD));
    private static readonly Brush WarningFg = new SolidColorBrush(Color.FromRgb(0x0D, 0x47, 0xA1));

    private static readonly string[] ContractClauses =
    [
        "يتحمل الطرف الثاني كافة المسؤوليات القانونية المترتبة على السيارة من تاريخ توقيع هذا العقد وما بعده.",
        "يتحمل الطرف الأول كافة الغرامات والحجوزات والمخالفات المترتبة على السيارة قبل تاريخ توقيع هذا العقد.",
        "يلتزم الطرف الثاني بنقل ملكية السيارة لدى الدوائر المختصة خلال مدة أقصاها تسعون يوماً من تاريخ العقد، وكل عقد غير مختوم بختم المعرض يعتبر باطلاً."
    ];

    public void PrintContract(ShowroomSaleContractPrintModel model, int copies = 1)
    {
        var document = BuildFlowDocument(model);
        DocumentPrintHelper.PrintWithPreview(document, $"عقد بيع وشراء {model.ContractNumber}", defaultCopies: copies);
    }

    private static FlowDocument BuildFlowDocument(ShowroomSaleContractPrintModel model)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
            FontSize = 12,
            FlowDirection = FlowDirection.RightToLeft,
            PagePadding = new Thickness(28, 12, 28, 18)
        };

        PrintBrandingFlowDocumentHelper.PrependBrandingHeader(doc);

        doc.Blocks.Add(BuildTitleBlock(model));
        doc.Blocks.Add(BuildPartiesBlock(model));
        doc.Blocks.Add(BuildContractBodyBlock(model));
        doc.Blocks.Add(BuildSignaturesBlock(model));
        doc.Blocks.Add(BuildStampWarningBlock());

        PrintBrandingFlowDocumentHelper.AppendBrandingFooter(doc);

        return doc;
    }

    private static Block BuildTitleBlock(ShowroomSaleContractPrintModel model)
    {
        var table = CreateTable(2, [3.5, 1.5]);
        var row = new TableRow();

        row.Cells.Add(new TableCell(new Paragraph(new Run("عقد بيع وشراء"))
        {
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 4, 0, 4)
        })
        {
            BorderBrush = BorderBrush,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8, 8, 8, 8),
            TextAlignment = TextAlignment.Center
        });

        var numberSection = new Section();
        numberSection.Blocks.Add(new Paragraph(new Run("الرقم"))
        {
            FontSize = 11,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 2)
        });
        numberSection.Blocks.Add(new Paragraph(new Run(OrDots(model.ContractNumber)))
        {
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0)
        });

        row.Cells.Add(new TableCell(numberSection)
        {
            BorderBrush = BorderBrush,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(6, 6, 6, 6),
            Background = LightBg
        });

        table.RowGroups[0].Rows.Add(row);
        table.Margin = new Thickness(0, 6, 0, 10);
        return table;
    }

    private static Block BuildPartiesBlock(ShowroomSaleContractPrintModel model)
    {
        var table = CreateTable(2, [1, 1]);
        var row = new TableRow();

        row.Cells.Add(WrapPartyCell("الطرف الأول", [
            Field("البائع", model.SellerName),
            Field("رقم الموبايل", model.SellerPhone),
            Field("رقم الهوية", model.SellerIdNumber),
            Field("جهة الإصدار", model.SellerIdIssuer),
            Field("العنوان", model.SellerAddress),
            Field("رقم السنوية", model.AnnualRegistrationNote)
        ]));

        row.Cells.Add(WrapPartyCell("الطرف الثاني", [
            Field("المشتري", model.BuyerName),
            Field("رقم الموبايل", model.BuyerPhone),
            Field("رقم الهوية", model.BuyerIdNumber),
            Field("جهة الإصدار", model.BuyerIdIssuer),
            Field("العنوان", model.BuyerAddress)
        ]));

        table.RowGroups[0].Rows.Add(row);
        table.Margin = new Thickness(0, 0, 0, 10);
        return table;
    }

    private static TableCell WrapPartyCell(string title, IEnumerable<Paragraph> fields)
    {
        var section = new Section { Margin = new Thickness(0) };
        section.Blocks.Add(new Paragraph(new Run(title))
        {
            FontWeight = FontWeights.Bold,
            FontSize = 13.5,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 6)
        });
        foreach (var field in fields)
            section.Blocks.Add(field);

        return new TableCell(section)
        {
            BorderBrush = BorderBrush,
            BorderThickness = new Thickness(1),
            Background = LightBg,
            Padding = new Thickness(10, 8, 10, 8)
        };
    }

    private static Block BuildContractBodyBlock(ShowroomSaleContractPrintModel model)
    {
        var section = new Section { Margin = new Thickness(0) };

        var header = new Paragraph(new Run("العقد"))
        {
            FontWeight = FontWeights.Bold,
            FontSize = 14,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8)
        };

        var saleLine = new Paragraph
        {
            FontSize = 12.2,
            TextAlignment = TextAlignment.Justify,
            LineHeight = 22,
            Margin = new Thickness(0, 0, 0, 8)
        };
        saleLine.Inlines.Add(new Run("1- باع الطرف الأول للطرف الثاني السيارة المرقمة ") { FontWeight = FontWeights.SemiBold });
        saleLine.Inlines.Add(BoldValue(model.PlateNumber));
        if (!string.IsNullOrWhiteSpace(model.PlateType))
        {
            saleLine.Inlines.Add(new Run(" (نوع اللوحة: "));
            saleLine.Inlines.Add(BoldValue(model.PlateType));
            saleLine.Inlines.Add(new Run(")"));
        }

        saleLine.Inlines.Add(new Run(" رقم الشاصي "));
        saleLine.Inlines.Add(BoldValue(model.ChassisNumber));
        saleLine.Inlines.Add(new Run(" الموديل "));
        saleLine.Inlines.Add(BoldValue(model.VehicleModel));
        saleLine.Inlines.Add(new Run(" اللون "));
        saleLine.Inlines.Add(BoldValue(model.VehicleColor));
        saleLine.Inlines.Add(new Run(" النوع "));
        saleLine.Inlines.Add(BoldValue(model.VehicleType));
        saleLine.Inlines.Add(new Run(" الحجم "));
        saleLine.Inlines.Add(BoldValue(model.VehicleSize));
        saleLine.Inlines.Add(new Run(" بمبلغ قدره رقماً "));
        saleLine.Inlines.Add(BoldValue(FormatMoney(model.TotalAmount)));
        saleLine.Inlines.Add(new Run(" كتابة "));
        saleLine.Inlines.Add(BoldValue(OrDots(model.TotalAmountInWords)));
        saleLine.Inlines.Add(new Run(" وقد قبض البائع "));
        saleLine.Inlines.Add(BoldValue(FormatMoney(model.PaidAmount)));
        saleLine.Inlines.Add(new Run(" والباقي "));
        saleLine.Inlines.Add(BoldValue(FormatMoney(model.RemainingAmount)));
        if (model.DueDate.HasValue)
        {
            saleLine.Inlines.Add(new Run(" تاريخ الاستحقاق "));
            saleLine.Inlines.Add(BoldValue(model.DueDate.Value.ToString("yyyy/MM/dd", ArabicCulture)));
        }

        saleLine.Inlines.Add(new Run("."));

        var bodyCellContent = new Section();
        bodyCellContent.Blocks.Add(header);
        bodyCellContent.Blocks.Add(saleLine);

        for (var i = 0; i < ContractClauses.Length; i++)
        {
            bodyCellContent.Blocks.Add(new Paragraph
            {
                FontSize = 11.8,
                TextAlignment = TextAlignment.Justify,
                LineHeight = 20,
                Margin = new Thickness(0, 0, 0, 6),
                Inlines =
                {
                    new Run($"{i + 2}- ") { FontWeight = FontWeights.Bold },
                    new Run(ContractClauses[i])
                }
            });
        }

        var city = string.IsNullOrWhiteSpace(model.City) ? "........" : model.City.Trim();
        var written = new Paragraph
        {
            FontSize = 12,
            Margin = new Thickness(0, 10, 0, 0),
            TextAlignment = TextAlignment.Right
        };
        written.Inlines.Add(new Run("كتبت في "));
        written.Inlines.Add(BoldValue(city));
        written.Inlines.Add(new Run(" بتاريخ "));
        written.Inlines.Add(BoldValue(model.ContractDate.ToString("yyyy/MM/dd", ArabicCulture)));
        written.Inlines.Add(new Run(" الساعة "));
        written.Inlines.Add(BoldValue(model.ContractDate.ToString("HH:mm", ArabicCulture)));
        bodyCellContent.Blocks.Add(written);

        var table = CreateTable(1, [1]);
        var row = new TableRow();
        row.Cells.Add(new TableCell(bodyCellContent)
        {
            BorderBrush = BorderBrush,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(12, 10, 12, 10),
            Background = LightBg
        });
        table.RowGroups[0].Rows.Add(row);
        table.Margin = new Thickness(0, 0, 0, 12);
        return table;
    }

    private static Block BuildSignaturesBlock(ShowroomSaleContractPrintModel model)
    {
        var table = CreateTable(2, [1, 1]);
        var row = new TableRow();
        row.Cells.Add(SignatureCell("الطرف الأول البائع", model.SellerName));
        row.Cells.Add(SignatureCell("الطرف الثاني المشتري", model.BuyerName));
        table.RowGroups[0].Rows.Add(row);
        table.Margin = new Thickness(0, 8, 0, 10);
        return table;
    }

    private static TableCell SignatureCell(string label, string name)
    {
        var panel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        panel.Children.Add(new TextBlock
        {
            Text = label,
            FontWeight = FontWeights.SemiBold,
            FontSize = 12,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8)
        });
        panel.Children.Add(new TextBlock
        {
            Text = OrDots(name, 22),
            FontSize = 12,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 28)
        });
        panel.Children.Add(new Border
        {
            BorderBrush = BorderBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Width = 160,
            Height = 1
        });

        return new TableCell(new BlockUIContainer(panel))
        {
            BorderBrush = BorderBrush,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8, 12, 8, 12),
            TextAlignment = TextAlignment.Center
        };
    }

    private static Block BuildStampWarningBlock()
    {
        var paragraph = new Paragraph(new Run("أي عقد غير مختوم بختم المعرض يعتبر باطل"))
        {
            FontWeight = FontWeights.Bold,
            FontSize = 12.5,
            TextAlignment = TextAlignment.Center,
            Foreground = WarningFg,
            Margin = new Thickness(0, 4, 0, 4)
        };

        var table = CreateTable(1, [1]);
        var row = new TableRow();
        row.Cells.Add(new TableCell(paragraph)
        {
            BorderBrush = WarningFg,
            BorderThickness = new Thickness(1.5),
            Background = WarningBg,
            Padding = new Thickness(10, 8, 10, 8)
        });
        table.RowGroups[0].Rows.Add(row);
        table.Margin = new Thickness(0, 4, 0, 8);
        return table;
    }

    private static Table CreateTable(int columns, double[] starWidths)
    {
        var table = new Table { CellSpacing = 4, Margin = new Thickness(0) };
        for (var i = 0; i < columns; i++)
        {
            var w = i < starWidths.Length ? starWidths[i] : 1;
            table.Columns.Add(new TableColumn { Width = new GridLength(w, GridUnitType.Star) });
        }

        table.RowGroups.Add(new TableRowGroup());
        return table;
    }

    private static Paragraph Field(string label, string? value)
    {
        var p = new Paragraph { Margin = new Thickness(0, 0, 0, 3), LineHeight = 18 };
        p.Inlines.Add(new Run($"{label} : ") { FontWeight = FontWeights.SemiBold, FontSize = 11.5 });
        p.Inlines.Add(new Run(OrDots(value)) { FontSize = 11.5 });
        return p;
    }

    private static Run BoldValue(string? value) => new(OrDots(value))
    {
        FontWeight = FontWeights.Bold,
        FontSize = 12.5
    };

    private static string FormatMoney(decimal amount) =>
        $"{amount.ToString("N0", ArabicCulture)} دينار";

    private static string OrDots(string? value, int count = 18) =>
        string.IsNullOrWhiteSpace(value) ? new string('.', count) : value.Trim();
}
