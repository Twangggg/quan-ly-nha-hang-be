using FluentAssertions;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Reservations.Commands.CreateReservation;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Reservations;
using FoodHub.Domain.Entities;
using Moq;

namespace FoodHub.Tests.Features.Reservation.Commands
{
    public class CreateReservationCommandValidatorTests
    {
        private readonly Mock<IMessageService> _messageService = new();
        private readonly Mock<IReservationSettingsProvider> _settingsProvider = new();

        public CreateReservationCommandValidatorTests()
        {
            _messageService.Setup(x => x.GetMessage(It.IsAny<string>()))
                .Returns<string>(key => key);
            _messageService.Setup(x => x.GetMessage(It.IsAny<string>(), It.IsAny<object[]>()))
                .Returns<string, object[]>((key, args) => $"{key}:{string.Join(',', args)}");
        }

        [Fact]
        public async Task ValidateAsync_Should_Pass_When_ReservationRespectsLeadTime()
        {
            _settingsProvider
                .Setup(x => x.GetOrCreateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ReservationSettings.CreateDefault());

            var validator = new CreateReservationCommandValidator(
                _messageService.Object,
                _settingsProvider.Object
            );

            var result = await validator.ValidateAsync(CreateValidCommand());

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateAsync_Should_Fail_When_ReservationIsTooSoon()
        {
            var settings = ReservationSettings.CreateDefault();
            settings.Update(
                settings.OpenTime,
                settings.CloseTime,
                settings.BreakEnabled,
                settings.BreakStart,
                settings.BreakEnd,
                settings.OverlapBufferMinutes,
                2000,
                settings.GracePeriodMinutes
            );

            _settingsProvider
                .Setup(x => x.GetOrCreateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(settings);

            var validator = new CreateReservationCommandValidator(
                _messageService.Object,
                _settingsProvider.Object
            );

            var result = await validator.ValidateAsync(CreateValidCommand());

            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.ErrorMessage.Contains(MessageKeys.Reservation.TimeTooSoon));
        }

        private static CreateReservationCommand CreateValidCommand()
        {
            var targetDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
            var targetTime = TimeSpan.FromHours(18);

            return new CreateReservationCommand
            {
                CustomerName = "Nguyen Van A",
                CustomerPhone = "0901234567",
                ReservationDate = targetDate,
                ReservationTime = targetTime,
                GuestCount = 2,
                Note = "Test",
                AreaId = Guid.NewGuid(),
            };
        }
    }
}
