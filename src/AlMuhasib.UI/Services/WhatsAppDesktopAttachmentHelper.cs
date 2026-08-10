using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

namespace AlMuhasib.UI.Services;

/// <summary>
/// WhatsApp URL schemes cannot attach files. This helper focuses the desktop app
/// and pastes a PDF from the clipboard into the open chat compose box.
/// </summary>
internal static class WhatsAppDesktopAttachmentHelper
{
    private const uint KeyeventfKeyup = 0x0002;
    private const byte VkControl = 0x11;
    private const byte VkV = 0x56;
    private const uint MouseeventfLeftdown = 0x0002;
    private const uint MouseeventfLeftup = 0x0004;

    public static bool TryAttachPdf(string pdfPath, int waitForWindowMs = 12000)
    {
        if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
            return false;

        var hwnd = WaitForWhatsAppWindow(waitForWindowMs);
        if (hwnd == IntPtr.Zero)
            return false;

        try
        {
            ForceForeground(hwnd);
            Thread.Sleep(700);
            ClickComposeArea(hwnd);
            Thread.Sleep(250);

            if (!SetPdfOnClipboard(pdfPath))
                return false;

            Thread.Sleep(150);
            ForceForeground(hwnd);
            Thread.Sleep(100);
            SendCtrlV();
            Thread.Sleep(400);

            // Second paste attempt helps when chat UI finishes loading late.
            ForceForeground(hwnd);
            Thread.Sleep(200);
            SendCtrlV();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool SetPdfOnClipboard(string pdfPath)
    {
        try
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher is null)
                return SetPdfOnClipboardCore(pdfPath);

            if (dispatcher.CheckAccess())
                return SetPdfOnClipboardCore(pdfPath);

            return dispatcher.Invoke(() => SetPdfOnClipboardCore(pdfPath));
        }
        catch
        {
            return false;
        }
    }

    private static bool SetPdfOnClipboardCore(string pdfPath)
    {
        var files = new System.Collections.Specialized.StringCollection { pdfPath };
        var data = new DataObject();
        data.SetFileDropList(files);

        // Preferred DropEffect = COPY (1) — improves paste compatibility with some apps.
        var effect = new MemoryStream([1, 0, 0, 0]);
        data.SetData("Preferred DropEffect", effect);

        Clipboard.SetDataObject(data, copy: true);
        return Clipboard.ContainsFileDropList();
    }

    private static IntPtr WaitForWhatsAppWindow(int timeoutMs)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            var hwnd = FindWhatsAppWindow();
            if (hwnd != IntPtr.Zero)
                return hwnd;
            Thread.Sleep(250);
        }

        return FindWhatsAppWindow();
    }

    private static IntPtr FindWhatsAppWindow()
    {
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (process.MainWindowHandle == IntPtr.Zero)
                    continue;

                var name = process.ProcessName ?? string.Empty;
                var title = process.MainWindowTitle ?? string.Empty;
                if (IsWhatsAppProcess(name, title))
                    return process.MainWindowHandle;
            }
            catch
            {
                // ignore process access errors
            }
            finally
            {
                process.Dispose();
            }
        }

        IntPtr found = IntPtr.Zero;
        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd))
                return true;

            var title = GetWindowTitle(hWnd);
            if (string.IsNullOrWhiteSpace(title))
                return true;

            if (title.Contains("WhatsApp", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("واتساب", StringComparison.OrdinalIgnoreCase))
            {
                found = hWnd;
                return false;
            }

            return true;
        }, IntPtr.Zero);

        return found;
    }

    private static bool IsWhatsAppProcess(string processName, string title)
    {
        if (processName.Contains("WhatsApp", StringComparison.OrdinalIgnoreCase))
            return true;

        if (title.Contains("WhatsApp", StringComparison.OrdinalIgnoreCase) ||
            title.Contains("واتساب", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static void ForceForeground(IntPtr hwnd)
    {
        ShowWindow(hwnd, 9); // SW_RESTORE
        SetForegroundWindow(hwnd);
    }

    private static void ClickComposeArea(IntPtr hwnd)
    {
        if (!GetWindowRect(hwnd, out var rect))
            return;

        var width = Math.Max(1, rect.Right - rect.Left);
        var height = Math.Max(1, rect.Bottom - rect.Top);

        // Message compose box is near the bottom-center of WhatsApp Desktop.
        var x = rect.Left + width / 2;
        var y = rect.Top + (int)(height * 0.92);

        SetCursorPos(x, y);
        mouse_event(MouseeventfLeftdown, 0, 0, 0, UIntPtr.Zero);
        mouse_event(MouseeventfLeftup, 0, 0, 0, UIntPtr.Zero);
    }

    private static void SendCtrlV()
    {
        keybd_event(VkControl, 0, 0, UIntPtr.Zero);
        keybd_event(VkV, 0, 0, UIntPtr.Zero);
        keybd_event(VkV, 0, KeyeventfKeyup, UIntPtr.Zero);
        keybd_event(VkControl, 0, KeyeventfKeyup, UIntPtr.Zero);
    }

    private static string GetWindowTitle(IntPtr hWnd)
    {
        var length = GetWindowTextLength(hWnd);
        if (length <= 0)
            return string.Empty;

        var buffer = new char[length + 1];
        _ = GetWindowText(hWnd, buffer, buffer.Length);
        return new string(buffer).TrimEnd('\0');
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, char[] lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
