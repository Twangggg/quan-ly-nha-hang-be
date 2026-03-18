using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Reservations.Commands.CreateInternalReservation;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.Reservations.CreateInternalReservation
{
    public class CreateInternalReservationHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ILogger<CreateInternalReservationHandler>> _mockLogger;
        private readonly Mock<IMessageService> _mockMessageService;

        public CreateInternalReservationHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<CreateInternalReservationHandler>>();
            _mockMessageService = new Mock<IMessageService>();
        }

        [Fact]
        public async Task Handle_ShouldThrowBusinessException_WhenNoTableAvailable()
        {
            // Arrange
            var command = new CreateInternalReservationCommand
            {
                CustomerName = "John Doe",
                CustomerPhone = "123456789",
                ReservationDate = DateOnly.FromDateTime(DateTime.Now),
                ReservationTime = DateTime.Now.TimeOfDay.Add(TimeSpan.FromHours(1)),
                GuestCount = 10,
            };

            var tableRepo = new Mock<IGenericRepository<Table>>();
            tableRepo.Setup(r => r.Query()).Returns(new List<Table>().AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Table>()).Returns(tableRepo.Object);

            var reservationRepo = new Mock<IGenericRepository<Reservation>>();
            reservationRepo
                .Setup(r => r.Query())
                .Returns(new List<Reservation>().AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Reservation>()).Returns(reservationRepo.Object);

            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.Reservation.NoTableAvailable))
                .Returns("Không có bàn trống phù hợp với yêu cầu.");

            var handler = new CreateInternalReservationHandler(
                _mockUow.Object,
                _mockLogger.Object,
                _mockMessageService.Object
            );

            // Act & Assert
            var ex = await Assert.ThrowsAsync<BusinessException>(() =>
                handler.Handle(command, CancellationToken.None)
            );
            ex.GetType().Should().Be(typeof(BusinessException));
        }

        [Fact]
        public async Task Handle_ShouldCreateReservation_WhenTableAvailable()
        {
            // Arrange
            var tableId = Guid.NewGuid();
            var areaId = Guid.NewGuid();
            var command = new CreateInternalReservationCommand
            {
                CustomerName = "Jane Doe",
                CustomerPhone = "987654321",
                ReservationDate = DateOnly.FromDateTime(DateTime.Now),
                ReservationTime = DateTime.Now.TimeOfDay.Add(TimeSpan.FromHours(1)),
                GuestCount = 2,
                AreaId = areaId,
            };

            var table = new Table
            {
                TableId = tableId,
                AreaId = areaId,
                Capacity = 4,
                Status = TableStatus.Available,
            };
            var tableRepo = new Mock<IGenericRepository<Table>>();
            tableRepo
                .Setup(r => r.Query())
                .Returns(
                    new List<Table> { table }
                        .AsQueryable()
                        .BuildMock()
                );

            var reservationRepo = new Mock<IGenericRepository<Reservation>>();
            reservationRepo
                .Setup(r => r.Query())
                .Returns(new List<Reservation>().AsQueryable().BuildMock());

            _mockUow.Setup(u => u.Repository<Table>()).Returns(tableRepo.Object);
            _mockUow.Setup(u => u.Repository<Reservation>()).Returns(reservationRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var handler = new CreateInternalReservationHandler(
                _mockUow.Object,
                _mockLogger.Object,
                _mockMessageService.Object
            );

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeEmpty();
            reservationRepo.Verify(
                r =>
                    r.AddAsync(
                        It.Is<Reservation>(res =>
                            res.TableId == tableId && res.CustomerName == "Jane Doe"
                        )
                    ),
                Times.Once
            );
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
