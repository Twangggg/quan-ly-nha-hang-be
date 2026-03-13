using FluentAssertions;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Tests.Features.Reservations.Domain
{
    public class ReservationTests
    {
        [Fact]
        public void CanFitTable_Should_ReturnTrue_When_TableCapacityIsEnough()
        {
            var reservation = CreateReservation(guestCount: 4, reservationTime: TimeSpan.FromHours(18));
            var table = CreateTable(capacity: 4);

            var result = reservation.CanFitTable(table);

            result.Should().BeTrue();
        }

        [Fact]
        public void OverlapsWith_Should_ReturnTrue_When_TimeDifferenceIsLessThanTwoHours()
        {
            var candidate = CreateReservation(guestCount: 4, reservationTime: TimeSpan.FromHours(18));
            var existing = CreateReservation(guestCount: 2, reservationTime: TimeSpan.FromHours(19));

            var result = candidate.OverlapsWith(existing);

            result.Should().BeTrue();
        }

        [Fact]
        public void OverlapsWith_Should_ReturnFalse_When_ExistingReservationIsCancelled()
        {
            var candidate = CreateReservation(guestCount: 4, reservationTime: TimeSpan.FromHours(18));
            var existing = CreateReservation(
                guestCount: 2,
                reservationTime: TimeSpan.FromHours(19),
                status: ReservationStatus.Cancelled
            );

            var result = candidate.OverlapsWith(existing);

            result.Should().BeFalse();
        }

        [Fact]
        public void CreateBooked_Should_InitializeBookedReservation()
        {
            var reservation = Reservation.CreateBooked(
                "Nguyen Van A",
                "0901234567",
                new DateOnly(2026, 3, 20),
                TimeSpan.FromHours(18),
                PartyType.Party,
                4,
                false,
                "Birthday",
                Guid.NewGuid(),
                Guid.NewGuid()
            );

            reservation.ReservationId.Should().NotBeEmpty();
            reservation.Status.Should().Be(ReservationStatus.Booked);
            reservation.CustomerName.Should().Be("Nguyen Van A");
        }

        private static Reservation CreateReservation(
            int guestCount,
            TimeSpan reservationTime,
            ReservationStatus status = ReservationStatus.Booked
        )
        {
            var tableId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var areaId = Guid.Parse("22222222-2222-2222-2222-222222222222");

            return new Reservation
            {
                ReservationId = Guid.NewGuid(),
                CustomerName = "Test Customer",
                CustomerPhone = "0900000000",
                ReservationDate = new DateOnly(2026, 3, 20),
                ReservationTime = reservationTime,
                PartyType = PartyType.Party,
                GuestCount = guestCount,
                HasChildren = false,
                Status = status,
                TableId = tableId,
                AreaId = areaId,
            };
        }

        private static Table CreateTable(int capacity)
        {
            return new Table
            {
                TableId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                TableNumber = 1,
                Capacity = capacity,
                AreaId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Status = TableStatus.Available,
                Area = new Area
                {
                    AreaId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Name = "Main Hall",
                    CodePrefix = "MH",
                    Type = AreaType.Normal,
                    Status = AreaStatus.Active,
                },
            };
        }
    }
}
