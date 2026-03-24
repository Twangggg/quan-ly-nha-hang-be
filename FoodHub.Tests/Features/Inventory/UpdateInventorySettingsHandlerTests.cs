using FluentAssertions;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Features.Inventory.Settings.Commands.UpdateInventorySettings;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.Inventory
{
    public class UpdateInventorySettingsHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICacheService> _mockCache;
        private readonly Mock<IMessageService> _mockMessage;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly UpdateInventorySettingsHandler _handler;

        public UpdateInventorySettingsHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCache = new Mock<ICacheService>();
            _mockMessage = new Mock<IMessageService>();
            _mockCurrentUser = new Mock<ICurrentUserService>();

            _handler = new UpdateInventorySettingsHandler(
                _mockUow.Object,
                _mockCache.Object,
                _mockMessage.Object,
                _mockCurrentUser.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<UpdateInventorySettingsHandler>>()
            );
        }

        [Fact]
        public async Task Handle_Should_UpdateExistingSettings_And_ClearCache()
        {
            var settings = InventorySettings.CreateDefault();
            var repo = new Mock<IGenericRepository<InventorySettings>>();
            repo.Setup(x => x.Query()).Returns(new List<InventorySettings> { settings }.AsQueryable().BuildMock());

            _mockUow.Setup(x => x.Repository<InventorySettings>()).Returns(repo.Object);
            _mockUow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUow.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockUow.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);

            var command = new UpdateInventorySettingsCommand(
                14,
                100,
                false,
                InventoryCostMethod.WeightedAverage,
                45
            );

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.ExpiryWarningDays.Should().Be(14);
            result.Data.DefaultLowStockThreshold.Should().Be(100);
            result.Data.AutoDeductOnCompleted.Should().BeFalse();
            result.Data.CostMethod.Should().Be(InventoryCostMethod.WeightedAverage);
            _mockUow.Verify(x => x.BeginTransactionAsync(), Times.Once);
            _mockUow.Verify(x => x.CommitTransactionAsync(), Times.Once);
            _mockCache.Verify(
                x => x.RemoveAsync(CacheKey.InventorySettings, It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_CreateSettings_When_Missing()
        {
            var repo = new Mock<IGenericRepository<InventorySettings>>();
            repo.Setup(x => x.Query()).Returns(new List<InventorySettings>().AsQueryable().BuildMock());

            _mockUow.Setup(x => x.Repository<InventorySettings>()).Returns(repo.Object);
            _mockUow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUow.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockUow.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);

            var command = new UpdateInventorySettingsCommand(
                10,
                5,
                true,
                InventoryCostMethod.WeightedAverage,
                30
            );

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            repo.Verify(x => x.AddAsync(It.IsAny<InventorySettings>()), Times.Once);
            _mockUow.Verify(x => x.BeginTransactionAsync(), Times.Once);
            _mockUow.Verify(x => x.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ThrowBusinessException_When_DomainValidationFails()
        {
            var settings = InventorySettings.CreateDefault();
            var repo = new Mock<IGenericRepository<InventorySettings>>();
            repo.Setup(x => x.Query()).Returns(new List<InventorySettings> { settings }.AsQueryable().BuildMock());
            _mockUow.Setup(x => x.Repository<InventorySettings>()).Returns(repo.Object);
            _mockUow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
            _mockMessage
                .Setup(x => x.GetMessage("InventorySettings.InvalidExpiryWarningDays"))
                .Returns("invalid settings");

            var command = new UpdateInventorySettingsCommand(
                0,
                5,
                true,
                InventoryCostMethod.WeightedAverage,
                30
            );

            var action = async () => await _handler.Handle(command, CancellationToken.None);

            await action.Should().ThrowAsync<BusinessException>().WithMessage("invalid settings");
            _mockUow.Verify(x => x.BeginTransactionAsync(), Times.Once);
            _mockUow.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        }
    }
}
