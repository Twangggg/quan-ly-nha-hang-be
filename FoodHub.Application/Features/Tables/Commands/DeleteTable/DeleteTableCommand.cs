using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Tables.Commands.DeleteTable
{
    public record DeleteTableCommand(Guid TableId) : IRequest<Result<DeleteTableResponse>>;
}
