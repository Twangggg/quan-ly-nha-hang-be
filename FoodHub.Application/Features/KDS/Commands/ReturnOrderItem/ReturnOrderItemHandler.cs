using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using MediatR;

namespace FoodHub.Application.Features.KDS.Commands.ReturnOrderItem
{
    public class ReturnOrderItemHandler : IRequestHandler<ReturnOrderItemCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public ReturnOrderItemHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService
        )
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<Guid>> Handle(
            ReturnOrderItemCommand request,
            CancellationToken cancellationToken
        )
        {
            // TODO: Implement handler logic
            throw new NotImplementedException();
        }
    }
}
