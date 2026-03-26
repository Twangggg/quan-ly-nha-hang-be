using FluentAssertions;
using FoodHub.Application.Features.Reservations.Queries.GetAvailableTables;
using FoodHub.Application.Features.Reservations.Services;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Reservations;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MockQueryable.Moq;
using Moq;
using ReservationEntity = FoodHub.Domain.Entities.Reservation;

namespace FoodHub.Tests.Features.Reservations.Queries
{
    public class GetAvailableTablesHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<IReservationSettingsProvider> _settingsProvider = new();

        public GetAvailableTablesHandlerTests()
        {
            _settingsProvider
                .Setup(x => x.GetOrCreateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ReservationSettings.CreateDefault());
        }

        [Fact]
        public async Task Handle_Should_FilterOutTable_When_ReservationFallsInsideBuffer()
        {
            var table = CreateTable();
            var existing = ReservationEntity.CreateBooked(
                "Existing",
                "0900000000",
                DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                TimeSpan.FromHours(19),
                2,
                null,
                table.TableId,
                table.AreaId
            );

            var tableRepo = new Mock<IGenericRepository<Table>>();
            tableRepo.Setup(x => x.Query()).Returns(new List<Table> { table }.AsQueryable().BuildMock());

            var reservationRepo = new Mock<IGenericRepository<ReservationEntity>>();
            reservationRepo.Setup(x => x.Query()).Returns(new List<ReservationEntity> { existing }.AsQueryable().BuildMock());

            _unitOfWork.Setup(x => x.Repository<Table>()).Returns(tableRepo.Object);
            _unitOfWork.Setup(x => x.Repository<ReservationEntity>()).Returns(reservationRepo.Object);

            var handler = new GetAvailableTablesHandler(
                _unitOfWork.Object,
                _settingsProvider.Object,
                new ReservationLifecyclePolicy()
            );

            var result = await handler.Handle(
                new GetAvailableTablesQuery
                {
                    ReservationDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                    ReservationTime = TimeSpan.FromHours(18),
                    GuestCount = 2,
                    AreaId = table.AreaId,
                },
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_Should_ReturnTable_When_BufferIsShortEnough()
        {
            var table = CreateTable();
            var existing = ReservationEntity.CreateBooked(
                "Existing",
                "0900000000",
                DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                TimeSpan.FromHours(19),
                2,
                null,
                table.TableId,
                table.AreaId
            );

            var tableRepo = new Mock<IGenericRepository<Table>>();
            tableRepo.Setup(x => x.Query()).Returns(new List<Table> { table }.AsQueryable().BuildMock());

            var reservationRepo = new Mock<IGenericRepository<ReservationEntity>>();
            reservationRepo.Setup(x => x.Query()).Returns(new List<ReservationEntity> { existing }.AsQueryable().BuildMock());

            var settings = ReservationSettings.CreateDefault();
            settings.Update(
                settings.OpenTime,
                settings.CloseTime,
                settings.BreakEnabled,
                settings.BreakStart,
                settings.BreakEnd,
                30,
                settings.MinLeadTimeMinutes,
                settings.GracePeriodMinutes
            );
            _settingsProvider
                .Setup(x => x.GetOrCreateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(settings);
            _unitOfWork.Setup(x => x.Repository<Table>()).Returns(tableRepo.Object);
            _unitOfWork.Setup(x => x.Repository<ReservationEntity>()).Returns(reservationRepo.Object);

            var handler = new GetAvailableTablesHandler(
                _unitOfWork.Object,
                _settingsProvider.Object,
                new ReservationLifecyclePolicy()
            );

            var result = await handler.Handle(
                new GetAvailableTablesQuery
                {
                    ReservationDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                    ReservationTime = TimeSpan.FromHours(18),
                    GuestCount = 2,
                    AreaId = table.AreaId,
                },
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(1);
            result.Data.Single().TableId.Should().Be(table.TableId);
        }

        private static Table CreateTable()
        {
            var areaId = Guid.NewGuid();

            return new Table
            {
                TableId = Guid.NewGuid(),
                TableNumber = 1,
                Capacity = 4,
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
