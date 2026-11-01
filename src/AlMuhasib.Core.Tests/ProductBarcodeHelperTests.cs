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

    [Fact]
    public void InfarkarMax_allows_empty_and_long_pipe_separated_field()
    {
        Assert.Equal("انفاركار ماكس", ProductBarcodeHelper.InfarkarMaxTypeName);
        Assert.Null(ProductBarcodeHelper.Normalize(null));
        Assert.Null(ProductBarcodeHelper.Normalize(""));
        Assert.Null(ProductBarcodeHelper.Normalize("   "));

        const string sample =
            "6292626181711|6282717230169|6282717238141|6282717237076|6930446840851|6930446829030|6691106078365|6124568797005|6292626182893|6930446830678|6292626191864|6691106076873|6292625172277|6930446839152|6930446839138|6930446829191|6930446844606|5060127731794|6930446809810|6282717212981|6282717234952|6282717226414|6930446844613|607469470032|6930446824509|6282717222690|6282717226384|6282717223017|6129226116594|6282717223024|6282717212998|6930446836786|6282717222966|6930446829283|6282717232675|6930446824516|6282717222676|6930446830654|6282717233672|8439627584310|6282717235119|6282717219683|6282717237106|6930446825186|6930446825193|6930446810816|6930446841018|6282717223864|6282717217627|697051001928|6936829067327|6930446828774|6282717213025|6282717226391|6282717235126|6930446830685|3346470133532|723803733684|6930446843166|6930446840844|4897138051860";

        Assert.Equal(850, sample.Length);
        var normalized = ProductBarcodeHelper.Normalize(sample);
        Assert.NotNull(normalized);
        Assert.True(ProductBarcodeHelper.IsWithinLength(normalized));
        Assert.True(ProductBarcodeHelper.Matches(normalized, "4897138051860"));
        Assert.True(ProductBarcodeHelper.Matches(normalized, "6292626181711"));
    }
}
