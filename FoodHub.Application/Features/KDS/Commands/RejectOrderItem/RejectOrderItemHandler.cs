using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using MediatR;

namespace FoodHub.Application.Features.KDS.Commands.RejectOrderItem
{
    public class RejectOrderItemHandler : IRequestHandler<RejectOrderItemCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public RejectOrderItemHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService
        )
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<Guid>> Handle(
            RejectOrderItemCommand request,
            CancellationToken cancellationToken
        )
        {
            // TODO: Implement handler logic
            throw new NotImplementedException();
        }
    }
}
