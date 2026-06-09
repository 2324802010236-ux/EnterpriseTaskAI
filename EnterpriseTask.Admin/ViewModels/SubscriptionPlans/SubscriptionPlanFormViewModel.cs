using System.ComponentModel.DataAnnotations;

namespace EnterpriseTask.Admin.ViewModels.SubscriptionPlans;

public class SubscriptionPlanFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên gói dịch vụ.")]
    [StringLength(150, ErrorMessage = "Tên gói không được vượt quá 150 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mã gói dịch vụ.")]
    [StringLength(50, ErrorMessage = "Mã gói không được vượt quá 50 ký tự.")]
    [RegularExpression("^[A-Za-z0-9_-]+$", ErrorMessage = "Mã gói chỉ gồm chữ, số, dấu gạch ngang hoặc gạch dưới.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mô tả gói dịch vụ.")]
    [StringLength(2000, ErrorMessage = "Mô tả không được vượt quá 2000 ký tự.")]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "9999999999999999", ErrorMessage = "Giá gói phải là số không âm.")]
    public decimal Price { get; set; }

    [Range(1, 3650, ErrorMessage = "Thời hạn phải từ 1 đến 3650 ngày.")]
    public int DurationDays { get; set; } = 30;

    [Range(1, 1000000, ErrorMessage = "Số nhân viên tối đa phải lớn hơn 0.")]
    public int MaxEmployees { get; set; }

    [Range(1, 100000, ErrorMessage = "Số phòng ban tối đa phải lớn hơn 0.")]
    public int MaxDepartments { get; set; }

    public bool EnableAI { get; set; }
    public bool EnableRealtimeChat { get; set; }
    public bool IsActive { get; set; } = true;
}
