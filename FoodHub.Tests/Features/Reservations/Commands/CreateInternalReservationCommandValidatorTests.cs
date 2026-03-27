using FluentAssertions;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Reservations.Commands.CreateInternalReservation;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Reservations;
using FoodHub.Domain.Entities;
using Moq;

namespace FoodHub.Tests.Features.Reservations.Commands
{
    public class CreateInternalReservationCommandValidatorTests
    {
        private readonly Mock<IMessageService> _messageService = new();
        private readonly Mock<IReservationSettingsProvider> _settingsProvider = new();

        public CreateInternalReservationCommandValidatorTests()
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

            var validator = new CreateInternalReservationCommandValidator(
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
                settings.GracePeriodMinutes,
                15
            );

            _settingsProvider
                .Setup(x => x.GetOrCreateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(settings);

            var validator = new CreateInternalReservationCommandValidator(
                _messageService.Object,
                _settingsProvider.Object
            );

            var result = await validator.ValidateAsync(CreateValidCommand());

            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(e =>
                e.ErrorMessage.Contains(MessageKeys.Reservation.TimeTooSoon)
            );
        }

        private static CreateInternalReservationCommand CreateValidCommand()
        {
            return new CreateInternalReservationCommand
            {
                CustomerName = "Nguyen Van A",
                CustomerPhone = "0901234567",
                ReservationDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                ReservationTime = TimeSpan.FromHours(18),
                GuestCount = 2,
            };
        }
    }
}
