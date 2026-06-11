using System.Text.RegularExpressions;
using EnterpriseTask.Application.AI;
using EnterpriseTask.Application.Interfaces;
using EnterpriseTask.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseTask.Infrastructure.AI;

// Mock AI service for demo. Replace with real AI provider later.
public partial class MockAiTaskService(
    IOptions<AiSettings> options,
    ILogger<MockAiTaskService> logger) : IAiTaskService
{
    private static readonly string[] UrgentKeywords = ["khẩn", "gấp", "urgent"];
    private readonly AiSettings settings = options.Value;

    public Task<AiAssignmentSuggestionResult> SuggestAssignmentAsync(
        AiAssignmentSuggestionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LogMockProvider();

        var user = request.CandidateUsers
            .OrderBy(CalculateUserLoad)
            .ThenBy(item => item.ActiveTaskCount)
            .ThenBy(item => item.FullName)
            .FirstOrDefault();
        var department = request.CandidateDepartments
            .OrderBy(CalculateDepartmentLoad)
            .ThenBy(item => item.ActiveTaskCount)
            .ThenBy(item => item.DepartmentName)
            .FirstOrDefault();

        if (user is null && department is null)
        {
            return Task.FromResult(new AiAssignmentSuggestionResult
            {
                SuggestedTargetType = "Unassigned",
                SuggestedName = "Chưa có ứng viên phù hợp",
                Reason = "Công ty chưa có nhân sự hoặc phòng ban active phù hợp để giao việc.",
                ConfidenceScore = 0.20m
            });
        }

        var userLoad = user is null ? decimal.MaxValue : CalculateUserLoad(user);
        var departmentLoad = department is null ? decimal.MaxValue : CalculateDepartmentLoad(department);
        var isUrgent = ContainsUrgentKeyword(request.TaskTitle, request.TaskDescription);
        var confidence = isUrgent ? 0.90m : 0.78m;

        if (user is not null && userLoad <= departmentLoad)
        {
            return Task.FromResult(new AiAssignmentSuggestionResult
            {
                SuggestedTargetType = "User",
                SuggestedUserId = user.UserId,
                SuggestedName = user.FullName,
                Reason =
                    $"{user.FullName} có tải công việc phù hợp: {user.ActiveTaskCount} task đang hoạt động, "
                    + $"{user.OverdueTaskCount} task quá hạn và {user.DoneTaskCount} task đã hoàn thành.",
                ConfidenceScore = confidence
            });
        }

        return Task.FromResult(new AiAssignmentSuggestionResult
        {
            SuggestedTargetType = "Department",
            SuggestedDepartmentId = department!.DepartmentId,
            SuggestedName = department.DepartmentName,
            Reason =
                $"{department.DepartmentName} có tải công việc bình quân phù hợp với "
                + $"{department.MemberCount} thành viên và {department.OverdueTaskCount} task quá hạn.",
            ConfidenceScore = confidence
        });
    }

    public Task<AiTaskSummaryResult> SummarizeTaskAsync(
        AiTaskSummaryRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LogMockProvider();

        var description = Normalize(request.Description);
        var sentences = ImportantSentenceRegex()
            .Split(description)
            .Select(Normalize)
            .Where(item => item.Length > 0)
            .Take(3)
            .ToList();
        var keyPoints = sentences.Count > 0
            ? sentences
            : request.Comments
                .Select(Normalize)
                .Where(item => item.Length > 0)
                .Take(3)
                .ToList();
        if (keyPoints.Count == 0)
        {
            keyPoints.Add(request.Title);
        }

        var summaryText = description.Length == 0
            ? request.Title
            : $"{request.Title}: {Truncate(description, 280)}";
        return Task.FromResult(new AiTaskSummaryResult
        {
            Summary = summaryText,
            KeyPoints = keyPoints
        });
    }

    public Task<AiPrioritySuggestionResult> SuggestPriorityAsync(
        AiPrioritySuggestionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LogMockProvider();

        var now = DateTime.UtcNow;
        if (ContainsUrgentKeyword(request.Title, request.Description))
        {
            return Task.FromResult(Priority(
                WorkTaskPriority.Urgent,
                "Tiêu đề hoặc mô tả chứa từ khóa khẩn cấp."));
        }

        if (request.DueDate.HasValue)
        {
            var remaining = request.DueDate.Value - now;
            if (remaining <= TimeSpan.FromDays(1))
            {
                return Task.FromResult(Priority(
                    WorkTaskPriority.Urgent,
                    "Deadline đã quá hạn hoặc còn không quá 24 giờ."));
            }

            if (remaining <= TimeSpan.FromDays(3))
            {
                return Task.FromResult(Priority(
                    WorkTaskPriority.High,
                    "Deadline còn không quá 3 ngày."));
            }

            if (remaining >= TimeSpan.FromDays(14))
            {
                return Task.FromResult(Priority(
                    WorkTaskPriority.Low,
                    "Deadline còn xa, có thể lên kế hoạch xử lý theo mức ưu tiên thấp."));
            }
        }

        return Task.FromResult(Priority(
            WorkTaskPriority.Medium,
            "Không phát hiện tín hiệu khẩn cấp hoặc deadline quá gần."));
    }

    public Task<AiProgressEvaluationResult> EvaluateProgressAsync(
        AiProgressEvaluationRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LogMockProvider();

        if (request.Status.Equals(nameof(WorkTaskStatus.Done), StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new AiProgressEvaluationResult
            {
                Evaluation = "Công việc đã hoàn thành.",
                IsAtRisk = false,
                Recommendation = "Kiểm tra kết quả cuối và lưu lại bài học triển khai."
            });
        }

        if (request.Status.Equals(nameof(WorkTaskStatus.Cancelled), StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new AiProgressEvaluationResult
            {
                Evaluation = "Công việc đã được hủy.",
                IsAtRisk = false,
                Recommendation = "Xác nhận lý do hủy và cập nhật các bên liên quan."
            });
        }

        var now = DateTime.UtcNow;
        if (request.Status.Equals(nameof(WorkTaskStatus.Overdue), StringComparison.OrdinalIgnoreCase)
            || (request.DueDate.HasValue && request.DueDate.Value < now))
        {
            return Task.FromResult(new AiProgressEvaluationResult
            {
                Evaluation = "Công việc đang quá hạn và có rủi ro cao.",
                IsAtRisk = true,
                Recommendation = "Rà soát nguyên nhân chậm, cập nhật deadline và phân bổ thêm nguồn lực."
            });
        }

        if (request.Status.Equals(nameof(WorkTaskStatus.InProgress), StringComparison.OrdinalIgnoreCase)
            && request.DueDate.HasValue
            && request.DueDate.Value - now <= TimeSpan.FromDays(2))
        {
            return Task.FromResult(new AiProgressEvaluationResult
            {
                Evaluation = "Công việc đang thực hiện nhưng deadline đã gần.",
                IsAtRisk = true,
                Recommendation = "Xác nhận phần việc còn lại và cập nhật tiến độ trong ngày."
            });
        }

        var hasActivity = request.StatusHistories.Count > 0 || request.Comments.Count > 0;
        return Task.FromResult(new AiProgressEvaluationResult
        {
            Evaluation = hasActivity
                ? "Công việc đang có hoạt động và chưa phát hiện rủi ro deadline rõ ràng."
                : "Công việc chưa có nhiều tín hiệu cập nhật tiến độ.",
            IsAtRisk = !hasActivity,
            Recommendation = hasActivity
                ? "Tiếp tục cập nhật trạng thái và trao đổi định kỳ."
                : "Yêu cầu người phụ trách cập nhật trạng thái hoặc bình luận tiến độ."
        });
    }

    private void LogMockProvider()
    {
        logger.LogDebug(
            "Using mock AI task service. Enabled: {Enabled}; Provider: {Provider}; Model: {Model}.",
            settings.Enabled,
            settings.Provider,
            settings.Model);
    }

    private static decimal CalculateUserLoad(AiCandidateUserDto candidate) =>
        candidate.ActiveTaskCount
        + candidate.OverdueTaskCount * 3m
        - Math.Min(candidate.DoneTaskCount, 10) * 0.05m;

    private static decimal CalculateDepartmentLoad(AiCandidateDepartmentDto candidate)
    {
        var members = Math.Max(candidate.MemberCount, 1);
        return (candidate.ActiveTaskCount + candidate.OverdueTaskCount * 3m) / members
            - Math.Min(candidate.DoneTaskCount, 20) * 0.02m;
    }

    private static bool ContainsUrgentKeyword(params string?[] values)
    {
        var input = string.Join(' ', values.Where(value => !string.IsNullOrWhiteSpace(value)));
        return UrgentKeywords.Any(keyword =>
            input.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static AiPrioritySuggestionResult Priority(
        WorkTaskPriority priority,
        string reason) =>
        new()
        {
            SuggestedPriority = priority,
            Reason = reason
        };

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : WhitespaceRegex().Replace(value.Trim(), " ");

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : $"{value[..(maxLength - 3)]}...";

    [GeneratedRegex(@"(?<=[.!?])\s+|\r?\n+")]
    private static partial Regex ImportantSentenceRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
