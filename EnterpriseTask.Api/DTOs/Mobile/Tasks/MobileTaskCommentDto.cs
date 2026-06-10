namespace EnterpriseTask.Api.DTOs.Mobile.Tasks;

public class MobileTaskCommentDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
