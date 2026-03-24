using AutoMapper;
using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Shifts.Commands.CreateShift;
using FoodHub.Application.Interfaces;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using Moq;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Microsoft.Extensions.Logging;
 
namespace FoodHub.Tests.Features.Shifts.Commands.CreateShift
{
    public class CreateShiftHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ICacheService> _mockCache;
        private readonly Mock<IMessageService> _mockMessage;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<ILogger<CreateShiftHandler>> _mockLogger;
 
        public CreateShiftHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockCache = new Mock<ICacheService>();
            _mockMessage = new Mock<IMessageService>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockLogger = new Mock<ILogger<CreateShiftHandler>>();
 
            _mockCurrentUser.Setup(c => c.UserId).Returns(Guid.NewGuid().ToString());
        }
 
        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenShiftIsCreatedSuccessfully()
        {
            // Arrange
            var command = new CreateShiftCommand
            {
                Name = "Morning Shift",
                StartTime = new TimeSpan(7, 0, 0),
                EndTime = new TimeSpan(12, 0, 0)
            };
 
            var shifts = new List<Shift>().AsQueryable().BuildMock();
            var repoMock = new Mock<IGenericRepository<Shift>>();
            repoMock.Setup(r => r.Query()).Returns(shifts);
            
            _mockUow.Setup(u => u.Repository<Shift>()).Returns(repoMock.Object);
            _mockUow.Setup(u => u.Repository<AuditLog>()).Returns(new Mock<IGenericRepository<AuditLog>>().Object);
 
            var handler = BuildHandler();
 
            // Act
            var result = await handler.Handle(command, CancellationToken.None);
 
            // Assert
            result.IsSuccess.Should().BeTrue();
            repoMock.Verify(r => r.AddAsync(It.IsAny<Shift>()), Times.Once);
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
 
        private CreateShiftHandler BuildHandler()
        {
            return new CreateShiftHandler(
                _mockUow.Object,
                _mockMapper.Object,
                _mockCache.Object,
                _mockCurrentUser.Object,
                _mockMessage.Object,
                _mockLogger.Object);
        }
    }
}
