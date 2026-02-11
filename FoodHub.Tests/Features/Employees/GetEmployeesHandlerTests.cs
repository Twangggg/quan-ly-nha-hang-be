using Moq;
using AutoMapper;
using FluentAssertions;
using FoodHub.Application.Features.Employees.Queries.GetEmployees;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Common.Constants;
using MockQueryable.Moq;

namespace FoodHub.Tests.Features.Employees
{
    public class GetEmployeesHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ICacheService> _mockCache;
        private readonly GetEmployeesHandler _handler;

        public GetEmployeesHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockCache = new Mock<ICacheService>();

            _handler = new GetEmployeesHandler(
                _mockUow.Object,
                _mockMapper.Object,
                _mockCache.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnCachedEmployees_When_CacheExists()
        {
            // Arrange
            var pagination = new PaginationParams();
            var query = new GetEmployeesQuery(pagination);
            var items = new List<GetEmployeesResponse>
            {
                new GetEmployeesResponse
                {
                    EmployeeId = Guid.NewGuid(),
                    FullName = "John Doe",
                    EmployeeCode = "EMP001"
                }
            };
            var cachedPagedResult = new PagedResult<GetEmployeesResponse>(items, pagination, 1);

            _mockCache.Setup(c => c.GetAsync<PagedResult<GetEmployeesResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedPagedResult);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(cachedPagedResult);
            _mockUow.Verify(u => u.Repository<Employee>(), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnEmployeesFromDb_When_CacheNotExists()
        {
            // Arrange
            var pagination = new PaginationParams();
            var query = new GetEmployeesQuery(pagination);

            _mockCache.Setup(c => c.GetAsync<PagedResult<GetEmployeesResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((PagedResult<GetEmployeesResponse>)null!);

            var employees = new List<Employee>
            {
                new Employee
                {
                    EmployeeId = Guid.NewGuid(),
                    FullName = "John Doe",
                    EmployeeCode = "EMP001",
                    Email = "john@example.com",
                    Role = EmployeeRole.Manager,
                    Status = EmployeeStatus.Active
                }
            }.AsQueryable().BuildMock();

            var repo = new Mock<IGenericRepository<Employee>>();
            repo.Setup(r => r.Query()).Returns(employees);
            _mockUow.Setup(u => u.Repository<Employee>()).Returns(repo.Object);

            _mockMapper.Setup(m => m.ConfigurationProvider).Returns(new MapperConfiguration(cfg =>
                cfg.CreateMap<Employee, GetEmployeesResponse>()));

            _mockCache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<PagedResult<GetEmployeesResponse>>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            _mockUow.Verify(u => u.Repository<Employee>(), Times.AtLeastOnce);
            _mockCache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<PagedResult<GetEmployeesResponse>>(), CacheTTL.Employees, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnEmptyList_When_NoEmployeesInDb()
        {
            // Arrange
            var pagination = new PaginationParams();
            var query = new GetEmployeesQuery(pagination);

            _mockCache.Setup(c => c.GetAsync<PagedResult<GetEmployeesResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((PagedResult<GetEmployeesResponse>)null!);

            var employees = new List<Employee>().AsQueryable().BuildMock();
            var repo = new Mock<IGenericRepository<Employee>>();
            repo.Setup(r => r.Query()).Returns(employees);
            _mockUow.Setup(u => u.Repository<Employee>()).Returns(repo.Object);

            _mockMapper.Setup(m => m.ConfigurationProvider).Returns(new MapperConfiguration(cfg =>
                cfg.CreateMap<Employee, GetEmployeesResponse>()));

            _mockCache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<PagedResult<GetEmployeesResponse>>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);
        }
    }
}
