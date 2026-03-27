using FluentAssertions;
using FoodHub.Application.Features.Reservations.Settings.Commands.UpdateReservationSettings;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Reservations;
using FoodHub.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace FoodHub.Tests.Features.Reservations.Settings
{
    public class UpdateReservationSettingsHandlerTests
    {
        [Fact]
        public async Task Handle_Should_UpdateSettings_And_ReturnNewValues()
        {
            var settings = ReservationSettings.CreateDefault();

            var provider = new Mock<IReservationSettingsProvider>();
            provider
                .Setup(x => x.GetOrCreateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(settings);

            var uow = new Mock<IUnitOfWork>();
            uow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
            uow.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
            uow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
            uow.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var handler = new UpdateReservationSettingsHandler(
                uow.Object,
                provider.Object,
                Mock.Of<IMessageService>(),
                Mock.Of<Microsoft.Extensions.Logging.ILogger<UpdateReservationSettingsHandler>>(),
                Mock.Of<ICacheService>()
            );

            var result = await handler.Handle(
                new UpdateReservationSettingsCommand(
                    "10:30",
                    "23:00",
                    true,
                    "14:00",
                    "17:00",
                    30,
                    60,
                    20,
                    15
                ),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            result.Data!.OpenTime.Should().Be("10:30");
            result.Data.CloseTime.Should().Be("23:00");
            result.Data.BreakEnabled.Should().BeTrue();
            result.Data.BreakStart.Should().Be("14:00");
            result.Data.BreakEnd.Should().Be("17:00");
            result.Data!.OverlapBufferMinutes.Should().Be(30);
            result.Data.MinLeadTimeMinutes.Should().Be(60);
            result.Data.GracePeriodMinutes.Should().Be(20);
            result.Data.UpcomingBufferMinutes.Should().Be(15);
            settings.OpenTime.Should().Be(new TimeOnly(10, 30));
            settings.CloseTime.Should().Be(new TimeOnly(23, 0));
            settings.BreakEnabled.Should().BeTrue();
            settings.BreakStart.Should().Be(new TimeOnly(14, 0));
            settings.BreakEnd.Should().Be(new TimeOnly(17, 0));
            settings.OverlapBufferMinutes.Should().Be(30);
            settings.MinLeadTimeMinutes.Should().Be(60);
            settings.GracePeriodMinutes.Should().Be(20);
            settings.UpcomingBufferMinutes.Should().Be(15);
        }
    }
}
