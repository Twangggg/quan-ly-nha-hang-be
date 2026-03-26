using AutoMapper;
using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.ShiftAssignments.Commands.AssignShift;
using FoodHub.Application.Interfaces;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
 
namespace FoodHub.Tests.Features.ShiftAssignments.Commands.AssignShift
{
    public class AssignShiftHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ICacheService> _mockCache;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<IMessageService> _mockMessage;
        private readonly Mock<ISignalRService> _mockSignalR;
        private readonly Mock<IBackgroundEmailSender> _mockEmail;
        private readonly Mock<ILogger<AssignShiftHandler>> _mockLogger;
 
        public AssignShiftHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockCache = new Mock<ICacheService>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockMessage = new Mock<IMessageService>();
            _mockSignalR = new Mock<ISignalRService>();
            _mockEmail = new Mock<IBackgroundEmailSender>();
            _mockLogger = new Mock<ILogger<AssignShiftHandler>>();
 
            _mockCurrentUser.Setup(u => u.UserId).Returns(Guid.NewGuid().ToString());
        }
 
        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenAssignmentIsValid()
        {
            // Arrange
            var employeeId = Guid.NewGuid();
            var shiftId = Guid.NewGuid();
            var command = new AssignShiftCommand 
            { 
                EmployeeId = employeeId, 
                ShiftId = shiftId, 
                AssignedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) 
            };
 
            var employee = new Employee { EmployeeId = employeeId, FullName = "Test Employee", Status = EmployeeStatus.Active, Email = "test@example.com" };
            var shift = Shift.Create("Morning", new TimeSpan(7,0,0), new TimeSpan(12,0,0), Guid.NewGuid());
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
            _mockUow.Setup(u => u.Repository<AuditLog>()).Returns(new Mock<IGenericRepository<AuditLog>>().Object);
 
            var handler = new AssignShiftHandler(
                _mockUow.Object, _mockMapper.Object, _mockCurrentUser.Object,
                _mockMessage.Object, _mockCache.Object,
                _mockEmail.Object, _mockSignalR.Object, _mockLogger.Object);
 
            // Act
            var result = await handler.Handle(command, CancellationToken.None);
 
            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockUow.Verify(u => u.BeginTransactionAsync(), Times.Once);
            _mockUow.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }
    }
}
