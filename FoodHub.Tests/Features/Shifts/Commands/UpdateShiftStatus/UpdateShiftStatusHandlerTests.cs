using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Shifts.Commands.UpdateShiftStatus;
using FoodHub.Application.Interfaces;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.Shifts.Commands.UpdateShiftStatus
{
    public class UpdateShiftStatusHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICacheService> _mockCache;
        private readonly Mock<IMessageService> _mockMessage;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;

        public UpdateShiftStatusHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCache = new Mock<ICacheService>();
            _mockMessage = new Mock<IMessageService>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenStatusIsUpdated()
        {
            // Arrange
            var shiftId = Guid.NewGuid();
            var shift = Shift.Create("Test", new TimeSpan(7, 0, 0), new TimeSpan(12, 0, 0), Guid.NewGuid());
            shift.ShiftId = shiftId;

            var command = new UpdateShiftStatusCommand(shiftId, false);
            
            var repo = new Mock<IGenericRepository<Shift>>();
            repo.Setup(r => r.Query()).Returns(new List<Shift> { shift }.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Shift>()).Returns(repo.Object);

            var handler = new UpdateShiftStatusHandler(
                _mockUow.Object,
                _mockCache.Object,
                _mockMessage.Object,
                _mockCurrentUser.Object);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            shift.Status.Should().Be(FoodHub.Domain.Enums.ShiftStatus.Inactive);
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
