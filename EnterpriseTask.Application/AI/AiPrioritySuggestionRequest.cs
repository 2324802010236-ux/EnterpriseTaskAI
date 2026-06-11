namespace EnterpriseTask.Application.AI;

public class AiPrioritySuggestionRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
}
