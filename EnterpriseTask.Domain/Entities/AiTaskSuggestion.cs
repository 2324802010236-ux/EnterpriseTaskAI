namespace EnterpriseTask.Domain.Entities;

public class AiTaskSuggestion
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int? WorkTaskId { get; set; }
    public string RequestedByUserId { get; set; } = string.Empty;
    public string InputText { get; set; } = string.Empty;
    public int? SuggestedDepartmentId { get; set; }
    public string? SuggestedUserId { get; set; }
    public string? Summary { get; set; }
    public string? Reason { get; set; }
    public decimal? Score { get; set; }
    public DateTime CreatedAt { get; set; }
}
