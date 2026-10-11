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
/// الهيدر/الفوتر من إعدادات الطباعة بنفس آلية فاتورة المبيعات.
/// </summary>
public sealed class ShowroomSaleContractPrintService : IShowroomSaleContractPrintService
{
    private static readonly CultureInfo ArabicCulture = CultureInfo.GetCultureInfo("ar-IQ");

    // A4 @ 96 DPI
    private const double A4Width = 793.7;
    private const double A4Height = 1122.5;

    private static readonly Brush PrimaryBlue = new SolidColorBrush(Color.FromRgb(0x1E, 0x5B, 0xA8));
    private static readonly Brush SectionBorder = new SolidColorBrush(Color.FromRgb(0x6B, 0xA3, 0xD6));
    private static readonly Brush ContractHeader = new SolidColorBrush(Color.FromRgb(0x5A, 0x5A, 0x5A));
    private static readonly Brush TitleGold = new SolidColorBrush(Color.FromRgb(0xC9, 0xA2, 0x27));
    private static readonly Brush LabelBlue = new SolidColorBrush(Color.FromRgb(0x1E, 0x5B, 0xA8));
    private static readonly Brush FooterBar = new SolidColorBrush(Color.FromRgb(0x0D, 0x3B, 0x7A));
    private static readonly Brush FieldLine = new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99));
    private static readonly Brush StampHint = new SolidColorBrush(Color.FromRgb(0x90, 0xA4, 0xC0));
    private static readonly Brush FilledValue = new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28));

    private static readonly string[] ContractClauses =
    [
        "يتحمل الطرف الثاني كافة المسؤوليات القانونية المترتبة على السيارة من تاريخ توقيع هذا العقد وما بعده.",
        "يتحمل الطرف الأول كافة الغرامات والحجوزات والمخالفات المترتبة على السيارة قبل تاريخ توقيع هذا العقد.",
        "يلتزم الطرف الثاني بنقل ملكية السيارة لدى الدوائر المختصة خلال مدة أقصاها تسعون يوماً من تاريخ العقد، وكل عقد غير مختوم بختم المعرض يعتبر باطلاً."
    ];

    public void PrintContract(ShowroomSaleContractPrintModel model, int copies = 1)
    {
        var document = BuildFlowDocument(model);
        DocumentPrintHelper.PrintWithPreview(
            document,
            $"عقد بيع وشراء {model.ContractNumber}",
            pageSize: new Size(A4Width, A4Height),
            defaultCopies: copies);
    }

    private static FlowDocument BuildFlowDocument(ShowroomSaleContractPrintModel model)
    {
        // نفس هوامش/آلية الهيدر المستخدمة في فاتورة المبيعات
        var pagePadding = new Thickness(44, 20, 44, 20);
        var contentWidth = A4Width - pagePadding.Left - pagePadding.Right;

        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
            FontSize = 12,
            FlowDirection = FlowDirection.RightToLeft,
            PageWidth = A4Width,
            PageHeight = A4Height,
            PagePadding = pagePadding,
            ColumnWidth = contentWidth
        };

        // بدون ضغط ارتفاع — نفس PrependBrandingHeader لفاتورة المبيعات
        PrintBrandingFlowDocumentHelper.PrependBrandingHeader(doc);

        var contractBody = BuildContractContent(model, contentWidth);
        doc.Blocks.Add(new BlockUIContainer(contractBody) { Margin = new Thickness(0, 4, 0, 0) });

        PrintBrandingFlowDocumentHelper.AppendBrandingFooter(doc);

        return doc;
    }

    private static FrameworkElement BuildContractContent(ShowroomSaleContractPrintModel model, double designWidth)
    {
        var root = new StackPanel
        {
            Width = designWidth,
            FlowDirection = FlowDirection.RightToLeft
        };

        root.Children.Add(BuildTitleBlock(model));
        root.Children.Add(BuildPartySection(
            "الطرف الأول",
            PrimaryBlue,
            rightFields:
            [
                ("البائع", model.SellerName),
                ("رقم الهوية", model.SellerIdNumber),
                ("العنوان", model.SellerAddress)
            ],
            leftFields:
            [
                ("رقم الموبايل", model.SellerPhone),
                ("جهة الإصدار", model.SellerIdIssuer)
            ]));
        root.Children.Add(BuildPartySection(
            "الطرف الثاني",
            PrimaryBlue,
            rightFields:
            [
                ("المشتري", model.BuyerName),
                ("رقم الهوية", model.BuyerIdNumber),
                ("العنوان", model.BuyerAddress)
            ],
            leftFields:
            [
                ("رقم الموبايل", model.BuyerPhone),
                ("جهة الإصدار", model.BuyerIdIssuer)
            ]));
        root.Children.Add(BuildContractBodyBlock(model));
        root.Children.Add(BuildSignaturesBlock(model));
        root.Children.Add(BuildStampWarningBlock());

        return root;
    }

    private static UIElement BuildTitleBlock(ShowroomSaleContractPrintModel model)
    {
        var root = new Grid { Margin = new Thickness(0, 0, 0, 6) };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3.2, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });

        var numberPanel = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(4, 0, 0, 0)
        };
        numberPanel.Children.Add(new TextBlock
        {
            Text = "الرقم",
            FontSize = 12,
            Foreground = LabelBlue,
            FontWeight = FontWeights.Bold,
            FlowDirection = FlowDirection.RightToLeft
        });
        numberPanel.Children.Add(new TextBlock
        {
            Text = OrDots(model.ContractNumber),
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            Foreground = PrimaryBlue,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = new Thickness(0, 1, 0, 0)
        });
        Grid.SetColumn(numberPanel, 0);
        root.Children.Add(numberPanel);

        var title = new TextBlock
        {
            Text = "عقد بيع وشراء",
            FontSize = 26,
            FontWeight = FontWeights.Bold,
            Foreground = TitleGold,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FlowDirection = FlowDirection.RightToLeft
        };
        Grid.SetColumn(title, 1);
        root.Children.Add(title);

        return root;
    }

    private static UIElement BuildPartySection(
        string title,
        Brush headerBrush,
        (string Label, string? Value)[] rightFields,
        (string Label, string? Value)[] leftFields)
    {
        var fieldsGrid = new Grid
        {
            Margin = new Thickness(12, 8, 12, 8),
            FlowDirection = FlowDirection.RightToLeft
        };
        fieldsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        fieldsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var rightPanel = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
        foreach (var (label, value) in rightFields)
            rightPanel.Children.Add(LabeledField(label, value, partyStyle: true));

        var leftPanel = new StackPanel { Margin = new Thickness(10, 0, 0, 0) };
        foreach (var (label, value) in leftFields)
            leftPanel.Children.Add(LabeledField(label, value, partyStyle: true));

        Grid.SetColumn(rightPanel, 0);
        Grid.SetColumn(leftPanel, 1);
        fieldsGrid.Children.Add(rightPanel);
        fieldsGrid.Children.Add(leftPanel);

        return SectionChrome(title, headerBrush, fieldsGrid, new Thickness(0, 0, 0, 8), headerFontSize: 15);
    }

    private static UIElement BuildContractBodyBlock(ShowroomSaleContractPrintModel model)
    {
        var content = new StackPanel { Margin = new Thickness(12, 10, 12, 10) };

        var saleLine = new TextBlock
        {
            FontSize = 13.5,
            TextAlignment = TextAlignment.Justify,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 24,
            Margin = new Thickness(0, 0, 0, 8),
            FlowDirection = FlowDirection.RightToLeft
        };
        saleLine.Inlines.Add(new Run("1- باع الطرف الأول للطرف الثاني السيارة "));
        saleLine.Inlines.Add(HighlightRun(model.VehicleName));
        saleLine.Inlines.Add(new Run(" المرقمة ") { FontWeight = FontWeights.SemiBold });
        saleLine.Inlines.Add(HighlightRun(model.PlateNumber));
        if (!string.IsNullOrWhiteSpace(model.PlateType))
        {
            saleLine.Inlines.Add(new Run(" (نوع اللوحة: "));
            saleLine.Inlines.Add(HighlightRun(model.PlateType));
            saleLine.Inlines.Add(new Run(")"));
        }

        saleLine.Inlines.Add(new Run(" صنف السيارة "));
        saleLine.Inlines.Add(HighlightRun(model.ChassisNumber));
        saleLine.Inlines.Add(new Run(" الموديل "));
        saleLine.Inlines.Add(HighlightRun(model.VehicleModel));
        saleLine.Inlines.Add(new Run(" اللون "));
        saleLine.Inlines.Add(HighlightRun(model.VehicleColor));
        saleLine.Inlines.Add(new Run(" النوع "));
        saleLine.Inlines.Add(HighlightRun(model.VehicleType));
        saleLine.Inlines.Add(new Run(" الحجم "));
        saleLine.Inlines.Add(HighlightRun(model.VehicleSize));
        saleLine.Inlines.Add(new Run(" بمبلغ قدره رقماً "));
        saleLine.Inlines.Add(HighlightText(FormatMoney(model.TotalAmount), filled: true));
        saleLine.Inlines.Add(new Run(" كتابة "));
        saleLine.Inlines.Add(HighlightRun(model.TotalAmountInWords));
        saleLine.Inlines.Add(new Run(" وقد قبض البائع "));
        saleLine.Inlines.Add(HighlightText(FormatMoney(model.PaidAmount), filled: true));
        saleLine.Inlines.Add(new Run(" والباقي "));
        saleLine.Inlines.Add(HighlightText(FormatMoney(model.RemainingAmount), filled: true));
        if (model.DueDate.HasValue)
        {
            saleLine.Inlines.Add(new Run(" تاريخ الاستحقاق "));
            saleLine.Inlines.Add(HighlightText(
                model.DueDate.Value.ToString("yyyy/MM/dd", ArabicCulture),
                filled: true));
        }

        saleLine.Inlines.Add(new Run("."));
        content.Children.Add(saleLine);

        for (var i = 0; i < ContractClauses.Length; i++)
        {
            var clause = new TextBlock
            {
                FontSize = 13.5,
                TextAlignment = TextAlignment.Justify,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 23,
                Margin = new Thickness(0, 0, 0, 6),
                FlowDirection = FlowDirection.RightToLeft
            };
            clause.Inlines.Add(new Run($"{i + 2}- ") { FontWeight = FontWeights.Bold, Foreground = LabelBlue });
            clause.Inlines.Add(new Run(ContractClauses[i]));
            content.Children.Add(clause);
        }

        var city = string.IsNullOrWhiteSpace(model.City) ? "........" : model.City.Trim();
        var written = new TextBlock
        {
            FontSize = 13,
            Margin = new Thickness(0, 8, 0, 0),
            TextAlignment = TextAlignment.Right,
            FlowDirection = FlowDirection.RightToLeft
        };
        written.Inlines.Add(new Run("كتبت في ") { Foreground = LabelBlue, FontWeight = FontWeights.SemiBold });
        written.Inlines.Add(HighlightText(city, filled: HasValue(model.City)));
        written.Inlines.Add(new Run(" بتاريخ ") { Foreground = LabelBlue, FontWeight = FontWeights.SemiBold });
        written.Inlines.Add(HighlightText(model.ContractDate.ToString("yyyy/MM/dd", ArabicCulture), filled: true));
        written.Inlines.Add(new Run(" الساعة ") { Foreground = LabelBlue, FontWeight = FontWeights.SemiBold });
        written.Inlines.Add(HighlightText(model.ContractDate.ToString("HH:mm", ArabicCulture), filled: true));
        content.Children.Add(written);

        return SectionChrome("العقد", ContractHeader, content, new Thickness(0, 0, 0, 8), headerFontSize: 15);
    }

    private static UIElement BuildSignaturesBlock(ShowroomSaleContractPrintModel model)
    {
        var grid = new Grid { Margin = new Thickness(2, 6, 2, 6), MinHeight = 88 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var seller = SignaturePanel("الطرف الأول البائع", model.SellerName);
        var stamp = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        stamp.Children.Add(new Border
        {
            Width = 78,
            Height = 78,
            CornerRadius = new CornerRadius(39),
            BorderBrush = StampHint,
            BorderThickness = new Thickness(1.5),
            Background = Brushes.Transparent,
            Child = new TextBlock
            {
                Text = "الختم",
                Foreground = StampHint,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center
            }
        });
        var buyer = SignaturePanel("الطرف الثاني المشتري", model.BuyerName);

        Grid.SetColumn(buyer, 0);
        Grid.SetColumn(stamp, 1);
        Grid.SetColumn(seller, 2);
        grid.Children.Add(buyer);
        grid.Children.Add(stamp);
        grid.Children.Add(seller);

        return grid;
    }

    private static StackPanel SignaturePanel(string label, string name)
    {
        var panel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(4, 0, 4, 0)
        };
        panel.Children.Add(new TextBlock
        {
            Text = label,
            FontWeight = FontWeights.Bold,
            FontSize = 13.5,
            Foreground = PrimaryBlue,
            TextAlignment = TextAlignment.Center,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = new Thickness(0, 0, 0, 4)
        });
        panel.Children.Add(new TextBlock
        {
            Text = OrDots(name, 22),
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = new Thickness(0, 0, 0, 22)
        });
        panel.Children.Add(new Border
        {
            BorderBrush = FieldLine,
            BorderThickness = new Thickness(0, 0, 0, 1.2),
            Width = 150,
            Height = 1,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        return panel;
    }

    private static UIElement BuildStampWarningBlock()
    {
        return new Border
        {
            Background = FooterBar,
            Padding = new Thickness(10, 8, 10, 8),
            CornerRadius = new CornerRadius(2),
            Margin = new Thickness(0, 4, 0, 0),
            Child = new TextBlock
            {
                Text = "أي عقد غير مختوم بختم المعرض يعتبر باطل",
                FontWeight = FontWeights.Bold,
                FontSize = 13.5,
                Foreground = Brushes.White,
                TextAlignment = TextAlignment.Center,
                FlowDirection = FlowDirection.RightToLeft
            }
        };
    }

    private static UIElement SectionChrome(
        string title,
        Brush headerBrush,
        UIElement content,
        Thickness margin,
        double headerFontSize = 13.5)
    {
        var stack = new StackPanel { Margin = margin };

        stack.Children.Add(new Border
        {
            Background = headerBrush,
            CornerRadius = new CornerRadius(14, 0, 0, 14),
            Padding = new Thickness(16, 5, 16, 5),
            HorizontalAlignment = HorizontalAlignment.Right,
            Child = new TextBlock
            {
                Text = title,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = headerFontSize,
                FlowDirection = FlowDirection.RightToLeft,
                TextAlignment = TextAlignment.Center
            }
        });

        stack.Children.Add(new Border
        {
            BorderBrush = SectionBorder,
            BorderThickness = new Thickness(1.5),
            Background = Brushes.White,
            Margin = new Thickness(0, -1, 0, 0),
            Child = content
        });

        return stack;
    }

    private static UIElement LabeledField(string label, string? value, bool partyStyle = false)
    {
        var fontSize = partyStyle ? 14 : 12.5;

        // RTL: التسمية يميناً ثم القيمة مباشرة بعدها، ثم خط يكمل الفراغ يساراً
        var panel = new Grid
        {
            Margin = new Thickness(0, 0, 0, 6),
            FlowDirection = FlowDirection.RightToLeft
        };
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var labelBlock = new TextBlock
        {
            Text = $"{label} : ",
            FontWeight = FontWeights.Bold,
            FontSize = fontSize,
            Foreground = LabelBlue,
            VerticalAlignment = VerticalAlignment.Bottom,
            FlowDirection = FlowDirection.RightToLeft
        };
        Grid.SetColumn(labelBlock, 0);

        var valueBlock = new TextBlock
        {
            Text = OrDots(value),
            FontSize = fontSize,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Thickness(2, 0, 2, 1)
        };

        var valueUnderline = new Border
        {
            BorderBrush = FieldLine,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = valueBlock,
            VerticalAlignment = VerticalAlignment.Bottom,
            MinWidth = 48,
            Margin = new Thickness(0, 0, 2, 0)
        };
        Grid.SetColumn(valueUnderline, 1);

        var fillerLine = new Border
        {
            BorderBrush = FieldLine,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Height = 1,
            Margin = new Thickness(0, 0, 0, 3),
            VerticalAlignment = VerticalAlignment.Bottom
        };
        Grid.SetColumn(fillerLine, 2);

        panel.Children.Add(labelBlock);
        panel.Children.Add(valueUnderline);
        panel.Children.Add(fillerLine);
        return panel;
    }

    private static Run HighlightRun(string? value)
    {
        var filled = HasValue(value);
        return HighlightText(OrDots(value), filled);
    }

    private static Run HighlightText(string text, bool filled) => new(text)
    {
        FontWeight = FontWeights.Bold,
        FontSize = 14,
        Foreground = filled ? FilledValue : Brushes.Black
    };

    private static bool HasValue(string? value) => !string.IsNullOrWhiteSpace(value);

    private static string FormatMoney(decimal amount) =>
        $"{amount.ToString("N0", ArabicCulture)} دينار";

    private static string OrDots(string? value, int count = 18) =>
        string.IsNullOrWhiteSpace(value) ? new string('.', count) : value.Trim();
}
