using System.Collections.ObjectModel;
using System.Windows.Threading;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public enum TaskFilterMode
{
    All,
    Pending,
    InProgress,
    Completed
}

public partial class MainWindowViewModel
{
    private readonly IUserTaskService _userTaskService;
    private readonly IUserNoteService _userNoteService;
    private DispatcherTimer? _noteAutoSaveTimer;
    private int? _pendingNoteSaveId;
    private bool _suppressNoteEditorSync;

    [ObservableProperty] private bool _isTasksPanelOpen;
    [ObservableProperty] private bool _isTasksLoading;
    [ObservableProperty] private int _pendingTaskCount;
    [ObservableProperty] private string _pendingTaskBadgeText = "0";
    [ObservableProperty] private TaskFilterMode _selectedTaskFilter = TaskFilterMode.All;
    [ObservableProperty] private string _newTaskTitle = string.Empty;
    [ObservableProperty] private string _newTaskDetails = string.Empty;
    [ObservableProperty] private DateTime? _newTaskDueDate;
    [ObservableProperty] private UserTaskStatus _newTaskStatus = UserTaskStatus.Pending;
    [ObservableProperty] private bool _hasFilteredTasks = true;
    [ObservableProperty] private bool _isTaskAddFormOpen;
    [ObservableProperty] private TaskAssigneeItem? _selectedTaskAssignee;
    [ObservableProperty] private string _selectedNoteLastEditedDisplay = "—";

    public ObservableCollection<TaskAssigneeItem> TaskAssignees { get; } = [];

    [ObservableProperty] private bool _isNotesPanelOpen;
    [ObservableProperty] private bool _isNotesLoading;
    [ObservableProperty] private int _notesCount;
    [ObservableProperty] private UserNoteItem? _selectedNote;
    [ObservableProperty] private string _noteEditorTitle = string.Empty;
    [ObservableProperty] private string _noteEditorContent = string.Empty;
    [ObservableProperty] private bool _isNoteSavedIndicatorVisible;

    public ObservableCollection<UserTaskItem> Tasks { get; } = [];
    public ObservableCollection<UserTaskItem> FilteredTasks { get; } = [];
    public ObservableCollection<UserNoteItem> Notes { get; } = [];

    partial void OnPendingTaskCountChanged(int value) =>
        PendingTaskBadgeText = value > 99 ? "99+" : value.ToString();

    partial void OnSelectedTaskFilterChanged(TaskFilterMode value) => ApplyTaskFilter();

    partial void OnNoteEditorTitleChanged(string value) => ScheduleNoteAutoSave();
    partial void OnNoteEditorContentChanged(string value) => ScheduleNoteAutoSave();

    partial void OnIsTasksPanelOpenChanged(bool value)
    {
        if (value)
        {
            IsTaskAddFormOpen = false;
            _ = RefreshTasksAsync();
        }
    }

    partial void OnIsNotesPanelOpenChanged(bool value)
    {
        if (value)
            _ = RefreshNotesAsync();
    }

    private void CloseOtherPanelsForPersonalWorkspace()
    {
        IsNotificationPanelOpen = false;
        IsQuickAssistOpen = false;
        IsSmartAssistantOpen = false;
        IsGlobalSearchOpen = false;
        IsMenuCustomizerOpen = false;
    }

    private void ClosePersonalWorkspacePanels()
    {
        IsTasksPanelOpen = false;
        IsNotesPanelOpen = false;
    }

    [RelayCommand]
    private async Task ToggleTasksPanelAsync()
    {
        if (IsTasksPanelOpen)
        {
            IsTasksPanelOpen = false;
            return;
        }

        CloseOtherPanelsForPersonalWorkspace();
        IsNotesPanelOpen = false;
        IsTasksPanelOpen = true;
        await RefreshTasksAsync();
    }

    [RelayCommand]
    private void CloseTasksPanel() => IsTasksPanelOpen = false;

