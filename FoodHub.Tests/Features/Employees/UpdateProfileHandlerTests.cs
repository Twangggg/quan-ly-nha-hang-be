using AutoMapper;
using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Employees.Commands.UpdateMyProfile;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.Employees
{
    public class UpdateProfileHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IMessageService> _mockMessage;
        private readonly Mock<ICacheService> _mockCache;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<ILogger<UpdateProfileHandler>> _mockLogger;
        private readonly UpdateProfileHandler _handler;

        public UpdateProfileHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockMessage = new Mock<IMessageService>();
            _mockCache = new Mock<ICacheService>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockLogger = new Mock<ILogger<UpdateProfileHandler>>();

            _handler = new UpdateProfileHandler(
                _mockUow.Object,
                _mockMapper.Object,
                _mockMessage.Object,
                _mockCache.Object,
                _mockCurrentUser.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Handle_Should_KeepExistingPhone_When_RequestPhoneIsNull()
        {
            var employeeId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.UserId).Returns(employeeId.ToString());

            var command = new UpdateProfileCommand(
                employeeId,
                "Updated Name",
                "updated@example.com",
                null,
                "Updated Address",
                new DateOnly(1990, 1, 1)
            );

            var employee = new Employee
            {
                EmployeeId = employeeId,
                FullName = "Old Name",
                Email = "old@example.com",
                Phone = "0987654321",
                Address = "Old Address",
                DateOfBirth = new DateOnly(1989, 1, 1),
                Status = EmployeeStatus.Active
            };

            var employees = new List<Employee> { employee }.AsQueryable().BuildMock();
            var repo = new Mock<IGenericRepository<Employee>>();
            repo.Setup(r => r.Query()).Returns(employees);
            _mockUow.Setup(u => u.Repository<Employee>()).Returns(repo.Object);
            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockCache
                .Setup(c => c.RemoveByPatternAsync("employee:list", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var response = new UpdateProfileResponse
            {
                FullName = "Updated Name",
                Email = "updated@example.com",
                Phone = "0987654321",
                Address = "Updated Address",
                DateOfBirth = new DateOnly(1990, 1, 1)
            };
            _mockMapper.Setup(m => m.Map<UpdateProfileResponse>(employee)).Returns(response);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            employee.Phone.Should().Be("0987654321");
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_ProvidedPhoneAlreadyExists()
        {
            var employeeId = Guid.NewGuid();
            _mockCurrentUser.Setup(c => c.UserId).Returns(employeeId.ToString());

            var command = new UpdateProfileCommand(
                employeeId,
                "Updated Name",
                "updated@example.com",
                "0987654321",
                "Updated Address",
                new DateOnly(1990, 1, 1)
            );

            var employee = new Employee
            {
                EmployeeId = employeeId,
                FullName = "Old Name",
                Email = "old@example.com",
                Phone = "0123456789",
                Address = "Old Address",
                DateOfBirth = new DateOnly(1989, 1, 1),
                Status = EmployeeStatus.Active
            };

            var otherEmployee = new Employee
            {
                EmployeeId = Guid.NewGuid(),
                Phone = "0987654321",
                Email = "other@example.com",
                Status = EmployeeStatus.Active
            };

            var employees = new List<Employee> { employee, otherEmployee }.AsQueryable().BuildMock();
            var repo = new Mock<IGenericRepository<Employee>>();
            repo.Setup(r => r.Query()).Returns(employees);
            _mockUow.Setup(u => u.Repository<Employee>()).Returns(repo.Object);

            _mockMessage
                .Setup(m => m.GetMessage(MessageKeys.Profile.PhoneExists))
                .Returns("Phone already exists");

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Phone already exists");
        }
    }
}
