using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Dashboard.Operational.Queries.GetOperationalStats
{
    public record GetOperationalStatsQuery : IRequest<Result<GetOperationalStatsResponse>>;
}
