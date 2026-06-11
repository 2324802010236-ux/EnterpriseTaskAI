using System.ComponentModel.DataAnnotations;

namespace EnterpriseTask.Api.DTOs.Mobile.AI;

public class SuggestPriorityRequest
{
    [Required]
    [StringLength(250)]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    public DateTime? DueDate { get; set; }
}
