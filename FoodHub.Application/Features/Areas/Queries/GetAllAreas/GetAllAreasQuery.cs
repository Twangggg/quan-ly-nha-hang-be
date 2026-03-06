using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Areas.Queries.GetAllAreas
{
    public record GetAllAreasQuery() : IRequest<Result<List<GetAllAreasResponse>>>;
}

