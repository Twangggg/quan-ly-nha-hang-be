using FoodHub.Application.Common.Models;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Order.Commands.CreateDraftOrder
{
    public record CreateDraftOrderCommand(
        OrderType OrderType,
        Guid? TableId,
        string? Note) : IRequest<Result<Guid>>;
}

