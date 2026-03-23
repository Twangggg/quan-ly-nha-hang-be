using FoodHub.Domain.Enums;

namespace FoodHub.Application.Interfaces.Messaging
{
    /// <summary>
    /// Định nghĩa các phương thức để thông báo cho Frontend (Bếp & Phục vụ)
    /// khi có thay đổi trạng thái món ăn.
    /// </summary>
    public interface ISignalRService
    {
        /// <summary>
        /// Thông báo cho Bếp/Bar khi có món mới vừa được Submit.
        /// </summary>
        Task NotifyNewOrderItemAsync(Guid orderId, Guid orderItemId, string station);

        /// <summary>
        /// Thông báo khi trạng thái món ăn thay đổi (StartCooking, MarkReady, Reject, Return).
        /// Frontend sẽ dựa vào status này để di chuyển item giữa các cột/màn hình.
        /// </summary>
        Task NotifyOrderItemStatusChangedAsync(
            Guid orderItemId,
            OrderItemStatus newStatus,
            string station
        );

        /// <summary>
        /// (Tùy chọn) Thông báo khi toàn bộ Order thay đổi trạng thái.
        /// </summary>
        Task NotifyOrderStatusChangedAsync(Guid orderId, string status);

        /// <summary>
        /// Thông báo cho nhân viên khi lịch ca làm việc của họ thay đổi (gán hoặc hủy).
        /// </summary>
        /// <param name="employeeId">ID nhân viên nhận thông báo.</param>
        /// <param name="shiftName">Tên ca làm việc.</param>
        /// <param name="assignedDate">Ngày phân công.</param>
        /// <param name="isCancelled">true = hủy ca, false = gán mới.</param>
        Task NotifyShiftAssignmentAsync(
            Guid employeeId,
            string shiftName,
            DateOnly assignedDate,
            bool isCancelled);
        /// Thông báo khi trạng thái bàn thay đổi (do đặt bàn đến giờ, check-in, huỷ, v.v).
        /// Frontend lắng nghe để cập nhật sơ đồ bàn theo thời gian thực.
        /// </summary>
        Task NotifyTableStatusChangedAsync(Guid tableId, string newStatus);
    }
}
