using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EnterpriseTask.Admin.ViewModels.Employees;

public class EmployeeFormViewModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên nhân viên.")]
    [StringLength(150, ErrorMessage = "Họ tên không được vượt quá 150 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email nhân viên.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(256, ErrorMessage = "Email không được vượt quá 256 ký tự.")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không đúng định dạng.")]
    [StringLength(30, ErrorMessage = "Số điện thoại không được vượt quá 30 ký tự.")]
    public string? PhoneNumber { get; set; }

    public int? DepartmentId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
    public string Role { get; set; } = string.Empty;

    [StringLength(150, ErrorMessage = "Chức vụ không được vượt quá 150 ký tự.")]
    public string? Position { get; set; }

    public bool IsActive { get; set; } = true;
    public List<SelectListItem> Departments { get; set; } = [];
    public List<SelectListItem> Roles { get; set; } = [];
}
