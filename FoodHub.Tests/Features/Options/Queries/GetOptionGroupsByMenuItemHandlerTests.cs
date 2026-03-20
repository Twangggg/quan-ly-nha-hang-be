using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Options.Queries.GetOptionGroupsByMenuItem;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
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
            _handler = new GetOptionGroupsByMenuItemHandler(
                _mockUow.Object,
                NullLogger<GetOptionGroupsByMenuItemHandler>.Instance
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_OptionGroupsFound()
        {
            // Arrange
            var menuItemId = Guid.NewGuid();
            var query = new GetOptionGroupsByMenuItemQuery(menuItemId);

            var sizeGroup = OptionGroup.Create("Size", OptionGroupType.Single, true, menuItemId);
            sizeGroup.OptionItems.Add(OptionItem.Create(sizeGroup.OptionGroupId, "Small", 0));
            sizeGroup.OptionItems.Add(OptionItem.Create(sizeGroup.OptionGroupId, "Large", 2.00m));

            var toppingsGroup = OptionGroup.Create("Toppings", OptionGroupType.Multi, false, menuItemId);

            var optionGroups = new List<MenuItemOptionGroup>
            {
                MenuItemOptionGroup.Create(
                    menuItemId,
                    sizeGroup.OptionGroupId,
                    sizeGroup.OptionType,
                    true,
                    1,
                    1,
                    0,
                    true
                ),
                MenuItemOptionGroup.Create(
                    menuItemId,
                    toppingsGroup.OptionGroupId,
                    toppingsGroup.OptionType,
                    false,
                    0,
                    3,
                    1,
                    true
                ),
            };
            optionGroups[0].AttachOptionGroup(sizeGroup);
            optionGroups[1].AttachOptionGroup(toppingsGroup);

            var optionItems = new List<OptionItem>
            {
                OptionItem.Create(sizeGroup.OptionGroupId, "Small", 0),
                OptionItem.Create(sizeGroup.OptionGroupId, "Large", 2.00m),
            };

            var mockRepo = new Mock<IGenericRepository<MenuItemOptionGroup>>();
            mockRepo.Setup(r => r.Query()).Returns(optionGroups.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<MenuItemOptionGroup>()).Returns(mockRepo.Object);

            var mockOptionItemRepo = new Mock<IGenericRepository<OptionItem>>();
            mockOptionItemRepo.Setup(r => r.Query()).Returns(optionItems.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<OptionItem>()).Returns(mockOptionItemRepo.Object);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Count.Should().Be(2);
            result.Data.First().Name.Should().Be("Size");
            result.Data.First().OptionItems!.Count.Should().Be(2);
        }

        [Fact]
        public async Task Handle_Should_ReturnEmptyList_When_NoOptionGroupsFound()
        {
            // Arrange
            var menuItemId = Guid.NewGuid();
            var query = new GetOptionGroupsByMenuItemQuery(menuItemId);

            var optionGroups = new List<MenuItemOptionGroup>();

            var mockRepo = new Mock<IGenericRepository<MenuItemOptionGroup>>();
            mockRepo.Setup(r => r.Query()).Returns(optionGroups.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<MenuItemOptionGroup>()).Returns(mockRepo.Object);

            var mockOptionItemRepo = new Mock<IGenericRepository<OptionItem>>();
            mockOptionItemRepo.Setup(r => r.Query()).Returns(Array.Empty<OptionItem>().AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<OptionItem>()).Returns(mockOptionItemRepo.Object);

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

            var optionGroup = OptionGroup.Create("Size", OptionGroupType.Single, true, menuItemId);
            optionGroup.OptionItems.Add(OptionItem.Create(optionGroup.OptionGroupId, "Medium", 1.00m));
            var assignment = MenuItemOptionGroup.Create(
                menuItemId,
                optionGroup.OptionGroupId,
                optionGroup.OptionType,
                true,
                1,
                1,
                0,
                true
            );
            assignment.AttachOptionGroup(optionGroup);

            var optionItems = new List<OptionItem>
            {
                OptionItem.Create(optionGroup.OptionGroupId, "Medium", 1.00m),
            };

            var mockRepo = new Mock<IGenericRepository<MenuItemOptionGroup>>();
            mockRepo.Setup(r => r.Query()).Returns(new List<MenuItemOptionGroup> { assignment }.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<MenuItemOptionGroup>()).Returns(mockRepo.Object);

            var mockOptionItemRepo = new Mock<IGenericRepository<OptionItem>>();
            mockOptionItemRepo.Setup(r => r.Query()).Returns(optionItems.AsQueryable().BuildMock());
            _mockUow.Setup(u => u.Repository<OptionItem>()).Returns(mockOptionItemRepo.Object);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            var firstGroup = result.Data!.First();
            firstGroup.Name.Should().Be("Size");
            firstGroup.Type.Should().Be((int)OptionGroupType.Single);
            firstGroup.IsRequired.Should().BeTrue();
            firstGroup.OptionItems.Should().NotBeNull();
            var firstItem = firstGroup.OptionItems!.First();
            firstItem.Label.Should().Be("Medium");
            firstItem.ExtraPrice.Should().Be(1.00m);
        }
    }
}
