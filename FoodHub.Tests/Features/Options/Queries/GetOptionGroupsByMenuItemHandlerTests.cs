using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Options.Queries.GetOptionGroupsByMenuItem;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FoodHub.Tests.Features.Options.Queries
{
    public class GetOptionGroupsByMenuItemHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly GetOptionGroupsByMenuItemHandler _handler;

        public GetOptionGroupsByMenuItemHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _handler = new GetOptionGroupsByMenuItemHandler(_mockUow.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_OptionGroupsFound()
        {
            // Arrange
            var menuItemId = Guid.NewGuid();
            var query = new GetOptionGroupsByMenuItemQuery(menuItemId);

            var optionGroups = new List<OptionGroup>
            {
                new OptionGroup
                {
                    OptionGroupId = Guid.NewGuid(),
                    MenuItemId = menuItemId,
                    Name = "Size",
                    OptionType = OptionGroupType.Single,
                    IsRequired = true,
                    OptionItems = new List<OptionItem>
                    {
                        new OptionItem
                        {
                            OptionItemId = Guid.NewGuid(),
                            OptionGroupId = Guid.NewGuid(),
                            Label = "Small",
                            ExtraPrice = 0
                        },
                        new OptionItem
                        {
                            OptionItemId = Guid.NewGuid(),
                            OptionGroupId = Guid.NewGuid(),
                            Label = "Large",
                            ExtraPrice = 2.00m
                        }
                    }
                },
                new OptionGroup
                {
                    OptionGroupId = Guid.NewGuid(),
                    MenuItemId = menuItemId,
                    Name = "Toppings",
                    OptionType = OptionGroupType.Multi,
                    IsRequired = false,
                    OptionItems = new List<OptionItem>()
                }
            };

            var mockRepo = new Mock<IGenericRepository<OptionGroup>>();
            mockRepo
                .Setup(r => r.Query())
                .Returns(optionGroups.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<OptionGroup>()).Returns(mockRepo.Object);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Count.Should().Be(2);
            result.Data.First().Name.Should().Be("Size");
            result.Data.First().OptionItems.Count.Should().Be(2);
        }

        [Fact]
        public async Task Handle_Should_ReturnEmptyList_When_NoOptionGroupsFound()
        {
            // Arrange
            var menuItemId = Guid.NewGuid();
            var query = new GetOptionGroupsByMenuItemQuery(menuItemId);

            var optionGroups = new List<OptionGroup>();

            var mockRepo = new Mock<IGenericRepository<OptionGroup>>();
            mockRepo
                .Setup(r => r.Query())
                .Returns(optionGroups.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<OptionGroup>()).Returns(mockRepo.Object);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Count.Should().Be(0);
        }

        [Fact]
        public async Task Handle_Should_ReturnOptionGroups_WithCorrectProperties()
        {
            // Arrange
            var menuItemId = Guid.NewGuid();
            var query = new GetOptionGroupsByMenuItemQuery(menuItemId);

            var optionGroups = new List<OptionGroup>
            {
                new OptionGroup
                {
                    OptionGroupId = Guid.NewGuid(),
                    MenuItemId = menuItemId,
                    Name = "Size",
                    OptionType = OptionGroupType.Single,
                    IsRequired = true,
                    OptionItems = new List<OptionItem>
                    {
                        new OptionItem
                        {
                            OptionItemId = Guid.NewGuid(),
                            OptionGroupId = Guid.NewGuid(),
                            Label = "Medium",
                            ExtraPrice = 1.00m
                        }
                    }
                }
            };

            var mockRepo = new Mock<IGenericRepository<OptionGroup>>();
            mockRepo
                .Setup(r => r.Query())
                .Returns(optionGroups.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<OptionGroup>()).Returns(mockRepo.Object);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var firstGroup = result.Data.First();
            firstGroup.Name.Should().Be("Size");
            firstGroup.Type.Should().Be((int)OptionGroupType.Single);
            firstGroup.IsRequired.Should().BeTrue();
            firstGroup.OptionItems.First().Label.Should().Be("Medium");
            firstGroup.OptionItems.First().ExtraPrice.Should().Be(1.00m);
        }
    }
}
