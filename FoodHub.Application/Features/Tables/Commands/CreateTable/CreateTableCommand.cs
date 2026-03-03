using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Tables.Commands.CreateTable
{
    public record CreateTableCommand(
        int Capacity,
        Guid AreaId
        ) : IRequest<Result<CreateTableResponse>>;
}
