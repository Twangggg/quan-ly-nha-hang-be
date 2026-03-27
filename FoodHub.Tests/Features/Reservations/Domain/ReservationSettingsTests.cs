using FluentAssertions;
using FoodHub.Domain.Entities;

namespace FoodHub.Tests.Features.Reservations.Domain
{
    public class ReservationSettingsTests
    {
        [Fact]
        public void CreateDefault_Should_ReturnExpectedDefaults()
        {
            var settings = ReservationSettings.CreateDefault();

            settings.SettingsKey.Should().Be(ReservationSettings.DefaultSettingsKey);
            settings.OverlapBufferMinutes.Should().Be(
                ReservationSettings.DefaultOverlapBufferMinutes
            );
            settings.MinLeadTimeMinutes.Should().Be(ReservationSettings.DefaultMinLeadTimeMinutes);
            settings.GracePeriodMinutes.Should().Be(ReservationSettings.DefaultGracePeriodMinutes);
        }

        [Fact]
        public void Update_Should_ApplyNewValues()
        {
            var settings = ReservationSettings.CreateDefault();

            var result = settings.Update(
                settings.OpenTime,
                settings.CloseTime,
                settings.BreakEnabled,
                settings.BreakStart,
                settings.BreakEnd,
                30,
                20,
                15,
                15
            );

            result.IsSuccess.Should().BeTrue();
            settings.OverlapBufferMinutes.Should().Be(30);
            settings.MinLeadTimeMinutes.Should().Be(20);
            settings.GracePeriodMinutes.Should().Be(15);
            settings.UpcomingBufferMinutes.Should().Be(15);
        }
    }
}
