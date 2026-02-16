using AutoMapper;
using FluentAssertions;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Employees.Commands.ChangeRole;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.Employees
{
    public class ChangeRoleHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IEmployeeServices> _mockEmployeeServices;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IBackgroundEmailSender> _mockEmailSender;
        private readonly Mock<IPasswordService> _mockPasswordService;
        private readonly Mock<IMessageService> _mockMessage;
        private readonly Mock<ILogger<ChangeRoleHandler>> _mockLogger;
        private readonly ChangeRoleHandler _handler;

        public ChangeRoleHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockEmployeeServices = new Mock<IEmployeeServices>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockMapper = new Mock<IMapper>();
            _mockEmailSender = new Mock<IBackgroundEmailSender>();
            _mockPasswordService = new Mock<IPasswordService>();
            _mockMessage = new Mock<IMessageService>();
            _mockLogger = new Mock<ILogger<ChangeRoleHandler>>();

            _handler = new ChangeRoleHandler(
                _mockUow.Object,
                _mockEmployeeServices.Object,
                _mockCurrentUser.Object,
                _mockEmailSender.Object,
                _mockMapper.Object,
                _mockPasswordService.Object,
                _mockMessage.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_RoleChanged()
        {
            // Arrange
            var auditorId = Guid.NewGuid();
            var oldEmployeeId = Guid.NewGuid();
            var newEmployeeId = Guid.NewGuid();
            var command = new ChangeRoleCommand
            {
                EmployeeCode = "EMP001",
                CurrentRole = EmployeeRole.Waiter,
                NewRole = EmployeeRole.ChefBar,
            };

            _mockCurrentUser.Setup(c => c.UserId).Returns(auditorId.ToString());

            var oldEmployee = new Employee
            {
                EmployeeId = oldEmployeeId,
                EmployeeCode = "EMP001",
                FullName = "John Doe",
                Email = "john@example.com",
                Username = "johndoe",
                Phone = "123456789",
                Address = "123 Main St",
                DateOfBirth = DateOnly.Parse("1990-01-01"),
                Role = EmployeeRole.Waiter,
                Status = EmployeeStatus.Active,
                PasswordHash = "hashedpassword",
            };

            var employees = new List<Employee> { oldEmployee }
                .AsQueryable()
                .BuildMock();
            var employeeRepo = new Mock<IGenericRepository<Employee>>();
            employeeRepo.Setup(r => r.Query()).Returns(employees);
            _mockUow.Setup(u => u.Repository<Employee>()).Returns(employeeRepo.Object);

            _mockEmployeeServices
                .Setup(s => s.GenerateEmployeeCodeAsync(EmployeeRole.ChefBar))
                .ReturnsAsync("EMP002");

            var refreshTokens = new List<RefreshToken>
            {
                new RefreshToken { EmployeeId = oldEmployeeId, IsRevoked = false },
            }
                .AsQueryable()
                .BuildMock();
            var tokenRepo = new Mock<IGenericRepository<RefreshToken>>();
            tokenRepo.Setup(r => r.Query()).Returns(refreshTokens);
            _mockUow.Setup(u => u.Repository<RefreshToken>()).Returns(tokenRepo.Object);

            var auditRepo = new Mock<IGenericRepository<AuditLog>>();
            _mockUow.Setup(u => u.Repository<AuditLog>()).Returns(auditRepo.Object);

            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _mockEmailSender
                .Setup(e =>
                    e.EnqueueRoleChangeEmailAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Guid>(),
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .Returns(ValueTask.CompletedTask);

            var response = new ChangeRoleResponse
            {
                EmployeeId = newEmployeeId,
                FullName = "John Doe",
                EmployeeCode = "EMP002",
            };
            _mockMapper
                .Setup(m => m.Map<ChangeRoleResponse>(It.IsAny<Employee>()))
                .Returns(response);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.EmployeeCode.Should().Be("EMP002");
            oldEmployee.Status.Should().Be(EmployeeStatus.Inactive);
            oldEmployee.Username.Should().BeNull();
            oldEmployee.Phone.Should().BeNull();
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_CannotPromoteToManager()
        {
            // Arrange
            var auditorId = Guid.NewGuid();
            var command = new ChangeRoleCommand
            {
                EmployeeCode = "EMP001",
                CurrentRole = EmployeeRole.Waiter,
                NewRole = EmployeeRole.Manager,
            };

            _mockCurrentUser.Setup(c => c.UserId).Returns(auditorId.ToString());
            _mockMessage
                .Setup(m => m.GetMessage(MessageKeys.Employee.CannotPromoteToManager))
                .Returns("Cannot promote to Manager");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Cannot promote to Manager");
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_SameRole()
        {
            // Arrange
            var auditorId = Guid.NewGuid();
            var command = new ChangeRoleCommand
            {
                EmployeeCode = "EMP001",
                CurrentRole = EmployeeRole.Waiter,
                NewRole = EmployeeRole.Waiter,
            };

            _mockCurrentUser.Setup(c => c.UserId).Returns(auditorId.ToString());
            _mockMessage
                .Setup(m => m.GetMessage(MessageKeys.Employee.NewRoleMustBeDifferent))
                .Returns("New role must be different");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("New role must be different");
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_EmployeeNotFound()
        {
            // Arrange
            var auditorId = Guid.NewGuid();
            var command = new ChangeRoleCommand
            {
                EmployeeCode = "EMP001",
                CurrentRole = EmployeeRole.Waiter,
                NewRole = EmployeeRole.ChefBar,
            };

            _mockCurrentUser.Setup(c => c.UserId).Returns(auditorId.ToString());

            var employees = new List<Employee>().AsQueryable().BuildMock();
            var employeeRepo = new Mock<IGenericRepository<Employee>>();
            employeeRepo.Setup(r => r.Query()).Returns(employees);
            _mockUow.Setup(u => u.Repository<Employee>()).Returns(employeeRepo.Object);

            _mockMessage
                .Setup(m => m.GetMessage(MessageKeys.Employee.NotFound))
                .Returns("Employee not found");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Employee not found");
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_EmployeeNotActive()
        {
            // Arrange
            var auditorId = Guid.NewGuid();
            var oldEmployeeId = Guid.NewGuid();
            var command = new ChangeRoleCommand
            {
                EmployeeCode = "EMP001",
                CurrentRole = EmployeeRole.Waiter,
                NewRole = EmployeeRole.ChefBar,
            };

            _mockCurrentUser.Setup(c => c.UserId).Returns(auditorId.ToString());

            var oldEmployee = new Employee
            {
                EmployeeId = oldEmployeeId,
                EmployeeCode = "EMP001",
                Role = EmployeeRole.Waiter,
                Status = EmployeeStatus.Inactive,
            };

            var employees = new List<Employee> { oldEmployee }
                .AsQueryable()
                .BuildMock();
            var employeeRepo = new Mock<IGenericRepository<Employee>>();
            employeeRepo.Setup(r => r.Query()).Returns(employees);
            _mockUow.Setup(u => u.Repository<Employee>()).Returns(employeeRepo.Object);

            _mockMessage
                .Setup(m => m.GetMessage(MessageKeys.Employee.NotActive))
                .Returns("Employee not active");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Employee not active");
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_UserNotAuthenticated()
        {
            // Arrange
            var command = new ChangeRoleCommand
            {
                EmployeeCode = "EMP001",
                CurrentRole = EmployeeRole.Waiter,
                NewRole = EmployeeRole.ChefBar,
            };

            _mockCurrentUser.Setup(c => c.UserId).Returns("invalid-guid");
            _mockMessage
                .Setup(m => m.GetMessage(MessageKeys.Employee.CannotIdentifyUser))
                .Returns("Cannot identify user");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Cannot identify user");
            result.ErrorType.Should().Be(ResultErrorType.Unauthorized);
        }
    }
}