    [RelayCommand]
    private async Task RefreshTasksAsync()
    {
        if (IsTasksLoading) return;
        IsTasksLoading = true;
        try
        {
            await LoadTaskAssigneesAsync();

            var items = await _userTaskService.GetAllAsync();
            var currentUserId = _currentUserService.UserId;
            Tasks.Clear();
            foreach (var t in items)
            {
                var assignedByOther = currentUserId.HasValue
                    && t.UserId == currentUserId.Value
                    && t.AssignedByUserId != currentUserId.Value;

                Tasks.Add(new UserTaskItem
                {
                    Id = t.Id,
                    Title = t.Title,
                    Details = t.Details,
                    DueDate = t.DueDate,
                    Status = t.Status,
                    CreatedAt = t.CreatedAt,
                    IsAssignedByOther = assignedByOther,
                    AssignedByDisplayName = assignedByOther
                        ? (string.IsNullOrWhiteSpace(t.AssignedByUser?.FullName)
                            ? t.AssignedByUser?.Username
                            : t.AssignedByUser.FullName)
                        : null
                });
            }

            PendingTaskCount = await _userTaskService.GetPendingCountAsync();
            ApplyTaskFilter();
        }
        finally
        {
            IsTasksLoading = false;
        }
    }

    private async Task LoadTaskAssigneesAsync()
    {
        var users = await _authService.GetAllUsersAsync();
        TaskAssignees.Clear();
        foreach (var user in users.Where(u => u.IsActive).OrderBy(u => u.FullName))
        {
            TaskAssignees.Add(new TaskAssigneeItem
            {
                Id = user.Id,
                DisplayName = string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName
            });
        }

        if (SelectedTaskAssignee is null && _currentUserService.UserId is int currentId)
            SelectedTaskAssignee = TaskAssignees.FirstOrDefault(a => a.Id == currentId) ?? TaskAssignees.FirstOrDefault();
    }

    [RelayCommand]
    private void ShowTaskAddForm()
    {
        IsTaskAddFormOpen = true;
        if (SelectedTaskAssignee is null && _currentUserService.UserId is int currentId)
            SelectedTaskAssignee = TaskAssignees.FirstOrDefault(a => a.Id == currentId) ?? TaskAssignees.FirstOrDefault();
    }

    [RelayCommand]
    private void CancelTaskAddForm()
    {
        IsTaskAddFormOpen = false;
        NewTaskTitle = string.Empty;
        NewTaskDetails = string.Empty;
        NewTaskDueDate = null;
        NewTaskStatus = UserTaskStatus.Pending;
        if (_currentUserService.UserId is int currentId)
            SelectedTaskAssignee = TaskAssignees.FirstOrDefault(a => a.Id == currentId);
    }

    private void ApplyTaskFilter()
    {
        FilteredTasks.Clear();
        foreach (var task in Tasks)
        {
            var include = SelectedTaskFilter switch
            {
                TaskFilterMode.Pending => task.Status == UserTaskStatus.Pending,
                TaskFilterMode.InProgress => task.Status == UserTaskStatus.InProgress,
                TaskFilterMode.Completed => task.Status == UserTaskStatus.Completed,
                _ => true
            };

            if (include)
                FilteredTasks.Add(task);
        }

        HasFilteredTasks = FilteredTasks.Count > 0;
    }

    [RelayCommand]
    private void SetTaskFilter(TaskFilterMode filter) => SelectedTaskFilter = filter;

