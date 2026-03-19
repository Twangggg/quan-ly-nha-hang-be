using FoodHub.Application.Common.Behaviors;
using FoodHub.Application.Common.Models;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Inventory.Reports.Queries.GetInventoryLedger
{
    public record GetInventoryLedgerQuery(
        Guid IngredientId,
        DateOnly FromDate,
        DateOnly ToDate,
        InventoryTransactionType? TransactionType
    ) : IRequest<Result<IReadOnlyList<GetInventoryLedgerResponse>>>, IMustBeActive;
}
