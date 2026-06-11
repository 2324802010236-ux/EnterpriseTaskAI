namespace EnterpriseTask.Application.AI;

public class AiProgressEvaluationResult
{
    public string Evaluation { get; set; } = string.Empty;
    public bool IsAtRisk { get; set; }
    public string Recommendation { get; set; } = string.Empty;
}
