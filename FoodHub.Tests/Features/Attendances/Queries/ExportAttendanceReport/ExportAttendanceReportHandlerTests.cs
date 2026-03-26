using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Attendances.Queries.ExportAttendanceReport;
using FoodHub.Application.Features.Attendances.Queries.GetAttendanceReport;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Reporting;
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

namespace FoodHub.Tests.Features.Attendances.Queries.ExportAttendanceReport
{
    public class ExportAttendanceReportHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAttendanceExcelService> _excelServiceMock;
        private readonly Mock<ILogger<ExportAttendanceReportHandler>> _loggerMock;
        private readonly IMapper _mapper;

        public ExportAttendanceReportHandlerTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _excelServiceMock = new Mock<IAttendanceExcelService>();
            _loggerMock = new Mock<ILogger<ExportAttendanceReportHandler>>();

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);

            var configurationProvider = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, mockLoggerFactory.Object);
            _mapper = configurationProvider.CreateMapper();
        }

        [Fact]
        public async Task Handle_ShouldReturnExcelBytes()
        {
            // Arrange
            var attendances = new List<Attendance>
            {
                new Attendance 
                { 
                    AttendanceId = Guid.NewGuid(), 
                    EmployeeId = Guid.NewGuid(), 
                    CheckInTime = DateTime.UtcNow,
                    CheckOutTime = null,
                    Employee = new Employee { FullName = "Jane Doe" },
                    isLate = false,
                    isEarlyLeave = false
                }
            }.AsQueryable();

            var mockRepo = new Mock<IGenericRepository<Attendance>>();
            mockRepo.Setup(r => r.Query()).Returns(attendances.BuildMock());
            _unitOfWorkMock.Setup(u => u.Repository<Attendance>()).Returns(mockRepo.Object);

            var excelBytes = new byte[] { 0x01, 0x02, 0x03 };
            _excelServiceMock.Setup(s => s.ExportAttendanceReportToExcel(It.IsAny<List<GetAttendanceReportResponse>>()))
                .Returns(excelBytes);

            var pagination = new PaginationParams { PageNumber = 1, PageSize = 10 };
            var query = new ExportAttendanceReportQuery(pagination);
            var handler = new ExportAttendanceReportHandler(
                _unitOfWorkMock.Object, 
                _mapper, 
                _excelServiceMock.Object, 
                _loggerMock.Object);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(excelBytes);
        }
    }
}
