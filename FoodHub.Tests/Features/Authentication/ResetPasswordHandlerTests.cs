using Moq;
using FluentAssertions;
using FoodHub.Application.Features.Authentication.Commands.ResetPassword;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;

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
