using FluentAssertions;
using FoodHub.Application.Common.Exceptions;
using FoodHub.Application.Features.Inventory.InventoryChecks.Commands.CreateInventoryCheck;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Constants;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.Inventory
{
    public class CreateInventoryCheckHandlerTests
    {
        private readonly CreateInventoryCheckHandler _handler;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<IGenericRepository<Ingredient>> _mockIngredientRepo;
        private readonly Mock<IGenericRepository<InventoryCheck>> _mockInventoryCheckRepo;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;

        public CreateInventoryCheckHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMessageService = new Mock<IMessageService>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockIngredientRepo = new Mock<IGenericRepository<Ingredient>>();
            _mockInventoryCheckRepo = new Mock<IGenericRepository<InventoryCheck>>();

            _mockUnitOfWork.Setup(x => x.Repository<Ingredient>()).Returns(_mockIngredientRepo.Object);
            _mockUnitOfWork
                .Setup(x => x.Repository<InventoryCheck>())
                .Returns(_mockInventoryCheckRepo.Object);

            _handler = new CreateInventoryCheckHandler(
                _mockUnitOfWork.Object,
                _mockMessageService.Object,
                _mockCurrentUser.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<CreateInventoryCheckHandler>>()
            );
        }

        [Fact]
        public async Task Handle_Should_CreateDraftInventoryCheck_WithSnapshotBookQuantity()
        {
            var ingredient = Ingredient.Create("ING001", "Salt", "Kg", 0, 12, 3, null);
            InventoryCheck? capturedInventoryCheck = null;

            _mockIngredientRepo
                .Setup(x => x.Query())
                .Returns(new List<Ingredient> { ingredient }.AsQueryable().BuildMock());
            _mockInventoryCheckRepo
                .Setup(x => x.AddAsync(It.IsAny<InventoryCheck>()))
                .Callback<InventoryCheck>(entity => capturedInventoryCheck = entity)
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockCurrentUser.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());

            var command = new CreateInventoryCheckCommand
            {
                CheckDate = new DateTime(2026, 3, 18, 0, 0, 0, DateTimeKind.Utc),
                Items = new List<CreateInventoryCheckItemDto>
                {
                    new() { IngredientId = ingredient.IngredientId, PhysicalQuantity = 10, Reason = "Counted" },
                },
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Status.Should().Be(InventoryCheckStatus.Draft);
            capturedInventoryCheck.Should().NotBeNull();
            capturedInventoryCheck!.Items.Should().HaveCount(1);
            capturedInventoryCheck.Items.Single().BookQuantity.Should().Be(12);
            capturedInventoryCheck.Items.Single().PhysicalQuantity.Should().Be(10);
            capturedInventoryCheck.Items.Single().DifferenceQuantity.Should().Be(-2);
            _mockUnitOfWork.Verify(x => x.BeginTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ThrowNotFoundException_WhenIngredientMissing()
        {
            _mockIngredientRepo.Setup(x => x.Query()).Returns(new List<Ingredient>().AsQueryable().BuildMock());
            _mockMessageService
                .Setup(x => x.GetMessage("Ingredient.NotFound"))
                .Returns("ingredient not found");

            var action = async () =>
                await _handler.Handle(
                    new CreateInventoryCheckCommand
                    {
                        CheckDate = DateTime.UtcNow,
                        Items = new List<CreateInventoryCheckItemDto>
                        {
                            new() { IngredientId = Guid.NewGuid(), PhysicalQuantity = 5 },
                        },
                    },
                    CancellationToken.None
                );

            await action.Should().ThrowAsync<NotFoundException>().WithMessage("ingredient not found");
        }

        [Fact]
        public async Task Handle_Should_ThrowBusinessException_WhenDuplicateIngredient()
        {
            var ingredient = Ingredient.Create("ING001", "Salt", "Kg", 0, 12, 3, null);

            _mockIngredientRepo
                .Setup(x => x.Query())
                .Returns(new List<Ingredient> { ingredient }.AsQueryable().BuildMock());
            _mockMessageService
                .Setup(x => x.GetMessage(DomainErrors.InventoryCheck.DuplicateIngredient))
                .Returns("duplicate ingredient");

            var action = async () =>
                await _handler.Handle(
                    new CreateInventoryCheckCommand
                    {
                        CheckDate = DateTime.UtcNow,
                        Items = new List<CreateInventoryCheckItemDto>
                        {
                            new() { IngredientId = ingredient.IngredientId, PhysicalQuantity = 10 },
                            new() { IngredientId = ingredient.IngredientId, PhysicalQuantity = 8 },
                        },
                    },
                    CancellationToken.None
                );

            await action.Should().ThrowAsync<BusinessException>().WithMessage("duplicate ingredient");
        }
    }
}
