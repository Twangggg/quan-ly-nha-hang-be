using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Attendances.Queries.GetAttendanceReport;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using MockQueryable.Moq;
using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using FoodHub.Application.Extensions.Mappings;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FoodHub.Tests.Features.Attendances.Queries.GetAttendanceReport
{
    public class GetAttendanceReportHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ICacheService> _cacheServiceMock;
        private readonly Mock<ILogger<GetAttendanceReportHandler>> _loggerMock;
        private readonly IMapper _mapper;

        public GetAttendanceReportHandlerTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _cacheServiceMock = new Mock<ICacheService>();
            _loggerMock = new Mock<ILogger<GetAttendanceReportHandler>>();

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
            
            var configurationProvider = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, mockLoggerFactory.Object);
            _mapper = configurationProvider.CreateMapper();
        }

        [Fact]
        public async Task Handle_ShouldReturnPagedAttendances_WhenNotInCache()
        {
            // Arrange
            var employeeId = Guid.NewGuid();
            var attendances = new List<Attendance>
            {
                new Attendance 
                { 
                    AttendanceId = Guid.NewGuid(), 
                    EmployeeId = employeeId, 
                    CheckInTime = DateTime.UtcNow.AddHours(-1),
                    CheckOutTime = DateTime.UtcNow,
                    Employee = new Employee { FullName = "John Doe", EmployeeCode = "EMP001" },
                    isLate = true,
                    isEarlyLeave = false
                }
            }.AsQueryable();

            var mockRepo = new Mock<IGenericRepository<Attendance>>();
            mockRepo.Setup(r => r.Query()).Returns(attendances.BuildMock());
            _unitOfWorkMock.Setup(u => u.Repository<Attendance>()).Returns(mockRepo.Object);

            var pagination = new PaginationParams { PageNumber = 1, PageSize = 10 };
            var query = new GetAttendanceReportQuery(pagination);
            var handler = new GetAttendanceReportHandler(
                _unitOfWorkMock.Object, 
                _mapper, 
                _cacheServiceMock.Object, 
                _loggerMock.Object);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCount(1);
            result.Data.Items.First().EmployeeName.Should().Be("John Doe");
            result.Data.Items.First().Status.Should().Be("Đi trễ");
            _cacheServiceMock.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<PagedResult<GetAttendanceReportResponse>>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnFromCache_WhenCacheExists()
        {
            // Arrange
            var pagination = new PaginationParams { PageNumber = 1, PageSize = 10 };
            var cachedData = new PagedResult<GetAttendanceReportResponse>(
                new List<GetAttendanceReportResponse> { new GetAttendanceReportResponse { EmployeeName = "Cached User" } },
                pagination, 1);

            _cacheServiceMock.Setup(x => x.GetAsync<PagedResult<GetAttendanceReportResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedData);
            var query = new GetAttendanceReportQuery(pagination);
            var handler = new GetAttendanceReportHandler(
                _unitOfWorkMock.Object, 
                _mapper, 
                _cacheServiceMock.Object, 
                _loggerMock.Object);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.First().EmployeeName.Should().Be("Cached User");
            _unitOfWorkMock.Verify(u => u.Repository<Attendance>(), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnEmpty_WhenNoDataFound()
        {
            // Arrange
            var attendances = new List<Attendance>().AsQueryable();
            var mockRepo = new Mock<IGenericRepository<Attendance>>();
            mockRepo.Setup(r => r.Query()).Returns(attendances.BuildMock());
            _unitOfWorkMock.Setup(u => u.Repository<Attendance>()).Returns(mockRepo.Object);

            var pagination = new PaginationParams { PageNumber = 1, PageSize = 10 };
            var query = new GetAttendanceReportQuery(pagination);
            var handler = new GetAttendanceReportHandler(
                _unitOfWorkMock.Object, 
                _mapper, 
                _cacheServiceMock.Object, 
                _loggerMock.Object);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().BeEmpty();
        }
    }
}
