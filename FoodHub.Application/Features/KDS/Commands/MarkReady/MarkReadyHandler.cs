using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using MediatR;

namespace FoodHub.Application.Features.KDS.Commands.MarkReady
{
    public class MarkReadyHandler : IRequestHandler<MarkReadyCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public MarkReadyHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<Guid>> Handle(
            MarkReadyCommand request,
            CancellationToken cancellationToken
        )
        {
            // TODO: Implement handler logic
            throw new NotImplementedException();
        }
    }
}
