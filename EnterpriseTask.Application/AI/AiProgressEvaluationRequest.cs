namespace EnterpriseTask.Application.AI;

public class AiProgressEvaluationRequest
{
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public List<string> StatusHistories { get; set; } = [];
    public List<string> Comments { get; set; } = [];
}
