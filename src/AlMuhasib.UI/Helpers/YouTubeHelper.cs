using System.Text.RegularExpressions;

namespace AlMuhasib.UI.Helpers;

public static partial class YouTubeHelper
{
    public static string? ExtractVideoId(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var trimmed = url.Trim();

        if (TryMatch(VideoIdOnlyPattern(), trimmed, out var direct))
            return direct;

        if (TryMatch(WatchPattern(), trimmed, out var watch))
            return watch;

        if (TryMatch(ShortPattern(), trimmed, out var shortLink))
            return shortLink;

        if (TryMatch(EmbedPattern(), trimmed, out var embed))
            return embed;

        return null;
    }

    public const string EmbedRefererOrigin = "https://app.almuhasib.local";

    public static int? ExtractStartSeconds(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var match = StartTimePattern().Match(url);
        if (!match.Success)
            return null;

        var raw = match.Groups["t"].Value;
        if (raw.EndsWith('s') || raw.EndsWith('S'))
            raw = raw[..^1];

        return int.TryParse(raw, out var seconds) && seconds > 0 ? seconds : null;
    }

    public static string BuildEmbedUri(string videoId, int? startSeconds = null, bool autoplay = true)
    {
        var query = BuildEmbedQuery(startSeconds, autoplay);
        return $"https://www.youtube-nocookie.com/embed/{videoId}?{query}";
    }

    public static string BuildEmbedHtml(string videoId, int? startSeconds = null, bool autoplay = true)
    {
        var embedSrc = BuildEmbedUri(videoId, startSeconds, autoplay);
        return $$"""
                <!DOCTYPE html>
                <html lang="ar" dir="rtl">
                <head>
                  <meta charset="utf-8"/>
                  <meta name="referrer" content="strict-origin-when-cross-origin"/>
                  <style>
                    * { margin: 0; padding: 0; box-sizing: border-box; }
                    html, body { width: 100%; height: 100%; background: #0A1628; overflow: hidden; }
                    iframe { width: 100%; height: 100%; border: 0; }
                  </style>
                </head>
                <body>
                  <iframe
                    src="{{embedSrc}}"
                    title="YouTube"
                    referrerpolicy="strict-origin-when-cross-origin"
                    allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share"
                    allowfullscreen>
                  </iframe>
                </body>
                </html>
                """;
    }

    public static string BuildWatchUri(string videoId) =>
        $"https://www.youtube.com/watch?v={videoId}";

    private static string BuildEmbedQuery(int? startSeconds, bool autoplay)
    {
        var parts = new List<string>
        {
            autoplay ? "autoplay=1" : "autoplay=0",
            "rel=0",
            "modestbranding=1",
            "playsinline=1",
            "enablejsapi=1"
        };

        if (startSeconds is > 0)
            parts.Add($"start={startSeconds.Value}");

        return string.Join('&', parts);
    }

    private static bool TryMatch(Regex regex, string input, out string? videoId)
    {
        var match = regex.Match(input);
        if (match.Success)
        {
            videoId = match.Groups["id"].Value;
            return true;
        }

        videoId = null;
        return false;
    }

    [GeneratedRegex(@"^[a-zA-Z0-9_-]{11}$", RegexOptions.CultureInvariant)]
    private static partial Regex VideoIdOnlyPattern();

    [GeneratedRegex(@"(?:youtube\.com/watch\?(?:.*&)?v=|youtube\.com/embed/|youtube\.com/v/)(?<id>[a-zA-Z0-9_-]{11})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex WatchPattern();

    [GeneratedRegex(@"youtu\.be/(?<id>[a-zA-Z0-9_-]{11})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ShortPattern();

    [GeneratedRegex(@"youtube\.com/embed/(?<id>[a-zA-Z0-9_-]{11})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EmbedPattern();

    [GeneratedRegex(@"(?:[?&]t=|(?:[?&]start=))(?<t>\d+)s?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex StartTimePattern();
}
