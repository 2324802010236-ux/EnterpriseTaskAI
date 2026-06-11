namespace EnterpriseTask.Application.Messaging;

public class EmailJobMessage
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public DateTime CreatedAt { get; set; }
}
