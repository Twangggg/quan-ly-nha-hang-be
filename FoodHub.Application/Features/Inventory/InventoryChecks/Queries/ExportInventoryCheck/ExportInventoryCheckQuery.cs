using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Inventory.InventoryChecks.Queries.ExportInventoryCheck;

public record ExportInventoryCheckQuery(Guid InventoryCheckId)
    : IRequest<Result<ExportInventoryCheckResponse>>;