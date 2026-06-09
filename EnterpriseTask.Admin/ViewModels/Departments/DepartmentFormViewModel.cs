using System.ComponentModel.DataAnnotations;

namespace EnterpriseTask.Admin.ViewModels.Departments;

public class DepartmentFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên phòng ban.")]
    [StringLength(150, ErrorMessage = "Tên phòng ban không được vượt quá 150 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự.")]
    public string? Description { get; set; }

    [StringLength(2000, ErrorMessage = "Mô tả chức năng không được vượt quá 2000 ký tự.")]
    public string? FunctionDescription { get; set; }

    public bool IsActive { get; set; } = true;
}
