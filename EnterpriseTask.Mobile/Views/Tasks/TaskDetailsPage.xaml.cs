using EnterpriseTask.Mobile.ViewModels;

namespace EnterpriseTask.Mobile.Views.Tasks;

public partial class TaskDetailsPage : ContentPage, IQueryAttributable
{
    private readonly TaskDetailsViewModel _viewModel;

    public TaskDetailsPage(TaskDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("taskId", out var value) && int.TryParse(value?.ToString(), out var id))
        {
            _viewModel.TaskId = id;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
