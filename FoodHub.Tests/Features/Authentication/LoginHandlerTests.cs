using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Authentication.Commands.Login;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.Authentication
{
    public class LoginHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IPasswordService> _mockPasswordService;
        private readonly Mock<ITokenService> _mockTokenService;
        private readonly Mock<IRateLimiter> _mockRateLimiter;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ILogger<LoginHandler>> _mockLogger;
        private readonly LoginHandler _handler;

        public LoginHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockPasswordService = new Mock<IPasswordService>();
            _mockTokenService = new Mock<ITokenService>();
            _mockRateLimiter = new Mock<IRateLimiter>();
            _mockMessageService = new Mock<IMessageService>();
            _mockLogger = new Mock<ILogger<LoginHandler>>();

            _handler = new LoginHandler(
                _mockUow.Object,
                _mockPasswordService.Object,
                _mockTokenService.Object,
                _mockRateLimiter.Object,
                _mockMessageService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_ValidCredentials()
        {
            // Arrange
            var employee = new Employee
            {
                EmployeeId = Guid.NewGuid(),
                EmployeeCode = "EMP001",
                PasswordHash = "hashedpassword",
                Email = "test@example.com",
                Role = EmployeeRole.Manager,
                Status = EmployeeStatus.Active,
            };
            var command = new LoginCommand("EMP001", "password", false);

            var employees = new List<Employee> { employee }
                .AsQueryable()
                .BuildMock();
            var repo = new Mock<IGenericRepository<Employee>>();
            repo.Setup(r => r.Query()).Returns(employees);
            _mockUow.Setup(u => u.Repository<Employee>()).Returns(repo.Object);

            _mockPasswordService
                .Setup(p => p.VerifyPassword("password", "hashedpassword"))
                .Returns(true);
            _mockRateLimiter
                .Setup(r => r.IsBlockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _mockRateLimiter
                .Setup(r => r.ResetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockTokenService.Setup(t => t.GenerateAccessToken(employee)).Returns("access_token");
            _mockTokenService.Setup(t => t.GetTokenExpirationSeconds()).Returns(3600);
            _mockTokenService.Setup(t => t.GenerateRefreshToken()).Returns("refresh_token");
            _mockTokenService.Setup(t => t.GetRefreshTokenExpirationDays()).Returns(7);

            var refreshTokenRepo = new Mock<IGenericRepository<RefreshToken>>();
            _mockUow.Setup(u => u.Repository<RefreshToken>()).Returns(refreshTokenRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.AccessToken.Should().Be("access_token");
            result.Data.RefreshToken.Should().Be("refresh_token");
            result.Data.EmployeeCode.Should().Be("EMP001");
            result.Data.Email.Should().Be("test@example.com");
            result.Data.Role.Should().Be("Manager");
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_UserNotFound()
        {
            // Arrange
            var command = new LoginCommand("EMP001", "password", false);

            var employees = new List<Employee>().AsQueryable().BuildMock();
            var repo = new Mock<IGenericRepository<Employee>>();
            repo.Setup(r => r.Query()).Returns(employees);
            _mockUow.Setup(u => u.Repository<Employee>()).Returns(repo.Object);

            _mockRateLimiter
                .Setup(r => r.IsBlockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.Auth.InvalidCredentials))
                .Returns("Invalid credentials");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Invalid credentials");
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_InvalidPassword()
        {
            // Arrange
            var employee = new Employee
            {
                EmployeeId = Guid.NewGuid(),
                EmployeeCode = "EMP001",
                PasswordHash = "hashedpassword",
                Status = EmployeeStatus.Active,
            };
            var command = new LoginCommand("EMP001", "wrongpassword", false);

            var employees = new List<Employee> { employee }
                .AsQueryable()
                .BuildMock();
            var repo = new Mock<IGenericRepository<Employee>>();
            repo.Setup(r => r.Query()).Returns(employees);
            _mockUow.Setup(u => u.Repository<Employee>()).Returns(repo.Object);

            _mockPasswordService
                .Setup(p => p.VerifyPassword("wrongpassword", "hashedpassword"))
                .Returns(false);
            _mockRateLimiter
                .Setup(r => r.IsBlockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _mockRateLimiter
                .Setup(r =>
                    r.RegisterFailAsync(
                        It.IsAny<string>(),
                        5,
                        TimeSpan.FromMinutes(15),
                        TimeSpan.FromMinutes(15),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(1);
            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.Auth.InvalidCredentials))
                .Returns("Invalid credentials");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Invalid credentials");
            _mockRateLimiter.Verify(
                r =>
                    r.RegisterFailAsync(
                        It.IsAny<string>(),
                        5,
                        TimeSpan.FromMinutes(15),
                        TimeSpan.FromMinutes(15),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_AccountBlocked()
        {
            // Arrange
            var command = new LoginCommand("EMP001", "password", false);

            _mockRateLimiter
                .Setup(r => r.IsBlockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.Auth.AccountBlocked))
                .Returns("Account blocked");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Account blocked");
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_AccountInactive()
        {
            // Arrange
            var employee = new Employee
            {
                EmployeeId = Guid.NewGuid(),
                EmployeeCode = "EMP001",
                PasswordHash = "hashedpassword",
                Status = EmployeeStatus.Inactive,
            };
            var command = new LoginCommand("EMP001", "password", false);

            var employees = new List<Employee> { employee }
                .AsQueryable()
                .BuildMock();
            var repo = new Mock<IGenericRepository<Employee>>();
            repo.Setup(r => r.Query()).Returns(employees);
            _mockUow.Setup(u => u.Repository<Employee>()).Returns(repo.Object);

            _mockPasswordService
                .Setup(p => p.VerifyPassword("password", "hashedpassword"))
                .Returns(true);
            _mockRateLimiter
                .Setup(r => r.IsBlockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _mockRateLimiter
                .Setup(r => r.ResetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockMessageService
                .Setup(m => m.GetMessage(MessageKeys.Auth.AccountInactive))
                .Returns("Account inactive");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Account inactive");
        }
    }
}
