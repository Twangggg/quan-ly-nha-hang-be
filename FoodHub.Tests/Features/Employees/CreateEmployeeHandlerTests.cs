using Moq;
using AutoMapper;
using FluentAssertions;
using FoodHub.Application.Features.Employees.Commands.CreateEmployee;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Constants;
using MockQueryable.Moq;

namespace FoodHub.Tests.Features.Employees
{
    public class CreateEmployeeHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<IPasswordService> _mockPasswordService;
        private readonly Mock<IBackgroundEmailSender> _mockEmailSender;
        private readonly Mock<IEmployeeServices> _mockEmployeeServices;
        private readonly Mock<IMessageService> _mockMessage;
        private readonly Mock<ICacheService> _mockCache;
        private readonly CreateEmployeeHandler _handler;

        public CreateEmployeeHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockPasswordService = new Mock<IPasswordService>();
            _mockEmailSender = new Mock<IBackgroundEmailSender>();
            _mockEmployeeServices = new Mock<IEmployeeServices>();
            _mockMessage = new Mock<IMessageService>();
            _mockCache = new Mock<ICacheService>();

            _handler = new CreateEmployeeHandler(
                _mockUow.Object,
                _mockMapper.Object,
                _mockPasswordService.Object,
                _mockCurrentUser.Object,
                _mockEmailSender.Object,
                _mockEmployeeServices.Object,
                _mockMessage.Object,
                _mockCache.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_EmployeeCreated()
        {
            // Arrange
            var auditorId = Guid.NewGuid();
            var command = new CreateEmployeeCommand
            {
                FullName = "John Doe",
                Email = "john@example.com",
                Role = EmployeeRole.Manager
            };

            _mockCurrentUser.Setup(c => c.UserId).Returns(auditorId.ToString());

            var employees = new List<Employee>().AsQueryable().BuildMock();
            var repo = new Mock<IGenericRepository<Employee>>();
            repo.Setup(r => r.Query()).Returns(employees);
            _mockUow.Setup(u => u.Repository<Employee>()).Returns(repo.Object);

            _mockEmployeeServices.Setup(s => s.GenerateEmployeeCodeAsync(EmployeeRole.Manager)).ReturnsAsync("EMP001");
            _mockPasswordService.Setup(p => p.GenerateRandomPassword()).Returns("randompass123");
            _mockPasswordService.Setup(p => p.HashPassword("randompass123")).Returns("hashedpassword");

            var mappedEmployee = new Employee
            {
                EmployeeId = Guid.NewGuid(),
                FullName = "John Doe",
                Email = "john@example.com",
                Role = EmployeeRole.Manager,
                EmployeeCode = "EMP001"
            };
            _mockMapper.Setup(m => m.Map<Employee>(command)).Returns(mappedEmployee);

            var auditRepo = new Mock<IGenericRepository<AuditLog>>();
            _mockUow.Setup(u => u.Repository<AuditLog>()).Returns(auditRepo.Object);
            _mockUow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);

            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockUow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);


            _mockEmailSender.Setup(e => e.EnqueueAccountCreationEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);


            _mockCache.Setup(c => c.RemoveByPatternAsync("employee:list", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var response = new CreateEmployeeResponse
            {
                EmployeeId = mappedEmployee.EmployeeId,
                FullName = "John Doe",
                EmployeeCode = "EMP001"
            };
            _mockMapper.Setup(m => m.Map<CreateEmployeeResponse>(It.IsAny<Employee>())).Returns(response);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.EmployeeCode.Should().Be("EMP001");
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockUow.Verify(u => u.CommitTransactionAsync(), Times.Once);
            _mockCache.Verify(c => c.RemoveByPatternAsync("employee:list", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_EmailAlreadyExists()
        {
            // Arrange
            var auditorId = Guid.NewGuid();
            var command = new CreateEmployeeCommand
            {
                FullName = "John Doe",
                Email = "john@example.com",
                Role = EmployeeRole.Manager
            };

            _mockCurrentUser.Setup(c => c.UserId).Returns(auditorId.ToString());

            var existingEmployee = new Employee
            {
                EmployeeId = Guid.NewGuid(),
                Email = "john@example.com",
                EmployeeCode = "EMP001"
            };
            var employees = new List<Employee> { existingEmployee }.AsQueryable().BuildMock();
            var repo = new Mock<IGenericRepository<Employee>>();
            repo.Setup(r => r.Query()).Returns(employees);
            _mockUow.Setup(u => u.Repository<Employee>()).Returns(repo.Object);

            _mockMessage.Setup(m => m.GetMessage(It.IsAny<string>())).Returns("Email already exists");


            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Email already exists");
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_UserNotAuthenticated()
        {
            // Arrange
            var command = new CreateEmployeeCommand
            {
                FullName = "John Doe",
                Email = "john@example.com",
                Role = EmployeeRole.Manager
            };

            _mockCurrentUser.Setup(c => c.UserId).Returns("invalid-guid");
            _mockMessage.Setup(m => m.GetMessage(It.IsAny<string>())).Returns("Cannot identify user");


            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Unauthorized);
        }
    }
}
