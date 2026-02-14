using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.SetMenus.Queries.GetSetMenuById;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.SetMenus.Queries
{
    public class GetSetMenuByIdHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly GetSetMenuByIdHandler _handler;

        public GetSetMenuByIdHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCacheService = new Mock<ICacheService>();
            _handler = new GetSetMenuByIdHandler(_mockUow.Object, _mockCacheService.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnFromCache_When_Cached()
        {
            // Arrange
            var setMenuId = Guid.NewGuid();
            var query = new GetSetMenuByIdQuery(setMenuId);

            var cachedResponse = new GetSetMenuByIdResponse
            {
                SetMenuId = setMenuId,
                Code = "SET001",
                Name = "Combo 1",
                SetType = SetType.SET_LUNCH,
                Price = 15.00m,
                Items = new List<GetSetMenuItemByIdResponse>(),
            };

            _mockCacheService
                .Setup(c =>
                    c.GetAsync<GetSetMenuByIdResponse>(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(cachedResponse);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeSameAs(cachedResponse);
            _mockUow.Verify(u => u.Repository<SetMenu>(), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_SetMenuNotFound()
        {
            // Arrange
            var setMenuId = Guid.NewGuid();
            var query = new GetSetMenuByIdQuery(setMenuId);

            _mockCacheService
                .Setup(c =>
                    c.GetAsync<GetSetMenuByIdResponse>(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((GetSetMenuByIdResponse?)null);

            var mockSetMenuRepo = new Mock<IGenericRepository<SetMenu>>();
            mockSetMenuRepo.Setup(r => r.GetByIdAsync(setMenuId)).ReturnsAsync((SetMenu?)null);
            _mockUow.Setup(u => u.Repository<SetMenu>()).Returns(mockSetMenuRepo.Object);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_SetMenuFound()
        {
            // Arrange
            var setMenuId = Guid.NewGuid();
            var query = new GetSetMenuByIdQuery(setMenuId);

            var existingSetMenu = new SetMenu
            {
                SetMenuId = setMenuId,
                Code = "SET001",
                Name = "Combo 1",
                SetType = SetType.SET_LUNCH,
                Price = 15.00m,
                CostPrice = 10.00m,
                Description = "Test combo",
                ImageUrl = "https://example.com/image.jpg",
            };

            _mockCacheService
                .Setup(c =>
                    c.GetAsync<GetSetMenuByIdResponse>(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((GetSetMenuByIdResponse?)null);

            var mockSetMenuRepo = new Mock<IGenericRepository<SetMenu>>();
            mockSetMenuRepo.Setup(r => r.GetByIdAsync(setMenuId)).ReturnsAsync(existingSetMenu);
            _mockUow.Setup(u => u.Repository<SetMenu>()).Returns(mockSetMenuRepo.Object);

            var mockSetMenuItemRepo = new Mock<IGenericRepository<SetMenuItem>>();
            mockSetMenuItemRepo
                .Setup(r => r.Query())
                .Returns(new List<SetMenuItem>().AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<SetMenuItem>()).Returns(mockSetMenuItemRepo.Object);

            _mockCacheService
                .Setup(c =>
                    c.SetAsync(
                        It.IsAny<string>(),
                        It.IsAny<GetSetMenuByIdResponse>(),
                        It.IsAny<TimeSpan?>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.SetMenuId.Should().Be(setMenuId);
            result.Data.Code.Should().Be("SET001");
            result.Data.Name.Should().Be("Combo 1");
        }

        [Fact]
        public async Task Handle_Should_CacheResult_When_SetMenuFound()
        {
            // Arrange
            var setMenuId = Guid.NewGuid();
            var query = new GetSetMenuByIdQuery(setMenuId);

            var existingSetMenu = new SetMenu
            {
                SetMenuId = setMenuId,
                Code = "SET001",
                Name = "Combo 1",
                SetType = SetType.SET_LUNCH,
                Price = 15.00m,
            };

            _mockCacheService
                .Setup(c =>
                    c.GetAsync<GetSetMenuByIdResponse>(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((GetSetMenuByIdResponse?)null);

            var mockSetMenuRepo = new Mock<IGenericRepository<SetMenu>>();
            mockSetMenuRepo.Setup(r => r.GetByIdAsync(setMenuId)).ReturnsAsync(existingSetMenu);
            _mockUow.Setup(u => u.Repository<SetMenu>()).Returns(mockSetMenuRepo.Object);

            var mockSetMenuItemRepo = new Mock<IGenericRepository<SetMenuItem>>();
            mockSetMenuItemRepo
                .Setup(r => r.Query())
                .Returns(new List<SetMenuItem>().AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<SetMenuItem>()).Returns(mockSetMenuItemRepo.Object);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockCacheService.Verify(
                c =>
                    c.SetAsync(
                        It.IsAny<string>(),
                        It.IsAny<GetSetMenuByIdResponse>(),
                        It.IsAny<TimeSpan?>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }
    }
}
