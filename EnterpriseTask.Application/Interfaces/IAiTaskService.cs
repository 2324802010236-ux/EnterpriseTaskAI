using EnterpriseTask.Application.AI;

namespace EnterpriseTask.Application.Interfaces;

public interface IAiTaskService
{
    Task<AiAssignmentSuggestionResult> SuggestAssignmentAsync(
        AiAssignmentSuggestionRequest request,
        CancellationToken cancellationToken = default);

    Task<AiTaskSummaryResult> SummarizeTaskAsync(
        AiTaskSummaryRequest request,
        CancellationToken cancellationToken = default);

    Task<AiPrioritySuggestionResult> SuggestPriorityAsync(
        AiPrioritySuggestionRequest request,
        CancellationToken cancellationToken = default);

    Task<AiProgressEvaluationResult> EvaluateProgressAsync(
        AiProgressEvaluationRequest request,
        CancellationToken cancellationToken = default);
}
