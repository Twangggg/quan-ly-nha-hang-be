using Moq;
using AutoMapper;
using FluentAssertions;
using FoodHub.Application.Features.Employees.Commands.DeleteEmployee;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Constants;

namespace FoodHub.Tests.Features.Employees
{
    public class DeleteEmployeeHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<IMessageService> _mockMessage;
        private readonly Mock<ICacheService> _mockCache;
        private readonly DeleteEmployeeHandler _handler;

        public DeleteEmployeeHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockMessage = new Mock<IMessageService>();
            _mockCache = new Mock<ICacheService>();

            _handler = new DeleteEmployeeHandler(
                _mockUow.Object,
                _mockMapper.Object,
                _mockCurrentUser.Object,
                _mockMessage.Object,
                _mockCache.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_EmployeeDeleted()
        {
            // Arrange
            var auditorId = Guid.NewGuid();
            var employeeId = Guid.NewGuid();
            var command = new DeleteEmployeeCommand(employeeId);

            _mockCurrentUser.Setup(c => c.UserId).Returns(auditorId.ToString());

            var employee = new Employee
            {
                EmployeeId = employeeId,
                Status = EmployeeStatus.Active
            };

            var repo = new Mock<IGenericRepository<Employee>>();
            repo.Setup(r => r.GetByIdAsync(employeeId)).ReturnsAsync(employee);
            _mockUow.Setup(u => u.Repository<Employee>()).Returns(repo.Object);

            var auditRepo = new Mock<IGenericRepository<AuditLog>>();
            _mockUow.Setup(u => u.Repository<AuditLog>()).Returns(auditRepo.Object);

            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _mockCache.Setup(c => c.RemoveByPatternAsync("employee:list", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var response = new DeleteEmployeeResponse
            {
                EmployeeId = employeeId,
                Role = "Manager",
                Status = EmployeeStatus.Inactive
            };
            _mockMapper.Setup(m => m.Map<DeleteEmployeeResponse>(employee)).Returns(response);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Status.Should().Be(EmployeeStatus.Inactive);
            employee.Status.Should().Be(EmployeeStatus.Inactive);
            employee.UpdatedBy.Should().Be(auditorId);
            employee.DeletedAt.Should().NotBeNull();
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockCache.Verify(c => c.RemoveByPatternAsync("employee:list", It.IsAny<CancellationToken>()), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_EmployeeNotFound()
        {
            // Arrange
            var auditorId = Guid.NewGuid();
            var employeeId = Guid.NewGuid();
            var command = new DeleteEmployeeCommand(employeeId);

            _mockCurrentUser.Setup(c => c.UserId).Returns(auditorId.ToString());

            var repo = new Mock<IGenericRepository<Employee>>();
            repo.Setup(r => r.GetByIdAsync(employeeId)).ReturnsAsync((Employee)null);
            _mockUow.Setup(u => u.Repository<Employee>()).Returns(repo.Object);

            _mockMessage.Setup(m => m.GetMessage(MessageKeys.Employee.NotFound)).Returns("Employee not found");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Employee not found");
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_UserNotAuthenticated()
        {
            // Arrange
            var employeeId = Guid.NewGuid();
            var command = new DeleteEmployeeCommand(employeeId);

            _mockCurrentUser.Setup(c => c.UserId).Returns("invalid-guid");
            _mockMessage.Setup(m => m.GetMessage(MessageKeys.Employee.CannotIdentifyUser)).Returns("Cannot identify user");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Cannot identify user");
            result.ErrorType.Should().Be(ResultErrorType.Unauthorized);
        }
    }
}
