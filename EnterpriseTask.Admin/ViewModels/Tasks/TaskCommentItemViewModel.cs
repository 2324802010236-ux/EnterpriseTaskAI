namespace EnterpriseTask.Admin.ViewModels.Tasks;

public class TaskCommentItemViewModel
{
    public string Content { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
