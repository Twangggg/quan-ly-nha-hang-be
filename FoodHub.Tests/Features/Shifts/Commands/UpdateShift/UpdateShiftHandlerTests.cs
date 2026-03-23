using AutoMapper;
using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Shifts.Commands.UpdateShift;
using FoodHub.Application.Interfaces;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
 
namespace FoodHub.Tests.Features.Shifts.Commands.UpdateShift
{
    public class UpdateShiftHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ICacheService> _mockCache;
        private readonly Mock<IMessageService> _mockMessage;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<ILogger<UpdateShiftHandler>> _mockLogger;
 
        public UpdateShiftHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockCache = new Mock<ICacheService>();
            _mockMessage = new Mock<IMessageService>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockLogger = new Mock<ILogger<UpdateShiftHandler>>();
 
            _mockCurrentUser.Setup(c => c.UserId).Returns(Guid.NewGuid().ToString());
        }
 
        [Fact]
        public async Task Handle_ShouldReturnNotFound_WhenShiftNotExists()
        {
            // Arrange
            var shiftId = Guid.NewGuid();
            var command = new UpdateShiftCommand { ShiftId = shiftId, Name = "Night", StartTime = new TimeSpan(22, 0, 0), EndTime = new TimeSpan(6, 0, 0) };
            
            var repo = new Mock<IGenericRepository<Shift>>();
            repo.Setup(r => r.Query()).Returns(new List<Shift>().AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Shift>()).Returns(repo.Object);
            _mockMessage.Setup(m => m.GetMessage(It.IsAny<string>())).Returns("Not found");
 
            var handler = BuildHandler();
 
            // Act
            var result = await handler.Handle(command, CancellationToken.None);
 
            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }
 
        [Fact]
        public async Task Handle_ShouldUpdate_WhenDataIsValid()
        {
            // Arrange
            var shiftId = Guid.NewGuid();
            var existingShift = Shift.Create("Old", new TimeSpan(7, 0, 0), new TimeSpan(12, 0, 0), Guid.NewGuid());
            existingShift.ShiftId = shiftId;
 
            var command = new UpdateShiftCommand { ShiftId = shiftId, Name = "New Name", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(11, 0, 0) };
            
            var repo = new Mock<IGenericRepository<Shift>>();
            repo.Setup(r => r.Query()).Returns(new List<Shift> { existingShift }.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Shift>()).Returns(repo.Object);
 
            var handler = BuildHandler();
 
            // Act
            var result = await handler.Handle(command, CancellationToken.None);
 
            // Assert
            result.IsSuccess.Should().BeTrue();
            existingShift.Name.Should().Be("New Name");
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once());
        }
 
        private UpdateShiftHandler BuildHandler()
        {
            return new UpdateShiftHandler(
                _mockUow.Object,
                _mockMapper.Object,
                _mockCache.Object,
                _mockMessage.Object,
                _mockCurrentUser.Object,
                _mockLogger.Object
            );
        }
    }
}
