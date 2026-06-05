using System.Diagnostics;
using System.Windows;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.ViewModels;
using Microsoft.Web.WebView2.Core;

namespace AlMuhasib.UI.Windows;

public partial class HelpVideosWindow : Window
{
    private readonly HelpVideosViewModel _viewModel;
    private bool _webViewReady;
    private CoreWebView2? _coreWebView;

    public HelpVideosWindow(HelpVideosViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        _viewModel.VideoSelectionChanged += OnVideoSelectionChanged;
        Loaded += OnLoadedAsync;
        Closed += OnClosed;
    }

    private async void OnLoadedAsync(object sender, RoutedEventArgs e)
    {
        try
        {
            await VideoWebView.EnsureCoreWebView2Async();
            _coreWebView = VideoWebView.CoreWebView2;
            ConfigureYouTubeReferrer(_coreWebView);
            _webViewReady = true;
            PlayerLoadingOverlay.Visibility = Visibility.Collapsed;
            UpdatePlayer(_viewModel.SelectedVideo);
        }
        catch
        {
            PlayerLoadingOverlay.Visibility = Visibility.Collapsed;
            NoVideoOverlay.Visibility = Visibility.Visible;
            VideoWebView.Visibility = Visibility.Collapsed;
        }
    }

    private static void ConfigureYouTubeReferrer(CoreWebView2 core)
    {
        core.AddWebResourceRequestedFilter("*youtube.com/*", CoreWebView2WebResourceContext.All);
        core.AddWebResourceRequestedFilter("*youtube-nocookie.com/*", CoreWebView2WebResourceContext.All);
        core.AddWebResourceRequestedFilter("*googlevideo.com/*", CoreWebView2WebResourceContext.All);
        core.AddWebResourceRequestedFilter("*ytimg.com/*", CoreWebView2WebResourceContext.All);
        core.WebResourceRequested += OnWebResourceRequested;
    }

    private static void OnWebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        var headers = e.Request.Headers;
        var origin = YouTubeHelper.EmbedRefererOrigin;

        if (!headers.Contains("Referer"))
            headers.SetHeader("Referer", origin + "/");

        if (!headers.Contains("Origin"))
            headers.SetHeader("Origin", origin);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel.VideoSelectionChanged -= OnVideoSelectionChanged;

        if (_coreWebView is not null)
            _coreWebView.WebResourceRequested -= OnWebResourceRequested;
    }

    private void OnVideoSelectionChanged(HelpVideoItemVm? video) => UpdatePlayer(video);

    private void UpdatePlayer(HelpVideoItemVm? video)
    {
        if (!_webViewReady)
            return;

        if (video is null || !video.HasVideo)
        {
            NoVideoOverlay.Visibility = Visibility.Visible;
            VideoWebView.Visibility = Visibility.Collapsed;
            return;
        }

        NoVideoOverlay.Visibility = Visibility.Collapsed;
        VideoWebView.Visibility = Visibility.Visible;

        var startSeconds = YouTubeHelper.ExtractStartSeconds(video.YoutubeUrl);
        var html = YouTubeHelper.BuildEmbedHtml(video.VideoId!, startSeconds);
        VideoWebView.NavigateToString(html);
    }

    private void OpenInBrowser_Click(object sender, RoutedEventArgs e)
    {
        var video = _viewModel.SelectedVideo;
        if (video?.VideoId is null)
            return;

        var url = string.IsNullOrWhiteSpace(video.YoutubeUrl)
            ? YouTubeHelper.BuildWatchUri(video.VideoId)
            : video.YoutubeUrl.Trim();

        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Controls.BeautifulMessageDialog.ShowError($"تعذّر فتح يوتيوب:\n{ex.Message}");
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
