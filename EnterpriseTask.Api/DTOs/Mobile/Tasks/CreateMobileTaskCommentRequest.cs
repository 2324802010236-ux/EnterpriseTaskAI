using System.ComponentModel.DataAnnotations;

namespace EnterpriseTask.Api.DTOs.Mobile.Tasks;

public class CreateMobileTaskCommentRequest
{
    [Required(ErrorMessage = "Nội dung bình luận không được để trống.")]
    [StringLength(4000, ErrorMessage = "Bình luận không được vượt quá 4000 ký tự.")]
    public string Content { get; set; } = string.Empty;
}
