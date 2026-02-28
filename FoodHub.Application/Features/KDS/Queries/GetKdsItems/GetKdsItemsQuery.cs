using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.KDS.Queries.GetKdsItems
{
    public class GetKdsItemsQuery : IRequest<Result<List<KdsItemResponse>>>
    {
        public string Station { get; set; } = null!;
    }
}
