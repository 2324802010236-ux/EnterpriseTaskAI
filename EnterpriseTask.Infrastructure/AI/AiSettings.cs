namespace EnterpriseTask.Infrastructure.AI;

public class AiSettings
{
    public const string SectionName = "AiSettings";

    public bool Enabled { get; set; }
    public string Provider { get; set; } = "Mock";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "mock-model";
    public string Endpoint { get; set; } = string.Empty;
}
