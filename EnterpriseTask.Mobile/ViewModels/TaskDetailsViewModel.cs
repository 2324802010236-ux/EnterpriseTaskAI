using System.Collections.ObjectModel;
using System.Windows.Input;
using EnterpriseTask.Mobile.Helpers;
using EnterpriseTask.Mobile.Models.Mobile;
using EnterpriseTask.Mobile.Services.Mobile;

namespace EnterpriseTask.Mobile.ViewModels;

public sealed class TaskDetailsViewModel(
    MobileTaskService taskService,
    WorkspaceSessionService sessionService) : WorkspaceViewModelBase(sessionService)
{
    private MobileTaskDetailsDto? _task;
    private string _selectedStatus = string.Empty;
    private string _statusNote = string.Empty;
    private string _commentText = string.Empty;

    public int TaskId { get; set; }

    public MobileTaskDetailsDto? Task
    {
        get => _task;
        private set
        {
            if (SetProperty(ref _task, value))
            {
                OnPropertyChanged(nameof(HasTask));
            }
        }
    }

    public bool HasTask => Task is not null;
    public IReadOnlyList<string> Statuses { get; } = ["New", "Assigned", "InProgress", "Review", "Done", "Cancelled"];
    public ObservableCollection<MobileTaskCommentDto> Comments { get; } = [];
    public ObservableCollection<MobileTaskStatusHistoryDto> Histories { get; } = [];

    public string SelectedStatus { get => _selectedStatus; set => SetProperty(ref _selectedStatus, value); }
    public string StatusNote { get => _statusNote; set => SetProperty(ref _statusNote, value); }
    public string CommentText { get => _commentText; set => SetProperty(ref _commentText, value); }

    public ICommand RefreshCommand => new Command(async () => await LoadAsync());
    public ICommand UpdateStatusCommand => new Command(async () => await UpdateStatusAsync());
    public ICommand AddCommentCommand => new Command(async () => await AddCommentAsync());

    public async Task LoadAsync()
    {
        if (TaskId <= 0 || IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            ApplyTask(await taskService.GetTaskDetailsAsync(TaskId));
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Không thể tải chi tiết công việc.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task UpdateStatusAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedStatus))
        {
            await Shell.Current.DisplayAlertAsync("Thiếu trạng thái", "Vui lòng chọn trạng thái mới.", "Đóng");
            return;
        }

        try
        {
            IsBusy = true;
            ApplyTask(await taskService.UpdateStatusAsync(TaskId, SelectedStatus, StatusNote));
            StatusNote = string.Empty;
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Không thể cập nhật trạng thái.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AddCommentAsync()
    {
        if (string.IsNullOrWhiteSpace(CommentText))
        {
            await Shell.Current.DisplayAlertAsync("Thiếu nội dung", "Vui lòng nhập bình luận.", "Đóng");
            return;
        }

        try
        {
            IsBusy = true;
            await taskService.AddCommentAsync(TaskId, CommentText.Trim());
            CommentText = string.Empty;
            ApplyTask(await taskService.GetTaskDetailsAsync(TaskId));
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Không thể gửi bình luận.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyTask(MobileTaskDetailsDto task)
    {
        Task = task;
        SelectedStatus = task.Status;
        Comments.Clear();
        Histories.Clear();
        foreach (var comment in task.Comments)
        {
            Comments.Add(comment);
        }

        foreach (var history in task.StatusHistories)
        {
            Histories.Add(history);
        }
    }
}
