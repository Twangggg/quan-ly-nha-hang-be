using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Authentication.Commands.RefreshToken;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.Authentication
{
    public class RefreshTokenHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ITokenService> _mockTokenService;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly RefreshTokenHandler _handler;

        public RefreshTokenHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockTokenService = new Mock<ITokenService>();
            _mockMessageService = new Mock<IMessageService>();

            _handler = new RefreshTokenHandler(
                _mockUow.Object,
                _mockTokenService.Object,
                _mockMessageService.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_TokenRefreshedSuccessfully()
        {
            // Arrange
            var employee = new Employee
            {
                EmployeeId = Guid.NewGuid(),
                EmployeeCode = "EMP001",
                Email = "test@example.com",
                Role = EmployeeRole.Manager,
                Status = EmployeeStatus.Active
            };
            var refreshTokenEntity = new RefreshToken
            {
                Token = "old_refresh_token",
                Expires = DateTime.UtcNow.AddDays(10),
                IsRevoked = false,
                EmployeeId = employee.EmployeeId,
                Employee = employee,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            };
            var command = new RefreshTokenCommand { RefreshToken = "old_refresh_token" };

            var refreshTokens = new List<RefreshToken> { refreshTokenEntity }.AsQueryable().BuildMock();
            var repo = new Mock<IGenericRepository<RefreshToken>>();
            repo.Setup(r => r.Query()).Returns(refreshTokens);
            _mockUow.Setup(u => u.Repository<RefreshToken>()).Returns(repo.Object);

            _mockTokenService.Setup(t => t.GenerateAccessToken(employee)).Returns("new_access_token");
            _mockTokenService.Setup(t => t.GenerateRefreshToken()).Returns("new_refresh_token");
            _mockTokenService.Setup(t => t.GetRefreshTokenExpirationDays()).Returns(7);
            _mockTokenService.Setup(t => t.GetTokenExpirationSeconds()).Returns(3600);

            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.AccessToken.Should().Be("new_access_token");
            result.Data.RefreshToken.Should().Be("new_refresh_token");
            result.Data.EmployeeCode.Should().Be("EMP001");
            result.Data.Email.Should().Be("test@example.com");
            result.Data.Role.Should().Be("Manager");
            refreshTokenEntity.IsRevoked.Should().BeTrue();
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_RefreshTokenNotFound()
        {
            // Arrange
            var command = new RefreshTokenCommand { RefreshToken = "invalid_token" };

            var refreshTokens = new List<RefreshToken>().AsQueryable().BuildMock();
            var repo = new Mock<IGenericRepository<RefreshToken>>();
            repo.Setup(r => r.Query()).Returns(refreshTokens);
            _mockUow.Setup(u => u.Repository<RefreshToken>()).Returns(repo.Object);

            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.Auth.RefreshTokenNotFound)).Returns("Refresh token not found");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Refresh token not found");
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_RefreshTokenExpired()
        {
            // Arrange
            var employee = new Employee
            {
                EmployeeId = Guid.NewGuid(),
                EmployeeCode = "EMP001",
                Status = EmployeeStatus.Active
            };
            var refreshTokenEntity = new RefreshToken
            {
                Token = "expired_token",
                Expires = DateTime.UtcNow.AddDays(-1),
                IsRevoked = false,
                EmployeeId = employee.EmployeeId,
                Employee = employee
            };
            var command = new RefreshTokenCommand { RefreshToken = "expired_token" };

            var refreshTokens = new List<RefreshToken> { refreshTokenEntity }.AsQueryable().BuildMock();
            var repo = new Mock<IGenericRepository<RefreshToken>>();
            repo.Setup(r => r.Query()).Returns(refreshTokens);
            _mockUow.Setup(u => u.Repository<RefreshToken>()).Returns(repo.Object);

            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.Auth.RefreshTokenExpired)).Returns("Refresh token expired");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Refresh token expired");
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_RefreshTokenRevoked()
        {
            // Arrange
            var employee = new Employee
            {
                EmployeeId = Guid.NewGuid(),
                EmployeeCode = "EMP001",
                Status = EmployeeStatus.Active
            };
            var refreshTokenEntity = new RefreshToken
            {
                Token = "revoked_token",
                Expires = DateTime.UtcNow.AddDays(10),
                IsRevoked = true,
                EmployeeId = employee.EmployeeId,
                Employee = employee
            };
            var command = new RefreshTokenCommand { RefreshToken = "revoked_token" };

            var refreshTokens = new List<RefreshToken> { refreshTokenEntity }.AsQueryable().BuildMock();
            var repo = new Mock<IGenericRepository<RefreshToken>>();
            repo.Setup(r => r.Query()).Returns(refreshTokens);
            _mockUow.Setup(u => u.Repository<RefreshToken>()).Returns(repo.Object);

            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.Auth.RefreshTokenRevoked)).Returns("Refresh token revoked");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Refresh token revoked");
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_EmployeeInactive()
        {
            // Arrange
            var employee = new Employee
            {
                EmployeeId = Guid.NewGuid(),
                EmployeeCode = "EMP001",
                Status = EmployeeStatus.Inactive
            };
            var refreshTokenEntity = new RefreshToken
            {
                Token = "valid_token",
                Expires = DateTime.UtcNow.AddDays(10),
                IsRevoked = false,
                EmployeeId = employee.EmployeeId,
                Employee = employee
            };
            var command = new RefreshTokenCommand { RefreshToken = "valid_token" };

            var refreshTokens = new List<RefreshToken> { refreshTokenEntity }.AsQueryable().BuildMock();
            var repo = new Mock<IGenericRepository<RefreshToken>>();
            repo.Setup(r => r.Query()).Returns(refreshTokens);
            _mockUow.Setup(u => u.Repository<RefreshToken>()).Returns(repo.Object);

            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.Auth.AccountInactive)).Returns("Account inactive");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Account inactive");
        }
    }
}
