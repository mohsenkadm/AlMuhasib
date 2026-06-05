using System.Collections.ObjectModel;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class HelpVideosViewModel : ObservableObject
{
    private readonly IHelpSupportService _helpSupport;
    private readonly List<HelpVideoItemVm> _allVideos = [];

    public ObservableCollection<HelpVideoItemVm> Videos { get; } = [];

    [ObservableProperty]
    private HelpVideoItemVm? _selectedVideo;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isEmpty;

    public HelpVideosViewModel(IHelpSupportService helpSupport)
    {
        _helpSupport = helpSupport;
        Reload();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedVideoChanged(HelpVideoItemVm? value) =>
        VideoSelectionChanged?.Invoke(value);

    public event Action<HelpVideoItemVm?>? VideoSelectionChanged;

    [RelayCommand]
    private void Reload()
    {
        _allVideos.Clear();
        _allVideos.AddRange(_helpSupport.GetAllVideos());
        ApplyFilter();

        if (Videos.Count > 0 && SelectedVideo is null)
            SelectedVideo = Videos[0];
    }

    private void ApplyFilter()
    {
        Videos.Clear();
        var term = SearchText?.Trim();
        IEnumerable<HelpVideoItemVm> query = _allVideos;

        if (!string.IsNullOrEmpty(term))
        {
            query = _allVideos.Where(v =>
                v.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                v.CategoryTitle.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                v.Description.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in query)
            Videos.Add(item);

        IsEmpty = Videos.Count == 0;

        if (SelectedVideo is null || !Videos.Contains(SelectedVideo))
            SelectedVideo = Videos.FirstOrDefault();
    }
}
