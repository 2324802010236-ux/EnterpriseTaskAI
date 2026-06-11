namespace EnterpriseTask.Application.AI;

public class AiAssignmentSuggestionRequest
{
    public int CompanyId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public string? TaskDescription { get; set; }
    public DateTime? DueDate { get; set; }
    public List<AiCandidateUserDto> CandidateUsers { get; set; } = [];
    public List<AiCandidateDepartmentDto> CandidateDepartments { get; set; } = [];
}
