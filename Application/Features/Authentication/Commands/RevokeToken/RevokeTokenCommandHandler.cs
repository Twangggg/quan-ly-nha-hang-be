using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Authentication.Commands.RevokeToken
{
    public class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public RevokeTokenCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
        {
            var token = await _unitOfWork.Repository<FoodHub.Domain.Entities.RefreshToken>()
                .Query()
                .FirstOrDefaultAsync(x => x.Token == request.RefreshToken, cancellationToken);

            if (token == null)
            {
                return Result<bool>.Failure("Invalid token.");
            }

            _unitOfWork.Repository<FoodHub.Domain.Entities.RefreshToken>().Delete(token);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
    }
}
