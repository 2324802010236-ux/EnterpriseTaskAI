namespace EnterpriseTask.Application.AI;

public class AiAssignmentSuggestionResult
{
    public string SuggestedTargetType { get; set; } = string.Empty;
    public string? SuggestedUserId { get; set; }
    public int? SuggestedDepartmentId { get; set; }
    public string SuggestedName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public decimal ConfidenceScore { get; set; }
}
