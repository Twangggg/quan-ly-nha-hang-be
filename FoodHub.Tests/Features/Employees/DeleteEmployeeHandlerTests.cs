using AutoMapper;
using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Employees.Commands.DeleteEmployee;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MockQueryable.Moq;
using Moq;

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
                _mockCache.Object
            );
        }

        [Fact]
        public async Task Handle_Should_RevokeRefreshTokens_When_EmployeeDeleted()
        {
            var auditorId = Guid.NewGuid();
            var employeeId = Guid.NewGuid();
            var employee = new Employee
            {
                EmployeeId = employeeId,
                EmployeeCode = "EMP001",
                FullName = "John Doe",
                Email = "john@example.com",
                Username = "johndoe",
                Phone = "0123456789",
                Status = EmployeeStatus.Active,
            };

            var employeeRepo = new Mock<IGenericRepository<Employee>>();
            employeeRepo.Setup(r => r.GetByIdAsync(employeeId)).ReturnsAsync(employee);
            _mockUow.Setup(u => u.Repository<Employee>()).Returns(employeeRepo.Object);

            var refreshTokens = new List<RefreshToken>
            {
                new RefreshToken { EmployeeId = employeeId, IsRevoked = false },
                new RefreshToken { EmployeeId = employeeId, IsRevoked = false },
            }.AsQueryable().BuildMock();
            var tokenRepo = new Mock<IGenericRepository<RefreshToken>>();
            tokenRepo.Setup(r => r.Query()).Returns(refreshTokens);
            _mockUow.Setup(u => u.Repository<RefreshToken>()).Returns(tokenRepo.Object);

            var auditRepo = new Mock<IGenericRepository<AuditLog>>();
            _mockUow.Setup(u => u.Repository<AuditLog>()).Returns(auditRepo.Object);

            _mockCurrentUser.Setup(c => c.UserId).Returns(auditorId.ToString());
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockCache
                .Setup(c => c.RemoveByPatternAsync("employee:list", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockCache
                .Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockMapper
                .Setup(m => m.Map<DeleteEmployeeResponse>(employee))
                .Returns(new DeleteEmployeeResponse { EmployeeId = employeeId });

            var result = await _handler.Handle(
                new DeleteEmployeeCommand(employeeId),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            refreshTokens.All(x => x.IsRevoked).Should().BeTrue();
            employee.Status.Should().Be(EmployeeStatus.Inactive);
            employee.DeletedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_EmployeeAlreadyInactive()
        {
            var auditorId = Guid.NewGuid();
            var employeeId = Guid.NewGuid();
            var employee = new Employee
            {
                EmployeeId = employeeId,
                EmployeeCode = "EMP001",
                Email = "john@example.com",
                FullName = "John Doe",
                Status = EmployeeStatus.Inactive,
            };

            var employeeRepo = new Mock<IGenericRepository<Employee>>();
            employeeRepo.Setup(r => r.GetByIdAsync(employeeId)).ReturnsAsync(employee);
            _mockUow.Setup(u => u.Repository<Employee>()).Returns(employeeRepo.Object);
            _mockCurrentUser.Setup(c => c.UserId).Returns(auditorId.ToString());
            _mockMessage
                .Setup(m => m.GetMessage(MessageKeys.Employee.NotActive))
                .Returns("Employee not active");

            var result = await _handler.Handle(
                new DeleteEmployeeCommand(employeeId),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Employee not active");
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
        }
    }
}
