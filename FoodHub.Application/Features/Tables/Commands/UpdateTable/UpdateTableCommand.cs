using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Tables.Commands.UpdateTable
{
    public record UpdateTableCommand(
        Guid TableId,
        int Capacity
        ) : IRequest<Result<UpdateTableResponse>>;
}
