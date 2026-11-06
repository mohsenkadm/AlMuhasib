using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace AlMuhasib.UI.Services;

/// <summary>
/// مسح ضوئي عبر WIA COM (late binding) مع حفظ JPEG موثوق.
/// </summary>
public static class WiaScannerHelper
{
    private const string WiaFormatJpeg = "{B96B3CAE-0728-11D3-9D7B-0000F81EF32E}";
    private const int WiaConversionError = unchecked((int)0x8021000E);

    public sealed class ScanResult
    {
        public bool Success { get; init; }
        public string? FilePath { get; init; }
        public string? ErrorMessage { get; init; }
        public bool Cancelled { get; init; }
    }

    /// <summary>
    /// يعرض حوار المسح ويحفظ الصورة كـ JPEG في <paramref name="destinationPath"/>.
    /// </summary>
    public static ScanResult AcquireJpegToFile(string destinationPath)
    {
        if (string.IsNullOrWhiteSpace(destinationPath))
            return Fail("مسار حفظ المسح غير صالح");

        Type? dialogType;
        try
        {
            dialogType = Type.GetTypeFromProgID("WIA.CommonDialog");
        }
        catch (Exception ex)
        {
            return Fail($"تعذر تهيئة واجهة المسح الضوئي: {ex.Message}");
        }

        if (dialogType is null)
            return Fail("خدمة المسح الضوئي (WIA) غير متوفرة على هذا الجهاز");

        object? dialog = null;
        object? image = null;
        try
        {
            dialog = Activator.CreateInstance(dialogType);
            if (dialog is null)
                return Fail("تعذر فتح حوار المسح الضوئي");

            // ShowAcquireImage(DeviceType=1 Scanner, Intent=0 Color, FormatID=JPEG, …)
            image = dialogType.InvokeMember(
                "ShowAcquireImage",
                System.Reflection.BindingFlags.InvokeMethod,
                binder: null,
                target: dialog,
                args: [1, 0, WiaFormatJpeg, false, true, false]);

            if (image is null)
                return Cancelled();

            EnsureDirectory(destinationPath);
            SaveAsJpeg(image, destinationPath);

            if (!File.Exists(destinationPath) || new FileInfo(destinationPath).Length == 0)
                return Fail("تم المسح لكن تعذر حفظ الملف");

            return new ScanResult { Success = true, FilePath = destinationPath };
        }
        catch (COMException ex) when (IsCancelOrNoDevice(ex))
        {
            return Cancelled("لم يتم العثور على ماسح ضوئي متصل أو تم إلغاء العملية");
        }
        catch (COMException ex)
        {
            return Fail($"فشل المسح الضوئي: {ex.Message}");
        }
        catch (TargetInvocationException ex) when (ex.InnerException is COMException com && IsCancelOrNoDevice(com))
        {
            return Cancelled("لم يتم العثور على ماسح ضوئي متصل أو تم إلغاء العملية");
        }
        catch (Exception ex)
        {
            return Fail($"خطأ أثناء المسح الضوئي: {ex.Message}");
        }
        finally
        {
            ReleaseCom(image);
            ReleaseCom(dialog);
        }
    }

    private static void SaveAsJpeg(object image, string destinationPath)
    {
        var imageType = image.GetType();
        try
        {
            // Prefer Convert to JPEG when available (avoids BMP/TIFF save quirks).
            var converted = imageType.InvokeMember(
                "Convert",
                System.Reflection.BindingFlags.InvokeMethod,
                binder: null,
                target: image,
                args: [WiaFormatJpeg]);

            if (converted is not null)
            {
                try
                {
                    converted.GetType().InvokeMember(
                        "SaveFile",
                        System.Reflection.BindingFlags.InvokeMethod,
                        binder: null,
                        target: converted,
                        args: [destinationPath]);
                    return;
                }
                finally
                {
                    ReleaseCom(converted);
                }
            }
        }
        catch (COMException ex) when (ex.ErrorCode == WiaConversionError)
        {
            // Fall through to direct SaveFile.
        }
        catch (TargetInvocationException)
        {
            // Fall through to direct SaveFile.
        }

        imageType.InvokeMember(
            "SaveFile",
            System.Reflection.BindingFlags.InvokeMethod,
            binder: null,
            target: image,
            args: [destinationPath]);
    }

    private static bool IsCancelOrNoDevice(COMException ex) =>
        // Common WIA cancel / no device HRESULTs
        ex.ErrorCode is unchecked((int)0x80210006)
            or unchecked((int)0x80210015)
            or unchecked((int)0x80210001)
            or unchecked((int)0x80210064)
            or unchecked((int)0x80004005);

    private static void EnsureDirectory(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
    }

    private static void ReleaseCom(object? comObject)
    {
        if (comObject is null || !Marshal.IsComObject(comObject)) return;
        try { Marshal.FinalReleaseComObject(comObject); }
        catch { /* ignore */ }
    }

    private static ScanResult Fail(string message) => new()
    {
        Success = false,
        ErrorMessage = message
    };

    private static ScanResult Cancelled(string? message = null) => new()
    {
        Success = false,
        Cancelled = true,
        ErrorMessage = message
    };
}
