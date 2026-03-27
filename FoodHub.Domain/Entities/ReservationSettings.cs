using FoodHub.Domain.Common;
using FoodHub.Domain.Constants;

namespace FoodHub.Domain.Entities
{
    public class ReservationSettings : BaseEntity
    {
        public const string DefaultSettingsKey = "reservation";
        public static readonly TimeOnly DefaultOpenTime = new(10, 30);
        public static readonly TimeOnly DefaultCloseTime = new(23, 0);
        public static readonly TimeOnly DefaultBreakStart = new(14, 0);
        public static readonly TimeOnly DefaultBreakEnd = new(17, 0);
        public const int DefaultOverlapBufferMinutes = 120;
        public const int DefaultMinLeadTimeMinutes = 45;
        public const int DefaultGracePeriodMinutes = 15;
        public const int DefaultUpcomingBufferMinutes = 30;
        public const bool DefaultBreakEnabled = true;

        protected ReservationSettings() { }

        public Guid ReservationSettingsId { get; private set; }
        public string SettingsKey { get; private set; } = DefaultSettingsKey;
        public TimeOnly OpenTime { get; private set; }
        public TimeOnly CloseTime { get; private set; }
        public bool BreakEnabled { get; private set; }
        public TimeOnly BreakStart { get; private set; }
        public TimeOnly BreakEnd { get; private set; }
        public int OverlapBufferMinutes { get; private set; }
        public int MinLeadTimeMinutes { get; private set; }
        public int GracePeriodMinutes { get; private set; }
        public int UpcomingBufferMinutes { get; private set; }

        public static ReservationSettings CreateDefault(Guid? createdBy = null)
        {
            return new ReservationSettings
            {
                ReservationSettingsId = Guid.NewGuid(),
                SettingsKey = DefaultSettingsKey,
                OpenTime = DefaultOpenTime,
                CloseTime = DefaultCloseTime,
                BreakEnabled = DefaultBreakEnabled,
                BreakStart = DefaultBreakStart,
                BreakEnd = DefaultBreakEnd,
                OverlapBufferMinutes = DefaultOverlapBufferMinutes,
                MinLeadTimeMinutes = DefaultMinLeadTimeMinutes,
                GracePeriodMinutes = DefaultGracePeriodMinutes,
                UpcomingBufferMinutes = DefaultUpcomingBufferMinutes,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                UpdatedBy = createdBy,
            };
        }

        public DomainResult Update(
            TimeOnly openTime,
            TimeOnly closeTime,
            bool breakEnabled,
            TimeOnly breakStart,
            TimeOnly breakEnd,
            int overlapBufferMinutes,
            int minLeadTimeMinutes,
            int gracePeriodMinutes,
            int upcomingBufferMinutes,
            Guid? updatedBy = null
        )
        {
            if (openTime >= closeTime)
            {
                return DomainResult.Failure(
                    DomainErrors.ReservationSettings.InvalidOperatingHours
                );
            }

            if (breakEnabled)
            {
                if (breakStart >= breakEnd)
                {
                    return DomainResult.Failure(DomainErrors.ReservationSettings.InvalidBreakHours);
                }

                if (breakStart < openTime || breakEnd > closeTime)
                {
                    return DomainResult.Failure(
                        DomainErrors.ReservationSettings.InvalidBreakOutsideOperatingHours
                    );
                }
            }

            if (overlapBufferMinutes < 0)
            {
                return DomainResult.Failure(
                    DomainErrors.ReservationSettings.InvalidOverlapBufferMinutes
                );
            }

            if (minLeadTimeMinutes < 0)
            {
                return DomainResult.Failure(
                    DomainErrors.ReservationSettings.InvalidMinLeadTimeMinutes
                );
            }

            if (gracePeriodMinutes < 0)
            {
                return DomainResult.Failure(
                    DomainErrors.ReservationSettings.InvalidGracePeriodMinutes
                );
            }

            if (upcomingBufferMinutes < 0)
            {
                return DomainResult.Failure(
                    DomainErrors.ReservationSettings.InvalidUpcomingBufferMinutes
                );
            }

            if (upcomingBufferMinutes > minLeadTimeMinutes)
            {
                return DomainResult.Failure(
                    DomainErrors.ReservationSettings.UpcomingBufferCannotExceedMinLeadTime
                );
            }

            OpenTime = openTime;
            CloseTime = closeTime;
            BreakEnabled = breakEnabled;
            BreakStart = breakStart;
            BreakEnd = breakEnd;
            OverlapBufferMinutes = overlapBufferMinutes;
            MinLeadTimeMinutes = minLeadTimeMinutes;
            GracePeriodMinutes = gracePeriodMinutes;
            UpcomingBufferMinutes = upcomingBufferMinutes;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;

            return DomainResult.Success();
        }
    }
}
