using System.Diagnostics;
using System.Windows;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.ViewModels;
using AlMuhasib.UI.Windows;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;

namespace AlMuhasib.UI.Services;

public sealed class ModuleDocumentWindowService : IModuleDocumentWindowService
{
    private readonly IServiceProvider _services;
    private readonly List<ModuleDocumentWindow> _windows = [];
    private readonly object _gate = new();

    public ModuleDocumentWindowService(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<bool> TryOpenAsync(
        Type viewModelType,
        string title,
        PackIconKind icon,
        bool activateIfExists,
        string? permissionScreenName)
    {
        if (!CustomsPermissionRegistry.IsCustomsDocumentViewModel(viewModelType))
            return false;

        if (activateIfExists)
        {
            ModuleDocumentWindow? existing;
            lock (_gate)
            {
                existing = _windows.FirstOrDefault(w => w.ViewModelType == viewModelType && w.IsLoaded);
            }

            if (existing is not null)
            {
                if (existing.WindowState == WindowState.Minimized)
                    existing.WindowState = WindowState.Maximized;
                existing.Activate();
                existing.Focus();
                if (existing.ViewModel is not null)
                    ConsumePendingActivation(existing.ViewModel);
                return true;
            }
        }

        IServiceScope? scope = null;
        ModuleDocumentWindow? window = null;
        try
        {
            scope = _services.CreateScope();
            var viewModel = (ViewModelBase)scope.ServiceProvider.GetRequiredService(viewModelType);
            // Window from root so disposing the VM scope does not dispose the Window mid-close.
            window = _services.GetRequiredService<ModuleDocumentWindow>();
            window.Initialize(viewModelType, viewModel, scope, title, permissionScreenName);
            window.Closed += OnWindowClosed;

            lock (_gate)
            {
                _windows.Add(window);
            }

            window.Show();
            window.Activate();
            window.WindowState = WindowState.Maximized;

            await SafeInitializeAsync(viewModel, title);
            ConsumePendingActivation(viewModel);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ModuleWindows] Open failed for {viewModelType.Name}: {ex}");
            if (window is not null)
            {
                window.Closed -= OnWindowClosed;
                lock (_gate)
                {
                    _windows.Remove(window);
                }

                try
                {
                    window.ForceClose();
                }
                catch
                {
                    // ignored
                }
            }
            else
            {
                scope?.Dispose();
            }

            BeautifulMessageDialog.ShowError(
                $"تعذّر فتح الشاشة «{title}»:\n\n{ex.InnerException?.Message ?? ex.Message}");
            // Path was handled as a customs document open attempt — do not also open a tab.
            return true;
        }
    }

    public bool TryCloseForViewModel(ViewModelBase viewModel)
    {
        ModuleDocumentWindow? window;
        lock (_gate)
        {
            window = _windows.FirstOrDefault(w => ReferenceEquals(w.ViewModel, viewModel));
        }

        if (window is null)
            return false;

        window.RequestClose();
        return true;
    }

    public bool IsOpen(Type viewModelType)
    {
        lock (_gate)
        {
            return _windows.Any(w => w.ViewModelType == viewModelType && w.IsLoaded);
        }
    }

    public bool TryCloseAll()
    {
        List<ModuleDocumentWindow> snapshot;
        lock (_gate)
        {
            snapshot = _windows.ToList();
        }

        foreach (var window in snapshot)
        {
            window.RequestClose();
            if (window.IsLoaded && window.IsVisible)
                return false;
        }

        return true;
    }

    public void ForceCloseAll()
    {
        List<ModuleDocumentWindow> snapshot;
        lock (_gate)
        {
            snapshot = _windows.ToList();
            _windows.Clear();
        }

        foreach (var window in snapshot)
        {
            try
            {
                window.Closed -= OnWindowClosed;
                window.ForceClose();
            }
            catch
            {
                // ignored on shutdown
            }
        }
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        if (sender is not ModuleDocumentWindow window)
            return;

        window.Closed -= OnWindowClosed;
        lock (_gate)
        {
            _windows.Remove(window);
        }
    }

    private static async Task SafeInitializeAsync(ViewModelBase viewModel, string title)
    {
        try
        {
            await viewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ModuleWindows] InitializeAsync failed for {title}: {ex}");
            BeautifulMessageDialog.ShowError(
                $"تعذّر تهيئة الشاشة «{title}»:\n\n{ex.InnerException?.Message ?? ex.Message}");
        }
    }

    private static void ConsumePendingActivation(ViewModelBase viewModel)
    {
        if (viewModel is not ViewModels.Customs.CustomsCustomersViewModel customers)
            return;
        if (!CustomsCustomersNavigationBridge.TakePendingOpenAdd())
            return;
        if (customers.OpenAddDialogCommand.CanExecute(null))
            customers.OpenAddDialogCommand.Execute(null);
    }
}
