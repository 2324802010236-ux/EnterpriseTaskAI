using EnterpriseTask.Mobile.Services.Mobile;

namespace EnterpriseTask.Mobile.Helpers;

public abstract class WorkspaceViewModelBase(WorkspaceSessionService sessionService) : ViewModelBase
{
    private string _errorMessage = string.Empty;

    protected WorkspaceSessionService SessionService { get; } = sessionService;

    public string ErrorMessage
    {
        get => _errorMessage;
        protected set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    protected async Task HandleErrorAsync(Exception exception, string fallbackMessage)
    {
        if (await SessionService.HandleAccessFailureAsync(exception))
        {
            return;
        }

        ErrorMessage = string.IsNullOrWhiteSpace(exception.Message) ? fallbackMessage : exception.Message;
        await Shell.Current.DisplayAlertAsync("Có lỗi xảy ra", ErrorMessage, "Đóng");
    }
}
