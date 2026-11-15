using System.Globalization;
using System.Windows;
using System.Windows.Media;
using AlMuhasib.Core.Helpers;

namespace AlMuhasib.Shared.Services;

/// <summary>
/// رمز EAN بأرقام متداخلة مع الأشرطة: حراسة أطول، ورقم EAN-13 الأول خارج الرمز.
/// </summary>
public sealed class EanSymbolElement : FrameworkElement
{
    private readonly EanSymbol.Layout _layout;
    private readonly string _code;

    private EanSymbolElement(EanSymbol.Layout layout, string code)
    {
        _layout = layout;
        _code = code;
        FlowDirection = FlowDirection.LeftToRight;
        SnapsToDevicePixels = true;
        UseLayoutRounding = true;
    }

    public static EanSymbolElement Create(string code, PriceLabelSymbology symbology, double width, double height)
    {
        var element = new EanSymbolElement(EanSymbol.Encode(code, symbology), code)
        {
            Width = Math.Max(40, width),
            Height = Math.Max(18, height)
        };
        return element;
    }

    protected override void OnRender(DrawingContext dc)
    {
        var width = ActualWidth > 1 ? ActualWidth : Width;
        var height = ActualHeight > 1 ? ActualHeight : Height;
        dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));

        var totalModules = _layout.LeftQuiet + _layout.Modules.Length + _layout.RightQuiet;
        var module = width / totalModules;
        if (module >= 1)
            module = Math.Floor(module);
        module = Math.Max(0.45, module);

        var drawn = module * totalModules;
        var originX = (width - drawn) / 2;
        var digitBand = Math.Clamp(height * 0.36, 8, height * 0.46);
        var barBottom = Math.Max(module * 4, height - digitBand);
        var symbolLeft = originX + _layout.LeftQuiet * module;

        for (var i = 0; i < _layout.Modules.Length; i++)
        {
            if (_layout.Modules[i] != '1')
                continue;

            var guard = EanSymbol.IsGuard(_layout, i);
            dc.DrawRectangle(
                Brushes.Black,
                null,
                new Rect(symbolLeft + i * module, 0, module, guard ? height : barBottom));
        }

        var dpi = 1.0;
        if (IsVisible)
        {
            var scale = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            if (scale > 0)
                dpi = scale;
        }

        var typeface = new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        var fontSize = FitDigitSize(typeface, dpi, module * 7 * 0.86, digitBand * 0.9);

        if (_layout.OutsideDigit)
            DrawDigit(dc, typeface, fontSize, dpi, _code[0], originX, _layout.LeftQuiet * module, barBottom, digitBand, alignRight: true);

        var leftStart = symbolLeft + 3 * module;
        for (var i = 0; i < _layout.LeftDigitCount; i++)
        {
            var digitIndex = _layout.OutsideDigit ? i + 1 : i;
            DrawDigit(dc, typeface, fontSize, dpi, _code[digitIndex], leftStart + i * 7 * module, 7 * module, barBottom, digitBand, alignRight: false);
        }

        var rightStart = symbolLeft + (_layout.CenterGuardIndex + 5) * module;
        for (var i = 0; i < _layout.RightDigitCount; i++)
        {
            var digitIndex = _code.Length - _layout.RightDigitCount + i;
            DrawDigit(dc, typeface, fontSize, dpi, _code[digitIndex], rightStart + i * 7 * module, 7 * module, barBottom, digitBand, alignRight: false);
        }
    }

    private static double FitDigitSize(Typeface typeface, double dpi, double maxWidth, double maxHeight)
    {
        var size = Math.Max(6, maxHeight);
        var text = MakeText("8", typeface, size, dpi);
        if (text.Width > maxWidth && text.Width > 0)
            size *= maxWidth / text.Width;
        text = MakeText("8", typeface, size, dpi);
        if (text.Height > maxHeight && text.Height > 0)
            size *= maxHeight / text.Height;
        return Math.Max(5, size);
    }

    private static void DrawDigit(
        DrawingContext dc,
        Typeface typeface,
        double fontSize,
        double dpi,
        char digit,
        double cellX,
        double cellWidth,
        double barBottom,
        double digitBand,
        bool alignRight)
    {
        var text = MakeText(digit.ToString(), typeface, fontSize, dpi);
        var x = alignRight
            ? cellX + cellWidth - text.Width - Math.Max(1, cellWidth * 0.06)
            : cellX + (cellWidth - text.Width) / 2;
        var y = barBottom + Math.Max(0, (digitBand - text.Height) / 2);
        dc.DrawText(text, new Point(Math.Max(0, x), y));
    }

    private static FormattedText MakeText(string value, Typeface typeface, double size, double dpi) =>
        new(value, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, size, Brushes.Black, dpi);
}
