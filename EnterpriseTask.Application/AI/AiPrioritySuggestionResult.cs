using System.Text.Json.Serialization;
using EnterpriseTask.Domain.Enums;

namespace EnterpriseTask.Application.AI;

public class AiPrioritySuggestionResult
{
    [JsonConverter(typeof(JsonStringEnumConverter<WorkTaskPriority>))]
    public WorkTaskPriority SuggestedPriority { get; set; }
    public string Reason { get; set; } = string.Empty;
}
