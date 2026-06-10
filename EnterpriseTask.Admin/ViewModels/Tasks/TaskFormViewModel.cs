using System.ComponentModel.DataAnnotations;
using EnterpriseTask.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EnterpriseTask.Admin.ViewModels.Tasks;

public class TaskFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề công việc.")]
    [StringLength(250, ErrorMessage = "Tiêu đề không được vượt quá 250 ký tự.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000, ErrorMessage = "Mô tả không được vượt quá 4000 ký tự.")]
    public string? Description { get; set; }

    [EnumDataType(typeof(WorkTaskPriority), ErrorMessage = "Độ ưu tiên không hợp lệ.")]
    public WorkTaskPriority Priority { get; set; } = WorkTaskPriority.Medium;

    [EnumDataType(typeof(WorkTaskStatus), ErrorMessage = "Trạng thái không hợp lệ.")]
    public WorkTaskStatus Status { get; set; } = WorkTaskStatus.Assigned;

    [DataType(DataType.Date)]
    public DateTime? DueDate { get; set; }

    [EnumDataType(typeof(AssignmentTargetType), ErrorMessage = "Kiểu phân công không hợp lệ.")]
    public AssignmentTargetType AssignmentTargetType { get; set; } = AssignmentTargetType.User;

    public string? AssignedUserId { get; set; }
    public int? DepartmentId { get; set; }
    public List<SelectListItem> Departments { get; set; } = [];
    public List<SelectListItem> Users { get; set; } = [];
    public List<SelectListItem> Priorities { get; set; } = [];
    public List<SelectListItem> Statuses { get; set; } = [];
    public List<SelectListItem> AssignmentTargetTypes { get; set; } = [];
}
