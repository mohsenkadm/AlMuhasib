using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Helpers;
using Xunit;

namespace AlMuhasib.Core.Tests;

public class ProductBarcodeHelperTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("  111  ", "111")]
    [InlineData("111|222", "111|222")]
    [InlineData(" 111 | 222 | ", "111|222")]
    [InlineData("111|222|111", "111|222")]
    [InlineData("AAA|aaa|BbB", "AAA|BbB")]
    public void Normalize_trims_and_dedupes(string? input, string? expected)
    {
        Assert.Equal(expected, ProductBarcodeHelper.Normalize(input));
    }

    [Fact]
    public void Matches_any_token_exact_ignore_case()
    {
        const string field = "111|222|333";
        Assert.True(ProductBarcodeHelper.Matches(field, "111"));
        Assert.True(ProductBarcodeHelper.Matches(field, "222"));
        Assert.True(ProductBarcodeHelper.Matches(field, "333"));
        Assert.True(ProductBarcodeHelper.Matches(field, " 222 "));
        Assert.False(ProductBarcodeHelper.Matches(field, "11"));
        Assert.False(ProductBarcodeHelper.Matches(field, "11222"));
        Assert.False(ProductBarcodeHelper.Matches(field, "444"));
    }

    [Fact]
    public void Matches_does_not_match_substring_inside_longer_code()
    {
        Assert.False(ProductBarcodeHelper.Matches("0123456789012", "123456"));
        Assert.False(ProductBarcodeHelper.Matches("AAA|0123456789012|BBB", "123456"));
        Assert.True(ProductBarcodeHelper.Matches("AAA|0123456789012|BBB", "0123456789012"));
    }

    [Fact]
    public void Primary_returns_first_token()
    {
        Assert.Equal("111", ProductBarcodeHelper.Primary("111|222"));
        Assert.Null(ProductBarcodeHelper.Primary(null));
        Assert.Null(ProductBarcodeHelper.Primary("  |  "));
    }

    [Fact]
    public void MatchesExpression_compiles_for_product()
    {
        var products = new List<Product>
        {
            new() { Id = 1, Name = "A", Barcode = "111|222|333" },
            new() { Id = 2, Name = "B", Barcode = "999" },
            new() { Id = 3, Name = "C", Barcode = null }
        }.AsQueryable();

        var expr = ProductBarcodeHelper.MatchesExpression("222");
        var hit = products.Where(expr.Compile()).Single();
        Assert.Equal(1, hit.Id);

        expr = ProductBarcodeHelper.MatchesExpression("999");
        hit = products.Where(expr.Compile()).Single();
        Assert.Equal(2, hit.Id);

        var noPartial = products.Where(ProductBarcodeHelper.MatchesExpression("22").Compile()).ToList();
        Assert.Equal(0, noPartial.Count);
    }

    [Fact]
    public void SharesAnyToken_detects_overlap()
    {
        Assert.True(ProductBarcodeHelper.SharesAnyToken("111|222", new[] { "222", "333" }));
        Assert.False(ProductBarcodeHelper.SharesAnyToken("111|222", new[] { "333", "444" }));
    }

    [Fact]
    public void IsWithinLength_respects_max()
    {
        Assert.True(ProductBarcodeHelper.IsWithinLength(null));
        Assert.True(ProductBarcodeHelper.IsWithinLength(new string('1', ProductBarcodeHelper.MaxStoredLength)));
        Assert.False(ProductBarcodeHelper.IsWithinLength(new string('1', ProductBarcodeHelper.MaxStoredLength + 1)));
    }
}
