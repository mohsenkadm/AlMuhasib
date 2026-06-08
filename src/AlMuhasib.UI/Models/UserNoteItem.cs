using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.Models;

public partial class UserNoteItem : ObservableObject
{
    public int Id { get; init; }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private DateTime _lastEditedAt;

    public string PreviewText
    {
        get
        {
            var text = string.IsNullOrWhiteSpace(Content) ? Title : Content;
            text = text.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return text.Length <= 40 ? text : text[..40] + "…";
        }
    }

    public string LastEditedDisplay => LastEditedAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm");

    partial void OnTitleChanged(string value) => OnPropertyChanged(nameof(PreviewText));
    partial void OnContentChanged(string value) => OnPropertyChanged(nameof(PreviewText));
    partial void OnLastEditedAtChanged(DateTime value) => OnPropertyChanged(nameof(LastEditedDisplay));
}