    [RelayCommand]
    private async Task AddTaskAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTaskTitle))
        {
            _toast.ShowWarning("يرجى إدخال عنوان المهمة.");
            return;
        }

        if (SelectedTaskAssignee is null)
        {
            _toast.ShowWarning("يرجى اختيار المستخدم المُسندة إليه المهمة.");
            return;
        }

        try
        {
            var assigneeName = SelectedTaskAssignee.DisplayName;
            var assignedToSelf = _currentUserService.UserId == SelectedTaskAssignee.Id;

            await _userTaskService.CreateAsync(
                NewTaskTitle, NewTaskDetails, NewTaskDueDate, NewTaskStatus, SelectedTaskAssignee.Id);

            IsTaskAddFormOpen = false;
            NewTaskTitle = string.Empty;
            NewTaskDetails = string.Empty;
            NewTaskDueDate = null;
            NewTaskStatus = UserTaskStatus.Pending;
            if (_currentUserService.UserId is int currentId)
                SelectedTaskAssignee = TaskAssignees.FirstOrDefault(a => a.Id == currentId);

            await RefreshTasksAsync();

            _sound.Play(SoundEffect.Success);
            _toast.ShowSuccess(assignedToSelf
                ? "تمت إضافة المهمة."
                : $"تم إسناد المهمة إلى {assigneeName}.");
        }
        catch (Exception ex)
        {
            _toast.ShowError($"تعذر إضافة المهمة: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ToggleTaskStatusAsync(UserTaskItem? task)
    {
        if (task is null) return;

        var newStatus = task.Status == UserTaskStatus.Completed
            ? UserTaskStatus.Pending
            : UserTaskStatus.Completed;

        try
        {
            await _userTaskService.UpdateStatusAsync(task.Id, newStatus);
            task.Status = newStatus;
            PendingTaskCount = await _userTaskService.GetPendingCountAsync();
            ApplyTaskFilter();
            _sound.Play(SoundEffect.Click);
        }
        catch (Exception ex)
        {
            _toast.ShowError($"تعذر تحديث المهمة: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DeleteTaskAsync(UserTaskItem? task)
    {
        if (task is null) return;

        try
        {
            await _userTaskService.DeleteAsync(task.Id);
            Tasks.Remove(task);
            PendingTaskCount = await _userTaskService.GetPendingCountAsync();
            ApplyTaskFilter();
            _toast.ShowInfo("تم حذف المهمة.");
        }
        catch (Exception ex)
        {
            _toast.ShowError($"تعذر حذف المهمة: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ToggleNotesPanelAsync()
    {
        if (IsNotesPanelOpen)
        {
            await FlushNoteSaveAsync();
            IsNotesPanelOpen = false;
            return;
        }

        CloseOtherPanelsForPersonalWorkspace();
        IsTasksPanelOpen = false;
        IsNotesPanelOpen = true;
        await RefreshNotesAsync();
    }

    [RelayCommand]
    private async Task CloseNotesPanelAsync()
    {
        await FlushNoteSaveAsync();
        IsNotesPanelOpen = false;
    }

    [RelayCommand]
    private async Task RefreshNotesAsync()
    {
        if (IsNotesLoading) return;
        IsNotesLoading = true;
        try
        {
            await FlushNoteSaveAsync();

            var items = await _userNoteService.GetAllAsync();
            Notes.Clear();
            foreach (var n in items)
            {
                Notes.Add(new UserNoteItem
                {
                    Id = n.Id,
                    Title = n.Title,
                    Content = n.Content,
                    LastEditedAt = n.LastEditedAt
                });
            }

            NotesCount = Notes.Count;

            if (Notes.Count == 0)
            {
                SelectedNote = null;
                SyncNoteEditor(null);
                return;
            }

            var current = SelectedNote is not null
                ? Notes.FirstOrDefault(n => n.Id == SelectedNote.Id)
                : null;

            SelectNote(current ?? Notes[0]);
        }
        finally
        {
            IsNotesLoading = false;
        }
    }

    [RelayCommand]
    private async Task CreateNoteAsync()
    {
        try
        {
            await FlushNoteSaveAsync();
            var created = await _userNoteService.CreateAsync();
            var item = new UserNoteItem
            {
                Id = created.Id,
                Title = created.Title,
                Content = created.Content,
                LastEditedAt = created.LastEditedAt
            };

            Notes.Insert(0, item);
            NotesCount = Notes.Count;
            SelectNote(item);
            _sound.Play(SoundEffect.Success);
        }
        catch (Exception ex)
        {
            _toast.ShowError($"تعذر إنشاء الملاحظة: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SelectNoteAsync(UserNoteItem? note)
    {
        if (note is null) return;
        await FlushNoteSaveAsync();
        SelectNote(note);
    }

    private void SelectNote(UserNoteItem? note)
    {
        SelectedNote = note;
        SyncNoteEditor(note);
        SelectedNoteLastEditedDisplay = note?.LastEditedDisplay ?? "—";
    }

    partial void OnSelectedNoteChanged(UserNoteItem? value) =>
        SelectedNoteLastEditedDisplay = value?.LastEditedDisplay ?? "—";

    private void SyncNoteEditor(UserNoteItem? note)
    {
        _suppressNoteEditorSync = true;
        NoteEditorTitle = note?.Title ?? string.Empty;
        NoteEditorContent = note?.Content ?? string.Empty;
        _suppressNoteEditorSync = false;
    }

    [RelayCommand]
    private async Task DeleteNoteAsync()
    {
        if (SelectedNote is null) return;

        try
        {
            var id = SelectedNote.Id;
            await _userNoteService.DeleteAsync(id);
            var index = Notes.IndexOf(SelectedNote);
            Notes.Remove(SelectedNote);

            NotesCount = Notes.Count;
            if (Notes.Count == 0)
            {
                SelectedNote = null;
                SyncNoteEditor(null);
                return;
            }

            var nextIndex = Math.Min(index, Notes.Count - 1);
            SelectNote(Notes[nextIndex]);
            _toast.ShowInfo("تم حذف الملاحظة.");
        }
        catch (Exception ex)
        {
            _toast.ShowError($"تعذر حذف الملاحظة: {ex.Message}");
        }
    }

    private void ScheduleNoteAutoSave()
    {
        if (_suppressNoteEditorSync || SelectedNote is null || !IsNotesPanelOpen)
            return;

        _pendingNoteSaveId = SelectedNote.Id;
        _noteAutoSaveTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
        _noteAutoSaveTimer.Tick -= OnNoteAutoSaveTick;
        _noteAutoSaveTimer.Tick += OnNoteAutoSaveTick;
        _noteAutoSaveTimer.Stop();
        _noteAutoSaveTimer.Start();
    }

    private async void OnNoteAutoSaveTick(object? sender, EventArgs e)
    {
        _noteAutoSaveTimer?.Stop();
        await SaveCurrentNoteAsync(showIndicator: true);
    }

    private async Task FlushNoteSaveAsync() =>
        await SaveCurrentNoteAsync(showIndicator: false);

    private async Task SaveCurrentNoteAsync(bool showIndicator)
    {
        if (SelectedNote is null || _suppressNoteEditorSync)
            return;

        if (_pendingNoteSaveId is int pendingId && pendingId != SelectedNote.Id)
            return;

        try
        {
            await _userNoteService.UpdateAsync(SelectedNote.Id, NoteEditorTitle, NoteEditorContent);
            SelectedNote.Title = NoteEditorTitle;
            SelectedNote.Content = NoteEditorContent;
            SelectedNote.LastEditedAt = DateTime.UtcNow;
            SelectedNoteLastEditedDisplay = SelectedNote.LastEditedDisplay;
            _pendingNoteSaveId = null;

            if (showIndicator)
            {
                IsNoteSavedIndicatorVisible = true;
                await Task.Delay(1200);
                IsNoteSavedIndicatorVisible = false;
            }
        }
        catch (Exception ex)
        {
            _toast.ShowError($"تعذر حفظ الملاحظة: {ex.Message}");
        }
    }

    public async Task InitializePersonalWorkspaceAsync()
    {
        try
        {
            PendingTaskCount = await _userTaskService.GetPendingCountAsync();
        }
        catch
        {
            PendingTaskCount = 0;
        }
    }

    public void ResetPersonalWorkspaceSession()
    {
        IsTasksPanelOpen = false;
        IsNotesPanelOpen = false;
        IsTaskAddFormOpen = false;
        Tasks.Clear();
        TaskAssignees.Clear();
        FilteredTasks.Clear();
        Notes.Clear();
        SelectedNote = null;
        PendingTaskCount = 0;
        NotesCount = 0;
    }
}
