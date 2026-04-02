using FoodHub.Domain.Common;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities
{
    public class Reservation : BaseEntity
    {
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
        public DateTime? CheckedInAt { get; set; }

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

        public void ReassignToTable(Guid tableId, DateTime updatedAt, Guid? updatedBy)
        {
            TableId = tableId;
            Status = ReservationStatus.CheckIn;
            UpdatedAt = updatedAt;
            UpdatedBy = updatedBy;
        }

        public void MarkCheckedIn(DateTime checkedInAt, Guid? updatedBy = null)
        {
            Status = ReservationStatus.CheckIn;
            CheckedInAt = checkedInAt;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }

        public void MarkNoShow(Guid? updatedBy = null)
        {
            Status = ReservationStatus.NoShow;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }

        public void MarkCompleted(Guid? updatedBy = null)
        {
            Status = ReservationStatus.Completed;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }

        public void Complete(DateTime completedAt, Guid? completedBy)
        {
            Status = ReservationStatus.Completed;
            UpdatedAt = completedAt;
            UpdatedBy = completedBy;
        }

        public bool CanFitTable(Table table)
        {
            ArgumentNullException.ThrowIfNull(table);
            return GuestCount <= table.Capacity;
        }

        public bool OverlapsWith(Reservation other, int bufferMinutes)
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

            var buffer = TimeSpan.FromMinutes(bufferMinutes);
            var minTime = ReservationTime.Subtract(buffer);
            var maxTime = ReservationTime.Add(buffer);

            return other.ReservationTime > minTime && other.ReservationTime < maxTime;
        }

        public bool CanMarkNoShow(DateTime now, ReservationSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            if (Status != ReservationStatus.Booked)
            {
                return false;
            }

            var reservationDateTime = ReservationDate.ToDateTime(
                TimeOnly.FromTimeSpan(ReservationTime)
            );
            return now >= reservationDateTime.AddMinutes(settings.GracePeriodMinutes);
        }

        public bool IsBlockingTable(DateTime now, ReservationSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            return Status switch
            {
                ReservationStatus.Booked => !CanMarkNoShow(now, settings),
                ReservationStatus.CheckIn => true,
                _ => false,
            };
        }

        private bool IsActiveForScheduling() =>
            Status == ReservationStatus.Booked || Status == ReservationStatus.CheckIn;
    }
}
