using FluentAssertions;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Features.Inventory.Settings.Queries.GetInventorySettings;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.Inventory
{
    public class GetInventorySettingsHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICacheService> _mockCache;
        private readonly GetInventorySettingsHandler _handler;

        public GetInventorySettingsHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCache = new Mock<ICacheService>();

            _handler = new GetInventorySettingsHandler(
                _mockUow.Object,
                _mockCache.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<GetInventorySettingsHandler>>()
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnCachedSettings_When_CacheHit()
        {
            var cached = new GetInventorySettingsResponse
            {
                ExpiryWarningDays = 7,
                DefaultLowStockThreshold = 0,
                AutoDeductOnCompleted = true,
                MaxCostRecalcDays = 31,
            };

            _mockCache
                .Setup(x =>
                    x.GetAsync<GetInventorySettingsResponse>(
                        CacheKey.InventorySettings,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(cached);

            var result = await _handler.Handle(new GetInventorySettingsQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(cached);
            _mockUow.Verify(x => x.Repository<InventorySettings>(), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_CreateDefaultSettings_When_Missing()
        {
            var repo = new Mock<IGenericRepository<InventorySettings>>();
            repo.Setup(x => x.Query()).Returns(new List<InventorySettings>().AsQueryable().BuildMock());

            _mockCache
                .Setup(x =>
                    x.GetAsync<GetInventorySettingsResponse>(
                        CacheKey.InventorySettings,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((GetInventorySettingsResponse?)null);
            _mockUow.Setup(x => x.Repository<InventorySettings>()).Returns(repo.Object);
            _mockUow.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var result = await _handler.Handle(new GetInventorySettingsQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.ExpiryWarningDays.Should().Be(InventorySettings.DefaultExpiryWarningDays);
            repo.Verify(x => x.AddAsync(It.IsAny<InventorySettings>()), Times.Once);
            _mockUow.Verify(x => x.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockCache.Verify(
                x =>
                    x.SetAsync(
                        CacheKey.InventorySettings,
                        It.IsAny<GetInventorySettingsResponse>(),
                        CacheTTL.InventorySettings,
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Throw_When_CacheReadFails()
        {
            _mockCache
                .Setup(x =>
                    x.GetAsync<GetInventorySettingsResponse>(
                        CacheKey.InventorySettings,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ThrowsAsync(new InvalidOperationException("cache failure"));

            var action = async () => await _handler.Handle(new GetInventorySettingsQuery(), CancellationToken.None);

            await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("cache failure");
            _mockUow.Verify(x => x.Repository<InventorySettings>(), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Throw_When_DatabaseQueryFails()
        {
            var repo = new Mock<IGenericRepository<InventorySettings>>();
            repo.Setup(x => x.Query()).Throws(new InvalidOperationException("db failure"));

            _mockCache
                .Setup(x =>
                    x.GetAsync<GetInventorySettingsResponse>(
                        CacheKey.InventorySettings,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((GetInventorySettingsResponse?)null);
            _mockUow.Setup(x => x.Repository<InventorySettings>()).Returns(repo.Object);

            var action = async () => await _handler.Handle(new GetInventorySettingsQuery(), CancellationToken.None);

            await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("db failure");
            _mockCache.Verify(
                x =>
                    x.SetAsync(
                        CacheKey.InventorySettings,
                        It.IsAny<GetInventorySettingsResponse>(),
                        CacheTTL.InventorySettings,
                        It.IsAny<CancellationToken>()
                    ),
                Times.Never
            );
        }
    }
}
