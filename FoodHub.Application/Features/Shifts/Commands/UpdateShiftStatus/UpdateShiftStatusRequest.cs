using System.Text.Json.Serialization;

namespace FoodHub.Application.Features.Shifts.Commands.UpdateShiftStatus
{
    /// <summary>
    /// Thông tin yêu cầu cập nhật trạng thái hoạt động của ca làm việc.
    /// </summary>
    public class UpdateShiftStatusRequest
    {
        /// <summary>Trạng thái hoạt động (true: kích hoạt, false: vô hiệu hóa).</summary>
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }
    }
}
