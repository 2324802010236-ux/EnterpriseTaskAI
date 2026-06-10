using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using EnterpriseTask.Domain.Enums;

namespace EnterpriseTask.Api.DTOs.Mobile.Tasks;

public class UpdateMobileTaskStatusRequest
{
    [EnumDataType(typeof(WorkTaskStatus), ErrorMessage = "Trạng thái công việc không hợp lệ.")]
    [JsonConverter(typeof(JsonStringEnumConverter<WorkTaskStatus>))]
    public WorkTaskStatus Status { get; set; }

    [StringLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự.")]
    public string? Note { get; set; }
}
