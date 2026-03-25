using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Features.Shifts.Queries.GetShiftById;
using FoodHub.Application.Features.Shifts.Queries.GetShifts;
using FoodHub.Application.Interfaces;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using MockQueryable.Moq;
using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FoodHub.Application.Extensions.Mappings;
 
namespace FoodHub.Tests.Features.Shifts.Queries.GetShifts
{
    public class GetShiftsHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ICacheService> _cacheServiceMock;
        private readonly Mock<IMessageService> _messageServiceMock;
        private readonly Mock<ILogger<GetShiftsHandler>> _loggerMock;
 
        public GetShiftsHandlerTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _cacheServiceMock = new Mock<ICacheService>();
            _messageServiceMock = new Mock<IMessageService>();
            _messageServiceMock.Setup(m => m.GetMessage(It.IsAny<string>())).Returns((string key) => key);
            _loggerMock = new Mock<ILogger<GetShiftsHandler>>();
        }
 
        [Fact]
        public async Task Handle_ShouldReturnPagedShifts()
        {
            // Arrange
            var shifts = new List<Shift>
            {
                Shift.Create("Morning", new TimeSpan(8, 0, 0), new TimeSpan(12, 0, 0), Guid.NewGuid()),
                Shift.Create("Afternoon", new TimeSpan(13, 0, 0), new TimeSpan(17, 0, 0), Guid.NewGuid())
            }.AsQueryable();
 
            var mockRepo = new Mock<IGenericRepository<Shift>>();
            mockRepo.Setup(r => r.Query()).Returns(shifts.BuildMock());
            _unitOfWorkMock.Setup(u => u.Repository<Shift>()).Returns(mockRepo.Object);
 
            var pagination = new PaginationParams { PageNumber = 1, PageSize = 10 };
            var query = new GetShiftsQuery(pagination);
            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
            var configurationProvider = new AutoMapper.MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, mockLoggerFactory.Object);
            var mapper = configurationProvider.CreateMapper();

            var handler = new GetShiftsHandler(
                _unitOfWorkMock.Object, 
                mapper, 
                _cacheServiceMock.Object, 
                _messageServiceMock.Object,
                _loggerMock.Object);
 
            // Act
            var result = await handler.Handle(query, CancellationToken.None);
 
            // Assert
            result.IsSuccess.Should().BeTrue(result.Error);
            result.Data.Should().NotBeNull();
        }
    }
}
