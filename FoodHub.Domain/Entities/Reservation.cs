using System.Diagnostics.CodeAnalysis;
using FoodHub.Domain.Common;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class Reservation : BaseEntity
    {
        public const int DefaultOverlapBufferHours = 2;

        public Reservation()
        {
        }

        public Guid ReservationId { get; set; }

        // Thông tin khách
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }

        // Thời gian
        public DateOnly ReservationDate { get; set; }
        public TimeSpan ReservationTime { get; set; }

        // Chi tiết
        public PartyType PartyType { get; set; }
        public int GuestCount { get; set; }
        public bool HasChildren { get; set; }
        public string? Note { get; set; }

        // Khu vực, Trạng thái & Bàn
        public ReservationStatus Status { get; set; }
        public Guid? AreaId { get; set; }
        public virtual Area? Area { get; set; }
        public Guid TableId { get; set; }
        public virtual Table Table { get; set; } = null!;

        [SetsRequiredMembers]
        private Reservation(
            string customerName,
            string customerPhone,
            DateOnly reservationDate,
            TimeSpan reservationTime,
            PartyType partyType,
            int guestCount,
            bool hasChildren,
            string? note,
            Guid tableId,
            Guid? areaId
        )
        {
            ReservationId = Guid.NewGuid();
            CustomerName = customerName;
            CustomerPhone = customerPhone;
            ReservationDate = reservationDate;
            ReservationTime = reservationTime;
            PartyType = partyType;
            GuestCount = guestCount;
            HasChildren = hasChildren;
            Note = note;
            Status = ReservationStatus.Booked;
            TableId = tableId;
            AreaId = areaId;
        }

        public static Reservation CreateBooked(
            string customerName,
            string customerPhone,
            DateOnly reservationDate,
            TimeSpan reservationTime,
            PartyType partyType,
            int guestCount,
            bool hasChildren,
            string? note,
            Guid tableId,
            Guid? areaId
        )
        {
            return new Reservation(
                customerName,
                customerPhone,
                reservationDate,
                reservationTime,
                partyType,
                guestCount,
                hasChildren,
                note,
                tableId,
                areaId
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

            if (Status != ReservationStatus.Booked || other.Status != ReservationStatus.Booked)
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
    }
}
