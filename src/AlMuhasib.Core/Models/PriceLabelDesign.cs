using AlMuhasib.Core.Helpers;

namespace AlMuhasib.Core.Models;

public enum PriceLabelPaper
{
    A4 = 0,
    SingleLabel = 1
}

/// <summary>إعداد تصميم بطاقة السعر. يُحفظ في تفضيلات المستخدم ولا يغيّر قاعدة البيانات.</summary>
public sealed class PriceLabelDesign
{
    public double MarginLeftMm { get; set; }
    public double MarginRightMm { get; set; }
    public double MarginTopMm { get; set; }
    public double MarginBottomMm { get; set; }
    public int Columns { get; set; } = 3;
    public double LabelWidthMm { get; set; } = 40;
    public double LabelHeightMm { get; set; } = 30;
    public double RowSpacingMm { get; set; } = 2;
    public double ColumnSpacingMm { get; set; } = 2;
    public PriceLabelPaper Paper { get; set; } = PriceLabelPaper.A4;
    public bool ShowProductName { get; set; } = true;
    public bool ShowPrice { get; set; } = true;
    public bool ShowSku { get; set; } = true;
    public bool ShowBarcode { get; set; } = true;
    public bool ShowTaxInclusivePrice { get; set; }
    public bool ShowBorder { get; set; } = true;
    public PriceLabelSymbology Symbology { get; set; } = PriceLabelSymbology.Ean13;
    public double ProductNameFontSize { get; set; } = 12;
    public double PriceFontSize { get; set; } = 16;
    public double BarcodeHeightMm { get; set; } = 12;
    public double TaxPercent { get; set; }

    public PriceLabelDesign Clone() => new()
    {
        MarginLeftMm = MarginLeftMm,
        MarginRightMm = MarginRightMm,
        MarginTopMm = MarginTopMm,
        MarginBottomMm = MarginBottomMm,
        Columns = Columns,
        LabelWidthMm = LabelWidthMm,
        LabelHeightMm = LabelHeightMm,
        RowSpacingMm = RowSpacingMm,
        ColumnSpacingMm = ColumnSpacingMm,
        Paper = Paper,
        ShowProductName = ShowProductName,
        ShowPrice = ShowPrice,
        ShowSku = ShowSku,
        ShowBarcode = ShowBarcode,
        ShowTaxInclusivePrice = ShowTaxInclusivePrice,
        ShowBorder = ShowBorder,
        Symbology = Symbology,
        ProductNameFontSize = ProductNameFontSize,
        PriceFontSize = PriceFontSize,
        BarcodeHeightMm = BarcodeHeightMm,
        TaxPercent = TaxPercent
    };

    public void Normalize()
    {
        MarginLeftMm = Clamp(MarginLeftMm, 0, 40);
        MarginRightMm = Clamp(MarginRightMm, 0, 40);
        MarginTopMm = Clamp(MarginTopMm, 0, 40);
        MarginBottomMm = Clamp(MarginBottomMm, 0, 40);
        Columns = (int)Clamp(Columns, 1, 8);
        LabelWidthMm = Clamp(LabelWidthMm, 15, 120);
        LabelHeightMm = Clamp(LabelHeightMm, 12, 80);
        RowSpacingMm = Clamp(RowSpacingMm, 0, 20);
        ColumnSpacingMm = Clamp(ColumnSpacingMm, 0, 20);
        ProductNameFontSize = Clamp(ProductNameFontSize, 8, 28);
        PriceFontSize = Clamp(PriceFontSize, 10, 36);
        BarcodeHeightMm = Clamp(BarcodeHeightMm, 6, 25);
        TaxPercent = Clamp(TaxPercent, 0, 100);
        if (!Enum.IsDefined(Paper))
            Paper = PriceLabelPaper.A4;
        if (!Enum.IsDefined(Symbology))
            Symbology = PriceLabelSymbology.Ean13;
    }

    public (double WidthMm, double HeightMm) PaperSizeMm()
    {
        if (Paper == PriceLabelPaper.SingleLabel)
            return (LabelWidthMm, LabelHeightMm);
        return (210, 297);
    }

    public PriceLabelFit Measure(int copies)
    {
        var safeCopies = Math.Clamp(copies, 1, 200);
        var (paperW, paperH) = PaperSizeMm();
        var usableW = paperW;
        var usableH = paperH;
        var maxColumns = FitCount(usableW, LabelWidthMm, ColumnSpacingMm);
        var maxRows = FitCount(usableH, LabelHeightMm, RowSpacingMm);

        if (Paper == PriceLabelPaper.SingleLabel)
        {
            maxColumns = LabelWidthMm <= usableW + 0.01 ? 1 : 0;
            maxRows = LabelHeightMm <= usableH + 0.01 ? 1 : 0;
        }

        if (maxColumns == 0 || maxRows == 0)
        {
            return new PriceLabelFit(0, 0, 0, 0, 0,
                "أبعاد البطاقة مع الهوامش أكبر من الورقة. صغّر البطاقة أو الهوامش.");
        }

        var columns = Math.Min(Math.Max(1, Columns), maxColumns);
        string? warning = null;
        if (columns < Columns)
            warning = $"عدد الأعمدة المطلوب لا يتسع في الورقة، وسيُستخدم {columns} فقط.";
        if (Paper == PriceLabelPaper.SingleLabel && Columns > 1)
            warning = "صفحة الملصق المفرد تطبع بطاقة واحدة في كل صفحة.";

        var rows = maxRows;
        var perPage = columns * rows;
        var pages = (int)Math.Ceiling(safeCopies / (double)perPage);
        return new PriceLabelFit(columns, rows, perPage, pages, safeCopies, warning);
    }

    private static int FitCount(double usable, double item, double gap)
    {
        if (item <= 0 || usable + 0.01 < item)
            return 0;
        return Math.Max(1, (int)Math.Floor((usable + gap + 0.01) / (item + gap)));
    }

    private static double Clamp(double value, double min, double max) => Math.Clamp(value, min, max);
}

public readonly record struct PriceLabelFit(
    int Columns,
    int RowsPerPage,
    int CardsPerPage,
    int PageCount,
    int PlacedCards,
    string? Warning);
