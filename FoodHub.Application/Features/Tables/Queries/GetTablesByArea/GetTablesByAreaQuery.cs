using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Tables.Queries.GetTablesByArea
{
    public record GetTablesByAreaQuery(Guid AreaId) : IRequest<Result<List<GetTablesByAreaResponse>>>;
}
