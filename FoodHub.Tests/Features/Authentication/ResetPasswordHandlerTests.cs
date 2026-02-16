using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Authentication.Commands.ResetPassword;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.Authentication
{
    public class ResetPasswordHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IPasswordService> _mockPasswordService;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly ResetPasswordHandler _handler;

        public ResetPasswordHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockPasswordService = new Mock<IPasswordService>();
            _mockMessageService = new Mock<IMessageService>();

            _handler = new ResetPasswordHandler(
                _mockUow.Object,
                _mockPasswordService.Object,
                _mockMessageService.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_PasswordResetSuccessfully()
        {
            // Arrange
            var employee = new Employee
            {
                EmployeeId = Guid.NewGuid(),
                EmployeeCode = "EMP001",
                Email = "test@example.com",
                FullName = "Test User",
                Status = EmployeeStatus.Active,
                PasswordHash = "oldhash"
            };
            var resetToken = new PasswordResetToken
            {
                TokenId = Guid.NewGuid(),
                EmployeeId = employee.EmployeeId,
                TokenHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", // SHA256 of empty string
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
                UsedAt = null,
                Employee = employee
            };
            var command = new ResetPasswordCommand("", "newpassword", "newpassword");

            var resetTokens = new List<PasswordResetToken> { resetToken }.AsQueryable().BuildMock();
            var repo = new Mock<IGenericRepository<PasswordResetToken>>();
            repo.Setup(r => r.Query()).Returns(resetTokens);
            _mockUow.Setup(u => u.Repository<PasswordResetToken>()).Returns(repo.Object);

            var otherTokens = new List<PasswordResetToken>().AsQueryable().BuildMock();
            var otherRepo = new Mock<IGenericRepository<PasswordResetToken>>();
            otherRepo.Setup(r => r.Query()).Returns(otherTokens);

            var refreshTokens = new List<RefreshToken>().AsQueryable().BuildMock();
            var refreshRepo = new Mock<IGenericRepository<RefreshToken>>();
            refreshRepo.Setup(r => r.Query()).Returns(refreshTokens);
            _mockUow.Setup(u => u.Repository<RefreshToken>()).Returns(refreshRepo.Object);

            var auditRepo = new Mock<IGenericRepository<AuditLog>>();
            _mockUow.Setup(u => u.Repository<AuditLog>()).Returns(auditRepo.Object);

            _mockPasswordService.Setup(p => p.HashPassword("newpassword")).Returns("newhash");
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.Auth.PasswordResetSuccess)).Returns("Password reset successfully");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("Password reset successfully");
            employee.PasswordHash.Should().Be("newhash");
            resetToken.UsedAt.Should().NotBeNull();
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_TokenNotFound()

        {
            // Arrange
            var command = new ResetPasswordCommand("invalid_token", "newpassword", "newpassword");

            var resetTokens = new List<PasswordResetToken>().AsQueryable().BuildMock();
            var repo = new Mock<IGenericRepository<PasswordResetToken>>();
            repo.Setup(r => r.Query()).Returns(resetTokens);
            _mockUow.Setup(u => u.Repository<PasswordResetToken>()).Returns(repo.Object);

            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.Auth.InvalidResetLink)).Returns("Invalid reset link");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Invalid reset link");
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_TokenExpired()
        {
            // Arrange
            var employee = new Employee
            {
                EmployeeId = Guid.NewGuid(),
                Status = EmployeeStatus.Active
            };
            var resetToken = new PasswordResetToken
            {
                TokenId = Guid.NewGuid(),
                EmployeeId = employee.EmployeeId,
                TokenHash = "hashed_token",
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1),
                UsedAt = null,
                Employee = employee
            };
            var command = new ResetPasswordCommand("plain_token", "newpassword", "newpassword");

            var resetTokens = new List<PasswordResetToken> { resetToken }.AsQueryable().BuildMock();
            var repo = new Mock<IGenericRepository<PasswordResetToken>>();
            repo.Setup(r => r.Query()).Returns(resetTokens);
            _mockUow.Setup(u => u.Repository<PasswordResetToken>()).Returns(repo.Object);

            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.Auth.InvalidResetLink)).Returns("Invalid reset link");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Invalid reset link");
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_TokenAlreadyUsed()
        {
            // Arrange
            var employee = new Employee
            {
                EmployeeId = Guid.NewGuid(),
                Status = EmployeeStatus.Active
            };
            var resetToken = new PasswordResetToken
            {
                TokenId = Guid.NewGuid(),
                EmployeeId = employee.EmployeeId,
                TokenHash = "hashed_token",
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
                UsedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
                Employee = employee
            };
            var command = new ResetPasswordCommand("plain_token", "newpassword", "newpassword");

            var resetTokens = new List<PasswordResetToken> { resetToken }.AsQueryable().BuildMock();
            var repo = new Mock<IGenericRepository<PasswordResetToken>>();
            repo.Setup(r => r.Query()).Returns(resetTokens);
            _mockUow.Setup(u => u.Repository<PasswordResetToken>()).Returns(repo.Object);

            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.Auth.InvalidResetLink)).Returns("Invalid reset link");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Invalid reset link");
        }
    }
}
