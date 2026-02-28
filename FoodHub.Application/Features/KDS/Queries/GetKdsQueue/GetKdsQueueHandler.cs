using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using MediatR;

namespace FoodHub.Application.Features.KDS.Queries.GetKdsQueue
{
    public class GetKdsQueueHandler
        : IRequestHandler<GetKdsQueueQuery, Result<List<KdsQueueResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetKdsQueueHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<List<KdsQueueResponse>>> Handle(
            GetKdsQueueQuery request,
            CancellationToken cancellationToken
        )
        {
            // TODO: Implement query logic
            // - Filter by station + status = Preparing only
            // - Order by CreatedAt FIFO
            // - Assign QueuePosition
            throw new NotImplementedException();
        }
    }
}
