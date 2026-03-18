using FluentAssertions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Inventory.InventoryChecks.Queries.GetInventoryChecks;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MockQueryable.Moq;
using Moq;

namespace FoodHub.Tests.Features.Inventory
{
    public class GetInventoryChecksHandlerTests
    {
        [Fact]
        public async Task Handle_Should_FilterByStatus_AndDateRange()
        {
            var draftCheck = InventoryCheck.Create(
                new DateTime(2026, 3, 11, 0, 0, 0, DateTimeKind.Utc)
            );
            draftCheck.AddItem(Guid.NewGuid(), 10, 10, null);

            var processedCheck = InventoryCheck.Create(
                new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc)
            );
            processedCheck.AddItem(Guid.NewGuid(), 10, 8, "Deficit");
            processedCheck.MarkProcessed();

            var mockRepo = new Mock<IGenericRepository<InventoryCheck>>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            mockUnitOfWork.Setup(x => x.Repository<InventoryCheck>()).Returns(mockRepo.Object);
            mockRepo
                .Setup(x => x.Query())
                .Returns(new List<InventoryCheck> { draftCheck, processedCheck }.AsQueryable().BuildMock());

            var handler = new GetInventoryChecksHandler(
                mockUnitOfWork.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<GetInventoryChecksHandler>>()
            );

            var result = await handler.Handle(
                new GetInventoryChecksQuery(
                    new PaginationParams { PageNumber = 1, PageSize = 10 },
                    InventoryCheckStatus.Draft,
                    new DateOnly(2026, 3, 10),
                    new DateOnly(2026, 3, 11)
                ),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().ContainSingle();
            result.Data.Items.Single().InventoryCheckId.Should().Be(draftCheck.InventoryCheckId);
        }
    }
}
