using System.ComponentModel.DataAnnotations;

namespace EnterpriseTask.Api.DTOs.Mobile.Chat;

public class SendChatMessageRequest
{
    [Required(ErrorMessage = "Nội dung tin nhắn không được để trống.")]
    [StringLength(2000, ErrorMessage = "Tin nhắn không được vượt quá 2000 ký tự.")]
    public string Content { get; set; } = string.Empty;
}
