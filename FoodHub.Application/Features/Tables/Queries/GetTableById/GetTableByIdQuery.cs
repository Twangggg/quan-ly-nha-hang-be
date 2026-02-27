using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Tables.Queries.GetTableById
{
    public record GetTableByIdQuery(Guid Id) : IRequest<Result<GetTableByIdResponse>>;
}
