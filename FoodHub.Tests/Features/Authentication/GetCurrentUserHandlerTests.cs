using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Authentication.Queries.GetCurrentUser;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.Authentication
{
    public class GetCurrentUserHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly GetCurrentUserHandler _handler;

        public GetCurrentUserHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockMessageService = new Mock<IMessageService>();

            _handler = new GetCurrentUserHandler(
                _mockUow.Object,
                _mockCurrentUserService.Object,
                _mockMessageService.Object);
        }


        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_UserExists()
        {
            // Arrange
            var employeeId = Guid.NewGuid();
            var employee = new Employee
            {
                EmployeeId = employeeId,
                EmployeeCode = "EMP001",
                Email = "test@example.com",
                Role = EmployeeRole.Manager,
                Status = EmployeeStatus.Active
            };

            var employees = new List<Employee> { employee }.AsQueryable().BuildMock();
            var repo = new Mock<IGenericRepository<Employee>>();
            repo.Setup(r => r.Query()).Returns(employees);
            _mockUow.Setup(u => u.Repository<Employee>()).Returns(repo.Object);

            _mockCurrentUserService.Setup(c => c.EmployeeCode).Returns("EMP001");

            // Act
            var result = await _handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.EmployeeCode.Should().Be("EMP001");
            result.Data.Email.Should().Be("test@example.com");
            result.Data.Role.Should().Be("Manager");
        }


        [Fact]
        public async Task Handle_Should_ReturnFailure_When_UserNotFound()
        {
            // Arrange
            var employees = new List<Employee>().AsQueryable().BuildMock();
            var repo = new Mock<IGenericRepository<Employee>>();
            repo.Setup(r => r.Query()).Returns(employees);
            _mockUow.Setup(u => u.Repository<Employee>()).Returns(repo.Object);

            _mockCurrentUserService.Setup(c => c.EmployeeCode).Returns("EMP001");

            _mockMessageService.Setup(m => m.GetMessage(MessageKeys.Employee.NotFound)).Returns("Employee not found");

            // Act
            var result = await _handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Employee not found");
        }

    }
}
