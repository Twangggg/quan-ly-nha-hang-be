using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using MediatR;

namespace FoodHub.Application.Features.KDS.Commands.StartCooking
{
    public class StartCookingHandler : IRequestHandler<StartCookingCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public StartCookingHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<Guid>> Handle(
            StartCookingCommand request,
            CancellationToken cancellationToken
        )
        {
            // TODO: Implement handler logic
            throw new NotImplementedException();
        }
    }
}
