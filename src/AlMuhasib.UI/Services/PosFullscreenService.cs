using System.Windows;
using AlMuhasib.UI.ViewModels;
using AlMuhasib.UI.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace AlMuhasib.UI.Services;

public sealed class PosFullscreenService : IPosFullscreenService
{
    private readonly IServiceProvider _services;
    private PosFullscreenWindow? _window;
    private PosQuickSaleViewModel? _viewModel;

    public PosFullscreenService(IServiceProvider services)
    {
        _services = services;
    }

    public bool IsOpen => _window is not null;

    public void Open(PosQuickSaleViewModel viewModel)
    {
        if (_window is not null)
        {
            _window.Activate();
            _window.WindowState = WindowState.Maximized;
            return;
        }

        _viewModel = viewModel;
        viewModel.IsFullscreenActive = true;

        _window = _services.GetRequiredService<PosFullscreenWindow>();
        _window.Initialize(viewModel);
        _window.Closed += OnWindowClosed;
        _window.Show();
        _window.Activate();
    }

    public void Close()
    {
        if (_window is null)
            return;

        var window = _window;
        _window = null;
        window.Closed -= OnWindowClosed;
        window.Close();
        FinishClose();
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        if (_window is not null)
            _window.Closed -= OnWindowClosed;
        _window = null;
        FinishClose();
    }

    private void FinishClose()
    {
        if (_viewModel is not null)
        {
            _viewModel.IsFullscreenActive = false;
            _viewModel = null;
        }
    }
}
