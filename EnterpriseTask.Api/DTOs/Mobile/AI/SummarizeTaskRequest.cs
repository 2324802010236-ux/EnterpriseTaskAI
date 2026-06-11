using System.ComponentModel.DataAnnotations;

namespace EnterpriseTask.Api.DTOs.Mobile.AI;

public class SummarizeTaskRequest
{
    public int? TaskId { get; set; }

    [StringLength(250)]
    public string? Title { get; set; }

    [StringLength(4000)]
    public string? Description { get; set; }

    public List<string> Comments { get; set; } = [];
}
