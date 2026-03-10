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
        public PartyType PartyType { get; set; }
        public int GuestCount { get; set; }
        public bool HasChildren { get; set; }
        public string? Note { get; set; }
        
        // Trạng thái & Bàn
        public ReservationStatus Status { get; set; }
        public Guid TableId { get; set; }
        public virtual Table Table { get; set; } = null!;
    }
}
