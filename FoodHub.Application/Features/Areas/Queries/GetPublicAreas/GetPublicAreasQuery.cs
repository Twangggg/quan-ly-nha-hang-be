using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Areas.Queries.GetPublicAreas
{
    public class GetPublicAreasQuery : IRequest<Result<List<GetPublicAreasResponse>>>
    {
    }
}
