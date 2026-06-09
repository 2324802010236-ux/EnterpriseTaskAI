using System.ComponentModel.DataAnnotations;
using EnterpriseTask.Domain.Enums;

namespace EnterpriseTask.Admin.ViewModels.Companies;

public class CompanyFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên công ty.")]
    [StringLength(200, ErrorMessage = "Tên công ty không được vượt quá 200 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Mã số thuế không được vượt quá 50 ký tự.")]
    public string? TaxCode { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập email công ty.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(256, ErrorMessage = "Email không được vượt quá 256 ký tự.")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không đúng định dạng.")]
    [StringLength(30, ErrorMessage = "Số điện thoại không được vượt quá 30 ký tự.")]
    public string? Phone { get; set; }

    [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự.")]
    public string? Address { get; set; }

    [StringLength(150, ErrorMessage = "Lĩnh vực không được vượt quá 150 ký tự.")]
    public string? Industry { get; set; }

    [EnumDataType(typeof(CompanyStatus), ErrorMessage = "Trạng thái công ty không hợp lệ.")]
    public CompanyStatus Status { get; set; } = CompanyStatus.Active;
}
