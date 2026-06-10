namespace EnterpriseTask.Api.DTOs.Mobile;

public class MobileCompanyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Industry { get; set; }
    public string Status { get; set; } = string.Empty;
}
