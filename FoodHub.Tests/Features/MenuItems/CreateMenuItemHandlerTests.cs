using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.MenuItems.Commands.CreateMenuItem;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.MenuItems
{
    public class CreateMenuItemHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<IMessageService> _mockMessage;
        private readonly Mock<ICacheService> _mockCache;
        private readonly Mock<ILogger<CreateMenuItemHandler>> _mockLogger;
        private readonly CreateMenuItemHandler _handler;

        public CreateMenuItemHandlerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockMessage = new Mock<IMessageService>();
            _mockCache = new Mock<ICacheService>();
            _mockLogger = new Mock<ILogger<CreateMenuItemHandler>>();

            _handler = new CreateMenuItemHandler(
                _mockUow.Object,
                _mockCurrentUser.Object,
                _mockMessage.Object,
                _mockCache.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_MenuItemCreated()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var command = new CreateMenuItemCommand(
                "MI001",
                "Phở Bò",
                "https://example.com/image.jpg",
                "Phở bò truyền thống",
                categoryId,
                Station.HotKitchen,
                15,
                50000m,
                45000m,
                25000m
            );

            _mockCurrentUser.Setup(c => c.UserId).Returns(userId.ToString());

            var menuItems = new List<MenuItem>().AsQueryable().BuildMock();
            var menuItemRepo = new Mock<IGenericRepository<MenuItem>>();
            menuItemRepo.Setup(r => r.Query()).Returns(menuItems);
            menuItemRepo
                .Setup(r =>
                    r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<MenuItem, bool>>>())
                )
                .ReturnsAsync(false);
            _mockUow.Setup(u => u.Repository<MenuItem>()).Returns(menuItemRepo.Object);

            var category = new Category { CategoryId = categoryId, Name = "Món chính" };
            var categoryRepo = new Mock<IGenericRepository<Category>>();
            categoryRepo.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync(category);
            _mockUow.Setup(u => u.Repository<Category>()).Returns(categoryRepo.Object);

            _mockUow.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockCache
                .Setup(c => c.RemoveByPatternAsync("menuitem:list", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Code.Should().Be("MI001");
            result.Data.Name.Should().Be("Phở Bò");
            result.Data.CategoryId.Should().Be(categoryId);
            result.Data.CategoryName.Should().Be("Món chính");
            _mockUow.Verify(
                u => u.Repository<MenuItem>().AddAsync(It.IsAny<MenuItem>()),
                Times.Once
            );
            _mockUow.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockCache.Verify(
                c => c.RemoveByPatternAsync("menuitem:list", It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_CodeAlreadyExists()
        {
            // Arrange
            var categoryId = Guid.NewGuid();
            var command = new CreateMenuItemCommand(
                "MI001",
                "Phở Bò",
                null,
                null,
                categoryId,
                Station.HotKitchen,
                15,
                50000m,
                null,
                null
            );

            var existingMenuItems = new List<MenuItem>
            {
                new MenuItem
                {
                    Code = "MI001",
                    Name = "Existing",
                    ImageUrl = "",
                },
            }
                .AsQueryable()
                .BuildMock();
            var menuItemRepo = new Mock<IGenericRepository<MenuItem>>();
            menuItemRepo.Setup(r => r.Query()).Returns(existingMenuItems);
            menuItemRepo
                .Setup(r =>
                    r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<MenuItem, bool>>>())
                )
                .ReturnsAsync(true);
            _mockUow.Setup(u => u.Repository<MenuItem>()).Returns(menuItemRepo.Object);

            _mockMessage
                .Setup(m => m.GetMessage(MessageKeys.MenuItem.CodeExists, "MI001"))
                .Returns("Menu item code MI001 already exists");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Conflict);
            result.Error.Should().Be("Menu item code MI001 already exists");
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_When_CategoryNotFound()
        {
            // Arrange
            var categoryId = Guid.NewGuid();
            var command = new CreateMenuItemCommand(
                "MI001",
                "Phở Bò",
                null,
                null,
                categoryId,
                Station.HotKitchen,
                15,
                50000m,
                null,
                null
            );

            var menuItems = new List<MenuItem>().AsQueryable().BuildMock();
            var menuItemRepo = new Mock<IGenericRepository<MenuItem>>();
            menuItemRepo.Setup(r => r.Query()).Returns(menuItems);
            menuItemRepo
                .Setup(r =>
                    r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<MenuItem, bool>>>())
                )
                .ReturnsAsync(false);
            _mockUow.Setup(u => u.Repository<MenuItem>()).Returns(menuItemRepo.Object);

            var categoryRepo = new Mock<IGenericRepository<Category>>();
            categoryRepo.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync((Category?)null);
            _mockUow.Setup(u => u.Repository<Category>()).Returns(categoryRepo.Object);

            _mockMessage
                .Setup(m => m.GetMessage(MessageKeys.Category.NotFound, categoryId))
                .Returns($"Category {categoryId} not found");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }
    }
}
