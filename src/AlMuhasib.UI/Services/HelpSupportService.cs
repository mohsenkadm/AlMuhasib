using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using AlMuhasib.Shared.Helpers;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace AlMuhasib.UI.Services;

public sealed class HelpSupportService : IHelpSupportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly IServiceProvider _services;
    private readonly string _manifestPath;
    private HelpVideosManifest? _cached;

    public HelpSupportService(IServiceProvider services)
    {
        _services = services;
        _manifestPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "help-videos.json");
    }

    public HelpVideosManifest GetManifest() => LoadManifest();

    public IReadOnlyList<HelpVideoItemVm> GetAllVideos()
    {
        var manifest = LoadManifest();
        var list = new List<HelpVideoItemVm>();

        foreach (var category in manifest.Categories)
        {
            foreach (var video in category.Videos)
            {
                list.Add(new HelpVideoItemVm
                {
                    CategoryId = category.Id,
                    CategoryTitle = category.Title,
                    CategoryIcon = category.Icon,
                    Title = video.Title,
                    Description = video.Description,
                    YoutubeUrl = video.YoutubeUrl,
                    Duration = video.Duration,
                    VideoId = YouTubeHelper.ExtractVideoId(video.YoutubeUrl)
                });
            }
        }

        return list;
    }

    public void OpenWhatsAppSupport()
    {
        var manifest = LoadManifest();
        if (!IraqiPhoneHelper.TryNormalizeForWhatsApp(
                manifest.SupportWhatsApp, out var waDigits, out _, out var error))
        {
            BeautifulMessageDialog.ShowError(error ?? "رقم الدعم غير صالح في help-videos.json");
            return;
        }

        var message = string.IsNullOrWhiteSpace(manifest.SupportMessage)
            ? "السلام عليكم، أحتاج مساعدة في نظام المحاسب."
            : manifest.SupportMessage.Trim();

        OpenWhatsAppChat(waDigits, message);
    }

    public void ShowVideosWindow(Window? owner)
    {
        _cached = null;
        var window = _services.GetRequiredService<HelpVideosWindow>();
        window.Owner = owner ?? Application.Current.MainWindow;
        window.ShowDialog();
    }

    private HelpVideosManifest LoadManifest()
    {
        if (_cached is not null)
            return _cached;

        if (!File.Exists(_manifestPath))
        {
            _cached = CreateDefaultManifest();
            return _cached;
        }

        try
        {
            var json = File.ReadAllText(_manifestPath);
            _cached = JsonSerializer.Deserialize<HelpVideosManifest>(json, JsonOptions) ?? CreateDefaultManifest();
        }
        catch
        {
            _cached = CreateDefaultManifest();
        }

        return _cached;
    }

    private static HelpVideosManifest CreateDefaultManifest() => new()
    {
        SupportWhatsApp = "07505496065",
        SupportMessage = "السلام عليكم، أحتاج مساعدة في نظام المحاسب.",
        Categories = []
    };

    private static void OpenWhatsAppChat(string waDigits, string message)
    {
        var encoded = Uri.EscapeDataString(message);
        var urls = new[]
        {
            $"whatsapp://send?phone={waDigits}&text={encoded}",
            $"https://wa.me/{waDigits}?text={encoded}"
        };

        foreach (var url in urls)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                return;
            }
            catch
            {
                // try next
            }
        }

        BeautifulMessageDialog.ShowError("تعذّر فتح واتساب. تأكد من تثبيت واتساب على الجهاز.");
    }
}
