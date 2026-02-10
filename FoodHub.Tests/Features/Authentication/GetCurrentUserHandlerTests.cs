using Moq;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using FoodHub.Application.Resources;
using FluentAssertions;
using FoodHub.Application.Features.Authentication.Queries.GetCurrentUser;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;

namespace FoodHub.Tests.Features.Authentication
{
    public class GetCurrentUserHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly GetCurrentUserHandler _handler;

        public GetCurrentUserHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

            _handler = new GetCurrentUserHandler(
                _mockUow.Object,
                _mockHttpContextAccessor.Object);
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

            var claims = new List<Claim> { new Claim("EmployeeCode", "EMP001") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = claimsPrincipal };
            _mockHttpContextAccessor.Setup(h => h.HttpContext).Returns(httpContext);

            // Act
            var result = await _handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.EmployeeCode.Should().Be("EMP001");
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

            var claims = new List<Claim> { new Claim("EmployeeCode", "EMP001") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = claimsPrincipal };
            _mockHttpContextAccessor.Setup(h => h.HttpContext).Returns(httpContext);

            // Act
            var result = await _handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(Messages.EmployeeNotFound);
        }
    }
}
