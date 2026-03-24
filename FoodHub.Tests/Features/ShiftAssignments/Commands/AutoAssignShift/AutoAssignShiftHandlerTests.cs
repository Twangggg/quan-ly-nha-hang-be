using AutoMapper;
using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.ShiftAssignments.Commands.AssignShift;
using FoodHub.Application.Features.ShiftAssignments.Commands.AutoAssignShift;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.ShiftAssignments.Commands.AutoAssignShift
{
    public class AutoAssignShiftHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<IMessageService> _mockMessage;
        private readonly Mock<ICacheService> _mockCache;
        private readonly Mock<IBackgroundEmailSender> _mockEmail;
        private readonly Mock<ISignalRService> _mockSignalR;
        private readonly Mock<ILogger<AutoAssignShiftHandler>> _mockLogger;

        public AutoAssignShiftHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockMessage = new Mock<IMessageService>();
            _mockCache = new Mock<ICacheService>();
            _mockEmail = new Mock<IBackgroundEmailSender>();
            _mockSignalR = new Mock<ISignalRService>();
            _mockLogger = new Mock<ILogger<AutoAssignShiftHandler>>();

            _mockCurrentUser.Setup(u => u.UserId).Returns(Guid.NewGuid().ToString());
        }

        [Fact]
        public async Task Handle_ShouldAssignShifts_WhenRangeIsValid()
        {
            // Arrange
            var employeeId = Guid.NewGuid();
            var shiftId = Guid.NewGuid();
            var fromDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
            var toDate = fromDate.AddDays(2); // 3 days total

            var command = new AutoAssignShiftCommand
            {
                EmployeeId = employeeId,
                ShiftId = shiftId,
                FromDate = fromDate,
                ToDate = toDate,
                Note = "Auto assignment"
            };

            var employee = new Employee { EmployeeId = employeeId, FullName = "Test Employee", Status = EmployeeStatus.Active, Email = "test@example.com" };
            var shift = Shift.Create("Morning", new TimeSpan(7, 0, 0), new TimeSpan(12, 0, 0), Guid.NewGuid());
            shift.ShiftId = shiftId;
            shift.Status = ShiftStatus.Active;

            var empRepo = new Mock<IGenericRepository<Employee>>();
            empRepo.Setup(r => r.Query()).Returns(new List<Employee> { employee }.AsQueryable().BuildMock());

            var shiftRepo = new Mock<IGenericRepository<Shift>>();
            shiftRepo.Setup(r => r.Query()).Returns(new List<Shift> { shift }.AsQueryable().BuildMock());

            var assignRepo = new Mock<IGenericRepository<ShiftAssignment>>();
            assignRepo.Setup(r => r.Query()).Returns(new List<ShiftAssignment>().AsQueryable().BuildMock());

            _mockUow.Setup(u => u.Repository<Employee>()).Returns(empRepo.Object);
            _mockUow.Setup(u => u.Repository<Shift>()).Returns(shiftRepo.Object);
            _mockUow.Setup(u => u.Repository<ShiftAssignment>()).Returns(assignRepo.Object);

            _mockMapper.Setup(m => m.Map<List<AssignShiftResponse>>(It.IsAny<List<ShiftAssignment>>()))
                .Returns((List<ShiftAssignment> source) => source.Select(s => new AssignShiftResponse { AssignedDate = s.AssignedDate }).ToList());

            var handler = new AutoAssignShiftHandler(
                _mockUow.Object, _mockMapper.Object, _mockCurrentUser.Object, _mockMessage.Object,
                _mockCache.Object, _mockEmail.Object, _mockSignalR.Object, _mockLogger.Object);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(3);

            _mockUow.Verify(u => u.BeginTransactionAsync(), Times.Once);
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockUow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldSkip_WhenDatesAreAlreadyAssigned()
        {
            // Arrange
            var employeeId = Guid.NewGuid();
            var shiftId = Guid.NewGuid();
            var fromDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
            var toDate = fromDate.AddDays(1); // 2 days total

            var command = new AutoAssignShiftCommand
            {
                EmployeeId = employeeId,
                ShiftId = shiftId,
                FromDate = fromDate,
                ToDate = toDate
            };

            var employee = new Employee { EmployeeId = employeeId, FullName = "Test Employee", Status = EmployeeStatus.Active, Email = "test@example.com" };
            var shift = Shift.Create("Morning", new TimeSpan(7, 0, 0), new TimeSpan(12, 0, 0), Guid.NewGuid());
            shift.ShiftId = shiftId;
            shift.Status = ShiftStatus.Active;

            // Existing assignment on fromDate
            var existingAssignment = new ShiftAssignment { EmployeeId = employeeId, AssignedDate = fromDate };

            var empRepo = new Mock<IGenericRepository<Employee>>();
            empRepo.Setup(r => r.Query()).Returns(new List<Employee> { employee }.AsQueryable().BuildMock());

            var shiftRepo = new Mock<IGenericRepository<Shift>>();
            shiftRepo.Setup(r => r.Query()).Returns(new List<Shift> { shift }.AsQueryable().BuildMock());

            var assignRepo = new Mock<IGenericRepository<ShiftAssignment>>();
            assignRepo.Setup(r => r.Query()).Returns(new List<ShiftAssignment> { existingAssignment }.AsQueryable().BuildMock());

            _mockUow.Setup(u => u.Repository<Employee>()).Returns(empRepo.Object);
            _mockUow.Setup(u => u.Repository<Shift>()).Returns(shiftRepo.Object);
            _mockUow.Setup(u => u.Repository<ShiftAssignment>()).Returns(assignRepo.Object);

            _mockMapper.Setup(m => m.Map<List<AssignShiftResponse>>(It.IsAny<List<ShiftAssignment>>()))
                .Returns((List<ShiftAssignment> source) => source.Select(s => new AssignShiftResponse { AssignedDate = s.AssignedDate }).ToList());

            var handler = new AutoAssignShiftHandler(
                _mockUow.Object, _mockMapper.Object, _mockCurrentUser.Object, _mockMessage.Object,
                _mockCache.Object, _mockEmail.Object, _mockSignalR.Object, _mockLogger.Object);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(1); // Only for day 2
            result.Data.First().AssignedDate.Should().Be(toDate);
        }
        [Fact]
        public async Task Handle_ShouldReturnNotFound_WhenEmployeeDoesNotExist()
        {
            // Arrange
            var command = new AutoAssignShiftCommand { EmployeeId = Guid.NewGuid(), ShiftId = Guid.NewGuid(), FromDate = DateOnly.FromDateTime(DateTime.UtcNow), ToDate = DateOnly.FromDateTime(DateTime.UtcNow) };
            
            var empRepo = new Mock<IGenericRepository<Employee>>();
            empRepo.Setup(r => r.Query()).Returns(new List<Employee>().AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Employee>()).Returns(empRepo.Object);

            var handler = new AutoAssignShiftHandler(
                _mockUow.Object, _mockMapper.Object, _mockCurrentUser.Object, _mockMessage.Object,
                _mockCache.Object, _mockEmail.Object, _mockSignalR.Object, _mockLogger.Object);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenEmployeeIsInactive()
        {
            // Arrange
            var employeeId = Guid.NewGuid();
            var command = new AutoAssignShiftCommand { EmployeeId = employeeId, ShiftId = Guid.NewGuid(), FromDate = DateOnly.FromDateTime(DateTime.UtcNow), ToDate = DateOnly.FromDateTime(DateTime.UtcNow) };
            
            var employee = new Employee { EmployeeId = employeeId, Status = EmployeeStatus.Inactive };
            var empRepo = new Mock<IGenericRepository<Employee>>();
            empRepo.Setup(r => r.Query()).Returns(new List<Employee> { employee }.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<Employee>()).Returns(empRepo.Object);

            var handler = new AutoAssignShiftHandler(
                _mockUow.Object, _mockMapper.Object, _mockCurrentUser.Object, _mockMessage.Object,
                _mockCache.Object, _mockEmail.Object, _mockSignalR.Object, _mockLogger.Object);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_ShouldReturnNotFound_WhenShiftDoesNotExist()
        {
            // Arrange
            var employeeId = Guid.NewGuid();
            var command = new AutoAssignShiftCommand { EmployeeId = employeeId, ShiftId = Guid.NewGuid(), FromDate = DateOnly.FromDateTime(DateTime.UtcNow), ToDate = DateOnly.FromDateTime(DateTime.UtcNow) };
            
            var employee = new Employee { EmployeeId = employeeId, Status = EmployeeStatus.Active };
            var empRepo = new Mock<IGenericRepository<Employee>>();
            empRepo.Setup(r => r.Query()).Returns(new List<Employee> { employee }.AsQueryable().BuildMock());
            
            var shiftRepo = new Mock<IGenericRepository<Shift>>();
            shiftRepo.Setup(r => r.Query()).Returns(new List<Shift>().AsQueryable().BuildMock());

            _mockUow.Setup(u => u.Repository<Employee>()).Returns(empRepo.Object);
            _mockUow.Setup(u => u.Repository<Shift>()).Returns(shiftRepo.Object);

            var handler = new AutoAssignShiftHandler(
                _mockUow.Object, _mockMapper.Object, _mockCurrentUser.Object, _mockMessage.Object,
                _mockCache.Object, _mockEmail.Object, _mockSignalR.Object, _mockLogger.Object);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }
    }
}
