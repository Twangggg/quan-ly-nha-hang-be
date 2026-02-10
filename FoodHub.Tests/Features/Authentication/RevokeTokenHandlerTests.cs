using Moq;
using FluentAssertions;
using FoodHub.Application.Features.Authentication.Commands.RevokeToken;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;

namespace FoodHub.Tests.Features.Authentication
{
    public class RevokeTokenHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly RevokeTokenHandler _handler;

        public RevokeTokenHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockMessageService = new Mock<IMessageService>();

            _handler = new RevokeTokenHandler(
                _mockUow.Object,
                _mockMessageService.Object);
        }



        [Fact]
        public async Task Handle_Should_ReturnFailure_When_TokenNotFound()
        {
            // Arrange
            var command = new RevokeTokenCommand { RefreshToken = "invalid_token" };

            var refreshTokens = new List<RefreshToken>().AsQueryable().BuildMock();
            var repo = new Mock<IGenericRepository<RefreshToken>>();
            repo.Setup(r => r.Query()).Returns(refreshTokens);
            _mockUow.Setup(u => u.Repository<RefreshToken>()).Returns(repo.Object);

            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.Auth.InvalidToken)).Returns("Invalid token");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Invalid token");
        }
    }
}
