using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AlMuhasib.UI.Helpers;

/// <summary>
/// Runtime product white-label. Default is Qayd; Duhok branch uses Express branding.
/// Call <see cref="Apply"/> once after loading/saving the system profile and before UI windows open.
/// </summary>
public static class ProductBranding
{
    public const string QaydNameAr = "قيد";
    public const string QaydNameEn = "Qayd";
    public const string QaydSupportWhatsApp = "07505496065";

    public const string ExpressNameAr = "اكسبريس للمبيعات";
    public const string ExpressNameEn = "Express";
    public const string ExpressSupportWhatsApp = "07504506695";

    private const string QaydMarkPack = "pack://application:,,,/Assets/Brand/qayd-mark.png";
    private const string QaydIconPngPack = "pack://application:,,,/Assets/Brand/qayd-icon.png";
    private const string QaydIconIcoPack = "pack://application:,,,/Assets/Brand/qayd-icon.ico";

    private const string ExpressMarkPack = "pack://application:,,,/Assets/Brand/express-mark.png";
    private const string ExpressIconPngPack = "pack://application:,,,/Assets/Brand/express-icon.png";
    private const string ExpressIconIcoPack = "pack://application:,,,/Assets/Brand/express-icon.ico";

    public static bool IsDuhokBranch { get; private set; }

    public static string AppNameAr => IsDuhokBranch ? ExpressNameAr : QaydNameAr;
    public static string AppNameEn => IsDuhokBranch ? ExpressNameEn : QaydNameEn;
    public static string SupportWhatsApp => IsDuhokBranch ? ExpressSupportWhatsApp : QaydSupportWhatsApp;

    public static string MarkPackUri => IsDuhokBranch ? ExpressMarkPack : QaydMarkPack;
    public static string IconPngPackUri => IsDuhokBranch ? ExpressIconPngPack : QaydIconPngPack;
    public static string IconIcoPackUri => IsDuhokBranch ? ExpressIconIcoPack : QaydIconIcoPack;

    public static ImageSource? MarkImage { get; private set; }
    public static ImageSource? IconPngImage { get; private set; }
    public static ImageSource? IconIcoImage { get; private set; }

    public static void Apply(bool isDuhokBranch)
    {
        IsDuhokBranch = isDuhokBranch;
        MarkImage = LoadBitmap(MarkPackUri);
        IconPngImage = LoadBitmap(IconPngPackUri);
        IconIcoImage = LoadBitmap(IconIcoPackUri);

        var app = Application.Current;
        if (app?.Resources is null)
            return;

        app.Resources["BrandMarkImage"] = MarkImage;
        app.Resources["BrandIconImage"] = IconPngImage;
        app.Resources["BrandWindowIcon"] = IconIcoImage;
    }

    public static void ApplyToWindow(Window window)
    {
        if (window is null || IconIcoImage is null) return;
        window.Icon = IconIcoImage;
    }

    private static BitmapImage LoadBitmap(string packUri)
    {
        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.UriSource = new Uri(packUri, UriKind.Absolute);
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.EndInit();
        bmp.Freeze();
        return bmp;
    }
}
