namespace AlMuhasib.UI.Services;

public interface INavigationService
{
    event Action<ViewModels.ViewModelBase>? CurrentViewModelChanged;
    ViewModels.ViewModelBase? CurrentViewModel { get; }
    void NavigateTo<TViewModel>() where TViewModel : ViewModels.ViewModelBase;
    void NavigateTo(Type viewModelType);
    bool CanGoBack { get; }
    void GoBack();
}
