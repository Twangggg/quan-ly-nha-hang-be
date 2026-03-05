using FoodHub.Application.Common.Models;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Tables.Commands.UpdateTableStatus
{
    public record UpdateTableStatusCommand(
        Guid TableId,
        TableStatus Status
    ) : IRequest<Result<UpdateTableStatusResponse>>;
}
