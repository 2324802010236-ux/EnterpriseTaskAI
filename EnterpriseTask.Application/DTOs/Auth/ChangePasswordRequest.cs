using System.ComponentModel.DataAnnotations;

namespace EnterpriseTask.Application.DTOs.Auth;

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    public string NewPassword { get; set; } = string.Empty;
}
