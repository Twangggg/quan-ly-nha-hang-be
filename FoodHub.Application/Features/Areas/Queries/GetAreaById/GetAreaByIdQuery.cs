using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Areas.Queries.GetAreaById
{
    public record GetAreaByIdQuery(Guid AreaId) : IRequest<Result<GetAreaByIdResponse>>;
}
