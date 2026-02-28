using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using MediatR;

namespace FoodHub.Application.Features.KDS.Queries.GetKdsItems
{
    public class GetKdsItemsHandler
        : IRequestHandler<GetKdsItemsQuery, Result<List<KdsItemResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetKdsItemsHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<List<KdsItemResponse>>> Handle(
            GetKdsItemsQuery request,
            CancellationToken cancellationToken
        )
        {
            // TODO: Implement query logic
            // - Filter by station + status (Preparing, Cooking)
            // - Order by CreatedAt FIFO (Phase 1)
            throw new NotImplementedException();
        }
    }
}
