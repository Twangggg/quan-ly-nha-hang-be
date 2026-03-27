using FluentAssertions;
using FoodHub.Application.Features.Reservations.Settings.Queries.GetReservationSettings;
using FoodHub.Application.Interfaces.Reservations;
using FoodHub.Domain.Entities;
using Moq;

namespace FoodHub.Tests.Features.Reservations.Settings
{
    public class GetReservationSettingsHandlerTests
    {
        [Fact]
        public async Task Handle_Should_ReturnMappedSettings()
        {
            var settings = ReservationSettings.CreateDefault();
            settings.Update(
                settings.OpenTime,
                settings.CloseTime,
                settings.BreakEnabled,
                settings.BreakStart,
                settings.BreakEnd,
                60,
                90,
                25,
                15
            );

            var provider = new Mock<IReservationSettingsProvider>();
            provider
                .Setup(x => x.GetOrCreateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(settings);

            var handler = new GetReservationSettingsHandler(
                provider.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<GetReservationSettingsHandler>>()
            );

            var result = await handler.Handle(new GetReservationSettingsQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.OverlapBufferMinutes.Should().Be(60);
            result.Data.MinLeadTimeMinutes.Should().Be(90);
            result.Data.GracePeriodMinutes.Should().Be(25);
            result.Data.UpcomingBufferMinutes.Should().Be(15);
        }
    }
}
