using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

static int ColorDistance(Color a, Color b)
{
    var dr = a.R - b.R;
    var dg = a.G - b.G;
    var db = a.B - b.B;
    return dr * dr + dg * dg + db * db;
}

static bool IsBackground(Color c, Color bg, int toleranceSq)
{
    if (c.A < 10) return true;
    return ColorDistance(c, bg) <= toleranceSq;
}

static Bitmap RemoveBackground(Bitmap source, int tolerance = 48)
{
    var corners = new[]
    {
        source.GetPixel(0, 0),
        source.GetPixel(source.Width - 1, 0),
        source.GetPixel(0, source.Height - 1),
        source.GetPixel(source.Width - 1, source.Height - 1)
    };
    var bg = corners.OrderBy(c => c.GetBrightness()).First();
    var toleranceSq = tolerance * tolerance * 3;

    var result = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
    for (var y = 0; y < source.Height; y++)
    {
        for (var x = 0; x < source.Width; x++)
        {
            var pixel = source.GetPixel(x, y);
            result.SetPixel(x, y, IsBackground(pixel, bg, toleranceSq)
                ? Color.FromArgb(0, pixel)
                : pixel);
        }
    }

    return result;
}

static Bitmap ResizeSquare(Bitmap source, int size)
{
    var result = new Bitmap(size, size, PixelFormat.Format32bppArgb);
    using var g = Graphics.FromImage(result);
    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
    g.Clear(Color.Transparent);

    var scale = Math.Min((float)size / source.Width, (float)size / source.Height);
    var w = source.Width * scale;
    var h = source.Height * scale;
    var x = (size - w) / 2f;
    var y = (size - h) / 2f;
    g.DrawImage(source, x, y, w, h);
    return result;
}

static void SaveIco(string path, IEnumerable<Bitmap> sizes)
{
    using var fs = File.Create(path);
    using var writer = new BinaryWriter(fs);

    writer.Write((ushort)0);
    writer.Write((ushort)1);
    var images = sizes.ToList();
    writer.Write((ushort)images.Count);

    var offset = 6 + 16 * images.Count;
    var pngDataList = new List<byte[]>();

    foreach (var bmp in images)
    {
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        var data = ms.ToArray();
        pngDataList.Add(data);

        writer.Write((byte)bmp.Width);
        writer.Write((byte)bmp.Height);
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((ushort)1);
        writer.Write((ushort)32);
        writer.Write((uint)data.Length);
        writer.Write((uint)offset);
        offset += data.Length;
    }

    foreach (var data in pngDataList)
        writer.Write(data);
}

static void SaveWizardBmp(string path, int width, int height, Color top, Color bottom, Bitmap? logo = null)
{
    using var bmp = new Bitmap(width, height);
    using var g = Graphics.FromImage(bmp);
    using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
        new Rectangle(0, 0, width, height), top, bottom, 90f);
    g.FillRectangle(brush, 0, 0, width, height);

    if (logo is not null)
    {
        var max = Math.Min(width, height) - 8;
        using var scaled = ResizeSquare(logo, max);
        g.DrawImage(scaled, (width - max) / 2, (height - max) / 2);
    }

    bmp.Save(path, ImageFormat.Bmp);
}

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: PrepareLogo <source.png> <outputDir>");
    return 1;
}

var sourcePath = Path.GetFullPath(args[0]);
var outputDir = Path.GetFullPath(args[1]);
Directory.CreateDirectory(outputDir);

using var original = new Bitmap(sourcePath);

var iconPath = Path.Combine(outputDir, "qayd-icon.png");
original.Save(iconPath, ImageFormat.Png);

using var mark = RemoveBackground(original);
var markPath = Path.Combine(outputDir, "qayd-mark.png");
mark.Save(markPath, ImageFormat.Png);

var icoSizes = new[] { 16, 32, 48, 256 }
    .Select(s => ResizeSquare(original, s))
    .ToList();
try
{
    SaveIco(Path.Combine(outputDir, "qayd-icon.ico"), icoSizes);
}
finally
{
    foreach (var bmp in icoSizes) bmp.Dispose();
}

var installerAssets = Path.Combine(outputDir, "..", "..", "..", "..", "installer", "assets");
if (Directory.Exists(Path.GetDirectoryName(installerAssets)!))
{
    Directory.CreateDirectory(installerAssets);
    File.Copy(Path.Combine(outputDir, "qayd-icon.ico"), Path.Combine(installerAssets, "qayd-icon.ico"), true);
    SaveWizardBmp(Path.Combine(installerAssets, "wizard-large.bmp"), 164, 314,
        Color.FromArgb(15, 32, 68), Color.FromArgb(30, 64, 120), original);
    SaveWizardBmp(Path.Combine(installerAssets, "wizard-small.bmp"), 55, 58,
        Color.FromArgb(20, 40, 80), Color.FromArgb(35, 70, 130), ResizeSquare(original, 40));
}

Console.WriteLine($"Logo assets written to {outputDir}");
return 0;
