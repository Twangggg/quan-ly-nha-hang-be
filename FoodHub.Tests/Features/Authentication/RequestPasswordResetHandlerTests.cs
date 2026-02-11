using Moq;
using FluentAssertions;
using FoodHub.Application.Features.Authentication.Commands.RequestPasswordReset;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Microsoft.Extensions.Configuration;

namespace FoodHub.Tests.Features.Authentication
{
    public class RequestPasswordResetHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IBackgroundEmailSender> _mockEmailSender;
        private readonly Mock<IRateLimiter> _mockRateLimiter;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly RequestPasswordResetHandler _handler;

        public RequestPasswordResetHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockEmailSender = new Mock<IBackgroundEmailSender>();
            _mockRateLimiter = new Mock<IRateLimiter>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockMessageService = new Mock<IMessageService>();

            _handler = new RequestPasswordResetHandler(
                _mockUow.Object,
                _mockEmailSender.Object,
                _mockRateLimiter.Object,
                _mockConfiguration.Object,
                _mockCurrentUserService.Object,
                _mockMessageService.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_EmployeeExistsAndActive()
        {
            // Arrange
            var employee = new Employee
            {
                EmployeeId = Guid.NewGuid(),
                EmployeeCode = "EMP001",
                Email = "test@example.com",
                FullName = "Test User",
                Status = EmployeeStatus.Active
            };
            var command = new RequestPasswordResetCommand("EMP001");

            var employees = new List<Employee> { employee }.AsQueryable().BuildMock();
            var repo = new Mock<IGenericRepository<Employee>>();
            repo.Setup(r => r.Query()).Returns(employees);
            _mockUow.Setup(u => u.Repository<Employee>()).Returns(repo.Object);

            var resetTokenRepo = new Mock<IGenericRepository<PasswordResetToken>>();
            _mockUow.Setup(u => u.Repository<PasswordResetToken>()).Returns(resetTokenRepo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _mockRateLimiter.Setup(r => r.IsBlockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _mockRateLimiter.Setup(r => r.RegisterFailAsync(It.IsAny<string>(), 3, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10), It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockConfiguration.Setup(c => c["FrontendUrl"]).Returns("http://localhost:3000");
            _mockEmailSender.Setup(e => e.EnqueuePasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), null, It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.Auth.PasswordResetGenericMessage)).Returns("Password reset email sent");

            _mockCurrentUserService.Setup(c => c.IpAddress).Returns("127.0.0.1");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("Password reset email sent");
            _mockEmailSender.Verify(e => e.EnqueuePasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_EmployeeNotFound()
        {
            // Arrange
            var command = new RequestPasswordResetCommand("EMP001");

            var employees = new List<Employee>().AsQueryable().BuildMock();
            var repo = new Mock<IGenericRepository<Employee>>();
            repo.Setup(r => r.Query()).Returns(employees);
            _mockUow.Setup(u => u.Repository<Employee>()).Returns(repo.Object);

            _mockRateLimiter.Setup(r => r.IsBlockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _mockRateLimiter.Setup(r => r.RegisterFailAsync(It.IsAny<string>(), 3, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10), It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.Auth.PasswordResetGenericMessage)).Returns("Password reset email sent");

            _mockCurrentUserService.Setup(c => c.IpAddress).Returns("127.0.0.1");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("Password reset email sent");
            _mockEmailSender.Verify(e => e.EnqueuePasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), null, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_EmployeeInactive()
        {
            // Arrange
            var employee = new Employee
            {
                EmployeeId = Guid.NewGuid(),
                EmployeeCode = "EMP001",
                Status = EmployeeStatus.Inactive
            };
            var command = new RequestPasswordResetCommand("EMP001");

            var employees = new List<Employee> { employee }.AsQueryable().BuildMock();
            var repo = new Mock<IGenericRepository<Employee>>();
            repo.Setup(r => r.Query()).Returns(employees);
            _mockUow.Setup(u => u.Repository<Employee>()).Returns(repo.Object);

            _mockRateLimiter.Setup(r => r.IsBlockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _mockRateLimiter.Setup(r => r.RegisterFailAsync(It.IsAny<string>(), 3, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10), It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.Auth.PasswordResetGenericMessage)).Returns("Password reset email sent");

            _mockCurrentUserService.Setup(c => c.IpAddress).Returns("127.0.0.1");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("Password reset email sent");
            _mockEmailSender.Verify(e => e.EnqueuePasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), null, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_RateLimited()
        {
            // Arrange
            var command = new RequestPasswordResetCommand("EMP001");

            _mockRateLimiter.Setup(r => r.IsBlockedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.Auth.ResetRequestLimit)).Returns("Too many reset requests");

            _mockCurrentUserService.Setup(c => c.IpAddress).Returns("127.0.0.1");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Too many reset requests");
        }
    }
}
