using FluentAssertions;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Reservations.Commands.UpdateReservation;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Reservations;
using FoodHub.Domain.Entities;
using Moq;

namespace FoodHub.Tests.Features.Reservations.Commands
{
    public class UpdateReservationCommandValidatorTests
    {
        private readonly Mock<IMessageService> _messageService = new();
        private readonly Mock<IReservationSettingsProvider> _settingsProvider = new();

        public UpdateReservationCommandValidatorTests()
        {
            _messageService.Setup(x => x.GetMessage(It.IsAny<string>()))
                .Returns<string>(key => key);
            _messageService.Setup(x => x.GetMessage(It.IsAny<string>(), It.IsAny<object[]>()))
                .Returns<string, object[]>((key, args) => $"{key}:{string.Join(',', args)}");
        }

        [Fact]
        public async Task ValidateAsync_Should_Fail_When_ReservationIsTooSoon()
        {
            var reservationDateTime = GetReservationDateTime();
            var settings = ReservationSettings.CreateDefault();
            settings.Update(
                settings.OpenTime,
                settings.CloseTime,
                settings.BreakEnabled,
                settings.BreakStart,
                settings.BreakEnd,
                settings.OverlapBufferMinutes,
                (int)Math.Ceiling((reservationDateTime - DateTime.Now).TotalMinutes) + 60,
                settings.GracePeriodMinutes,
                15
            );

            _settingsProvider
                .Setup(x => x.GetOrCreateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(settings);

            var validator = new UpdateReservationCommandValidator(
                _messageService.Object,
                _settingsProvider.Object
            );

            var result = await validator.ValidateAsync(CreateValidCommand());

            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(e =>
                e.ErrorMessage.Contains(MessageKeys.Reservation.TimeTooSoon)
            );
        }

        private static UpdateReservationCommand CreateValidCommand()
        {
            var reservationDateTime = GetReservationDateTime();

            return new UpdateReservationCommand
            {
                ReservationId = Guid.NewGuid(),
                CustomerName = "Nguyen Van A",
                CustomerPhone = "0901234567",
                ReservationDate = DateOnly.FromDateTime(reservationDateTime),
                ReservationTime = reservationDateTime.TimeOfDay,
                GuestCount = 2,
                AreaId = Guid.NewGuid(),
            };
        }

        private static DateTime GetReservationDateTime()
        {
            var now = DateTime.Now;
            var reservationDateTime = now.Date.AddDays(1).AddHours(18);

            if (reservationDateTime <= now.AddHours(2))
            {
                reservationDateTime = reservationDateTime.AddDays(1);
            }

            return reservationDateTime;
        }
    }
}
