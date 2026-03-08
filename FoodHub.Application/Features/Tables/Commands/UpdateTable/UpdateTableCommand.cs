using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Tables.Commands.UpdateTable
{
    public record UpdateTableCommand(
        Guid TableId,
        int TableNumber,
        int Capacity,
        Guid AreaId
        ) : IRequest<Result<UpdateTableResponse>>;
}
