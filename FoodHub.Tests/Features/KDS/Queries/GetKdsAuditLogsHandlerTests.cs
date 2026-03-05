using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.KDS.Queries.GetKdsAuditLogs;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.KDS.Queries
{
    public class GetKdsAuditLogsHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ILogger<GetKdsAuditLogsHandler>> _mockLogger;
        private readonly GetKdsAuditLogsHandler _handler;

        public GetKdsAuditLogsHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<GetKdsAuditLogsHandler>>();

            _handler = new GetKdsAuditLogsHandler(_mockUow.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_ValidRequest_ReturnsPagedResults()
        {
            // Arrange
            var query = new GetKdsAuditLogsQuery
            {
                PageNumber = 1,
                PageSize = 10,
                Station = null,
                Action = null,
            };

            var employee = new Employee
            {
                EmployeeId = Guid.NewGuid(),
                FullName = "Test Chef",
                Role = EmployeeRole.ChefBar,
            };

            var order = new FoodHub.Domain.Entities.Order
            {
                OrderId = Guid.NewGuid(),
                OrderCode = "ORD001",
                OrderItems = new List<OrderItem>(),
            };

            var auditLogs = new List<OrderAuditLog>
            {
                new OrderAuditLog
                {
                    LogId = Guid.NewGuid(),
                    OrderId = order.OrderId,
                    Order = order,
                    EmployeeId = employee.EmployeeId,
                    Employee = employee,
                    Action = "KDS_START_COOKING",
                    ChangeReason = "Started cooking",
                    NewValue = "Cooking",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                },
            };

            var mockRepo = new Mock<IGenericRepository<OrderAuditLog>>();
            var mockQuery = auditLogs.AsQueryable().BuildMockDbSet();

            mockRepo.Setup(x => x.Query()).Returns(mockQuery.Object);

            _mockUow.Setup(x => x.Repository<OrderAuditLog>()).Returns(mockRepo.Object);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Items.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Handle_WithStationFilter_ReturnsFilteredResults()
        {
            // Arrange
            var query = new GetKdsAuditLogsQuery
            {
                PageNumber = 1,
                PageSize = 10,
                Station = "Bar",
            };

            var employee = new Employee
            {
                EmployeeId = Guid.NewGuid(),
                FullName = "Test Chef",
                Role = EmployeeRole.ChefBar,
            };

            var order = new FoodHub.Domain.Entities.Order
            {
                OrderId = Guid.NewGuid(),
                OrderCode = "ORD001",
                OrderItems = new List<OrderItem>(),
            };

            var auditLogs = new List<OrderAuditLog>
            {
                new OrderAuditLog
                {
                    LogId = Guid.NewGuid(),
                    OrderId = order.OrderId,
                    Order = order,
                    EmployeeId = employee.EmployeeId,
                    Employee = employee,
                    Action = "KDS_START_COOKING",
                    ChangeReason = "Started cooking",
                    NewValue = "Cooking",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                },
            };

            var mockRepo = new Mock<IGenericRepository<OrderAuditLog>>();
            var mockQuery = auditLogs.AsQueryable().BuildMockDbSet();

            mockRepo.Setup(x => x.Query()).Returns(mockQuery.Object);

            _mockUow.Setup(x => x.Repository<OrderAuditLog>()).Returns(mockRepo.Object);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_WithDateRange_ReturnsFilteredResults()
        {
            // Arrange
            var fromDate = DateTime.UtcNow.AddDays(-7);
            var toDate = DateTime.UtcNow;

            var query = new GetKdsAuditLogsQuery
            {
                PageNumber = 1,
                PageSize = 10,
                FromDate = fromDate,
                ToDate = toDate,
            };

            var employee = new Employee
            {
                EmployeeId = Guid.NewGuid(),
                FullName = "Test Chef",
                Role = EmployeeRole.ChefBar,
            };

            var order = new FoodHub.Domain.Entities.Order
            {
                OrderId = Guid.NewGuid(),
                OrderCode = "ORD001",
                OrderItems = new List<OrderItem>(),
            };

            var auditLogs = new List<OrderAuditLog>
            {
                new OrderAuditLog
                {
                    LogId = Guid.NewGuid(),
                    OrderId = order.OrderId,
                    Order = order,
                    EmployeeId = employee.EmployeeId,
                    Employee = employee,
                    Action = "KDS_MARK_READY",
                    ChangeReason = null,
                    NewValue = "Ready",
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                },
            };

            var mockRepo = new Mock<IGenericRepository<OrderAuditLog>>();
            var mockQuery = auditLogs.AsQueryable().BuildMockDbSet();

            mockRepo.Setup(x => x.Query()).Returns(mockQuery.Object);

            _mockUow.Setup(x => x.Repository<OrderAuditLog>()).Returns(mockRepo.Object);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_EmptyResults_ReturnsEmptyPagedResult()
        {
            // Arrange
            var query = new GetKdsAuditLogsQuery { PageNumber = 1, PageSize = 10 };

            var auditLogs = new List<OrderAuditLog>();

            var mockRepo = new Mock<IGenericRepository<OrderAuditLog>>();
            var mockQuery = auditLogs.AsQueryable().BuildMockDbSet();

            mockRepo.Setup(x => x.Query()).Returns(mockQuery.Object);

            _mockUow.Setup(x => x.Repository<OrderAuditLog>()).Returns(mockRepo.Object);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.TotalCount.Should().Be(0);
            result.Data.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_Pagination_ReturnsCorrectPage()
        {
            // Arrange
            var query = new GetKdsAuditLogsQuery { PageNumber = 2, PageSize = 2 };

            var employee = new Employee
            {
                EmployeeId = Guid.NewGuid(),
                FullName = "Test Chef",
                Role = EmployeeRole.ChefBar,
            };

            var order = new FoodHub.Domain.Entities.Order
            {
                OrderId = Guid.NewGuid(),
                OrderCode = "ORD001",
                OrderItems = new List<OrderItem>(),
            };

            var auditLogs = new List<OrderAuditLog>
            {
                new OrderAuditLog
                {
                    LogId = Guid.NewGuid(),
                    OrderId = order.OrderId,
                    Order = order,
                    EmployeeId = employee.EmployeeId,
                    Employee = employee,
                    Action = "KDS_START_COOKING",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-30),
                },
                new OrderAuditLog
                {
                    LogId = Guid.NewGuid(),
                    OrderId = order.OrderId,
                    Order = order,
                    EmployeeId = employee.EmployeeId,
                    Employee = employee,
                    Action = "KDS_MARK_READY",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-20),
                },
                new OrderAuditLog
                {
                    LogId = Guid.NewGuid(),
                    OrderId = order.OrderId,
                    Order = order,
                    EmployeeId = employee.EmployeeId,
                    Employee = employee,
                    Action = "KDS_REJECT",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                },
            };

            var mockRepo = new Mock<IGenericRepository<OrderAuditLog>>();
            var mockQuery = auditLogs.AsQueryable().BuildMockDbSet();

            mockRepo.Setup(x => x.Query()).Returns(mockQuery.Object);

            _mockUow.Setup(x => x.Repository<OrderAuditLog>()).Returns(mockRepo.Object);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.TotalCount.Should().Be(3);
            result.Data.Items.Should().HaveCount(1);
        }
    }
}
