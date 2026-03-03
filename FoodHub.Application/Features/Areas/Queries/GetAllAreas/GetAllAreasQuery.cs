using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Areas.Queries.GetAllAreas
{
    public record GetAllAreasQuery(PaginationParams Pagination) : IRequest<Result<PagedResult<GetAllAreasResponse>>>;
}
