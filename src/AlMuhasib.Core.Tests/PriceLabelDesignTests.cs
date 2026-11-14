using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Models;
using Xunit;

namespace AlMuhasib.Core.Tests;

public class EanBarcodeTests
{
    [Fact]
    public void Ean13_completes_check_digit()
    {
        var result = EanBarcode.Normalize("400638133393", PriceLabelSymbology.Ean13);
        Assert.True(result.IsValid);
        Assert.Equal("4006381333931", result.Code);
    }

    [Fact]
    public void Ean13_accepts_valid_full_code()
    {
        var result = EanBarcode.Normalize("4006381333931", PriceLabelSymbology.Ean13);
        Assert.True(result.IsValid);
        Assert.Equal("4006381333931", result.Code);
    }

    [Fact]
    public void Ean13_rejects_wrong_check_digit()
    {
        var result = EanBarcode.Normalize("4006381333932", PriceLabelSymbology.Ean13);
        Assert.False(result.IsValid);
        Assert.Contains("التحقق", result.Error);
    }

    [Fact]
    public void Ean8_completes_check_digit()
    {
        var result = EanBarcode.Normalize("5512345", PriceLabelSymbology.Ean8);
        Assert.True(result.IsValid);
        Assert.Equal("55123457", result.Code);
    }

    [Fact]
    public void Ean8_rejects_letters()
    {
        var result = EanBarcode.Normalize("55A2345", PriceLabelSymbology.Ean8);
        Assert.False(result.IsValid);
    }
}

public class PriceLabelFitTests
{
    [Fact]
    public void A4_keeps_cards_inside_the_page_and_places_every_copy()
    {
        var design = new PriceLabelDesign
        {
            Paper = PriceLabelPaper.A4,
            Columns = 3,
            LabelWidthMm = 40,
            LabelHeightMm = 30,
            ColumnSpacingMm = 2,
            RowSpacingMm = 2
        };

        var fit = design.Measure(10);
        Assert.Equal(3, fit.Columns);
        Assert.True(fit.RowsPerPage >= 1);
        Assert.Equal(fit.Columns * fit.RowsPerPage, fit.CardsPerPage);
        Assert.Equal(10, fit.PlacedCards);
        Assert.True(fit.PageCount >= 1);
        Assert.Null(fit.Warning);

        var usedWidth = design.MarginLeftMm + design.MarginRightMm
                        + fit.Columns * design.LabelWidthMm
                        + (fit.Columns - 1) * design.ColumnSpacingMm;
        Assert.True(usedWidth <= 210.01);
    }

    [Fact]
    public void Too_many_columns_are_reduced_with_a_warning()
    {
        var design = new PriceLabelDesign
        {
            Paper = PriceLabelPaper.A4,
            Columns = 8,
            LabelWidthMm = 70,
            LabelHeightMm = 30
        };

        var fit = design.Measure(4);
        Assert.True(fit.Columns < 8);
        Assert.True(fit.Columns >= 1);
        Assert.False(string.IsNullOrWhiteSpace(fit.Warning));
        Assert.Equal(4, fit.PlacedCards);
    }

    [Fact]
    public void Single_label_paper_prints_one_card_per_page()
    {
        var design = new PriceLabelDesign
        {
            Paper = PriceLabelPaper.SingleLabel,
            Columns = 3,
            LabelWidthMm = 40,
            LabelHeightMm = 30,
            MarginLeftMm = 1
        };

        var fit = design.Measure(5);
        Assert.Equal(1, fit.Columns);
        Assert.Equal(1, fit.RowsPerPage);
        Assert.Equal(5, fit.PageCount);
        Assert.Equal(5, fit.PlacedCards);
    }
}
