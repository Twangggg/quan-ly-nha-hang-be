using Moq;
using FluentAssertions;
using FoodHub.Application.Features.Authentication.Commands.ChangePassword;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;

namespace FoodHub.Tests.Features.Authentication
{
    public class ChangePasswordHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IPasswordService> _mockPasswordService;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IRateLimiter> _mockRateLimiter;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly ChangePasswordHandler _handler;

        public ChangePasswordHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockPasswordService = new Mock<IPasswordService>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockRateLimiter = new Mock<IRateLimiter>();
            _mockMessageService = new Mock<IMessageService>();

            _handler = new ChangePasswordHandler(
                _mockUow.Object,
                _mockPasswordService.Object,
                _mockCurrentUserService.Object,
                _mockRateLimiter.Object,
                _mockMessageService.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_PasswordChangedSuccessfully()
        {
            // Arrange
            var employeeId = Guid.NewGuid();
            var employee = new Employee
            {
                EmployeeId = employeeId,
                EmployeeCode = "EMP001",
                PasswordHash = "oldhash",
                Status = EmployeeStatus.Active
            };
            var command = new ChangePasswordCommand { CurrentPassword = "oldpass", NewPassword = "newpass", ConfirmPassword = "newpass" };

            _mockCurrentUserService.Setup(c => c.UserId).Returns(employeeId.ToString());
            var employees = new List<Employee> { employee }.AsQueryable().BuildMock();
            var repo = new Mock<IGenericRepository<Employee>>();
            repo.Setup(r => r.Query()).Returns(employees);
            _mockUow.Setup(u => u.Repository<Employee>()).Returns(repo.Object);

            _mockPasswordService.Setup(p => p.VerifyPassword("oldpass", "oldhash")).Returns(true);
            _mockPasswordService.Setup(p => p.HashPassword("newpass")).Returns("newhash");
            _mockRateLimiter.Setup(r => r.IsBlockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _mockRateLimiter.Setup(r => r.ResetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var refreshTokens = new List<RefreshToken> { new RefreshToken { Token = "token1", EmployeeId = employeeId, IsRevoked = false } }.AsQueryable().BuildMock();
            var refreshRepo = new Mock<IGenericRepository<RefreshToken>>();
            refreshRepo.Setup(r => r.Query()).Returns(refreshTokens);
            _mockUow.Setup(u => u.Repository<RefreshToken>()).Returns(refreshRepo.Object);

            var auditRepo = new Mock<IGenericRepository<AuditLog>>();
            _mockUow.Setup(u => u.Repository<AuditLog>()).Returns(auditRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.Auth.PasswordChangedSuccess)).Returns("Password changed successfully");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("Password changed successfully");
            employee.PasswordHash.Should().Be("newhash");
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_UserNotLoggedIn()
        {
            // Arrange
            var command = new ChangePasswordCommand { CurrentPassword = "oldpass", NewPassword = "newpass", ConfirmPassword = "newpass" };

            _mockCurrentUserService.Setup(c => c.UserId).Returns((string)null!);
            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.Auth.UserNotLoggedIn)).Returns("User not logged in");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("User not logged in");
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_UserNotFound()
        {
            // Arrange
            var command = new ChangePasswordCommand { CurrentPassword = "oldpass", NewPassword = "newpass", ConfirmPassword = "newpass" };

            _mockCurrentUserService.Setup(c => c.UserId).Returns(Guid.NewGuid().ToString());
            var employees = new List<Employee>().AsQueryable().BuildMock();
            var repo = new Mock<IGenericRepository<Employee>>();
            repo.Setup(r => r.Query()).Returns(employees);
            _mockUow.Setup(u => u.Repository<Employee>()).Returns(repo.Object);

            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.Auth.InvalidAction)).Returns("Invalid action");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Invalid action");
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_AccountInactive()
        {
            // Arrange
            var employeeId = Guid.NewGuid();
            var employee = new Employee
            {
                EmployeeId = employeeId,
                EmployeeCode = "EMP001",
                PasswordHash = "oldhash",
                Status = EmployeeStatus.Inactive
            };
            var command = new ChangePasswordCommand { CurrentPassword = "oldpass", NewPassword = "newpass", ConfirmPassword = "newpass" };

            _mockCurrentUserService.Setup(c => c.UserId).Returns(employeeId.ToString());
            var employees = new List<Employee> { employee }.AsQueryable().BuildMock();
            var repo = new Mock<IGenericRepository<Employee>>();
            repo.Setup(r => r.Query()).Returns(employees);
            _mockUow.Setup(u => u.Repository<Employee>()).Returns(repo.Object);

            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.Auth.InvalidAction)).Returns("Invalid action");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Invalid action");
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_RateLimiterBlocked()
        {
            // Arrange
            var employeeId = Guid.NewGuid();
            var employee = new Employee
            {
                EmployeeId = employeeId,
                EmployeeCode = "EMP001",
                PasswordHash = "oldhash",
                Status = EmployeeStatus.Active
            };
            var command = new ChangePasswordCommand { CurrentPassword = "oldpass", NewPassword = "newpass", ConfirmPassword = "newpass" };

            _mockCurrentUserService.Setup(c => c.UserId).Returns(employeeId.ToString());
            var employees = new List<Employee> { employee }.AsQueryable().BuildMock();
            var repo = new Mock<IGenericRepository<Employee>>();
            repo.Setup(r => r.Query()).Returns(employees);
            _mockUow.Setup(u => u.Repository<Employee>()).Returns(repo.Object);

            _mockRateLimiter.Setup(r => r.IsBlockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.Auth.TooManyAttempts)).Returns("Too many attempts");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Too many attempts");
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_InvalidCurrentPassword()
        {
            // Arrange
            var employeeId = Guid.NewGuid();
            var employee = new Employee
            {
                EmployeeId = employeeId,
                EmployeeCode = "EMP001",
                PasswordHash = "oldhash",
                Status = EmployeeStatus.Active
            };
            var command = new ChangePasswordCommand { CurrentPassword = "wrongpass", NewPassword = "newpass", ConfirmPassword = "newpass" };

            _mockCurrentUserService.Setup(c => c.UserId).Returns(employeeId.ToString());
            var employees = new List<Employee> { employee }.AsQueryable().BuildMock();
            var repo = new Mock<IGenericRepository<Employee>>();
            repo.Setup(r => r.Query()).Returns(employees);
            _mockUow.Setup(u => u.Repository<Employee>()).Returns(repo.Object);

            _mockPasswordService.Setup(p => p.VerifyPassword("wrongpass", "oldhash")).Returns(false);
            _mockRateLimiter.Setup(r => r.IsBlockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _mockRateLimiter.Setup(r => r.RegisterFailAsync(It.IsAny<string>(), 5, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15), It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.Password.IncorrectCurrent)).Returns("Incorrect current password");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Incorrect current password");
            _mockRateLimiter.Verify(r => r.RegisterFailAsync(It.IsAny<string>(), 5, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
