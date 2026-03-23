using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Reservations.Commands.CheckInReservation;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.Reservations.Commands
{
    public class CheckInReservationTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ILogger<CheckInReservationHandler>> _mockLogger;
        private readonly CheckInReservationHandler _handler;

        public CheckInReservationTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockMessageService = new Mock<IMessageService>();
            _mockLogger = new Mock<ILogger<CheckInReservationHandler>>();

            _handler = new CheckInReservationHandler(
                _mockUow.Object,
                _mockCurrentUserService.Object,
                _mockMessageService.Object,
                _mockLogger.Object
            );
        }

        private (FoodHub.Domain.Entities.Reservation Reservation, Table Table, Area Area) CreateTestData(
            ReservationStatus status = ReservationStatus.Booked,
            TableStatus tableStatus = TableStatus.Available,
            AreaType areaType = AreaType.Normal
        )
        {
            var area = new Area
            {
                AreaId = Guid.NewGuid(),
                Name = areaType == AreaType.VIP ? "VIP" : "Standard",
                CodePrefix = areaType == AreaType.VIP ? "VIP" : "STD",
                Type = areaType,
                Status = AreaStatus.Active,
            };
            var table = new Table
            {
                TableId = Guid.NewGuid(),
                TableNumber = 1,
                Capacity = 4,
                Status = tableStatus,
                Area = area,
                AreaId = area.AreaId,
            };
            var reservation = new FoodHub.Domain.Entities.Reservation
            {
                ReservationId = Guid.NewGuid(),
                CustomerName = "Nguyen Van A",
                CustomerPhone = "0901234567",
                ReservationDate = DateOnly.FromDateTime(DateTime.UtcNow),
                ReservationTime = TimeSpan.FromHours(19),
                GuestCount = 4,
                Note = "Test note",
                Status = status,
                TableId = table.TableId,
                Table = table,
                AreaId = area.AreaId,
            };

            return (reservation, table, area);
        }

        private void SetupCommonMocks(
            FoodHub.Domain.Entities.Reservation reservation,
            List<FoodHub.Domain.Entities.Order>? existingOrders = null
        )
        {
            var userId = Guid.NewGuid().ToString();
            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);

            // Reservation repository
            var reservationRepo = new Mock<IGenericRepository<FoodHub.Domain.Entities.Reservation>>();
            reservationRepo
                .Setup(r => r.Query())
                .Returns(
                    new List<FoodHub.Domain.Entities.Reservation> { reservation }
                        .AsQueryable()
                        .BuildMock()
                );
            _mockUow
                .Setup(u => u.Repository<FoodHub.Domain.Entities.Reservation>())
                .Returns(reservationRepo.Object);

            // Order repository
            var orderRepo = new Mock<IGenericRepository<FoodHub.Domain.Entities.Order>>();
            orderRepo
                .Setup(r => r.Query())
                .Returns(
                    (existingOrders ?? new List<FoodHub.Domain.Entities.Order>()).AsQueryable().BuildMock()
                );
            _mockUow.Setup(u => u.Repository<FoodHub.Domain.Entities.Order>()).Returns(orderRepo.Object);

            // Table repository
            var tableRepo = new Mock<IGenericRepository<Table>>();
            tableRepo.Setup(r => r.Query()).Returns(new List<Table>().AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Table>()).Returns(tableRepo.Object);

            // Audit log repository
            var auditRepo = new Mock<IGenericRepository<OrderAuditLog>>();
            _mockUow.Setup(u => u.Repository<OrderAuditLog>()).Returns(auditRepo.Object);

            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        }

        // ──────── AC-RC-01: Check-in tạo Order thành công ────────

        [Fact]
        public async Task Handle_Should_ReturnSuccess_And_CreateOrder()
        {
            // Arrange
            var (reservation, table, _) = CreateTestData();
            SetupCommonMocks(reservation);

            var command = new CheckInReservationCommand
            {
                ReservationId = reservation.ReservationId,
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.OrderId.Should().NotBeEmpty();
            result.Data.OrderCode.Should().StartWith("ORD-");

            _mockUow.Verify(
                u =>
                    u.Repository<FoodHub.Domain.Entities.Order>()
                        .AddAsync(
                            It.Is<FoodHub.Domain.Entities.Order>(o =>
                                o.OrderType == OrderType.DineIn
                                && o.Status == OrderStatus.Serving
                                && o.TableId == table.TableId
                                && o.ReservationId == reservation.ReservationId
                            )
                        ),
                Times.Once
            );
        }

        // ──────── AC-RC-02: Reservation chuyển CHECKED_IN ────────

        [Fact]
        public async Task Handle_Should_SetReservationStatus_ToCheckedIn()
        {
            // Arrange
            var (reservation, _, _) = CreateTestData();
            SetupCommonMocks(reservation);

            var command = new CheckInReservationCommand
            {
                ReservationId = reservation.ReservationId,
            };

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            reservation.Status.Should().Be(ReservationStatus.CheckIn);
            _mockUow.Verify(
                u => u.Repository<FoodHub.Domain.Entities.Reservation>().Update(reservation),
                Times.Once
            );
        }

        // ──────── AC-RC-03: Bàn chuyển OCCUPIED ────────

        [Fact]
        public async Task Handle_Should_SetTableStatus_ToOccupied()
        {
            // Arrange
            var (reservation, table, _) = CreateTestData();
            SetupCommonMocks(reservation);

            var command = new CheckInReservationCommand
            {
                ReservationId = reservation.ReservationId,
            };

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            table.Status.Should().Be(TableStatus.Occupied);
            _mockUow.Verify(u => u.Repository<Table>().Update(table), Times.Once);
        }

        // ──────── AC-RC-04: VIP auto set IsPriority = true ────────

        [Fact]
        public async Task Handle_Should_SetIsPriority_When_VIPArea()
        {
            // Arrange
            var (reservation, _, _) = CreateTestData(areaType: AreaType.VIP);
            SetupCommonMocks(reservation);

            var command = new CheckInReservationCommand
            {
                ReservationId = reservation.ReservationId,
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockUow.Verify(
                u =>
                    u.Repository<FoodHub.Domain.Entities.Order>()
                        .AddAsync(It.Is<FoodHub.Domain.Entities.Order>(o => o.IsPriority == true)),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_NotSetIsPriority_When_NormalArea()
        {
            // Arrange
            var (reservation, _, _) = CreateTestData(areaType: AreaType.Normal);
            SetupCommonMocks(reservation);

            var command = new CheckInReservationCommand
            {
                ReservationId = reservation.ReservationId,
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockUow.Verify(
                u =>
                    u.Repository<FoodHub.Domain.Entities.Order>()
                        .AddAsync(It.Is<FoodHub.Domain.Entities.Order>(o => o.IsPriority == false)),
                Times.Once
            );
        }

        // ──────── AC-RC-05: Không cho check-in nếu trạng thái không hợp lệ ────────

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_ReservationNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);

            var reservationRepo = new Mock<IGenericRepository<FoodHub.Domain.Entities.Reservation>>();
            reservationRepo
                .Setup(r => r.Query())
                .Returns(
                    new List<FoodHub.Domain.Entities.Reservation>().AsQueryable().BuildMock()
                );
            _mockUow
                .Setup(u => u.Repository<FoodHub.Domain.Entities.Reservation>())
                .Returns(reservationRepo.Object);

            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.Reservation.NotFound))
                .Returns("Reservation not found");

            var command = new CheckInReservationCommand
            {
                ReservationId = Guid.NewGuid(),
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_StatusCancelled()
        {
            // Arrange
            var (reservation, _, _) = CreateTestData(status: ReservationStatus.Cancelled);
            SetupCommonMocks(reservation);

            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.Reservation.InvalidStatusForCheckIn))
                .Returns("Invalid status for check-in");

            var command = new CheckInReservationCommand
            {
                ReservationId = reservation.ReservationId,
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_StatusAlreadyCheckedIn()
        {
            // Arrange
            var (reservation, _, _) = CreateTestData(status: ReservationStatus.CheckIn);
            SetupCommonMocks(reservation);

            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.Reservation.InvalidStatusForCheckIn))
                .Returns("Invalid status for check-in");

            var command = new CheckInReservationCommand
            {
                ReservationId = reservation.ReservationId,
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_TableOccupied()
        {
            // Arrange
            var (reservation, _, _) = CreateTestData(tableStatus: TableStatus.Occupied);
            SetupCommonMocks(reservation);

            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.Reservation.TableOccupied))
                .Returns("Table is occupied");

            var command = new CheckInReservationCommand
            {
                ReservationId = reservation.ReservationId,
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Conflict);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_UserNotLoggedIn()
        {
            // Arrange
            _mockCurrentUserService.Setup(s => s.UserId).Returns((string?)null);
            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.Auth.UserNotLoggedIn))
                .Returns("User not logged in");

            var command = new CheckInReservationCommand
            {
                ReservationId = Guid.NewGuid(),
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Unauthorized);
        }
    }
}
