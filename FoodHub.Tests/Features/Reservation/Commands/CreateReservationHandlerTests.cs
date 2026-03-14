using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Reservations.Commands.CreateReservation;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.Reservations.Commands
{
    public class CreateReservationHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ILogger<CreateReservationHandler>> _mockLogger;
        private readonly CreateReservationHandler _handler;

        public CreateReservationHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<CreateReservationHandler>>();
            _handler = new CreateReservationHandler(_mockUow.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_Should_CreateReservation_When_RequestIsValid()
        {
            var table = CreateTable(capacity: 4);
            var tableRepo = new Mock<IGenericRepository<Table>>();
            tableRepo.Setup(x => x.Query()).Returns(new List<Table> { table }.AsQueryable().BuildMock());

            var reservationRepo = new Mock<IGenericRepository<Reservation>>();
            reservationRepo
                .Setup(x => x.Query())
                .Returns(new List<Reservation>().AsQueryable().BuildMock());

            _mockUow.Setup(x => x.Repository<Table>()).Returns(tableRepo.Object);
            _mockUow.Setup(x => x.Repository<Reservation>()).Returns(reservationRepo.Object);
            _mockUow.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var command = CreateCommand(table.TableId, guestCount: 4, reservationTime: TimeSpan.FromHours(18));

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeEmpty();
            reservationRepo.Verify(
                x => x.AddAsync(
                    It.Is<Reservation>(r =>
                        r.TableId == table.TableId
                        && r.AreaId == table.AreaId
                        && r.Status == ReservationStatus.Booked
                        && r.GuestCount == command.GuestCount
                    )
                ),
                Times.Once
            );
            _mockUow.Verify(x => x.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_LogRequest_AtStart()
        {
            var table = CreateTable(capacity: 4);
            var tableRepo = new Mock<IGenericRepository<Table>>();
            tableRepo.Setup(x => x.Query()).Returns(new List<Table> { table }.AsQueryable().BuildMock());

            var reservationRepo = new Mock<IGenericRepository<Reservation>>();
            reservationRepo
                .Setup(x => x.Query())
                .Returns(new List<Reservation>().AsQueryable().BuildMock());

            _mockUow.Setup(x => x.Repository<Table>()).Returns(tableRepo.Object);
            _mockUow.Setup(x => x.Repository<Reservation>()).Returns(reservationRepo.Object);
            _mockUow.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var command = CreateCommand(table.TableId, guestCount: 4, reservationTime: TimeSpan.FromHours(18));

            await _handler.Handle(command, CancellationToken.None);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) =>
                        v.ToString()!.Contains("Creating reservation request for Table")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
                Times.AtLeastOnce
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnBadRequest_When_TableCapacityIsInsufficient()
        {
            var table = CreateTable(capacity: 2);
            var tableRepo = new Mock<IGenericRepository<Table>>();
            tableRepo.Setup(x => x.Query()).Returns(new List<Table> { table }.AsQueryable().BuildMock());

            var reservationRepo = new Mock<IGenericRepository<Reservation>>();
            reservationRepo
                .Setup(x => x.Query())
                .Returns(new List<Reservation>().AsQueryable().BuildMock());

            _mockUow.Setup(x => x.Repository<Table>()).Returns(tableRepo.Object);
            _mockUow.Setup(x => x.Repository<Reservation>()).Returns(reservationRepo.Object);

            var command = CreateCommand(table.TableId, guestCount: 4, reservationTime: TimeSpan.FromHours(18));

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
            reservationRepo.Verify(x => x.AddAsync(It.IsAny<Reservation>()), Times.Never);
            _mockUow.Verify(x => x.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnConflict_When_BookingOverlaps()
        {
            var table = CreateTable(capacity: 4);
            var existingReservation = Reservation.CreateBooked(
                "Existing Customer",
                "0900000000",
                new DateOnly(2026, 3, 20),
                TimeSpan.FromHours(19),
                PartyType.Party,
                2,
                false,
                null,
                table.TableId,
                table.AreaId
            );

            var tableRepo = new Mock<IGenericRepository<Table>>();
            tableRepo.Setup(x => x.Query()).Returns(new List<Table> { table }.AsQueryable().BuildMock());

            var reservationRepo = new Mock<IGenericRepository<Reservation>>();
            reservationRepo
                .Setup(x => x.Query())
                .Returns(new List<Reservation> { existingReservation }.AsQueryable().BuildMock());

            _mockUow.Setup(x => x.Repository<Table>()).Returns(tableRepo.Object);
            _mockUow.Setup(x => x.Repository<Reservation>()).Returns(reservationRepo.Object);

            var command = CreateCommand(table.TableId, guestCount: 4, reservationTime: TimeSpan.FromHours(18));

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Conflict);
            reservationRepo.Verify(x => x.AddAsync(It.IsAny<Reservation>()), Times.Never);
            _mockUow.Verify(x => x.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        private static CreateReservationCommand CreateCommand(
            Guid tableId,
            int guestCount,
            TimeSpan reservationTime
        )
        {
            return new CreateReservationCommand
            {
                CustomerName = "Nguyen Van A",
                CustomerPhone = "0901234567",
                ReservationDate = new DateOnly(2026, 3, 20),
                ReservationTime = reservationTime,
                PartyType = PartyType.Party,
                GuestCount = guestCount,
                HasChildren = false,
                Note = "Test note",
                TableId = tableId,
            };
        }

        private static Table CreateTable(int capacity)
        {
            var areaId = Guid.NewGuid();

            return new Table
            {
                TableId = Guid.NewGuid(),
                TableNumber = 1,
                Capacity = capacity,
                AreaId = areaId,
                Area = new Area
                {
                    AreaId = areaId,
                    Name = "Main Hall",
                    CodePrefix = "MH",
                    Type = AreaType.Normal,
                    Status = AreaStatus.Active,
                },
                Status = TableStatus.Available,
            };
        }
    }
}
