using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Authentication.Commands.RevokeToken;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;

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
        public async Task Handle_Should_ReturnSuccess_When_TokenRevokedSuccessfully()
        {
            // Arrange
            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = "valid_refresh_token",

                EmployeeId = Guid.NewGuid(),
                Expires = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            };
            var command = new RevokeTokenCommand { RefreshToken = "valid_refresh_token" };

            var refreshTokens = new List<RefreshToken> { refreshToken }.AsQueryable().BuildMock();
            var repo = new Mock<IGenericRepository<RefreshToken>>();
            repo.Setup(r => r.Query()).Returns(refreshTokens);
            _mockUow.Setup(u => u.Repository<RefreshToken>()).Returns(repo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            refreshToken.IsRevoked.Should().BeTrue();
            refreshToken.UpdatedAt.Should().NotBeNull();
            _mockUow.Verify(u => u.Repository<RefreshToken>().Update(refreshToken), Times.Once);
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
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
