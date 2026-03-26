using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.KDS.Queries.GetKdsBacklogSummary
{
    public record GetKdsBacklogSummaryQuery : IRequest<Result<GetKdsBacklogSummaryResponse>>;
}
