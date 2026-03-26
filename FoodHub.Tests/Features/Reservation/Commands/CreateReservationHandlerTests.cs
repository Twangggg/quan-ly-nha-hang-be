using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Reservations.Commands.CreateReservation;
using FoodHub.Application.Features.Reservations.Services;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Reservations;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using ReservationEntity = FoodHub.Domain.Entities.Reservation;

namespace FoodHub.Tests.Features.Reservations.Commands
{
    public class CreateReservationHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ILogger<CreateReservationHandler>> _mockLogger;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<IReservationSettingsProvider> _mockReservationSettingsProvider;
        private readonly CreateReservationHandler _handler;

        public CreateReservationHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<CreateReservationHandler>>();
            _mockMessageService = new Mock<IMessageService>();
            _mockCacheService = new Mock<ICacheService>();
            _mockReservationSettingsProvider = new Mock<IReservationSettingsProvider>();

            _mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>()))
                .Returns<string>(key => key);
            _mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>(), It.IsAny<object[]>()))
                .Returns<string, object[]>((key, _) => key);
            _mockReservationSettingsProvider
                .Setup(x => x.GetOrCreateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ReservationSettings.CreateDefault());
            _mockUow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUow.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockUow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
            _mockCacheService
                .Setup(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _handler = new CreateReservationHandler(
                _mockUow.Object,
                _mockReservationSettingsProvider.Object,
                new ReservationLifecyclePolicy(),
                _mockLogger.Object,
                _mockMessageService.Object,
                _mockCacheService.Object
            );
        }

        [Fact]
        public async Task Handle_Should_CreateReservation_When_RequestIsValid()
        {
            var table = CreateTable(capacity: 4);
            var tableRepo = new Mock<IGenericRepository<Table>>();
            tableRepo.Setup(x => x.Query()).Returns(new List<Table> { table }.AsQueryable().BuildMock());

            var reservationRepo = new Mock<IGenericRepository<ReservationEntity>>();
            reservationRepo
                .Setup(x => x.Query())
                .Returns(new List<ReservationEntity>().AsQueryable().BuildMock());

            _mockUow.Setup(x => x.Repository<Table>()).Returns(tableRepo.Object);
            _mockUow.Setup(x => x.Repository<ReservationEntity>()).Returns(reservationRepo.Object);
            _mockUow.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var command = CreateCommand(table.AreaId, guestCount: 4, reservationTime: TimeSpan.FromHours(18));

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.TableId.Should().Be(table.TableId);
            reservationRepo.Verify(
                x => x.AddAsync(
                    It.Is<ReservationEntity>(r =>
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

            var reservationRepo = new Mock<IGenericRepository<ReservationEntity>>();
            reservationRepo
                .Setup(x => x.Query())
                .Returns(new List<ReservationEntity>().AsQueryable().BuildMock());

            _mockUow.Setup(x => x.Repository<Table>()).Returns(tableRepo.Object);
            _mockUow.Setup(x => x.Repository<ReservationEntity>()).Returns(reservationRepo.Object);
            _mockUow.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var command = CreateCommand(table.AreaId, guestCount: 4, reservationTime: TimeSpan.FromHours(18));

            await _handler.Handle(command, CancellationToken.None);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) =>
                        v.ToString()!.Contains("Creating reservation request for Area")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
                Times.AtLeastOnce
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_NoTableMatchesAreaAndCapacity()
        {
            var table = CreateTable(capacity: 2);
            var tableRepo = new Mock<IGenericRepository<Table>>();
            tableRepo.Setup(x => x.Query()).Returns(new List<Table> { table }.AsQueryable().BuildMock());

            var reservationRepo = new Mock<IGenericRepository<ReservationEntity>>();
            reservationRepo
                .Setup(x => x.Query())
                .Returns(new List<ReservationEntity>().AsQueryable().BuildMock());

            _mockUow.Setup(x => x.Repository<Table>()).Returns(tableRepo.Object);
            _mockUow.Setup(x => x.Repository<ReservationEntity>()).Returns(reservationRepo.Object);

            var command = CreateCommand(Guid.NewGuid(), guestCount: 4, reservationTime: TimeSpan.FromHours(18));

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
            result.Error.Should().Be(MessageKeys.Table.NotFound);
            reservationRepo.Verify(x => x.AddAsync(It.IsAny<ReservationEntity>()), Times.Never);
            _mockUow.Verify(x => x.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnConflict_When_BookingOverlaps()
        {
            var table = CreateTable(capacity: 4);
            var existingReservation = ReservationEntity.CreateBooked(
                "Existing Customer",
                "0900000000",
                new DateOnly(2026, 3, 20),
                TimeSpan.FromHours(19),
                2,
                null,
                table.TableId,
                table.AreaId
            );

            var tableRepo = new Mock<IGenericRepository<Table>>();
            tableRepo.Setup(x => x.Query()).Returns(new List<Table> { table }.AsQueryable().BuildMock());

            var reservationRepo = new Mock<IGenericRepository<ReservationEntity>>();
            reservationRepo
                .Setup(x => x.Query())
                .Returns(new List<ReservationEntity> { existingReservation }.AsQueryable().BuildMock());

            _mockUow.Setup(x => x.Repository<Table>()).Returns(tableRepo.Object);
            _mockUow.Setup(x => x.Repository<ReservationEntity>()).Returns(reservationRepo.Object);

            var command = CreateCommand(table.AreaId, guestCount: 4, reservationTime: TimeSpan.FromHours(18));

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Conflict);
            result.Error.Should().Be(MessageKeys.Reservation.Overlapped);
            reservationRepo.Verify(x => x.AddAsync(It.IsAny<ReservationEntity>()), Times.Never);
            _mockUow.Verify(x => x.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        private static CreateReservationCommand CreateCommand(
            Guid areaId,
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
                GuestCount = guestCount,
                Note = "Test note",
                AreaId = areaId,
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
