using System.Collections.ObjectModel;
using System.Windows.Input;
using EnterpriseTask.Mobile.Constants;
using EnterpriseTask.Mobile.Helpers;
using EnterpriseTask.Mobile.Models.Mobile;
using EnterpriseTask.Mobile.Services.Mobile;

namespace EnterpriseTask.Mobile.ViewModels;

public sealed class TasksViewModel(
    MobileTaskService taskService,
    WorkspaceSessionService sessionService) : WorkspaceViewModelBase(sessionService)
{
    public ObservableCollection<MobileTaskListItemDto> Tasks { get; } = [];

    public bool HasTasks => Tasks.Count > 0;

    public ICommand RefreshCommand => new Command(async () => await LoadAsync());

    public ICommand OpenTaskCommand => new Command<MobileTaskListItemDto>(async task =>
    {
        if (task is null)
        {
            return;
        }

        await Shell.Current.GoToAsync(
            AppConstants.TaskDetailsRoute,
            new ShellNavigationQueryParameters { ["taskId"] = task.Id });
    });

    public async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            var tasks = await taskService.GetTasksAsync();
            Tasks.Clear();
            foreach (var task in tasks)
            {
                Tasks.Add(task);
            }

            OnPropertyChanged(nameof(HasTasks));
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Không thể tải danh sách công việc.");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
