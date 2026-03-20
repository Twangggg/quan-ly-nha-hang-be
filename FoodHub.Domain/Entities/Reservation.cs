using FoodHub.Domain.Common;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class Reservation : BaseEntity
    {
        public const int DefaultOverlapBufferHours = 2;

        public Reservation() { }

        public Guid ReservationId { get; set; }

        public string CustomerName { get; set; } = null!;
        public string CustomerPhone { get; set; } = null!;

        public DateOnly ReservationDate { get; set; }
        public TimeSpan ReservationTime { get; set; }

        public int GuestCount { get; set; }
        public bool HasChildren { get; set; } = false;
        public string? Note { get; set; }
        public ReservationStatus Status { get; set; }
        public Guid? AreaId { get; set; }
        public virtual Area? Area { get; set; }
        public Guid TableId { get; set; }
        public virtual Table Table { get; set; } = null!;

        private Reservation(
            string customerName,
            string customerPhone,
            DateOnly reservationDate,
            TimeSpan reservationTime,
            int guestCount,
            string? note,
            Guid tableId,
            Guid? areaId,
            bool hasChildren = false
        )
        {
            ReservationId = Guid.NewGuid();
            CustomerName = customerName;
            CustomerPhone = customerPhone;
            ReservationDate = reservationDate;
            ReservationTime = reservationTime;
            GuestCount = guestCount;
            Note = note;
            Status = ReservationStatus.Booked;
            TableId = tableId;
            AreaId = areaId;
            HasChildren = hasChildren;
        }

        public static Reservation CreateBooked(
            string customerName,
            string customerPhone,
            DateOnly reservationDate,
            TimeSpan reservationTime,
            int guestCount,
            string? note,
            Guid tableId,
            Guid? areaId,
            bool hasChildren = false
        )
        {
            return new Reservation(
                customerName,
                customerPhone,
                reservationDate,
                reservationTime,
                guestCount,
                note,
                tableId,
                areaId,
                hasChildren
            );
        }

        public bool CanFitTable(Table table)
        {
            ArgumentNullException.ThrowIfNull(table);
            return GuestCount <= table.Capacity;
        }

        public bool OverlapsWith(Reservation other, int bufferHours = DefaultOverlapBufferHours)
        {
            ArgumentNullException.ThrowIfNull(other);

            if (!IsActiveForScheduling() || !other.IsActiveForScheduling())
            {
                return false;
            }

            if (TableId != other.TableId || ReservationDate != other.ReservationDate)
            {
                return false;
            }

            var buffer = TimeSpan.FromHours(bufferHours);
            var minTime = ReservationTime.Subtract(buffer);
            var maxTime = ReservationTime.Add(buffer);

            return other.ReservationTime > minTime && other.ReservationTime < maxTime;
        }

        private bool IsActiveForScheduling() =>
            Status == ReservationStatus.Booked || Status == ReservationStatus.CheckIn;
    }
}
