using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.KDS.Queries.GetKdsQueue
{
    public class GetKdsQueueQuery : IRequest<Result<List<KdsQueueResponse>>>
    {
        public string Station { get; set; } = null!;
    }
}
