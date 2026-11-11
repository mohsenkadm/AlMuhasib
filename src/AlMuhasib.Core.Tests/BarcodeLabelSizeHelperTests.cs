using AlMuhasib.Core.Enums;
using Xunit;

namespace AlMuhasib.Core.Tests;

public class BarcodeLabelSizeHelperTests
{
    [Theory]
    [InlineData(BarcodeLabelSize.Mm30x15, 30, 15)]
    [InlineData(BarcodeLabelSize.Mm30x20, 30, 20)]
    [InlineData(BarcodeLabelSize.Mm38x25, 38, 25)]
    [InlineData(BarcodeLabelSize.Mm40x20, 40, 20)]
    [InlineData(BarcodeLabelSize.Mm40x25, 40, 25)]
    [InlineData(BarcodeLabelSize.Mm40x30, 40, 30)]
    [InlineData(BarcodeLabelSize.Mm50x25, 50, 25)]
    [InlineData(BarcodeLabelSize.Mm50x30, 50, 30)]
    [InlineData(BarcodeLabelSize.Mm58x40, 58, 40)]
    [InlineData(BarcodeLabelSize.Mm60x40, 60, 40)]
    [InlineData(BarcodeLabelSize.Mm80x50, 80, 50)]
    [InlineData(BarcodeLabelSize.Mm100x50, 100, 50)]
    public void GetMillimeters_returns_preset_dimensions(BarcodeLabelSize size, double w, double h)
    {
        var (width, height) = BarcodeLabelSizeHelper.GetMillimeters(size);
        Assert.Equal(w, width);
        Assert.Equal(h, height);
    }

    [Fact]
    public void GetMillimeters_custom_uses_provided_values()
    {
        var (w, h) = BarcodeLabelSizeHelper.GetMillimeters(BarcodeLabelSize.Custom, 45, 22);
        Assert.Equal(45, w);
        Assert.Equal(22, h);
    }

    [Fact]
    public void GetMillimeters_custom_falls_back_to_defaults()
    {
        var (w, h) = BarcodeLabelSizeHelper.GetMillimeters(BarcodeLabelSize.Custom);
        Assert.Equal(BarcodeLabelSizeHelper.DefaultCustomWidthMm, w);
        Assert.Equal(BarcodeLabelSizeHelper.DefaultCustomHeightMm, h);
    }

    [Fact]
    public void ClampDimension_limits_range()
    {
        Assert.Equal(10, BarcodeLabelSizeHelper.ClampDimension(1));
        Assert.Equal(200, BarcodeLabelSizeHelper.ClampDimension(500));
        Assert.Equal(33, BarcodeLabelSizeHelper.ClampDimension(33));
    }

    [Fact]
    public void MmToDip_converts_correctly()
    {
        var dip = BarcodeLabelSizeHelper.MmToDip(25.4);
        Assert.Equal(96.0, dip, 3);
    }

    [Fact]
    public void GetLayout_custom_uses_custom_page_size()
    {
        var layout = BarcodeLabelSizeHelper.GetLayout(BarcodeLabelSize.Custom, 50, 30);
        Assert.Equal(BarcodeLabelSizeHelper.MmToDip(50), layout.PageWidth, 3);
        Assert.Equal(BarcodeLabelSizeHelper.MmToDip(30), layout.PageHeight, 3);
        Assert.True(layout.BarcodeWidth > 0);
        Assert.True(layout.BarcodeHeight > 0);
    }

    [Fact]
    public void GetDisplayName_custom_is_arabic()
    {
        Assert.Equal("مخصص", BarcodeLabelSizeHelper.GetDisplayName(BarcodeLabelSize.Custom));
        Assert.Contains("40", BarcodeLabelSizeHelper.GetDisplayName(BarcodeLabelSize.Mm40x30));
    }

    [Fact]
    public void AllSizes_includes_custom_and_default()
    {
        Assert.Contains(BarcodeLabelSize.Custom, BarcodeLabelSizeHelper.AllSizes);
        Assert.Contains(BarcodeLabelSizeHelper.DefaultSize, BarcodeLabelSizeHelper.AllSizes);
        Assert.Equal(13, BarcodeLabelSizeHelper.AllSizes.Count);
    }
}
