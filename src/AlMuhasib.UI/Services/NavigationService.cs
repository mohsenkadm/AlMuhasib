using AlMuhasib.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.Services;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;

    public event Action<ViewModelBase>? CurrentViewModelChanged;

    public ViewModelBase? CurrentViewModel { get; private set; }
    public bool CanGoBack => _previousViewModel is not null;

    private ViewModelBase? _previousViewModel;

    // Each navigation gets its own DI scope → its own DbContext
    private IServiceScope? _currentScope;
    private IServiceScope? _previousScope;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
    {
        NavigateTo(typeof(TViewModel));
    }

    public void NavigateTo(Type viewModelType)
    {
        // Dispose the scope that is falling off (two navigations back)
        _previousScope?.Dispose();

        // Current becomes previous
        _previousViewModel = CurrentViewModel;
        _previousScope = _currentScope;

        // Create a fresh scope for the new navigation
        _currentScope = _serviceProvider.CreateScope();
        var viewModel = (ViewModelBase)_currentScope.ServiceProvider.GetRequiredService(viewModelType);
        CurrentViewModel = viewModel;
        _ = SafeInitializeAsync(viewModel);
        CurrentViewModelChanged?.Invoke(viewModel);
    }

    private static async Task SafeInitializeAsync(ViewModelBase viewModel)
    {
        try
        {
            await viewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Navigation] InitializeAsync failed for {viewModel.GetType().Name}: {ex}");
            try
            {
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
                {
                    Controls.BeautifulMessageDialog.ShowError(
                        $"خطأ في تحميل الشاشة:\n\n{ex.InnerException?.Message ?? ex.Message}");
                });
            }
            catch
            {
                // Last resort: at least log it
                System.Diagnostics.Debug.WriteLine($"[Navigation] Could not show error dialog: {ex}");
            }
        }
    }

    public void GoBack()
    {
        if (_previousViewModel is null) return;

        // Dispose current scope
        _currentScope?.Dispose();

        // Restore previous
        _currentScope = _previousScope;
        _previousScope = null;

        CurrentViewModel = _previousViewModel;
        _previousViewModel = null;
        CurrentViewModelChanged?.Invoke(CurrentViewModel);
    }
}
