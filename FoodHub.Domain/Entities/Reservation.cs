using FoodHub.Domain.Common;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class Reservation : BaseEntity
    {
        public Guid ReservationId { get; set; }
        
        // Thông tin khách
        public required string CustomerName { get; set; }
        public required string CustomerPhone { get; set; }
        
        // Thời gian
        public DateOnly ReservationDate { get; set; }
        public TimeSpan ReservationTime { get; set; }
        
        // Chi tiết
        public int GuestCount { get; set; }
        public string? Note { get; set; }
        
        // Khu vực, Trạng thái & Bàn
        public ReservationStatus Status { get; set; }
        public Guid? AreaId { get; set; }
        public virtual Area? Area { get; set; }
        public Guid TableId { get; set; }
        public virtual Table Table { get; set; } = null!;
        
        // Order đặt trước
        public Guid? OrderId { get; set; }
        public virtual Order? Order { get; set; }
    }
}
