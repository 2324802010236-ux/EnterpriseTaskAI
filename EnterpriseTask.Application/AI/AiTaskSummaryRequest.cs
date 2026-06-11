namespace EnterpriseTask.Application.AI;

public class AiTaskSummaryRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Comments { get; set; } = [];
}
