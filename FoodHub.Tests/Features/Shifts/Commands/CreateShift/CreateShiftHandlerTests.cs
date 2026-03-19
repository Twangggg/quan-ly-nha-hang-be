using AutoMapper;
using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Shifts.Commands.CreateShift;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace FoodHub.Tests.Features.Shifts.Commands.CreateShift
{
    public class CreateShiftHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICacheService> _mockCache;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<IMessageService> _mockMessage;
        private readonly Mock<ILogger<CreateShiftHandler>> _mockLogger;

        public CreateShiftHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCache = new Mock<ICacheService>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockMessage = new Mock<IMessageService>();
            _mockLogger = new Mock<ILogger<CreateShiftHandler>>();
        }

        [Fact]
        public async Task Handle_ShouldCreateShift_WhenDataIsValid()
        {
            // Arrange
            var command = new CreateShiftCommand
            {
                Name = "Morning Shift",
                StartTime = new TimeSpan(7, 0, 0),
                EndTime = new TimeSpan(12, 0, 0)
            };

            var shiftId = Guid.NewGuid();
            var repo = new Mock<IGenericRepository<Shift>>();
            _mockUow.Setup(u => u.Repository<Shift>()).Returns(repo.Object);
            _mockCurrentUser.Setup(u => u.UserId).Returns(Guid.NewGuid().ToString());
            _mockCurrentUser.Setup(u => u.Role).Returns("Manager");

            var handler = new CreateShiftHandler(
                _mockUow.Object,
                _mockCurrentUser.Object,
                _mockMessage.Object,
                _mockCache.Object,
                _mockLogger.Object);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Name.Should().Be(command.Name);
            repo.Verify(r => r.AddAsync(It.IsAny<Shift>()), Times.Once);
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockUow.Verify(u => u.BeginTransactionAsync(), Times.Once);
            _mockUow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }
    }
}
