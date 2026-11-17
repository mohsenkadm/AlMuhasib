using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace AlMuhasib.UI.Services;

/// <summary>
/// مسح ضوئي عبر WIA COM (late binding) مع حفظ JPEG موثوق.
/// </summary>
public static class WiaScannerHelper
{
    private const string WiaFormatJpeg = "{B96B3CAE-0728-11D3-9D7B-0000F81EF32E}";

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

            image = ShowAcquireImage(dialog);
            if (image is null)
                return Cancelled();

            EnsureDirectory(destinationPath);
            SaveAsJpeg(image, destinationPath);

            if (!File.Exists(destinationPath) || new FileInfo(destinationPath).Length == 0)
                return Fail("تم المسح لكن تعذر حفظ الملف");

            return new ScanResult { Success = true, FilePath = destinationPath };
        }
        catch (Exception ex) when (TryGetCancel(ex, out var cancelMessage))
        {
            return Cancelled(cancelMessage);
        }
        catch (Exception ex)
        {
            return Fail($"خطأ أثناء المسح الضوئي: {RootMessage(ex)}");
        }
        finally
        {
            ReleaseCom(image);
            ReleaseCom(dialog);
        }
    }

    /// <summary>
    /// حوار WIA الافتراضي بدون فرض صيغة.
    /// الاستدعاء السابق كان يمرّر 6 معاملات بينما التوقيع يتطلب Bias قبل FormatID،
    /// فيُرفض فوراً. وفرض JPEG أثناء النقل يفشل أيضاً على كثير من الماسحات.
    /// التحويل إلى JPEG يتم بعد وصول الصورة.
    /// </summary>
    private static object? ShowAcquireImage(object dialog)
    {
        dynamic wia = dialog;
        return wia.ShowAcquireImage();
    }

    private static void SaveAsJpeg(object image, string destinationPath)
    {
        var folder = Path.GetDirectoryName(destinationPath);
        var rawPath = Path.Combine(
            string.IsNullOrEmpty(folder) ? Path.GetTempPath() : folder,
            Path.GetFileNameWithoutExtension(destinationPath) + ".wia-raw" + GuessExtension(image));

        try
        {
            if (TryConvertAndSaveJpeg(image, destinationPath) && IsJpeg(destinationPath))
                return;

            if (File.Exists(destinationPath))
                File.Delete(destinationPath);

            SaveFile(image, rawPath);
            if (IsJpeg(rawPath))
            {
                File.Move(rawPath, destinationPath, overwrite: true);
                return;
            }

            TranscodeToJpeg(rawPath, destinationPath);
        }
        finally
        {
            if (!string.Equals(rawPath, destinationPath, StringComparison.OrdinalIgnoreCase) && File.Exists(rawPath))
            {
                try { File.Delete(rawPath); }
                catch { /* ignore */ }
            }
        }
    }

    private static bool TryConvertAndSaveJpeg(object image, string destinationPath)
    {
        object? converted = null;
        try
        {
            converted = image.GetType().InvokeMember(
                "Convert",
                BindingFlags.InvokeMethod,
                binder: null,
                target: image,
                args: [WiaFormatJpeg]);

            if (converted is null)
                return false;

            SaveFile(converted, destinationPath);
            return true;
        }
        catch
        {
            if (File.Exists(destinationPath))
            {
                try { File.Delete(destinationPath); }
                catch { /* ignore */ }
            }

            return false;
        }
        finally
        {
            ReleaseCom(converted);
        }
    }

    private static void SaveFile(object image, string path)
    {
        image.GetType().InvokeMember(
            "SaveFile",
            BindingFlags.InvokeMethod,
            binder: null,
            target: image,
            args: [path]);
    }

    private static string GuessExtension(object image)
    {
        try
        {
            var ext = image.GetType().InvokeMember(
                "FileExtension",
                BindingFlags.GetProperty,
                binder: null,
                target: image,
                args: null) as string;

            if (string.IsNullOrWhiteSpace(ext))
                return ".bmp";

            ext = ext.Trim();
            return ext.StartsWith('.') ? ext : "." + ext;
        }
        catch
        {
            return ".bmp";
        }
    }

    private static void TranscodeToJpeg(string sourcePath, string destinationPath)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
        bitmap.UriSource = new Uri(sourcePath, UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();

        var encoder = new JpegBitmapEncoder { QualityLevel = 90 };
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(destinationPath);
        encoder.Save(stream);
    }

    private static bool IsJpeg(string path)
    {
        if (!File.Exists(path) || new FileInfo(path).Length < 3)
            return false;

        Span<byte> header = stackalloc byte[3];
        using var stream = File.OpenRead(path);
        return stream.Read(header) == 3
            && header[0] == 0xFF
            && header[1] == 0xD8
            && header[2] == 0xFF;
    }

    private static bool TryGetCancel(Exception ex, out string message)
    {
        message = "لم يتم العثور على ماسح ضوئي متصل أو تم إلغاء العملية";
        return IsCancelOrNoDevice(ex);
    }

    private static bool IsCancelOrNoDevice(Exception ex)
    {
        var root = Unwrap(ex);
        return root is COMException com && IsCancelOrNoDevice(com);
    }

    private static bool IsCancelOrNoDevice(COMException ex) =>
        ex.ErrorCode is unchecked((int)0x80210006)
            or unchecked((int)0x80210015)
            or unchecked((int)0x80210001)
            or unchecked((int)0x80210064)
            or unchecked((int)0x80004005)
            or unchecked((int)0x800704C7);

    private static string RootMessage(Exception ex) => Unwrap(ex).Message;

    private static Exception Unwrap(Exception ex)
    {
        while (ex.InnerException is not null
               && ex is TargetInvocationException or Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
        {
            ex = ex.InnerException;
        }

        return ex;
    }

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
